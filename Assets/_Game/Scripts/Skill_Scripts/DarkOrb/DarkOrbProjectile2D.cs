// UTF-8
// Assets/_Game/Scripts/Skill_Scripts/DarkOrb/DarkOrbProjectile2D.cs
//
// ╔══════════════════════════════════════════════════════════╗
// ║  [레거시 구현]                                           ║
// ║  공용 GameProjectileManager 도입 후 비활성 상태.          ║
// ║  검증 완료 전까지 삭제하지 않음.                          ║
// ║                                                          ║
// ║  새 시스템: GameProjectileManager.TrySpawnDarkOrb()       ║
// ║  새 발사기: DarkOrbWeapon2D → Manager 호출 방식           ║
// ╚══════════════════════════════════════════════════════════╝

using UnityEngine;

[System.Obsolete("레거시. GameProjectileManager로 대체됨. 삭제 금지 — 검증 완료 후 정리.")]
[DisallowMultipleComponent]
public sealed class DarkOrbProjectile2D : MonoBehaviour
{
    // 기존 Inspector 필드 유지 (프리팹 참조 깨짐 방지)
    [SerializeField] private DarkOrbProjectile2D splitSpawnPrefab;
    [SerializeField] private float splitAngleDeg = 40f;
    [SerializeField] private float spawnEps = 0.4f;
    [SerializeField] private float collisionGracePeriod = 0.05f;

    private void Awake()
    {
#if UNITY_EDITOR
        Debug.LogWarning(
            $"[DarkOrbProjectile2D] 레거시 컴포넌트가 활성화됨. " +
            $"GameProjectileManager를 사용하세요. 오브젝트: {gameObject.name}",
            this);
#endif
        enabled = false;
    }

    // 기존 public API 시그니처 유지 (다른 스크립트 컴파일 에러 방지)
    public static int ActiveCount => GameProjectileManager.Instance != null
        ? GameProjectileManager.Instance.ActiveDarkOrbCount : 0;

    public static void Prewarm(DarkOrbProjectile2D prefab, int count) { }

    public static DarkOrbProjectile2D Spawn(DarkOrbProjectile2D prefab,
                                             Vector2 pos, bool autoActivate = true)
    { return null; }

    public void Init(
        LayerMask enemyMask, int damage, float speed, float lifeSeconds,
        Vector2 dir, float explosionRadius, int splitCount,
        float splitSpeed, float splitLifeSeconds, int splitDamage,
        ProjectilePool2D splitPool, float orbAlpha)
    { /* no-op */ }
}