// UTF-8
// Assets/_Game/Scripts/Combat/Projectiles/DarkOrbProjectileSpec.cs

/// <summary>
/// 다크오브 발사 시 설정값 DTO.
/// DarkOrbWeapon2D가 레벨/스탯에서 계산하여 채운 뒤 Manager에 전달.
/// 순수 데이터 — 로직 금지.
///
/// [v4 리팩토링 — 설계도 기준]
/// - 접촉 판정 없음 (CollisionRadius 제거). 다크오브는 수명 만료형 폭발 구체.
/// - 분열은 MaxDepth로 표현:
///     MaxDepth=1 → 분열 없음 (폭발만)
///     MaxDepth=2 → 1→2
///     MaxDepth=3 → 1→2→4
///     MaxDepth=4 → 1→2→4→8
/// </summary>
public struct DarkOrbProjectileSpec
{
    public UnityEngine.LayerMask EnemyMask;
    public int   Damage;
    public float Speed;
    public float Lifetime;          // 수명 (이 시간 후 폭발)
    public float ExplosionRadius;   // 폭발 범위

    // depth 기반 재귀 트리
    public int   MaxDepth;          // 1=분열없음, 2=1→2, 3=1→2→4, 4=1→2→4→8
    public float SplitAngleDeg;     // 부모 진행방향 기준 ±각도 (기본 30)
    public float SplitSpeed;        // 자식 이동 속도
    public float SplitLifetime;     // 자식 수명
    public int   SplitDamage;       // 자식 폭발 데미지 (0이면 부모와 동일)

    public float OrbAlpha;          // 시각 투명도 (기본 1)
}

/// <summary>
/// Linear 투사체(화살/총알) 발사 시 설정값 묶음. 2차 이식용 예약.
/// </summary>
public struct LinearProjectileSpec
{
    public UnityEngine.LayerMask EnemyMask;
    public int   Damage;
    public float Speed;
    public float Lifetime;
    public float CollisionRadius;
    public int   PierceCount;
}