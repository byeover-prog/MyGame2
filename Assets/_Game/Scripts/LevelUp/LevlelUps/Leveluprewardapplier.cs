// ──────────────────────────────────────────────
// LevelUpRewardApplier.cs
// 레벨업 카드 선택 결과를 실제 게임 데이터에 반영
//
// 현재 구현 범위:
//   - 스킬 카드: TryAddSkill / TryUpgradeSkill 호출
//   - 대체 카드 (Heal/Gold/Invincible/BonusExp): 로그 출력만
//     → 실제 적용은 다음 단계에서 구현
//
// 사용법:
//   Systems > LevelUpSystem 오브젝트에 AddComponent
//   Inspector에서 PlayerSkillLoadout 할당
// ──────────────────────────────────────────────

using UnityEngine;
using _Game.Player;
using _Game.Skills;

namespace _Game.LevelUp
{
    public sealed class LevelUpRewardApplier : MonoBehaviour
    {
        // ── 참조 ───────────────────────────────────

        [Header("=== 플레이어 참조 ===")]

        [SerializeField, Tooltip("플레이어 스킬 로드아웃")]
        private PlayerSkillLoadout loadout;

        // TODO: 실제 적용 시 아래 참조 활성화
        // [SerializeField, Tooltip("플레이어 체력 컴포넌트")]
        // private PlayerHealth playerHealth;
        //
        // [SerializeField, Tooltip("플레이어 재화 컴포넌트")]
        // private MonoBehaviour playerCurrency;
        //
        // [SerializeField, Tooltip("플레이어 경험치 컴포넌트")]
        // private PlayerExp playerExperience;

        // ════════════════════════════════════════════
        //  외부 API
        // ════════════════════════════════════════════

        /// <summary>
        /// 카드 데이터를 받아 실제 보상을 적용한다.
        /// 성공하면 true, 실패하면 false.
        /// </summary>
        public bool Apply(LevelUpCardData cardData)
        {
            if (cardData == null)
                return false;

            switch (cardData.RewardType)
            {
                case LevelUpRewardType.Skill:
                    return ApplySkillCard(cardData);

                case LevelUpRewardType.Heal:
                    // TODO: playerHealth.Heal(cardData.HealAmount);
                    Debug.Log($"[RewardApplier] 체력 회복: +{cardData.HealAmount}", this);
                    return true;

                case LevelUpRewardType.Gold:
                    // TODO: playerCurrency.AddGold(cardData.GoldAmount);
                    Debug.Log($"[RewardApplier] 재화 획득: +{cardData.GoldAmount}", this);
                    return true;

                case LevelUpRewardType.Invincible:
                    // TODO: invincibleSystem.Activate(cardData.InvincibleDuration);
                    Debug.Log($"[RewardApplier] 무적: {cardData.InvincibleDuration}초", this);
                    return true;

                case LevelUpRewardType.BonusExp:
                    // TODO: playerExperience.AddExp(cardData.BonusExpAmount);
                    Debug.Log($"[RewardApplier] 경험치: +{cardData.BonusExpAmount}", this);
                    return true;

                default:
                    Debug.LogWarning($"[RewardApplier] 알 수 없는 보상 타입: {cardData.RewardType}", this);
                    return false;
            }
        }

        // ════════════════════════════════════════════
        //  내부 로직
        // ════════════════════════════════════════════

        /// <summary>
        /// 스킬 카드 보상을 적용한다.
        /// 미보유면 신규 획득, 보유 중이면 레벨업.
        /// </summary>
        private bool ApplySkillCard(LevelUpCardData cardData)
        {
            if (loadout == null)
            {
                Debug.LogWarning("[RewardApplier] loadout 미할당", this);
                return false;
            }

            if (cardData.SkillDefinition == null)
            {
                Debug.LogWarning("[RewardApplier] SkillDefinition이 null", this);
                return false;
            }

            string skillId = cardData.SkillDefinition.SkillId;

            // 이미 보유 중이면 레벨업
            if (loadout.HasSkill(skillId))
            {
                bool upgraded = loadout.TryUpgradeSkill(skillId);
                Debug.Log($"[RewardApplier] 스킬 강화: {cardData.Title} → {(upgraded ? "성공" : "실패(최대레벨)")}", this);
                return upgraded;
            }

            // 미보유면 신규 획득
            bool added = loadout.TryAddSkill(cardData.SkillDefinition);
            Debug.Log($"[RewardApplier] 스킬 획득: {cardData.Title} → {(added ? "성공" : "실패(슬롯 가득)")}", this);
            return added;
        }
    }
}