// ──────────────────────────────────────────────
// LevelUpRewardApplier.cs
// 레벨업 카드 선택 결과를 실제 게임 데이터에 반영
//
// 구현 원리 요약:
// 보상 적용은 "현재 레벨업 세션에서 전달받은 loadout"을 우선 사용한다.
// 인스펙터에 연결된 loadout이 있더라도, 패널에서 SetRuntimeLoadout()으로 넘긴 값을 최우선으로 본다.
// 이렇게 해서 생성 / 리롤 / 적용이 모두 같은 PlayerSkillLoadout 인스턴스를 사용하게 만든다.
// ──────────────────────────────────────────────

using UnityEngine;
using _Game.Player;
using _Game.Skills;

namespace _Game.LevelUp
{
    /// <summary>
    /// 레벨업 카드 선택 결과를 실제 런타임에 반영한다.
    /// </summary>
    public sealed class LevelUpRewardApplier : MonoBehaviour
    {
        [Header("=== 플레이어 참조 ===")]
        [SerializeField, Tooltip("기본 플레이어 스킬 로드아웃입니다. 실제 동작은 세션에서 전달된 loadout이 우선됩니다.")]
        private PlayerSkillLoadout loadout;

        [SerializeField, Tooltip("패시브 적용 후 실제 스탯을 다시 계산할 컴포넌트입니다.")]
        private PlayerStatRuntimeApplier2D statRuntimeApplier;

        [SerializeField, Tooltip("체력 회복/무적 적용 대상입니다.")]
        private PlayerHealth playerHealth;

        [SerializeField, Tooltip("즉시 경험치 적용 대상입니다.")]
        private PlayerExp playerExp;

        [SerializeField, Tooltip("재화 적용 대상입니다.")]
        private PlayerCurrency2D playerCurrency;

        [Header("=== 액티브 스킬 런타임 연결 ===")]
        [SerializeField, Tooltip("실제 무기 프리팹을 스폰/업그레이드할 공통 스킬 매니저입니다.")]
        private CommonSkillManager2D commonSkillManager;

        [SerializeField, Tooltip("SkillDefinitionSO를 CommonSkillConfigSO로 변환할 카탈로그입니다.")]
        private CommonSkillCatalogSO commonSkillCatalog;

        /// <summary>
        /// 현재 레벨업 세션에서 강제로 사용할 loadout.
        /// 패널이 열릴 때 주입된다.
        /// </summary>
        private PlayerSkillLoadout runtimeLoadout;

        private void Awake()
        {
            if (loadout == null) loadout = FindFirstObjectByType<PlayerSkillLoadout>();
            if (statRuntimeApplier == null) statRuntimeApplier = FindFirstObjectByType<PlayerStatRuntimeApplier2D>();
            if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerExp == null) playerExp = FindFirstObjectByType<PlayerExp>();
            if (playerCurrency == null) playerCurrency = FindFirstObjectByType<PlayerCurrency2D>();
            if (commonSkillManager == null) commonSkillManager = FindFirstObjectByType<CommonSkillManager2D>();
            if (commonSkillCatalog == null && commonSkillManager != null)
                commonSkillCatalog = commonSkillManager.Catalog;
        }

        /// <summary>
        /// 현재 레벨업 세션에서 사용할 loadout을 주입한다.
        /// </summary>
        public void SetRuntimeLoadout(PlayerSkillLoadout value)
        {
            runtimeLoadout = value;

            if (runtimeLoadout != null)
            {
                GameLogger.Log(
                    $"[RewardApplier] runtimeLoadout 설정 | instanceId={runtimeLoadout.GetInstanceID()} | name={runtimeLoadout.name}",
                    runtimeLoadout);
            }
        }

        /// <summary>
        /// 카드 보상을 적용한다.
        /// </summary>
        public bool Apply(LevelUpCardData cardData)
        {
            if (cardData == null) return false;

            switch (cardData.RewardType)
            {
                case LevelUpRewardType.Skill:
                    return ApplySkillCard(cardData);

                case LevelUpRewardType.Heal:
                    if (playerHealth == null) { GameLogger.LogWarning("[RewardApplier] playerHealth 미할당", this); return false; }
                    playerHealth.Heal(cardData.HealAmount);
                    GameLogger.Log($"[RewardApplier] 체력 회복: +{cardData.HealAmount}", this);
                    return true;

                case LevelUpRewardType.Gold:
                    if (playerCurrency == null) { GameLogger.LogWarning("[RewardApplier] playerCurrency 미할당", this); return false; }
                    playerCurrency.AddGold(cardData.GoldAmount);
                    GameLogger.Log($"[RewardApplier] 재화 획득: +{cardData.GoldAmount}", this);
                    return true;

                case LevelUpRewardType.Invincible:
                    if (playerHealth == null) { GameLogger.LogWarning("[RewardApplier] playerHealth 미할당", this); return false; }
                    playerHealth.ActivateTemporaryInvincibility(cardData.InvincibleDuration);
                    GameLogger.Log($"[RewardApplier] 무적: {cardData.InvincibleDuration:0.#}초", this);
                    return true;

                case LevelUpRewardType.BonusExp:
                    if (playerExp == null) { GameLogger.LogWarning("[RewardApplier] playerExp 미할당", this); return false; }
                    playerExp.AddExp(cardData.BonusExpAmount);
                    GameLogger.Log($"[RewardApplier] 즉시 경험치: +{cardData.BonusExpAmount}", this);
                    return true;

                default:
                    GameLogger.LogWarning($"[RewardApplier] 알 수 없는 보상: {cardData.RewardType}", this);
                    return false;
            }
        }

        /// <summary>
        /// 스킬 카드 보상을 적용한다.
        /// </summary>
        private bool ApplySkillCard(LevelUpCardData cardData)
        {
            PlayerSkillLoadout targetLoadout = runtimeLoadout != null ? runtimeLoadout : loadout;

            if (targetLoadout == null || cardData.SkillDefinition == null)
            {
                GameLogger.LogWarning("[RewardApplier] loadout 또는 SkillDefinition 누락", this);
                return false;
            }

            SkillDefinitionSO definition = cardData.SkillDefinition;
            string skillId = definition.SkillId;

            CommonSkillConfigSO runtimeSkillConfig = null;

            if (definition.SkillType == SkillType.Active)
            {
                if (commonSkillManager == null)
                {
                    GameLogger.LogWarning("[RewardApplier] commonSkillManager 미할당", this);
                    return false;
                }

                if (!TryResolveCommonSkill(definition, out runtimeSkillConfig))
                {
                    GameLogger.LogWarning($"[RewardApplier] 액티브 매핑 실패: {skillId} ({definition.DisplayName})", this);
                    return false;
                }
            }
            else if (definition.SkillType == SkillType.Passive)
            {
                if (statRuntimeApplier == null)
                {
                    GameLogger.LogWarning("[RewardApplier] statRuntimeApplier 미할당", this);
                    return false;
                }
            }

            bool alreadyOwned = targetLoadout.HasSkill(skillId);
            bool changed;

            GameLogger.Log(
                $"[RewardApplier] 적용 시작 | title={cardData.Title} | skillId={skillId} | alreadyOwned={alreadyOwned} | loadoutInstanceId={targetLoadout.GetInstanceID()}",
                targetLoadout);

            if (alreadyOwned)
            {
                changed = targetLoadout.TryUpgradeSkill(skillId);
                if (!changed)
                {
                    GameLogger.Log($"[RewardApplier] 강화 실패(최대레벨): {cardData.Title}", this);
                    return false;
                }
            }
            else
            {
                changed = targetLoadout.TryAddSkill(definition);
                if (!changed)
                {
                    GameLogger.Log($"[RewardApplier] 획득 실패(슬롯 가득): {cardData.Title}", this);
                    return false;
                }
            }

            if (definition.SkillType == SkillType.Active)
            {
                commonSkillManager.Upgrade(runtimeSkillConfig);
                GameLogger.Log($"[RewardApplier] 액티브 반영: {cardData.Title} ({(alreadyOwned ? "강화" : "획득")})", this);
            }
            else if (definition.SkillType == SkillType.Passive)
            {
                statRuntimeApplier.ReapplyFromLoadout();
                GameLogger.Log($"[RewardApplier] 패시브 반영: {cardData.Title} ({(alreadyOwned ? "강화" : "획득")})", this);
            }

            return true;
        }

        /// <summary>
        /// 액티브 스킬 정의를 실제 런타임 CommonSkillConfigSO로 변환한다.
        /// </summary>
        private bool TryResolveCommonSkill(SkillDefinitionSO definition, out CommonSkillConfigSO config)
        {
            config = null;
            if (definition == null) return false;

            if (commonSkillCatalog == null && commonSkillManager != null)
                commonSkillCatalog = commonSkillManager.Catalog;

            if (commonSkillCatalog == null) return false;
            if (!commonSkillCatalog.TryResolve(definition, out config)) return false;
            if (config == null) return false;

            if (config.weaponPrefab == null)
            {
                GameLogger.LogWarning($"[RewardApplier] weaponPrefab 비어있음: {config.name}", this);
                return false;
            }

            return true;
        }
    }
}