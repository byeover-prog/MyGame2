// ──────────────────────────────────────────────
// LevelUpRewardApplier.cs
// 레벨업 카드 선택 결과를 실제 게임 데이터에 반영
//
// 구현 범위:
//   - 액티브 카드: PlayerSkillLoadout + CommonSkillManager2D 동시 반영
//   - 패시브 카드: PlayerSkillLoadout + PlayerStatRuntimeApplier2D 즉시 재계산
//   - 대체 카드: Heal / Gold / Invincible / BonusExp 실제 적용
// ──────────────────────────────────────────────

using UnityEngine;
using _Game.Player;
using _Game.Skills;

namespace _Game.LevelUp
{
    public sealed class LevelUpRewardApplier : MonoBehaviour
    {
        [Header("=== 플레이어 참조 ===")]

        [SerializeField, Tooltip("플레이어 스킬 로드아웃")]
        private PlayerSkillLoadout loadout;

        [SerializeField, Tooltip("패시브 적용 후 실제 스탯을 다시 계산할 컴포넌트")]
        private PlayerStatRuntimeApplier2D statRuntimeApplier;

        [SerializeField, Tooltip("체력 회복/무적 적용 대상")]
        private PlayerHealth playerHealth;

        [SerializeField, Tooltip("즉시 경험치 적용 대상")]
        private PlayerExp playerExp;

        [SerializeField, Tooltip("재화 적용 대상")]
        private PlayerCurrency2D playerCurrency;

        [Header("=== 액티브 스킬 런타임 연결 ===")]

        [SerializeField, Tooltip("실제 무기 프리팹을 스폰/업그레이드할 공통 스킬 매니저")]
        private CommonSkillManager2D commonSkillManager;

        [SerializeField, Tooltip("SkillDefinitionSO를 CommonSkillConfigSO로 변환할 카탈로그")]
        private CommonSkillCatalogSO commonSkillCatalog;

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

        public bool Apply(LevelUpCardData cardData)
        {
            if (cardData == null) return false;

            switch (cardData.RewardType)
            {
                case LevelUpRewardType.Skill:
                    return ApplySkillCard(cardData);

                case LevelUpRewardType.Heal:
                    if (playerHealth == null) { Debug.LogWarning("[RewardApplier] playerHealth 미할당", this); return false; }
                    playerHealth.Heal(cardData.HealAmount);
                    Debug.Log($"[RewardApplier] 체력 회복: +{cardData.HealAmount}", this);
                    return true;

                case LevelUpRewardType.Gold:
                    if (playerCurrency == null) { Debug.LogWarning("[RewardApplier] playerCurrency 미할당", this); return false; }
                    playerCurrency.AddGold(cardData.GoldAmount);
                    Debug.Log($"[RewardApplier] 재화 획득: +{cardData.GoldAmount}", this);
                    return true;

                case LevelUpRewardType.Invincible:
                    if (playerHealth == null) { Debug.LogWarning("[RewardApplier] playerHealth 미할당", this); return false; }
                    playerHealth.ActivateTemporaryInvincibility(cardData.InvincibleDuration);
                    Debug.Log($"[RewardApplier] 무적: {cardData.InvincibleDuration:0.#}초", this);
                    return true;

                case LevelUpRewardType.BonusExp:
                    if (playerExp == null) { Debug.LogWarning("[RewardApplier] playerExp 미할당", this); return false; }
                    playerExp.AddExp(cardData.BonusExpAmount);
                    Debug.Log($"[RewardApplier] 즉시 경험치: +{cardData.BonusExpAmount}", this);
                    return true;

                default:
                    Debug.LogWarning($"[RewardApplier] 알 수 없는 보상: {cardData.RewardType}", this);
                    return false;
            }
        }

        private bool ApplySkillCard(LevelUpCardData cardData)
        {
            if (loadout == null || cardData.SkillDefinition == null)
            {
                Debug.LogWarning("[RewardApplier] loadout 또는 SkillDefinition 누락", this);
                return false;
            }

            SkillDefinitionSO definition = cardData.SkillDefinition;
            string skillId = definition.SkillId;

            // ★ 액티브: 런타임 매핑 확인 먼저
            CommonSkillConfigSO runtimeSkillConfig = null;

            if (definition.SkillType == SkillType.Active)
            {
                if (commonSkillManager == null)
                {
                    Debug.LogWarning("[RewardApplier] commonSkillManager 미할당", this);
                    return false;
                }

                if (!TryResolveCommonSkill(definition, out runtimeSkillConfig))
                {
                    Debug.LogWarning($"[RewardApplier] 액티브 매핑 실패: {skillId} ({definition.DisplayName})", this);
                    return false;
                }
            }
            else if (definition.SkillType == SkillType.Passive)
            {
                if (statRuntimeApplier == null)
                {
                    Debug.LogWarning("[RewardApplier] statRuntimeApplier 미할당", this);
                    return false;
                }
            }

            // 로드아웃 반영
            bool alreadyOwned = loadout.HasSkill(skillId);
            bool changed;

            if (alreadyOwned)
            {
                changed = loadout.TryUpgradeSkill(skillId);
                if (!changed)
                {
                    Debug.Log($"[RewardApplier] 강화 실패(최대레벨): {cardData.Title}", this);
                    return false;
                }
            }
            else
            {
                changed = loadout.TryAddSkill(definition);
                if (!changed)
                {
                    Debug.Log($"[RewardApplier] 획득 실패(슬롯 가득): {cardData.Title}", this);
                    return false;
                }
            }

            // ★ 실제 런타임 반영
            if (definition.SkillType == SkillType.Active)
            {
                commonSkillManager.Upgrade(runtimeSkillConfig);
                Debug.Log($"[RewardApplier] 액티브 반영: {cardData.Title} ({(alreadyOwned ? "강화" : "획득")})", this);
            }
            else if (definition.SkillType == SkillType.Passive)
            {
                statRuntimeApplier.ReapplyFromLoadout();
                Debug.Log($"[RewardApplier] 패시브 반영: {cardData.Title} ({(alreadyOwned ? "강화" : "획득")})", this);
            }

            return true;
        }

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
                Debug.LogWarning($"[RewardApplier] weaponPrefab 비어있음: {config.name}", this);
                return false;
            }

            return true;
        }
    }
}