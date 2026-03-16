// UTF-8
// Assets/_Game/Scripts/Combat/Projectiles/GameProjectileTypes.cs

/// <summary>투사체 종류. 향후 스킬 추가 시 여기에 enum 추가.</summary>
public enum GameProjectileKind : byte
{
    None = 0,
    DarkOrb = 1,
    Linear = 2,     // 화살/총알 계열 (2차 이식용 예약)
    // Shuriken = 3,
    // Homing = 4,
    // Boomerang = 5,
}

/// <summary>투사체 동작 플래그. 비트마스크.</summary>
[System.Flags]
public enum GameProjectileFlags : ushort
{
    None            = 0,
    CanSplit        = 1 << 0,   // 분열 가능
    SplitOnContact  = 1 << 1,   // 적 접촉 시 분열
    SplitOnExpire   = 1 << 2,   // 수명 만료 시 분열
    AreaDamage      = 1 << 3,   // 폭발 범위 데미지
    Pierce          = 1 << 4,   // 관통 (DarkOrb는 미사용)
    // 향후 확장 예약
}