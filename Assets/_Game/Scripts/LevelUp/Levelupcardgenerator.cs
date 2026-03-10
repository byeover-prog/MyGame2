// ──────────────────────────────────────────────
// LevelUpCardGenerator.cs
// 레벨업 시 카드 4장 데이터를 생성하는 생성기
//
// 규칙:
//   1) 액티브 후보 2장 + 패시브 후보 2장 = 4장
//   2) 한쪽이 부족하면 반대쪽으로 보충
//   3) 둘 다 부족하면 대체 카드(회복/재화/무적/경험치)로 보충
//   4) 전부 소진이면 대체 카드 4장
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using _Game.Player;
using _Game.Skills;

namespace _Game.LevelUp
{
    public sealed class LevelUpCardGenerator : MonoBehaviour
    {
        // ── 상수 ───────────────────────────────────

        private const int CARD_COUNT           = 4;
        private const int DEFAULT_ACTIVE_COUNT = 2;
        private const int DEFAULT_PASSIVE_COUNT = 2;

        // ── 참조 ───────────────────────────────────

        [Header("=== 카탈로그 ===")]

        [SerializeField, Tooltip("전체 공통 스킬 / 패시브 목록")]
        private SkillCatalogSO skillCatalog;

        // ── 대체 카드 수치 (인스펙터에서 조절 가능) ─

        [Header("=== 대체 카드 수치 ===")]

        [SerializeField, Tooltip("체력 회복량")]
        private int fallbackHealAmount = 30;

        [SerializeField, Tooltip("재화 획득량")]
        private int fallbackGoldAmount = 200;

        [SerializeField, Tooltip("무적 지속 시간(초)")]
        private float fallbackInvincibleDuration = 5f;

        [SerializeField, Tooltip("즉시 획득 경험치")]
        private int fallbackBonusExpAmount = 50;

        // ════════════════════════════════════════════
        //  외부 API
        // ════════════════════════════════════════════

        /// <summary>
        /// 레벨업 카드 4장을 생성하여 반환한다.
        /// </summary>
        public List<LevelUpCardData> Generate(PlayerSkillLoadout loadout)
        {
            List<LevelUpCardData> result = new List<LevelUpCardData>(CARD_COUNT);

            // 카탈로그 또는 로드아웃이 없으면 대체 카드로 채움
            if (skillCatalog == null || loadout == null)
            {
                FillWithFallbackCards(result);
                return result;
            }

            // 후보 목록 빌드
            List<SkillDefinitionSO> activeCandidates  = BuildCandidates(skillCatalog.GetByType(SkillType.Active), loadout);
            List<SkillDefinitionSO> passiveCandidates = BuildCandidates(skillCatalog.GetByType(SkillType.Passive), loadout);

            // 둘 다 0이면 전부 대체 카드
            if (activeCandidates.Count == 0 && passiveCandidates.Count == 0)
            {
                FillWithFallbackCards(result);
                return result;
            }

            HashSet<string> usedSkillIds = new HashSet<string>();

            // 1차: 액티브 2장 시도
            AddSkillCards(result, activeCandidates, usedSkillIds, loadout, DEFAULT_ACTIVE_COUNT);

            // 2차: 패시브 2장 시도
            AddSkillCards(result, passiveCandidates, usedSkillIds, loadout, DEFAULT_PASSIVE_COUNT);

            // 3차: 부족하면 액티브로 보충
            if (result.Count < CARD_COUNT)
                AddSkillCards(result, activeCandidates, usedSkillIds, loadout, CARD_COUNT - result.Count);

            // 4차: 그래도 부족하면 패시브로 보충
            if (result.Count < CARD_COUNT)
                AddSkillCards(result, passiveCandidates, usedSkillIds, loadout, CARD_COUNT - result.Count);

            // 5차: 최종 부족하면 대체 카드로 보충
            if (result.Count < CARD_COUNT)
                FillRemainingWithFallbackCards(result);

            return result;
        }

        // ════════════════════════════════════════════
        //  내부 로직
        // ════════════════════════════════════════════

        /// <summary>
        /// 카드 후보로 쓸 수 있는 스킬만 필터링한다.
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

                if (skill == null)
                    continue;

                if (!loadout.CanAppearAsCard(skill))
                    continue;

                result.Add(skill);
            }

            return result;
        }

        /// <summary>
        /// 후보 리스트에서 셔플 후 중복 없이 카드를 추가한다.
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

            // 원본 보존을 위해 복사 후 셔플
            List<SkillDefinitionSO> shuffled = new List<SkillDefinitionSO>(candidates);
            Shuffle(shuffled);

            for (int i = 0; i < shuffled.Count; i++)
            {
                if (result.Count >= CARD_COUNT)
                    return;

                SkillDefinitionSO skill = shuffled[i];

                if (skill == null)
                    continue;

                // 이미 이번 카드 세트에 포함됐으면 스킵
                if (!usedSkillIds.Add(skill.SkillId))
                    continue;

                string description = loadout.BuildCardDescription(skill);
                result.Add(LevelUpCardData.CreateSkillCard(skill, description));

                count--;
                if (count <= 0)
                    return;
            }
        }

        /// <summary>
        /// 후보가 전혀 없을 때 대체 카드 4장으로 채운다.
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
        /// 스킬 카드로 4장을 못 채웠을 때 나머지를 대체 카드로 보충한다.
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
                if (result.Count >= CARD_COUNT)
                    return;

                result.Add(fallbackPool[i]);
            }
        }

        /// <summary>
        /// Fisher-Yates 셔플
        /// </summary>
        private void Shuffle<T>(List<T> list)
        {
            if (list == null || list.Count <= 1)
                return;

            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }
    }
}