using System;

public enum EquipmentEffectType
{
    /// <summary>기본 피해량 증가 (%) — attackDamage</summary>
    AttackDamagePercent = 0,

    /// <summary>방어력 증가 (정수) — defense +X</summary>
    DefenseFlat = 1,

    /// <summary>방어력 증가 (%) — defense ×(1+X%)</summary>
    DefensePercent = 2,

    /// <summary>최대 체력 증가 (%) — maxHp ×(1+X%)</summary>
    MaxHpPercent = 3,

    /// <summary>이동속도 증가 (%) — moveSpeed ×(1+X%)</summary>
    MoveSpeedPercent = 4,

    /// <summary>치명타 확률 증가 (%) — critChance</summary>
    CritChancePercent = 5,

    /// <summary>치명타 피해량 증가 (%) — critMultiplier</summary>
    CritDamagePercent = 6,

    /// <summary>체력 재생 증가 (정수/초) — hpRegen +X</summary>
    HpRegenFlat = 7,

    /// <summary>경험치 획득량 증가 (%) — expGainMultiplier</summary>
    ExpGainPercent = 8,

    /// <summary>재화(냥) 획득량 증가 (%) — goldGainMultiplier</summary>
    GoldGainPercent = 9,

    /// <summary>픽업 범위 증가 (%) — pickupRange ×(1+X%)</summary>
    PickupRangePercent = 10,

    /// <summary>시전 횟수 증가 (정수) — projectileCount +X</summary>
    ProjectileCountFlat = 20,

    /// <summary>스킬 가속 증가 (정수) — skillHaste +X</summary>
    SkillHasteFlat = 21,

    /// <summary>스킬 범위 증가 (%) — skillAreaMultiplier</summary>
    SkillAreaPercent = 22,

    /// <summary>투사체 속도 증가 (%) — projectileSpeed ×(1+X%)</summary>
    ProjectileSpeedPercent = 23,

    /// <summary>시작 스킬 전용 피해량 배율 (%) — 신규 필드 StartingSkillDamageMultiplier</summary>
    StartingSkillDamagePercent = 24,
    
    /// <summary>받는 피해량 감소 (%) — damageTakenMultiplier ×(1 - X%)</summary>
    DamageTakenReducePercent = 30,

    /// <summary>받는 피해량 증가 (%) — 디메리트 아이템용 damageTakenMultiplier ×(1 + X%)</summary>
    DamageTakenIncreasePercent = 31,

    /// <summary>대쉬 쿨타임 감소 (%) — dashCooldown ×(1 - X%)</summary>
    DashCooldownReducePercent = 40,

    /// <summary>빙결 속성 피해 증가 (%) — 빙결 훅에서 데미지 계산 시 적용</summary>
    IceDamageBonusPercent = 50,

    /// <summary>전기 속성 피해 증가 (%) — ElectricChainSystem2D 훅</summary>
    ElectricDamageBonusPercent = 51,

    /// <summary>화염 속성 피해 증가 (%) — 화상 도트/적중 데미지 훅</summary>
    FireDamageBonusPercent = 52,

    /// <summary>음/양 속성 효과 증가 (%) — 흡혈율/재생량 배율</summary>
    YinYangEffectBonusPercent = 53,
}