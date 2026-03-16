// UTF-8
// Assets/_Game/Scripts/Combat/Projectiles/Data/DarkOrbProjectileSpec.cs
using UnityEngine;

/// <summary>
/// DarkOrb 발사 시 필요한 런타임 설정값 묶음.
/// DarkOrbWeapon2D가 레벨/스탯에서 계산하여 채운 뒤 Manager에 전달.
/// 순수 DTO — 로직 금지.
/// </summary>
public struct DarkOrbProjectileSpec
{
    public LayerMask EnemyMask;
    public int Damage;
    public float Speed;
    public float Lifetime;
    public float ExplosionRadius;
    public float CollisionRadius;       // 비행 중 접촉 판정 반경
    public float CollisionGracePeriod;  // 생성 직후 충돌 무시 시간

    public byte MaxGeneration;          // 0=분열 없음, 1=1→2, 2=1→2→4
    public int SplitChildrenCount;      // 분열 시 자식 수 (보통 2)
    public float SplitAngleDeg;         // 분열 각도 (±도)
    public float SplitSpeed;
    public float SplitLifetime;
    public int SplitDamage;             // 0이면 부모와 동일

    public float OrbAlpha;              // 시각 투명도
}

/// <summary>
/// Linear 투사체(화살/총알) 발사 시 설정값 묶음.
/// 2차 이식용 예약 — 아직 내부 구현 미완성.
/// </summary>
public struct LinearProjectileSpec
{
    public LayerMask EnemyMask;
    public int Damage;
    public float Speed;
    public float Lifetime;
    public float CollisionRadius;
    public int PierceCount;
}