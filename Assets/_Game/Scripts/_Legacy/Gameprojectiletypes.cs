// ============================================================================
// GameProjectileTypes.cs
// 경로: Assets/_Game/Scripts/Combat/Projectiles/GameProjectileTypes.cs
// 용도: 투사체 상태를 관리하는 struct 정의. 순수 데이터만, 로직 금지.
//
// [설계도 기준]
// - MonoBehaviour 없이 struct 배열로 상태 관리
// - GC 할당 0. 모든 필드는 값 타입.
// - GameProjectileManager가 이 struct의 배열을 소유하고 매 프레임 갱신.
// ============================================================================
using UnityEngine;

/// <summary>
/// 다크오브 투사체 1개의 런타임 상태.
/// Manager의 _darkOrbs[] 배열에 저장된다.
/// </summary>
public struct DarkOrbState
{
    // ── 위치/이동 ──
    public Vector2 Position;
    public Vector2 Direction;   // 정규화된 이동 방향
    public float   Speed;

    // ── 수명 ──
    public float   Lifetime;    // 남은 수명(초). ≤0 이면 폭발.

    // ── 전투 ──
    public float   ExplosionRadius;
    public float   ExplosionDamage;
    public LayerMask EnemyMask;

    // ── 분열 (depth 기반 재귀 트리) ──
    public int     Generation;     // 현재 depth (루트=0, 자식=1, 손자=2 ...)
    public int     MaxGeneration;  // 최대 depth. 레벨에서 계산됨.
    public float   SplitAngleDeg;  // ±분열 각도(도)
    public float   SplitSpeed;     // 분열체 속도
    public float   SplitLifetime;  // 분열체 수명

    // ── 뷰/VFX 참조 ──
    public int     ViewId;         // ViewPool 슬롯 ID. -1이면 뷰 없음.
    public GameObject BodyVfxGo;   // 본체 VFX 오브젝트. null이면 없음.

    // ── 상태 ──
    public bool    Active;         // true = 비행 중, false = 슬롯 비어있음
}

/// <summary>
/// 분열 대기열에 들어가는 요청 1건.
/// ExplodeDarkOrb()에서 큐에 넣고, FlushSplits()에서 꺼내서 자식을 생성.
/// </summary>
public struct DarkOrbSplitRequest
{
    public Vector2   ParentPosition;
    public Vector2   ParentDirection;
    public float     SplitAngleDeg;
    public float     Speed;
    public float     Lifetime;
    public float     ExplosionRadius;
    public float     ExplosionDamage;
    public LayerMask EnemyMask;
    public int       ChildGeneration;   // 부모 Generation + 1
    public int       MaxGeneration;
    public float     ChildSplitAngleDeg;
    public float     ChildSplitSpeed;
    public float     ChildSplitLifetime;
}