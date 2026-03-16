// ============================================================================
// DarkOrbProjectileSpec.cs
// 경로: Assets/_Game/Scripts/Combat/Projectiles/Data/DarkOrbProjectileSpec.cs
// 용도: DarkOrb 발사 시 런타임 설정값 묶음 (순수 DTO, 로직 금지)
//
// DarkOrbWeapon2D가 레벨/스탯에서 계산한 값을 이 struct에 담아서
// GameProjectileManager.TrySpawnDarkOrb(spec)으로 전달합니다.
//
// [설계도 기준]
// - 접촉 판정 관련 필드 없음 (수명 만료형 폭발 구체)
// - 매 프레임 OverlapCircle 없음. 폭발 시 1회만.
// ============================================================================
using UnityEngine;

public struct DarkOrbProjectileSpec
{
    // ── 발사 ──
    public Vector2   SpawnPosition;     // 발사 위치 (플레이어 위치)
    public Vector2   Direction;         // 발사 방향 (적 방향, 정규화)
    public float     Speed;             // 투사체 속도
    public float     Lifetime;          // 루트 투사체 수명(초)

    // ── 전투 ──
    public float     ExplosionRadius;   // 폭발 반경
    public float     ExplosionDamage;   // 폭발 피해량
    public LayerMask EnemyMask;         // 적 레이어 마스크

    // ── 분열 (depth 기반) ──
    public int       MaxGeneration;     // 최대 분열 깊이 (Lv1=0, Lv2=1, Lv3=2, Lv4+=3)
    public float     SplitAngleDeg;     // ±분열 각도(도). 예) 40 = 좌 40° + 우 40°
    public float     SplitSpeed;        // 분열체 속도
    public float     SplitLifetime;     // 분열체 수명(초)

    // ── 비주얼 ──
    public float     OrbAlpha;          // 스프라이트 알파값 (0~1)
}