using _Game.Skills;
using UnityEngine;

namespace _Game.LevelUp
{
    [System.Serializable]
    public sealed class LevelUpCardData
    {
        public LevelUpRewardType RewardType;
        public string Title;
        public string Description;
        public Sprite Icon;
        public string Tag;

        public SkillDefinitionSO SkillDefinition;
        public CharacterSkillDefinitionSO CharacterSkillDefinition;
        public GameObject CharacterSkillPrefab;
        public bool IsExclusive;

        public int HealAmount;
        public int GoldAmount;
        public float InvincibleDuration;
        public int BonusExpAmount;

        public int CurrentLevel;
        public int NextLevel;
        public string AddInfo;

        public static LevelUpCardData CreateSkillCard(SkillDefinitionSO definition, string description)
        {
            string tag = string.Empty;
            if (definition != null)
            {
                tag = !string.IsNullOrWhiteSpace(definition.TagKr)
                    ? definition.TagKr
                    : (definition.SkillType == SkillType.Active ? "공통 스킬" : "패시브");
            }

            return new LevelUpCardData
            {
                RewardType = LevelUpRewardType.Skill,
                SkillDefinition = definition,
                Title = definition != null ? definition.DisplayName : "스킬",
                Description = description,
                Icon = definition != null ? definition.Icon : null,
                Tag = tag,
                IsExclusive = false
            };
        }

        public static LevelUpCardData CreateCharacterSkillCard(
            SkillDefinitionSO definition,
            string description,
            GameObject prefab)
        {
            string tag = string.Empty;
            if (definition != null)
            {
                tag = !string.IsNullOrWhiteSpace(definition.TagKr)
                    ? $"전용 · {definition.TagKr}"
                    : "전용";
            }

            return new LevelUpCardData
            {
                RewardType = LevelUpRewardType.CharacterSkill,
                SkillDefinition = definition,
                CharacterSkillPrefab = prefab,
                Title = definition != null ? definition.DisplayName : "전용 스킬",
                Description = description,
                Icon = definition != null ? definition.Icon : null,
                Tag = tag,
                IsExclusive = true
            };
        }

        public static LevelUpCardData CreateCharacterSkillCard(
            CharacterSkillDefinitionSO definition,
            string description)
        {
            return new LevelUpCardData
            {
                RewardType = LevelUpRewardType.CharacterSkill,
                CharacterSkillDefinition = definition,
                CharacterSkillPrefab = definition != null ? definition.WeaponPrefab : null,
                Title = definition != null ? definition.DisplayName : "전용 스킬",
                Description = description,
                Icon = definition != null ? definition.Icon : null,
                Tag = GetExclusiveOwnerTag(definition),
                IsExclusive = true
            };
        }

        public static LevelUpCardData CreateHealCard(int healAmount)
        {
            return new LevelUpCardData
            {
                RewardType = LevelUpRewardType.Heal,
                Title = "체력 회복",
                Description = $"체력을 {healAmount} 회복합니다.",
                Tag = "보너스",
                HealAmount = healAmount
            };
        }

        public static LevelUpCardData CreateGoldCard(int goldAmount)
        {
            return new LevelUpCardData
            {
                RewardType = LevelUpRewardType.Gold,
                Title = "재화 획득",
                Description = $"재화를 {goldAmount} 획득합니다.",
                Tag = "보너스",
                GoldAmount = goldAmount
            };
        }

        public static LevelUpCardData CreateInvincibleCard(float duration)
        {
            return new LevelUpCardData
            {
                RewardType = LevelUpRewardType.Invincible,
                Title = "일시 무적",
                Description = $"{duration:0.#}초 동안 무적 상태가 됩니다.",
                Tag = "보너스",
                InvincibleDuration = duration
            };
        }

        public static LevelUpCardData CreateBonusExpCard(int expAmount)
        {
            return new LevelUpCardData
            {
                RewardType = LevelUpRewardType.BonusExp,
                Title = "즉시 경험치",
                Description = $"경험치를 {expAmount} 즉시 획득합니다.",
                Tag = "보너스",
                BonusExpAmount = expAmount
            };
        }

        private static string GetExclusiveOwnerTag(CharacterSkillDefinitionSO definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.OwnerCharacterId))
                return "전용";

            switch (definition.OwnerCharacterId.Trim().ToLowerInvariant())
            {
                case "hayul":
                    return "하율";
                case "yoonseol":
                    return "윤설";
                case "harin":
                    return "하린";
                default:
                    return definition.OwnerCharacterId;
            }
        }
    }
}
