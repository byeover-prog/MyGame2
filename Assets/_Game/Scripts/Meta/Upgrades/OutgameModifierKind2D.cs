public enum OutgameModifierKind2D
{
    None = 0,

    // ─── 기존 (스킬 트리용) ───
    AttackPowerPercent = 1,
    DefensePercent = 2,
    MaxHpFlat = 3,
    BasicSkillDamagePercent = 4,
    BasicSkillCooldownPercent = 5,
    UltimateDamagePercent = 6,
    UltimateCooldownPercent = 7,
    PassivePowerPercent = 8,
    StoryExpGainPercent = 9,
    CasualExpGainPercent = 10,
    NyangGainPercent = 11,

    // ─── 상점 아이템용 추가 ───
    /// <summary>시전 횟수 +N (정수)</summary>
    CastCountFlat = 12,

    /// <summary>스킬 범위 % 증가</summary>
    SkillAreaPercent = 13,

    /// <summary>스킬 가속 +N (정수, LoL 스킬 가속 공식)</summary>
    SkillAccelerationFlat = 14,

    /// <summary>방어력 +N (정수)</summary>
    DefenseFlat = 15,

    /// <summary>체력 재생 +N (정수, 초당)</summary>
    HpRegenFlat = 16,

    /// <summary>치명타 확률 % 증가</summary>
    CritChancePercent = 17,

    /// <summary>치명타 피해량 % 증가</summary>
    CritDamagePercent = 18,

    /// <summary>픽업 범위 % 증가</summary>
    PickupRangePercent = 19,

    /// <summary>최대 체력 % 증가</summary>
    MaxHpPercent = 20,

    /// <summary>재사용 대기시간 % 감소 (음수 효과)</summary>
    CooldownReductionPercent = 21,
}