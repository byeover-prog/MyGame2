using System;
using System.Collections.Generic;
using UnityEngine;

// 슬롯 구조:
//   슬롯 1~3 = 스킬 카드 (공통 스킬 + 메인 캐릭터 전용 스킬)
//   슬롯 4~5 = 패시브 카드 (Lv1~5 구간은 1장만, Lv6+ 2장)
// 핵심 기능:
//   - 메인 캐릭터 ID 기반 전용 스킬 카드 풀 동적 구성
//   - 전용 스킬 첫 등장 보장 (Lv2~4)
//   - 리롤 피티 (리롤마다 전용 스킬 가중치 +15% 누적)
//   - 동일 카드 연속 등장 soft penalty
//   - 패시브 초반 과속 방지
//   - maxLevel 도달 카드 자동 제외

[DisallowMultipleComponent]
public sealed class OfferService : MonoBehaviour
{
    //  카탈로그 항목 (인스펙터 입력)

    [Serializable]
    public struct OfferCatalogItem
    {
        [Header("식별")]
        [Tooltip("고유 ID(중복 금지). 비우면 prefab.name으로 자동 채움(OnValidate).")]
        public string id;

        [Tooltip("카드 종류(무기/패시브)")]
        public OfferKind kind;

        [Header("카드 표시")]
        public Sprite icon;
        public string titleKr;
        public string tagKr;

        [Header("프리팹")]
        public GameObject prefab;

        [Header("정책")]
        public bool showInRunOffers;

        [Range(0, 8)]
        public int maxLevel;

        [Header("가중치")]
        [Tooltip("기본 등장 가중치 (높을수록 잘 나옴). 0이면 100으로 간주.")]
        [Min(0)]
        public int weight;

        [Header("레벨별 설명")]
        [TextArea] public string descLv1;
        [TextArea] public string descLv2;
        [TextArea] public string descLv3;
        [TextArea] public string descLv4;
        [TextArea] public string descLv5;
        [TextArea] public string descLv6;
        [TextArea] public string descLv7;
        [TextArea] public string descLv8;

        public int GetMaxLevelOrDefault() => (maxLevel <= 0) ? 8 : Mathf.Clamp(maxLevel, 1, 8);
        public int GetWeightOrDefault()   => (weight <= 0) ? 100 : weight;

        public string GetDescriptionForLevel(int nextLevel)
        {
            nextLevel = Mathf.Clamp(nextLevel, 1, 8);

            string d = nextLevel switch
            {
                1 => descLv1, 2 => descLv2, 3 => descLv3, 4 => descLv4,
                5 => descLv5, 6 => descLv6, 7 => descLv7, 8 => descLv8,
                _ => descLv1
            };

            if (!string.IsNullOrWhiteSpace(d)) return d;

            for (int lv = nextLevel; lv >= 1; lv--)
            {
                string dd = lv switch
                {
                    1 => descLv1, 2 => descLv2, 3 => descLv3, 4 => descLv4,
                    5 => descLv5, 6 => descLv6, 7 => descLv7, 8 => descLv8,
                    _ => descLv1
                };
                if (!string.IsNullOrWhiteSpace(dd)) return dd;
            }
            return "";
        }
    }
    
    //  캐릭터 전용 스킬 세트 (인스펙터 입력)

    [Serializable]
    public struct CharacterSkillSet
    {
        [Tooltip("캐릭터 ID (SquadLoadoutRuntime.MainId와 일치해야 함)")]
        public string characterId;

        [Tooltip("이 캐릭터의 전용 스킬 (최대 2개)")]
        public OfferCatalogItem[] skills;
    }
    
    //  인스펙터 필드

    [Header("=== 공통 스킬 카탈로그 (슬롯 1~3) ===")]
    [SerializeField] private OfferCatalogItem[] weaponCatalog = Array.Empty<OfferCatalogItem>();

    [Header("=== 패시브 카탈로그 (슬롯 4~5) ===")]
    [SerializeField] private OfferCatalogItem[] passiveCatalog = Array.Empty<OfferCatalogItem>();

    [Header("=== 캐릭터 전용 스킬 세트 ===")]
    [Tooltip("캐릭터별 전용 스킬 2개씩 등록. 메인 캐릭터에 해당하는 세트만 카드 풀에 포함됩니다.")]
    [SerializeField] private CharacterSkillSet[] characterSkillSets = Array.Empty<CharacterSkillSet>();

    [Header("=== 전용 스킬 가중치 ===")]
    [Tooltip("전용 스킬 기본 가중치 배율 (공통 스킬 대비)")]
    [SerializeField, Range(1f, 5f)] private float exclusiveWeightMultiplier = 1.5f;

    [Header("=== 전용 스킬 보장 ===")]
    [Tooltip("전용 스킬 첫 등장 보장 시작 레벨 (이 레벨부터 보장 윈도우 시작)")]
    [SerializeField, Min(1)] private int exclusiveGuaranteeStartLevel = 2;

    [Tooltip("전용 스킬 첫 등장 보장 종료 레벨 (이 레벨까지 미등장이면 강제 배치)")]
    [SerializeField, Min(2)] private int exclusiveGuaranteeEndLevel = 4;

    [Tooltip("리롤 시 전용 스킬 가중치 누적 보너스 (피티)")]
    [SerializeField, Range(0f, 1f)] private float rerollPityBonusPerRoll = 0.15f;

    [Header("=== 패시브 과속 방지 ===")]
    [Tooltip("이 레벨까지는 패시브 슬롯 1장만 열림")]
    [SerializeField, Min(1)] private int passiveSingleSlotUntilLevel = 5;

    [Header("=== 연속 등장 방지 ===")]
    [Tooltip("직전 레벨업에서 등장한 카드의 가중치 페널티")]
    [SerializeField, Range(0f, 1f)] private float recentPenalty1 = 0.5f;

    [Tooltip("2회 전 레벨업에서 등장한 카드의 가중치 페널티")]
    [SerializeField, Range(0f, 1f)] private float recentPenalty2 = 0.75f;

    [Header("=== 리롤 ===")]
    [SerializeField, Min(0)] private int rerollMaxCount = 3;

    [Header("=== 디버그 ===")]
    [SerializeField] private bool enableLogs = false;

    //  내부 상태

    private SkillRuntimeState _state;

    // 런 진행 추적
    private int _levelUpCount;
    private bool _exclusiveEverOffered;
    private int _currentRerollCount;
    private float _currentPityBonus;

    // 연속 등장 방지: id → 마지막 등장 레벨업 번호
    private readonly Dictionary<string, int> _recentHistory = new(32);

    // 재사용 버퍼
    private readonly List<WeightedCandidate> _skillCandidates = new(32);
    private readonly List<WeightedCandidate> _passiveCandidates = new(16);
    private readonly HashSet<string> _dedupIds = new();
    private readonly List<Offer> _resultOffers = new(5);

    private struct WeightedCandidate
    {
        public OfferCatalogItem item;
        public float weight;
        public bool isExclusive;
        public string ownerId;
    }
    
    //  이벤트 연결

    private void OnEnable()
    {
        GameSignals.RerollRequested += HandleReroll;
        GameSignals.LevelUpClosed += HandleLevelUpClosed;
    }

    private void OnDisable()
    {
        GameSignals.RerollRequested -= HandleReroll;
        GameSignals.LevelUpClosed -= HandleLevelUpClosed;
    }

    //  공개 API

    public void Bind(SkillRuntimeState state)
    {
        _state = state;
    }
    
    // 런 시작 시 호출 — 내부 추적 상태를 초기화한다.
    
    public void ResetRunState()
    {
        _levelUpCount = 0;
        _exclusiveEverOffered = false;
        _currentRerollCount = 0;
        _currentPityBonus = 0f;
        _recentHistory.Clear();
    }
    
    // 오퍼를 생성하고 GameSignals.RaiseOffersReady()로 발행한다.
    
    public void RequestOffers(int countOverride)
    {
        if (_state == null)
        {
            GameLogger.LogWarning("[OfferService] RequestOffers 무시 — Bind(state) 미호출.", this);
            return;
        }

        // 레벨업 카운트 증가 (리롤이 아닌 첫 요청일 때만)
        if (_currentRerollCount == 0)
        {
            _levelUpCount++;
            _currentPityBonus = 0f;
        }

        var offers = BuildOffers();

        if (enableLogs)
            GameLogger.Log($"[OfferService] OffersReady => {offers.Length}장 (LevelUp #{_levelUpCount}, Reroll #{_currentRerollCount})", this);

        GameSignals.RaiseOffersReady(offers);
    }
    
    //  오퍼 생성 — 핵심 로직

    private Offer[] BuildOffers()
    {
        _resultOffers.Clear();

        // 1) 메인 캐릭터 전용 스킬 세트 찾기
        string mainId = SquadLoadoutRuntime.MainId;
        CharacterSkillSet? activeSet = FindCharacterSkillSet(mainId);

        // 2) 스킬 슬롯 수 결정 (3장 기본, 패시브가 1장이면 4장까지 가능)
        int passiveSlotCount = GetPassiveSlotCount();
        int skillSlotCount = 5 - passiveSlotCount; // 3 또는 4

        // 3) 스킬 후보 수집 (공통 + 전용)
        BuildSkillCandidates(activeSet);

        // 4) 패시브 후보 수집
        BuildPassiveCandidates();

        // 5) 전용 스킬 보장 체크
        bool needGuarantee = NeedExclusiveGuarantee();

        // 6) 스킬 슬롯 채우기
        FillSkillSlots(skillSlotCount, needGuarantee);

        // 7) 패시브 슬롯 채우기
        FillPassiveSlots(passiveSlotCount);

        // 8) 연속 등장 기록 갱신
        UpdateRecentHistory();

        return _resultOffers.ToArray();
    }

    // 패시브 슬롯 수를 결정한다. 초반에는 1장만.
    private int GetPassiveSlotCount()
    {
        if (_levelUpCount <= passiveSingleSlotUntilLevel)
            return 1;
        return 2;
    }

    // 메인 캐릭터에 해당하는 전용 스킬 세트를 찾는다.
    private CharacterSkillSet? FindCharacterSkillSet(string mainCharId)
    {
        if (string.IsNullOrWhiteSpace(mainCharId))
            return null;

        for (int i = 0; i < characterSkillSets.Length; i++)
        {
            if (string.Equals(characterSkillSets[i].characterId, mainCharId, StringComparison.OrdinalIgnoreCase))
                return characterSkillSets[i];
        }

        if (enableLogs)
            GameLogger.LogWarning($"[OfferService] 메인 캐릭터 '{mainCharId}'에 해당하는 전용 스킬 세트를 찾지 못했습니다.", this);

        return null;
    }
    
    //  후보 수집

    private void BuildSkillCandidates(CharacterSkillSet? charSet)
    {
        _skillCandidates.Clear();
        _dedupIds.Clear();

        // 공통 스킬
        for (int i = 0; i < weaponCatalog.Length; i++)
        {
            var item = weaponCatalog[i];
            if (!TryValidateCandidate(ref item, OfferKind.Weapon))
                continue;

            float w = item.GetWeightOrDefault() * GetRecentPenalty(item.id);

            _skillCandidates.Add(new WeightedCandidate
            {
                item = item,
                weight = w,
                isExclusive = false,
                ownerId = null
            });
        }

        // 전용 스킬
        if (charSet.HasValue && charSet.Value.skills != null)
        {
            string ownerId = charSet.Value.characterId;
            var skills = charSet.Value.skills;

            for (int i = 0; i < skills.Length; i++)
            {
                var item = skills[i];
                // 전용 스킬은 kind를 CharacterSkill로 강제
                item.kind = OfferKind.CharacterSkill;

                if (!TryValidateCandidate(ref item, OfferKind.CharacterSkill))
                    continue;

                float baseW = item.GetWeightOrDefault() * exclusiveWeightMultiplier;
                float pity = 1f + _currentPityBonus;
                float penalty = GetRecentPenalty(item.id);

                _skillCandidates.Add(new WeightedCandidate
                {
                    item = item,
                    weight = baseW * pity * penalty,
                    isExclusive = true,
                    ownerId = ownerId
                });
            }
        }

        if (enableLogs)
            GameLogger.Log($"[OfferService] 스킬 후보: {_skillCandidates.Count}개 (공통+전용)", this);
    }

    private void BuildPassiveCandidates()
    {
        _passiveCandidates.Clear();
        // _dedupIds는 스킬 후보에서 이미 사용 중이므로 패시브는 별도로 처리
        var passiveDedup = new HashSet<string>();

        for (int i = 0; i < passiveCatalog.Length; i++)
        {
            var item = passiveCatalog[i];
            item.kind = OfferKind.Passive; // 강제

            if (!TryValidateCandidateWithDedup(ref item, OfferKind.Passive, passiveDedup))
                continue;

            float w = item.GetWeightOrDefault() * GetRecentPenalty(item.id);

            _passiveCandidates.Add(new WeightedCandidate
            {
                item = item,
                weight = w,
                isExclusive = false,
                ownerId = null
            });
        }

        if (enableLogs)
            GameLogger.Log($"[OfferService] 패시브 후보: {_passiveCandidates.Count}개", this);
    }
    
    //  후보 검증

    private bool TryValidateCandidate(ref OfferCatalogItem item, OfferKind kind)
    {
        return TryValidateCandidateWithDedup(ref item, kind, _dedupIds);
    }

    private bool TryValidateCandidateWithDedup(ref OfferCatalogItem item, OfferKind kind, HashSet<string> dedup)
    {
        if (!item.showInRunOffers)
        {
            LogSkip(item, "showInRunOffers=false");
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.id))
        {
            LogSkip(item, "id empty");
            return false;
        }

        if (item.prefab == null)
        {
            LogSkip(item, "prefab null");
            return false;
        }

        if (!dedup.Add(item.id))
        {
            LogSkip(item, "duplicate id");
            return false;
        }

        int max = item.GetMaxLevelOrDefault();
        int cur = _state.GetLevel(kind, item.id);

        if (cur >= max)
        {
            LogSkip(item, $"maxLevel (cur={cur}, max={max})");
            return false;
        }

        if (cur <= 0 && !_state.CanAcquire(kind))
        {
            LogSkip(item, "slot full");
            return false;
        }

        return true;
    }
    
    //  슬롯 채우기

    private void FillSkillSlots(int count, bool needGuarantee)
    {
        if (_skillCandidates.Count == 0) return;

        var usedIds = new HashSet<string>();
        bool guaranteeFulfilled = false;

        // 보장이 필요하면 먼저 전용 스킬 1장 강제 배치
        if (needGuarantee)
        {
            for (int i = 0; i < _skillCandidates.Count; i++)
            {
                if (!_skillCandidates[i].isExclusive) continue;

                var c = _skillCandidates[i];
                _resultOffers.Add(MakeOffer(c));
                usedIds.Add(c.item.id);
                _exclusiveEverOffered = true;
                guaranteeFulfilled = true;
                count--;

                if (enableLogs)
                    GameLogger.Log($"[OfferService] 전용 스킬 보장 배치: {c.item.id}", this);

                break;
            }
        }

        // 나머지 스킬 슬롯은 가중치 랜덤
        int safety = 256;
        while (count > 0 && safety-- > 0)
        {
            int picked = WeightedRandomPick(_skillCandidates, usedIds);
            if (picked < 0) break;

            var c = _skillCandidates[picked];
            _resultOffers.Add(MakeOffer(c));
            usedIds.Add(c.item.id);
            count--;

            if (c.isExclusive)
                _exclusiveEverOffered = true;
        }
    }

    private void FillPassiveSlots(int count)
    {
        if (_passiveCandidates.Count == 0) return;

        var usedIds = new HashSet<string>();
        int safety = 64;

        while (count > 0 && safety-- > 0)
        {
            int picked = WeightedRandomPick(_passiveCandidates, usedIds);
            if (picked < 0) break;

            var c = _passiveCandidates[picked];
            _resultOffers.Add(MakeOffer(c));
            usedIds.Add(c.item.id);
            count--;
        }
    }
    
    //  가중치 랜덤 선택

    private int WeightedRandomPick(List<WeightedCandidate> candidates, HashSet<string> usedIds)
    {
        float totalWeight = 0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (usedIds.Contains(candidates[i].item.id)) continue;
            totalWeight += Mathf.Max(0f, candidates[i].weight);
        }

        if (totalWeight <= 0f)
            return -1;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float accumulated = 0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (usedIds.Contains(candidates[i].item.id)) continue;
            float w = Mathf.Max(0f, candidates[i].weight);
            accumulated += w;
            if (roll <= accumulated)
                return i;
        }

        // fallback: 마지막 유효 후보
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            if (!usedIds.Contains(candidates[i].item.id))
                return i;
        }

        return -1;
    }

    //  Offer 생성

    private Offer MakeOffer(WeightedCandidate c)
    {
        var item = c.item;
        OfferKind kind = c.isExclusive ? OfferKind.CharacterSkill : item.kind;
        int cur = _state.GetLevel(kind, item.id);
        int next = Mathf.Clamp(cur + 1, 1, item.GetMaxLevelOrDefault());

        return new Offer
        {
            id = item.id,
            kind = kind,
            icon = item.icon,
            titleKr = string.IsNullOrWhiteSpace(item.titleKr) ? item.id : item.titleKr,
            tagKr = string.IsNullOrWhiteSpace(item.tagKr) ? "" : item.tagKr,
            descKr = item.GetDescriptionForLevel(next),
            prefab = item.prefab,
            isExclusive = c.isExclusive,
            ownerId = c.ownerId,
            previewLevel = next
        };
    }

    //  보장 / 피티 / 페널티
    // 전용 스킬 보장이 필요한지 판단한다.
    private bool NeedExclusiveGuarantee()
    {
        if (_exclusiveEverOffered)
            return false;

        // 전용 스킬 후보가 있는지 확인
        bool hasExclusiveCandidate = false;
        for (int i = 0; i < _skillCandidates.Count; i++)
        {
            if (_skillCandidates[i].isExclusive)
            {
                hasExclusiveCandidate = true;
                break;
            }
        }

        if (!hasExclusiveCandidate)
            return false;

        // 보장 윈도우 내인지 체크
        if (_levelUpCount >= exclusiveGuaranteeStartLevel && _levelUpCount <= exclusiveGuaranteeEndLevel)
            return true;

        // 보장 윈도우를 넘겼는데 아직 안 나왔으면 강제
        if (_levelUpCount > exclusiveGuaranteeEndLevel)
            return true;

        return false;
    }

    // 연속 등장 페널티 계수를 반환한다.
    private float GetRecentPenalty(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return 1f;

        if (_recentHistory.TryGetValue(id, out int lastSeen))
        {
            int gap = _levelUpCount - lastSeen;
            if (gap <= 1) return recentPenalty1;
            if (gap <= 2) return recentPenalty2;
        }

        return 1f;
    }

    // 이번 레벨업에서 제시된 카드를 연속 등장 기록에 저장한다.
    private void UpdateRecentHistory()
    {
        for (int i = 0; i < _resultOffers.Count; i++)
        {
            string id = _resultOffers[i].id;
            if (!string.IsNullOrWhiteSpace(id))
                _recentHistory[id] = _levelUpCount;
        }
    }
    
    //  이벤트 핸들러

    private void HandleReroll()
    {
        if (_state == null) return;

        _currentRerollCount++;
        _currentPityBonus += rerollPityBonusPerRoll;

        if (enableLogs)
            GameLogger.Log($"[OfferService] Reroll #{_currentRerollCount} (pity={_currentPityBonus:F2})", this);

        RequestOffers(5);
    }

    private void HandleLevelUpClosed()
    {
        // 리롤 카운트/피티를 다음 레벨업을 위해 초기화
        _currentRerollCount = 0;
        _currentPityBonus = 0f;
    }
    
    //  디버그 로그

    private void LogSkip(OfferCatalogItem item, string reason)
    {
        if (!enableLogs) return;
        string id = string.IsNullOrWhiteSpace(item.id) ? "(empty)" : item.id;
        GameLogger.Log($"[OfferService] SKIP id={id} kind={item.kind} reason={reason}", this);
    }

    //  에디터 자동 채움

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoFillCatalog(weaponCatalog);
        AutoFillCatalog(passiveCatalog);

        if (characterSkillSets != null)
        {
            for (int i = 0; i < characterSkillSets.Length; i++)
            {
                if (characterSkillSets[i].skills != null)
                    AutoFillCatalog(characterSkillSets[i].skills);
            }
        }
    }

    private static void AutoFillCatalog(OfferCatalogItem[] catalog)
    {
        if (catalog == null) return;
        for (int i = 0; i < catalog.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(catalog[i].id) && catalog[i].prefab != null)
                catalog[i].id = catalog[i].prefab.name;
            if (catalog[i].maxLevel < 0) catalog[i].maxLevel = 0;
            if (catalog[i].maxLevel > 8) catalog[i].maxLevel = 8;
        }
    }
#endif
}