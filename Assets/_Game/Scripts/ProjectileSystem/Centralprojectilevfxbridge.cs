// [용도]
// CentralProjectileManager의 OnExplosionVFX 이벤트를 구독하여
// 투사체 종류별로 적절한 폭발 VFX를 생성합니다.
//
// [왜 필요한가]
// CentralProjectileManager는 GC 0을 위해 VFX를 직접 호출하지 않고
// static event로 외부에 알립니다. 이 스크립트가 그 이벤트를 받아서
// VFXSpawner.Spawn()으로 실제 VFX를 생성합니다.
//
// [Hierarchy / Inspector 설정]
// 오브젝트: [CentralProjectileManager] (기존 오브젝트에 Add Component)
// 컴포넌트: CentralProjectileVFXBridge
//
// Inspector:
//   Dark Orb Explosion Vfx  → eff_weapon_darkorb_explosion (Project 창에서 드래그)
//   Explosion Lifetime      → 2.0 (폭발 VFX 표시 시간)
//
// [주의사항]
// - GameProjectileManager의 darkOrbExplosionVfxPrefab과 같은 프리팹을 연결
// - OnEnable에서 구독, OnDisable에서 해제 (메모리 누수 방지)
// ============================================================================
using UnityEngine;

/// <summary>
/// CentralProjectileManager의 폭발 이벤트를 받아 VFX를 생성하는 브릿지.
/// </summary>
[DisallowMultipleComponent]
public sealed class CentralProjectileVFXBridge : MonoBehaviour
{

    [Header("암흑구 VFX")]
    [Tooltip("암흑구 폭발 VFX 프리팹. GameProjectileManager에 연결된 것과 동일한 프리팹을 사용하세요.")]
    [SerializeField] private GameObject darkOrbExplosionVfx;

    [Header("설정")]
    [Tooltip("폭발 VFX 자동 반환 시간(초)")]
    [SerializeField] private float explosionLifetime = 2.0f;

    // ══════════════════════════════════════════════════════════════
    // 이벤트 구독
    // ══════════════════════════════════════════════════════════════

    private void OnEnable()
    {
        CentralProjectileManager.OnExplosionVFX += HandleExplosion;
    }

    private void OnDisable()
    {
        CentralProjectileManager.OnExplosionVFX -= HandleExplosion;
    }

    // ══════════════════════════════════════════════════════════════
    // 폭발 처리
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// CentralProjectileManager에서 투사체가 폭발할 때 호출됩니다.
    /// MoveKind에 따라 적절한 VFX 프리팹을 선택합니다.
    /// </summary>
    private void HandleExplosion(Vector2 position, ProjectileMoveKind moveKind)
    {
        GameObject vfxPrefab = null;

        switch (moveKind)
        {
            case ProjectileMoveKind.SplitOnExpiry:
                // 암흑구 폭발
                vfxPrefab = darkOrbExplosionVfx;
                break;

            // ── 미래 확장: 다른 스킬의 폭발 VFX ──
            // case ProjectileMoveKind.Straight:
            //     vfxPrefab = straightExplosionVfx;
            //     break;
        }

        if (vfxPrefab == null) return;

        Vector3 pos3 = new Vector3(position.x, position.y, 0f);
        VFXSpawner.Spawn(vfxPrefab, pos3, Quaternion.identity, explosionLifetime);
    }
}