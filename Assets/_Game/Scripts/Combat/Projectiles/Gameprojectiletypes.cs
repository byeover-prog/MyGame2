// UTF-8
// Assets/_Game/Scripts/Combat/Projectiles/GameProjectileTypes.cs

/// <summary>투사체 종류</summary>
public enum GameProjectileKind : byte
{
    DarkOrb = 0,
    Linear  = 1,   // 화살/총알 등 직선 투사체 (기존 코드 호환용)
}

/// <summary>투사체 플래그 (비트 마스크)</summary>
[System.Flags]
public enum GameProjectileFlags : byte
{
    None       = 0,
    AreaDamage = 1 << 0,   // 폭발 범위 데미지
}