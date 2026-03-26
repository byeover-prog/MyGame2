// 모든 투사체의 로직 데이터를 담는 struct.
// MonoBehaviour 없음. GC 0. CentralProjectileManager의 배열 원소.
//
// [설계 원칙]
// - 모든 투사체 종류(직선, 호밍, 부메랑, 분열, 궤도)를 하나의 struct로 표현
// - 종류별로 안 쓰는 필드가 있어도 struct padding 비용 < MonoBehaviour 오버헤드
// - 종류별 로직은 CentralProjectileManager.Process___() static 메서드에서 분기
// ============================================================================

using UnityEngine;

/// <summary>투사체 이동/충돌 유형.</summary>
public enum ProjectileMoveKind : byte
{
    /// <summary>직선 이동 + 수명 만료 시 소멸. 곡궁, 화승총, 수리검, 낙뢰부.</summary>
    Straight,

    /// <summary>타겟 추적 호밍. 정화구.</summary>
    Homing,

    /// <summary>최대 거리까지 전진 → 복귀. 부메랑.</summary>
    Boomerang,

    /// <summary>수명 만료 시 폭발 + depth 기반 분열. 암흑구.</summary>
    SplitOnExpiry,

    /// <summary>플레이어 주위 궤도 회전. 월륜검.</summary>
    Orbit,
}

/// <summary>투사체 충돌 유형.</summary>
public enum ProjectileHitKind : byte
{
    /// <summary>접촉 즉시 데미지 + 소멸 (곡궁).</summary>
    HitAndDie,

    /// <summary>접촉 데미지 + 관통 (화승총). hitCount로 관통 제한.</summary>
    Pierce,

    /// <summary>수명 만료 시 OverlapCircle 범위 데미지 (암흑구 폭발).</summary>
    AreaOnExpiry,

    /// <summary>주기적 OverlapCircle (월륜검, 낙뢰부).</summary>
    AreaPeriodic,

    /// <summary>타겟 도달 시 데미지 (정화구). 사망 시 재탐색.</summary>
    HomingHit,

    /// <summary>접촉 데미지 + 튕김 (수리검). bounceCount로 제한.</summary>
    Bounce,
}

/// <summary>
/// 투사체 1개의 전체 상태. MonoBehaviour 아닌 순수 데이터.
/// CentralProjectileManager의 _slots[] 배열 원소.
/// </summary>
public struct ProjectileSlot
{
    // ═══════════════════════════════════════════
    //  활성 여부
    // ═══════════════════════════════════════════
    public bool Active;

    // ═══════════════════════════════════════════
    //  종류
    // ═══════════════════════════════════════════
    public ProjectileMoveKind MoveKind;
    public ProjectileHitKind HitKind;
    public DamageElement2D Element;

    // ═══════════════════════════════════════════
    //  운동
    // ═══════════════════════════════════════════
    public Vector2 Position;
    public Vector2 Direction;     // normalized
    public float Speed;
    public float Lifetime;        // 남은 수명 (초)

    // ═══════════════════════════════════════════
    //  전투
    // ═══════════════════════════════════════════
    public int Damage;
    public float HitRadius;       // 충돌 반경 (거리 판정용)
    public float ExplosionRadius; // 폭발 범위 (AreaOnExpiry/AreaPeriodic)
    public LayerMask EnemyMask;

    // ═══════════════════════════════════════════
    //  관통 / 바운스
    // ═══════════════════════════════════════════
    public int MaxHitCount;       // Pierce: 최대 관통 수
    public int CurrentHitCount;   // 현재까지 적중 수
    public int MaxBounceCount;    // Bounce: 최대 튕김 수
    public int CurrentBounceCount;

    // ═══════════════════════════════════════════
    //  분열 (SplitOnExpiry)
    // ═══════════════════════════════════════════
    public int Generation;        // 현재 세대 (0=루트)
    public int MaxGeneration;     // 최대 세대 (Lv1=1, Lv4=4)
    public float SplitAngleDeg;
    public float SplitSpeed;
    public float SplitLifetime;

    // ═══════════════════════════════════════════
    //  호밍 (Homing)
    // ═══════════════════════════════════════════
    public int TargetInstanceId;  // 추적 대상 InstanceID (0=없음)
    public float HomingTurnSpeed; // 회전 속도 (도/초)

    // ═══════════════════════════════════════════
    //  부메랑 (Boomerang)
    // ═══════════════════════════════════════════
    public float MaxDistance;     // 최대 전진 거리
    public float TraveledDist;   // 현재까지 이동 거리
    public bool Returning;        // 복귀 중 여부
    public int OwnerInstanceId;   // 복귀 대상 (플레이어)

    // ═══════════════════════════════════════════
    //  궤도 (Orbit)
    // ═══════════════════════════════════════════
    public float OrbitAngle;      // 현재 각도 (라디안)
    public float OrbitRadius;
    public float OrbitAngularSpeed; // 라디안/초
    public float HitInterval;    // 주기적 판정 간격
    public float HitTimer;       // 다음 판정까지 남은 시간

    // ═══════════════════════════════════════════
    //  뷰 연결 (View/Logic 분리)
    // ═══════════════════════════════════════════
    public ProjectileVisualId VisualId; // 어떤 뷰 프리팹을 쓸지 (스킬별 구분)
    public int ViewId;            // CentralViewPool 슬롯 ID (-1=없음)
    public int VfxViewId;         // VFX 뷰 ID (-1=없음)

    // ═══════════════════════════════════════════
    //  중복 히트 방지
    // ═══════════════════════════════════════════
    /// <summary>
    /// 이미 맞은 적 InstanceID 목록.
    /// struct 안에 배열을 넣으면 GC가 발생하므로,
    /// CentralProjectileManager가 공유 버퍼로 관리합니다.
    /// 이 필드는 공유 버퍼 내의 시작 인덱스입니다.
    /// </summary>
    public int HitCacheStartIndex;
    public int HitCacheCount;
}

/// <summary>
/// 분열 대기열 엔트리. 한 프레임에 폭발→분열을 바로 처리하면 배열이 꼬이므로
/// 큐에 넣고 다음 단계에서 처리.
/// </summary>
public struct SplitRequest
{
    public Vector2 ParentPosition;
    public Vector2 ParentDirection;
    public float SplitAngleDeg;
    public float Speed;
    public float Lifetime;
    public float ExplosionRadius;
    public int ExplosionDamage;
    public LayerMask EnemyMask;
    public DamageElement2D Element;
    public int ChildGeneration;
    public int MaxGeneration;
    public float ChildSplitAngleDeg;
    public float ChildSplitSpeed;
    public float ChildSplitLifetime;
    public float HitRadius;
    public ProjectileVisualId VisualId; // ★ v2: 분열 자식도 같은 비주얼
}