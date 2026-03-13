public enum PassiveKind
{
    AttackDamage = 0,       // 1. 공격력 증가
    Defense = 1,            // 2. 방어력 증가
    CooldownReduction = 2,  // 8. 스킬 가속 증가 (LoL haste 공식)
    MoveSpeed = 3,          // 5. 이동속도 증가
    PickupRange = 4,        // 3. 픽업범위 증가
    MaxHp = 5,              // 4. 최대체력 증가
    ExpGain = 6,            // 7. 경험치 획득량 증가 (구 ElementDamage)
    SkillArea = 7           // 6. 스킬 범위 증가
}