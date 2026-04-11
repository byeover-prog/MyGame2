namespace _Game.LevelUp
{
    /// <summary>
    /// 레벨업 카드가 제공하는 보상 타입.
    /// Skill이면 SO 기반 스킬/패시브, CharacterSkill이면 전용 스킬(SkillRunner 경로).
    /// </summary>
    public enum LevelUpRewardType
    {
        /// <summary>스킬/패시브 SO 기반 선택지</summary>
        Skill = 0,

        /// <summary>체력 회복</summary>
        Heal = 1,

        /// <summary>재화 획득</summary>
        Gold = 2,

        /// <summary>일시 무적</summary>
        Invincible = 3,

        /// <summary>경험치 즉시 획득</summary>
        BonusExp = 4,

        /// <summary> 캐릭터 전용 스킬 (SkillRunner 경로)</summary>
        CharacterSkill = 5
    }
}