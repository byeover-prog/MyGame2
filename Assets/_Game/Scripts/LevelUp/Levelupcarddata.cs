// ──────────────────────────────────────────────
// LevelUpCardData.cs
// 레벨업 카드 1장의 데이터 컨테이너
// UI는 이 데이터를 받아서 카드에 표시한다.
//
// ★ v2: CharacterSkill 타입 추가
//   - CharacterSkillPrefab: SkillRunner에 장착할 프리팹
//   - IsExclusive: 전용 스킬 여부 (UI 시각 구분용)
// ──────────────────────────────────────────────

using _Game.Skills;
using UnityEngine;

namespace _Game.LevelUp
{
    /// <summary>
    /// 레벨업 카드 1장이 담고 있는 정보.
    /// 팩토리 메서드(CreateXxxCard)로 생성한다.
    /// </summary>
    [System.Serializable]
    public sealed class LevelUpCardData
    {
        // ── 공통 필드 ──────────────────────────────

        /// <summary>보상 종류</summary>
        public LevelUpRewardType RewardType;

        /// <summary>카드 제목 (UI 표시용)</summary>
        public string Title;

        /// <summary>카드 설명 (UI 표시용)</summary>
        public string Description;

        /// <summary>카드 아이콘 (UI 표시용)</summary>
        public Sprite Icon;

        /// <summary>카드 태그 (UI 표시용, 예: 원거리, 근접, 공격)</summary>
        public string Tag;

        // ── 스킬 카드 전용 ─────────────────────────

        /// <summary>스킬/패시브 정의 SO (RewardType == Skill 또는 CharacterSkill 일 때 사용)</summary>
        public SkillDefinitionSO SkillDefinition;

        // ── ★ 전용 스킬 카드 전용 ──────────────────

        /// <summary>전용 스킬 프리팹 (RewardType == CharacterSkill일 때 SkillRunner에 장착)</summary>
        public GameObject CharacterSkillPrefab;

        /// <summary>전용 스킬 여부 (UI에서 테두리/태그 시각 구분용)</summary>
        public bool IsExclusive;

        // ── 대체 카드 전용 ─────────────────────────

        /// <summary>회복량 (RewardType == Heal)</summary>
        public int HealAmount;

        /// <summary>재화량 (RewardType == Gold)</summary>
        public int GoldAmount;

        /// <summary>무적 지속 시간 (RewardType == Invincible)</summary>
        public float InvincibleDuration;

        /// <summary>즉시 경험치량 (RewardType == BonusExp)</summary>
        public int BonusExpAmount;
        
        /// <summary>현재 레벨 (0 = 미보유)</summary>
        public int CurrentLevel;

        /// <summary>다음 레벨</summary>
        public int NextLevel;

        /// <summary>추가 스탯 텍스트 (예: "피해량 +99%")</summary>
        public string AddInfo;

        // ════════════════════════════════════════════
        //  팩토리 메서드
        // ════════════════════════════════════════════

        /// <summary>스킬/패시브 카드 생성</summary>
        public static LevelUpCardData CreateSkillCard(SkillDefinitionSO definition, string description)
        {
            string tag = "";
            if (definition != null)
            {
                tag = !string.IsNullOrWhiteSpace(definition.TagKr)
                    ? definition.TagKr
                    : (definition.SkillType == _Game.Skills.SkillType.Active ? "공통 스킬" : "패시브");
            }

            return new LevelUpCardData
            {
                RewardType      = LevelUpRewardType.Skill,
                SkillDefinition = definition,
                Title           = definition != null ? definition.DisplayName : "스킬",
                Description     = description,
                Icon            = definition != null ? definition.Icon : null,
                Tag             = tag,
                IsExclusive     = false
            };
        }

        /// <summary>★ 캐릭터 전용 스킬 카드 생성</summary>
        public static LevelUpCardData CreateCharacterSkillCard(
            SkillDefinitionSO definition,
            string description,
            GameObject prefab)
        {
            string tag = "";
            if (definition != null)
            {
                tag = !string.IsNullOrWhiteSpace(definition.TagKr)
                    ? $"전용 · {definition.TagKr}"
                    : "전용";
            }

            return new LevelUpCardData
            {
                RewardType            = LevelUpRewardType.CharacterSkill,
                SkillDefinition       = definition,
                CharacterSkillPrefab  = prefab,
                Title                 = definition != null ? definition.DisplayName : "전용 스킬",
                Description           = description,
                Icon                  = definition != null ? definition.Icon : null,
                Tag                   = tag,
                IsExclusive           = true
            };
        }

        /// <summary>체력 회복 카드 생성</summary>
        public static LevelUpCardData CreateHealCard(int healAmount)
        {
            return new LevelUpCardData
            {
                RewardType  = LevelUpRewardType.Heal,
                Title       = "체력 회복",
                Description = $"체력을 {healAmount} 회복합니다.",
                Tag         = "보너스",
                HealAmount  = healAmount
            };
        }

        /// <summary>재화 획득 카드 생성</summary>
        public static LevelUpCardData CreateGoldCard(int goldAmount)
        {
            return new LevelUpCardData
            {
                RewardType  = LevelUpRewardType.Gold,
                Title       = "재화 획득",
                Description = $"재화를 {goldAmount} 획득합니다.",
                Tag         = "보너스",
                GoldAmount  = goldAmount
            };
        }

        /// <summary>일시 무적 카드 생성</summary>
        public static LevelUpCardData CreateInvincibleCard(float duration)
        {
            return new LevelUpCardData
            {
                RewardType          = LevelUpRewardType.Invincible,
                Title               = "일시 무적",
                Description         = $"{duration:0.#}초 동안 무적 상태가 됩니다.",
                Tag                 = "보너스",
                InvincibleDuration  = duration
            };
        }

        /// <summary>즉시 경험치 카드 생성</summary>
        public static LevelUpCardData CreateBonusExpCard(int expAmount)
        {
            return new LevelUpCardData
            {
                RewardType      = LevelUpRewardType.BonusExp,
                Title           = "즉시 경험치",
                Description     = $"경험치를 {expAmount} 즉시 획득합니다.",
                Tag             = "보너스",
                BonusExpAmount  = expAmount
            };
        }
    }
}