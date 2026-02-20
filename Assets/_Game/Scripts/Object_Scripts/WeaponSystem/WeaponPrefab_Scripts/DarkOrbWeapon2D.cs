using UnityEngine;

[DisallowMultipleComponent]
public sealed class DarkOrbWeapon2D : CommonSkillWeapon2D
{
    [Header("풀")]
    [SerializeField] private ProjectilePool2D orbPool;
    [SerializeField] private ProjectilePool2D splitPool;

    [Header("스폰")]
    [SerializeField] private Transform spawnPoint;

    [Header("표현")]
    [Range(0.1f, 1f)]
    [SerializeField] private float orbAlpha = 0.55f;

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;
    }

    private void Update()
    {
        if (config == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        EnemyRegistryMember2D target = null;

        if (requireTargetToFire)
        {
            if (!TryGetNearest(out target))
                return;
        }
        else
        {
            TryGetNearest(out target);
        }

        // 쿨다운 소비는 TryBeginFireConsumeCooldown 내부에서 "실제 발사 시점"에 처리한다.
        TryBeginFireConsumeCooldown(() => Fire(target));
    }

    private void Fire(EnemyRegistryMember2D target)
    {
        if (orbPool == null || owner == null) return;

        Vector2 origin = GetSpawnOrigin(spawnPoint);
        Vector2 dir = (target != null) ? (target.Position - origin).normalized : Vector2.right;

        var orb = orbPool.Get<DarkOrbProjectile2D>(origin, Quaternion.identity);
        ApplyProjectileSorting(orb.gameObject);

        int splitDamage = Mathf.Max(1, Mathf.RoundToInt(P.damage * 0.5f));

        orb.Init(
            enemyMask,
            P.damage,
            P.projectileSpeed,
            P.lifeSeconds,
            dir,
            Mathf.Max(0.1f, P.explosionRadius),
            Mathf.Max(0, P.splitCount),
            Mathf.Max(0.1f, P.childSpeed),
            Mathf.Max(0.1f, P.lifeSeconds * 0.8f),
            splitDamage,
            splitPool,
            orbAlpha
        );
    }
}