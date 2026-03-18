// ──────────────────────────────────────────────
// LevelUpCardGenerator.cs
// 레벨업 시 카드 4장 데이터를 생성하는 생성기
//
// 구현 원리 요약:
// 후보 생성은 전달받은 PlayerSkillLoadout 하나만 기준으로 판단한다.
// 최대 레벨 도달 스킬은 loadout.CanAppearAsCard()에서 탈락해야 하며,
// 리롤도 같은 loadout을 넘기면 동일 결과가 나와야 한다.
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using _Game.Player;
using _Game.Skills;

namespace _Game.LevelUp
{
    /// <summary>
    /// 레벨업 카드 4장을 생성한다.
    /// </summary>
    public sealed class LevelUpCardGenerator : MonoBehaviour
    {
        private const int CARD_COUNT = 4;
        private const int DEFAULT_ACTIVE_COUNT = 2;
        private const int DEFAULT_PASSIVE_COUNT = 2;

        [Header("=== 카탈로그 ===")]
        [SerializeField, Tooltip("전체 공통 스킬 / 패시브 목록입니다.")]
        private SkillCatalogSO skillCatalog;

        [SerializeField, Tooltip("액티브 스킬을 실제 런타임 공통 스킬과 매핑할 카탈로그입니다.")]
        private CommonSkillCatalogSO commonSkillCatalog;

        [Header("=== 대체 카드 수치 ===")]
        [SerializeField, Tooltip("체력 회복량입니다.")]
        private int fallbackHealAmount = 30;

        [SerializeField, Tooltip("재화 획득량입니다.")]
        private int fallbackGoldAmount = 200;

        [SerializeField, Tooltip("무적 지속 시간(초)입니다.")]
        private float fallbackInvincibleDuration = 5f;

        [SerializeField, Tooltip("즉시 획득 경험치입니다.")]
        private int fallbackBonusExpAmount = 50;

        /// <summary>
        /// 전달받은 loadout 기준으로 카드 4장을 생성한다.
        /// </summary>
        public List<LevelUpCardData> Generate(PlayerSkillLoadout loadout)
        {
            List<LevelUpCardData> result = new List<LevelUpCardData>(CARD_COUNT);

            if (skillCatalog == null || loadout == null)
            {
                Debug.LogWarning("[CardGen] skillCatalog 또는 loadout이 없어 대체 카드로 채웁니다.", this);
                FillWithFallbackCards(result);
                return result;
            }

            Debug.Log(
                $"[CardGen] Generate 시작 | loadoutInstanceId={loadout.GetInstanceID()} | loadoutName={loadout.name}",
                loadout);

            List<SkillDefinitionSO> activeCandidates = BuildCandidates(skillCatalog.GetByType(SkillType.Active), loadout);
            List<SkillDefinitionSO> passiveCandidates = BuildCandidates(skillCatalog.GetByType(SkillType.Passive), loadout);

            Debug.Log($"[CardGen] 후보: 액티브={activeCandidates.Count} 패시브={passiveCandidates.Count} 합계={activeCandidates.Count + passiveCandidates.Count}", this);

            if (activeCandidates.Count == 0 && passiveCandidates.Count == 0)
            {
                FillWithFallbackCards(result);
                return result;
            }

            HashSet<string> usedSkillIds = new HashSet<string>();

            AddSkillCards(result, activeCandidates, usedSkillIds, loadout, DEFAULT_ACTIVE_COUNT);
            AddSkillCards(result, passiveCandidates, usedSkillIds, loadout, DEFAULT_PASSIVE_COUNT);

            if (result.Count < CARD_COUNT)
                AddSkillCards(result, activeCandidates, usedSkillIds, loadout, CARD_COUNT - result.Count);

            if (result.Count < CARD_COUNT)
                AddSkillCards(result, passiveCandidates, usedSkillIds, loadout, CARD_COUNT - result.Count);

            if (result.Count < CARD_COUNT)
                FillRemainingWithFallbackCards(result);

            return result;
        }

        /// <summary>
        /// 특정 타입의 후보군을 만든다.
        /// </summary>
        private List<SkillDefinitionSO> BuildCandidates(
            IReadOnlyList<SkillDefinitionSO> source,
            PlayerSkillLoadout loadout)
        {
            List<SkillDefinitionSO> result = new List<SkillDefinitionSO>();

            if (source == null || loadout == null)
                return result;

            for (int i = 0; i < source.Count; i++)
            {
                SkillDefinitionSO skill = source[i];
                if (skill == null) continue;
                if (string.IsNullOrWhiteSpace(skill.SkillId)) continue;
                if (string.IsNullOrWhiteSpace(skill.DisplayName)) continue;

                if (skill.SkillType == SkillType.Passive && skill.PassiveStatType == PassiveStatType.None)
                {
                    Debug.Log($"[CardGen] [패시브] 제외(PassiveStatType=None): {skill.DisplayName}", this);
                    continue;
                }

                if (skill.SkillType == SkillType.Active && commonSkillCatalog != null)
                {
                    if (!commonSkillCatalog.TryResolve(skill, out CommonSkillConfigSO config))
                    {
                        Debug.Log($"[CardGen] [액티브] 제외(런타임 매핑 실패): {skill.DisplayName}", this);
                        continue;
                    }

                    if (config == null || config.weaponPrefab == null)
                    {
                        Debug.Log($"[CardGen] [액티브] 제외(weaponPrefab 누락): {skill.DisplayName}", this);
                        continue;
                    }
                }

                bool canAppear = loadout.CanAppearAsCard(skill);

                if (!canAppear)
                {
                    Debug.Log($"[CardGen] [{(skill.SkillType == SkillType.Active ? "액티브" : "패시브")}] 제외(CanAppearAsCard=false): {skill.DisplayName}", this);
                    continue;
                }

                Debug.Log($"[CardGen] [{(skill.SkillType == SkillType.Active ? "액티브" : "패시브")}] 후보 통과: {skill.DisplayName}", this);
                result.Add(skill);
            }

            return result;
        }

        /// <summary>
        /// 후보군에서 카드를 추가한다.
        /// </summary>
        private void AddSkillCards(
            List<LevelUpCardData> result,
            List<SkillDefinitionSO> candidates,
            HashSet<string> usedSkillIds,
            PlayerSkillLoadout loadout,
            int count)
        {
            if (count <= 0 || candidates == null || candidates.Count == 0)
                return;

            List<SkillDefinitionSO> shuffled = new List<SkillDefinitionSO>(candidates);
            Shuffle(shuffled);

            for (int i = 0; i < shuffled.Count; i++)
            {
                if (result.Count >= CARD_COUNT) return;

                SkillDefinitionSO skill = shuffled[i];
                if (skill == null) continue;
                if (!usedSkillIds.Add(skill.SkillId)) continue;

                string description = loadout.BuildCardDescription(skill);
                result.Add(LevelUpCardData.CreateSkillCard(skill, description));

                count--;
                if (count <= 0) return;
            }
        }

        /// <summary>
        /// 결과를 대체 카드 4장으로 채운다.
        /// </summary>
        private void FillWithFallbackCards(List<LevelUpCardData> result)
        {
            result.Clear();
            result.Add(LevelUpCardData.CreateHealCard(fallbackHealAmount));
            result.Add(LevelUpCardData.CreateGoldCard(fallbackGoldAmount));
            result.Add(LevelUpCardData.CreateInvincibleCard(fallbackInvincibleDuration));
            result.Add(LevelUpCardData.CreateBonusExpCard(fallbackBonusExpAmount));
        }

        /// <summary>
        /// 부족한 칸만 대체 카드로 채운다.
        /// </summary>
        private void FillRemainingWithFallbackCards(List<LevelUpCardData> result)
        {
            List<LevelUpCardData> fallbackPool = new List<LevelUpCardData>
            {
                LevelUpCardData.CreateHealCard(fallbackHealAmount),
                LevelUpCardData.CreateGoldCard(fallbackGoldAmount),
                LevelUpCardData.CreateInvincibleCard(fallbackInvincibleDuration),
                LevelUpCardData.CreateBonusExpCard(fallbackBonusExpAmount)
            };

            Shuffle(fallbackPool);

            for (int i = 0; i < fallbackPool.Count; i++)
            {
                if (result.Count >= CARD_COUNT) return;
                result.Add(fallbackPool[i]);
            }
        }

        /// <summary>
        /// 리스트를 셔플한다.
        /// </summary>
        private void Shuffle<T>(List<T> list)
        {
            if (list == null || list.Count <= 1) return;

            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }
    }
}