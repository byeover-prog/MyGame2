using UnityEngine;
using _Game.Player;
using _Game.Skills;

namespace _Game.LevelUp
{
    // 레벨업 카드 선택 결과를 실제 런타임에 반영한다.

    public sealed class LevelUpRewardApplier : MonoBehaviour
    {
        [Header("=== 플레이어 참조 ===")]
        [SerializeField, Tooltip("기본 플레이어 스킬 로드아웃입니다.")]
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
        [SerializeField, Tooltip("공통 스킬 매니저입니다.")]
        private CommonSkillManager2D commonSkillManager;

        [SerializeField, Tooltip("SkillDefinitionSO → CommonSkillConfigSO 변환 카탈로그입니다.")]
        private CommonSkillCatalogSO commonSkillCatalog;

        [Header("=== ★ 전용 스킬 런타임 연결 ===")]
        [SerializeField, Tooltip("전용 스킬 프리팹을 장착/레벨업할 SkillRunner입니다.")]
        private SkillRunner skillRunner;

        // 세션 중 강제로 사용할 loadout.
        private PlayerSkillLoadout runtimeLoadout;

        private void Awake()
        {
            if (loadout == null) loadout = FindFirstObjectByType<PlayerSkillLoadout>();
            if (statRuntimeApplier == null) statRuntimeApplier = FindFirstObjectByType<PlayerStatRuntimeApplier2D>();
            if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerExp == null) playerExp = FindFirstObjectByType<PlayerExp>();
            if (playerCurrency == null) playerCurrency = FindFirstObjectByType<PlayerCurrency2D>();
            if (commonSkillManager == null) commonSkillManager = FindFirstObjectByType<CommonSkillManager2D>();
            if (skillRunner == null) skillRunner = FindFirstObjectByType<SkillRunner>();
            if (commonSkillCatalog == null && commonSkillManager != null)
                commonSkillCatalog = commonSkillManager.Catalog;
        }

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

        // 카드 보상을 적용한다.
        public bool Apply(LevelUpCardData cardData)
        {
            if (cardData == null) return false;

            switch (cardData.RewardType)
            {
                case LevelUpRewardType.Skill:
                    return ApplySkillCard(cardData);

                case LevelUpRewardType.CharacterSkill:
                    return ApplyCharacterSkillCard(cardData);

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
        
        //  공통 스킬 적용 (기존)

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

            if (alreadyOwned)
            {
                changed = targetLoadout.TryUpgradeSkill(skillId);
                if (!changed) return false;
            }
            else
            {
                changed = targetLoadout.TryAddSkill(definition);
                if (!changed) return false;
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
        
        // 전용 스킬 적용 (신규)

        private bool ApplyCharacterSkillCard(LevelUpCardData cardData)
        {
            PlayerSkillLoadout targetLoadout = runtimeLoadout != null ? runtimeLoadout : loadout;

            if (targetLoadout == null || cardData.SkillDefinition == null)
            {
                GameLogger.LogWarning("[RewardApplier] loadout 또는 SkillDefinition 누락 (전용)", this);
                return false;
            }

            if (skillRunner == null)
            {
                GameLogger.LogWarning("[RewardApplier] skillRunner 미할당 — 전용 스킬 적용 불가", this);
                return false;
            }

            SkillDefinitionSO definition = cardData.SkillDefinition;
            string skillId = definition.SkillId;
            GameObject prefab = cardData.CharacterSkillPrefab;

            // 1) PlayerSkillLoadout에 기록 (슬롯 추적)
            bool alreadyOwned = targetLoadout.HasSkill(skillId);
            bool changed;

            if (alreadyOwned)
            {
                changed = targetLoadout.TryUpgradeSkill(skillId);
                if (!changed)
                {
                    GameLogger.Log($"[RewardApplier] 전용 스킬 강화 실패(최대레벨): {cardData.Title}", this);
                    return false;
                }
            }
            else
            {
                changed = targetLoadout.TryAddSkill(definition);
                if (!changed)
                {
                    GameLogger.Log($"[RewardApplier] 전용 스킬 획득 실패(슬롯 가득): {cardData.Title}", this);
                    return false;
                }
            }

            // 2) loadout에서 현재 레벨 가져오기
            var state = targetLoadout.GetSkill(skillId);
            int newLevel = state != null ? state.Level : 1;

            // 3) SkillRunner로 장착/레벨업
            if (!alreadyOwned && prefab != null)
            {
                skillRunner.AttachSkillPrefab(skillId, prefab);
                GameLogger.Log($"[RewardApplier] 전용 스킬 장착: {cardData.Title} (Lv.{newLevel})", this);
            }

            skillRunner.ApplyLevel(skillId, newLevel);
            GameLogger.Log($"[RewardApplier] 전용 스킬 반영: {cardData.Title} Lv.{newLevel} ({(alreadyOwned ? "강화" : "획득")})", this);

            return true;
        }
        
        //  유틸
        
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