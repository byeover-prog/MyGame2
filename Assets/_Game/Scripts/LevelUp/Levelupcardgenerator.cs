// ──────────────────────────────────────────────
// LevelUpCardGenerator.cs
// 레벨업 시 카드 4장 데이터를 생성하는 생성기
//
// 규칙:
//   1) 액티브 후보 2장 + 패시브 후보 2장 = 4장
//   2) 한쪽이 부족하면 반대쪽으로 보충
//   3) 둘 다 부족하면 대체 카드로 보충
//   4) 전부 소진이면 대체 카드 4장
//   5) 런타임으로 연결 불가능한 액티브는 후보에서 제외
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using _Game.Player;
using _Game.Skills;

namespace _Game.LevelUp
{
    public sealed class LevelUpCardGenerator : MonoBehaviour
    {
        private const int CARD_COUNT           = 4;
        private const int DEFAULT_ACTIVE_COUNT = 2;
        private const int DEFAULT_PASSIVE_COUNT = 2;

        [Header("=== 카탈로그 ===")]

        [SerializeField, Tooltip("전체 공통 스킬 / 패시브 목록")]
        private SkillCatalogSO skillCatalog;

        [SerializeField, Tooltip("액티브 스킬을 실제 런타임 공통 스킬과 매핑할 카탈로그")]
        private CommonSkillCatalogSO commonSkillCatalog;

        [Header("=== 대체 카드 수치 ===")]

        [SerializeField, Tooltip("체력 회복량")]
        private int fallbackHealAmount = 30;

        [SerializeField, Tooltip("재화 획득량")]
        private int fallbackGoldAmount = 200;

        [SerializeField, Tooltip("무적 지속 시간(초)")]
        private float fallbackInvincibleDuration = 5f;

        [SerializeField, Tooltip("즉시 획득 경험치")]
        private int fallbackBonusExpAmount = 50;

        public List<LevelUpCardData> Generate(PlayerSkillLoadout loadout)
        {
            List<LevelUpCardData> result = new List<LevelUpCardData>(CARD_COUNT);

            if (skillCatalog == null || loadout == null)
            {
                FillWithFallbackCards(result);
                return result;
            }

            List<SkillDefinitionSO> activeCandidates  = BuildCandidates(skillCatalog.GetByType(SkillType.Active), loadout);
            List<SkillDefinitionSO> passiveCandidates = BuildCandidates(skillCatalog.GetByType(SkillType.Passive), loadout);

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

                // ★ 패시브인데 PassiveStatType이 None이면 제외
                if (skill.SkillType == SkillType.Passive && skill.PassiveStatType == PassiveStatType.None)
                    continue;

                // ★ 액티브인데 실제 런타임 매핑이 안 되면 제외
                if (skill.SkillType == SkillType.Active && commonSkillCatalog != null)
                {
                    if (!commonSkillCatalog.TryResolve(skill, out CommonSkillConfigSO config))
                        continue;

                    if (config == null || config.weaponPrefab == null)
                        continue;
                }

                if (!loadout.CanAppearAsCard(skill))
                    continue;

                result.Add(skill);
            }

            return result;
        }

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