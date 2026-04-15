using System;
using System.Collections.Generic;
using UnityEngine;
using _Game.Player;
using _Game.Skills;

namespace _Game.LevelUp
{
    // 레벨업 카드 5장을 생성한다.
    
    public sealed class LevelUpCardGenerator : MonoBehaviour
    {
        //  전용 스킬 세트 (인스펙터)

        [Serializable]
        public struct CharacterExclusiveSkill
        {
            [Tooltip("스킬 정의 SO (PlayerSkillLoadout 추적 + UI 표시용)")]
            public SkillDefinitionSO definition;

            [Tooltip("SkillRunner에 장착할 프리팹")]
            public GameObject prefab;
        }

        [Serializable]
        public struct CharacterSkillSet
        {
            [Tooltip("캐릭터 ID (SquadLoadoutRuntime.MainId와 일치)")]
            public string characterId;

            [Tooltip("이 캐릭터의 전용 스킬 (최대 2개)")]
            public CharacterExclusiveSkill[] skills;
        }
        
        //  인스펙터

        [Header("=== 카탈로그 ===")]
        [SerializeField, Tooltip("전체 공통 스킬 / 패시브 목록입니다.")]
        private SkillCatalogSO skillCatalog;

        [SerializeField, Tooltip("액티브 스킬을 실제 런타임 공통 스킬과 매핑할 카탈로그입니다.")]
        private CommonSkillCatalogSO commonSkillCatalog;

        [Header("=== 캐릭터 전용 스킬 ===")]
        [Tooltip("캐릭터별 전용 스킬 세트. 메인 캐릭터에 해당하는 세트만 카드 풀에 포함.")]
        [SerializeField] private CharacterSkillSet[] characterSkillSets = Array.Empty<CharacterSkillSet>();

        [Header("=== 카드 수 ===")]
        [SerializeField, Min(3)] private int totalCardCount = 5;

        [Header("=== 전용 스킬 가중치 ===")]
        [Tooltip("전용 스킬이 공통 스킬 대비 선택될 확률 배율")]
        [SerializeField, Range(1f, 5f)] private float exclusiveWeightMultiplier = 1.5f;

        [Header("=== 전용 스킬 보장 ===")]
        [Tooltip("이 레벨부터 전용 스킬이 한 번도 안 나왔으면 강제 배치")]
        [SerializeField, Min(1)] private int guaranteeStartLevel = 2;

        [Tooltip("리롤 시 전용 스킬 확률 누적 보너스")]
        [SerializeField, Range(0f, 1f)] private float rerollPityPerRoll = 0.15f;

        [Header("=== 패시브 과속 방지 ===")]
        [Tooltip("이 레벨까지는 패시브 슬롯 1장만")]
        [SerializeField, Min(1)] private int passiveSingleSlotUntilLevel = 5;

        [Header("=== 연속 등장 방지 ===")]
        [SerializeField, Range(0f, 1f)] private float recentPenalty1 = 0.5f;
        [SerializeField, Range(0f, 1f)] private float recentPenalty2 = 0.75f;

        [Header("=== 대체 카드 수치 ===")]
        [SerializeField] private int fallbackHealAmount = 30;
        [SerializeField] private int fallbackGoldAmount = 200;
        [SerializeField] private float fallbackInvincibleDuration = 5f;
        [SerializeField] private int fallbackBonusExpAmount = 50;
        
        //  런 추적 상태

        private int _levelUpCount;
        private bool _exclusiveEverOffered;
        private int _currentRerollCount;
        private float _currentPityBonus;

        // 연속 등장 방지: skillId -> 마지막 등장 레벨업 번호
        private readonly Dictionary<string, int> _recentHistory = new(32);

        // 가중치 후보 버퍼
        private readonly List<WeightedSkill> _skillCandidates = new(32);
        private readonly List<SkillDefinitionSO> _passiveCandidates = new(16);

        private struct WeightedSkill
        {
            public SkillDefinitionSO definition;
            public GameObject prefab;       // 전용 스킬이면 프리팹, 공통이면 null
            public float weight;
            public bool isExclusive;
        }
        
        // 런 시작 시 호출 — 추적 상태 초기화.
        public void ResetRunState()
        {
            _levelUpCount = 0;
            _exclusiveEverOffered = false;
            _currentRerollCount = 0;
            _currentPityBonus = 0f;
            _recentHistory.Clear();
        }

        //리롤 시 호출 — 피티 누적.
        public void NotifyReroll()
        {
            _currentRerollCount++;
            _currentPityBonus += rerollPityPerRoll;
        }

        // 레벨업 닫힐 때 호출 — 리롤 상태 초기화.
        public void NotifyLevelUpClosed()
        {
            _currentRerollCount = 0;
            _currentPityBonus = 0f;
        }

        // 전달받은 loadout 기준으로 카드를 생성한다.
        
        public List<LevelUpCardData> Generate(PlayerSkillLoadout loadout)
        {
            List<LevelUpCardData> result = new List<LevelUpCardData>(totalCardCount);

            if (skillCatalog == null || loadout == null)
            {
                GameLogger.LogWarning("[CardGen] skillCatalog 또는 loadout이 없어 대체 카드로 채웁니다.", this);
                FillWithFallbackCards(result);
                return result;
            }

            // 레벨업 카운트 증가 (리롤이 아닌 첫 생성일 때만)
            if (_currentRerollCount == 0)
                _levelUpCount++;

            // 패시브 슬롯 수 결정
            int passiveSlotCount = _levelUpCount <= passiveSingleSlotUntilLevel ? 1 : 2;
            int skillSlotCount = totalCardCount - passiveSlotCount;

            // 후보 수집
            BuildSkillCandidates(loadout);
            BuildPassiveCandidates(loadout);

            GameLogger.Log($"[CardGen] Generate Lv#{_levelUpCount} | 스킬후보={_skillCandidates.Count} 패시브후보={_passiveCandidates.Count} | 스킬슬롯={skillSlotCount} 패시브슬롯={passiveSlotCount}", this);

            // 후보 0이면 대체 카드
            if (_skillCandidates.Count == 0 && _passiveCandidates.Count == 0)
            {
                FillWithFallbackCards(result);
                return result;
            }

            // 스킬 슬롯 채우기 (전용 보장 포함)
            HashSet<string> usedIds = new HashSet<string>();
            bool needGuarantee = NeedExclusiveGuarantee();

            FillSkillSlots(result, loadout, usedIds, skillSlotCount, needGuarantee);

            // 패시브 슬롯 채우기
            FillPassiveSlots(result, loadout, usedIds, passiveSlotCount);

            // 부족하면 대체 카드로 채움
            if (result.Count < totalCardCount)
                FillRemainingWithFallbackCards(result);

            // 연속 등장 기록 갱신
            UpdateRecentHistory(result);

            return result;
        }
        
        //  후보 수집

        private void BuildSkillCandidates(PlayerSkillLoadout loadout)
        {
            _skillCandidates.Clear();

            // 1) 공통 액티브 스킬
            IReadOnlyList<SkillDefinitionSO> actives = skillCatalog.GetByType(SkillType.Active);
            if (actives != null)
            {
                for (int i = 0; i < actives.Count; i++)
                {
                    SkillDefinitionSO skill = actives[i];
                    if (!IsValidCandidate(skill, loadout)) continue;

                    // CommonSkillCatalog 매핑 확인
                    if (commonSkillCatalog != null)
                    {
                        if (!commonSkillCatalog.TryResolve(skill, out CommonSkillConfigSO config)) continue;
                        if (config == null || config.weaponPrefab == null) continue;
                    }

                    float w = 1f * GetRecentPenalty(skill.SkillId);

                    _skillCandidates.Add(new WeightedSkill
                    {
                        definition = skill,
                        prefab = null,
                        weight = w,
                        isExclusive = false
                    });
                }
            }

            // 2) 메인 캐릭터 전용 스킬
            string mainId = SquadLoadoutRuntime.MainId;
            CharacterSkillSet? activeSet = FindCharacterSkillSet(mainId);

            if (activeSet.HasValue && activeSet.Value.skills != null)
            {
                var skills = activeSet.Value.skills;
                for (int i = 0; i < skills.Length; i++)
                {
                    var exSkill = skills[i];
                    if (exSkill.definition == null) continue;
                    if (exSkill.prefab == null) continue;
                    if (!IsValidCandidate(exSkill.definition, loadout)) continue;

                    float baseW = exclusiveWeightMultiplier;
                    float pity = 1f + _currentPityBonus;
                    float penalty = GetRecentPenalty(exSkill.definition.SkillId);

                    _skillCandidates.Add(new WeightedSkill
                    {
                        definition = exSkill.definition,
                        prefab = exSkill.prefab,
                        weight = baseW * pity * penalty,
                        isExclusive = true
                    });
                }
            }
        }

        private void BuildPassiveCandidates(PlayerSkillLoadout loadout)
        {
            _passiveCandidates.Clear();

            IReadOnlyList<SkillDefinitionSO> passives = skillCatalog.GetByType(SkillType.Passive);
            if (passives == null) return;

            for (int i = 0; i < passives.Count; i++)
            {
                SkillDefinitionSO skill = passives[i];
                if (!IsValidCandidate(skill, loadout)) continue;

                if (skill.PassiveStatType == PassiveStatType.None) continue;

                _passiveCandidates.Add(skill);
            }
        }
        
        //  후보 검증

        private bool IsValidCandidate(SkillDefinitionSO skill, PlayerSkillLoadout loadout)
        {
            if (skill == null) return false;
            if (string.IsNullOrWhiteSpace(skill.SkillId)) return false;
            if (string.IsNullOrWhiteSpace(skill.DisplayName)) return false;
            return loadout.CanAppearAsCard(skill);
        }
        
        //  슬롯 채우기

        private void FillSkillSlots(
            List<LevelUpCardData> result,
            PlayerSkillLoadout loadout,
            HashSet<string> usedIds,
            int count,
            bool needGuarantee)
        {
            if (_skillCandidates.Count == 0) return;

            // 보장: 전용 스킬 1장 강제 배치
            if (needGuarantee)
            {
                for (int i = 0; i < _skillCandidates.Count; i++)
                {
                    if (!_skillCandidates[i].isExclusive) continue;

                    var c = _skillCandidates[i];
                    result.Add(MakeCardData(c, loadout));
                    usedIds.Add(c.definition.SkillId);
                    _exclusiveEverOffered = true;
                    count--;

                    GameLogger.Log($"[CardGen] 전용 스킬 보장: {c.definition.DisplayName}", this);
                    break;
                }
            }

            // 나머지: 가중치 랜덤
            int safety = 256;
            while (count > 0 && safety-- > 0)
            {
                int picked = WeightedRandomPick(usedIds);
                if (picked < 0) break;

                var c = _skillCandidates[picked];
                result.Add(MakeCardData(c, loadout));
                usedIds.Add(c.definition.SkillId);
                count--;

                if (c.isExclusive)
                    _exclusiveEverOffered = true;
            }
        }

        private void FillPassiveSlots(
            List<LevelUpCardData> result,
            PlayerSkillLoadout loadout,
            HashSet<string> usedIds,
            int count)
        {
            if (_passiveCandidates.Count == 0) return;

            List<SkillDefinitionSO> shuffled = new List<SkillDefinitionSO>(_passiveCandidates);
            Shuffle(shuffled);

            for (int i = 0; i < shuffled.Count && count > 0; i++)
            {
                SkillDefinitionSO skill = shuffled[i];
                if (skill == null) continue;
                if (!usedIds.Add(skill.SkillId)) continue;

                string desc = loadout.BuildCardDescription(skill);
                var card = LevelUpCardData.CreateSkillCard(skill, desc);

                // 현재 레벨 세팅
                var state = loadout.GetSkill(skill.SkillId);
                card.CurrentLevel = state != null ? state.Level : 0;
                card.NextLevel    = card.CurrentLevel + 1;

                result.Add(card);
                count--;
            }
        }
        
        //  카드 데이터 생성

        private LevelUpCardData MakeCardData(WeightedSkill ws, PlayerSkillLoadout loadout)
        {
            string desc = loadout.BuildCardDescription(ws.definition);

            // 현재 레벨 조회
            var state = loadout.GetSkill(ws.definition.SkillId);
            int curLevel = state != null ? state.Level : 0;

            LevelUpCardData card;
            if (ws.isExclusive)
                card = LevelUpCardData.CreateCharacterSkillCard(ws.definition, desc, ws.prefab);
            else
                card = LevelUpCardData.CreateSkillCard(ws.definition, desc);

            card.CurrentLevel = curLevel;
            card.NextLevel    = curLevel + 1;
            card.AddInfo = ws.definition.GetAddInfoForLevel(curLevel + 1);

            return card;
        }

        //  가중치 랜덤

        private int WeightedRandomPick(HashSet<string> usedIds)
        {
            float totalWeight = 0f;
            for (int i = 0; i < _skillCandidates.Count; i++)
            {
                if (usedIds.Contains(_skillCandidates[i].definition.SkillId)) continue;
                totalWeight += Mathf.Max(0f, _skillCandidates[i].weight);
            }

            if (totalWeight <= 0f) return -1;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float acc = 0f;

            for (int i = 0; i < _skillCandidates.Count; i++)
            {
                if (usedIds.Contains(_skillCandidates[i].definition.SkillId)) continue;
                float w = Mathf.Max(0f, _skillCandidates[i].weight);
                acc += w;
                if (roll <= acc) return i;
            }

            // fallback
            for (int i = _skillCandidates.Count - 1; i >= 0; i--)
            {
                if (!usedIds.Contains(_skillCandidates[i].definition.SkillId))
                    return i;
            }

            return -1;
        }
        
        //  보장 / 피티 / 페널티

        private bool NeedExclusiveGuarantee()
        {
            if (_exclusiveEverOffered) return false;

            // 전용 후보 존재 확인
            bool hasExclusive = false;
            for (int i = 0; i < _skillCandidates.Count; i++)
            {
                if (_skillCandidates[i].isExclusive) { hasExclusive = true; break; }
            }
            if (!hasExclusive) return false;

            // 보장 윈도우: startLevel ~ endLevel 사이면 보장
            // endLevel 이후에도 미등장이면 강제 보장
            return _levelUpCount >= guaranteeStartLevel;
        }

        private float GetRecentPenalty(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId)) return 1f;
            if (_recentHistory.TryGetValue(skillId, out int lastSeen))
            {
                int gap = _levelUpCount - lastSeen;
                if (gap <= 1) return recentPenalty1;
                if (gap <= 2) return recentPenalty2;
            }
            return 1f;
        }

        private void UpdateRecentHistory(List<LevelUpCardData> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].SkillDefinition == null) continue;
                _recentHistory[cards[i].SkillDefinition.SkillId] = _levelUpCount;
            }
        }

        private CharacterSkillSet? FindCharacterSkillSet(string mainId)
        {
            if (string.IsNullOrWhiteSpace(mainId)) return null;
            for (int i = 0; i < characterSkillSets.Length; i++)
            {
                if (string.Equals(characterSkillSets[i].characterId, mainId, StringComparison.OrdinalIgnoreCase))
                    return characterSkillSets[i];
            }
            return null;
        }
        
        //  대체 카드
        
        private void FillWithFallbackCards(List<LevelUpCardData> result)
        {
            result.Clear();
            result.Add(LevelUpCardData.CreateHealCard(fallbackHealAmount));
            result.Add(LevelUpCardData.CreateGoldCard(fallbackGoldAmount));
            result.Add(LevelUpCardData.CreateInvincibleCard(fallbackInvincibleDuration));
            result.Add(LevelUpCardData.CreateBonusExpCard(fallbackBonusExpAmount));
        }

        private void FillRemainingWithFallbackCards(List<LevelUpCardData> result)
        {
            List<LevelUpCardData> pool = new List<LevelUpCardData>
            {
                LevelUpCardData.CreateHealCard(fallbackHealAmount),
                LevelUpCardData.CreateGoldCard(fallbackGoldAmount),
                LevelUpCardData.CreateInvincibleCard(fallbackInvincibleDuration),
                LevelUpCardData.CreateBonusExpCard(fallbackBonusExpAmount)
            };
            Shuffle(pool);

            for (int i = 0; i < pool.Count && result.Count < totalCardCount; i++)
                result.Add(pool[i]);
        }
        
        //  유틸
        
        private void Shuffle<T>(List<T> list)
        {
            if (list == null || list.Count <= 1) return;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}