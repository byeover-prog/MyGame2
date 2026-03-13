using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArrowShotWeapon2D : CommonSkillWeapon2D
{
    [SerializeField] private ProjectilePool2D pool;
    [SerializeField] private Transform spawnPoint;

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

        TryBeginFireConsumeCooldown(() => Fire(target));
    }

    private void Fire(EnemyRegistryMember2D target)
    {
        if (pool == null || owner == null) return;

        Vector2 origin = GetSpawnOrigin(spawnPoint);
        Vector2 baseDir = (target != null) ? (target.Position - origin).normalized : Vector2.right;

        int count = Mathf.Max(1, P.projectileCount);
        // ★ spreadAngleDeg를 "총 부채꼴 각도"로 사용 (LoL 애쉬 W 방식)
        // 기본값이 0이면 투사체 수에 맞춰 자동 계산 (투사체당 12도)
        float totalSpread = P.spreadAngleDeg > 0f ? P.spreadAngleDeg : (count - 1) * 12f;

        for (int i = 0; i < count; i++)
        {
            float ang;
            if (count == 1)
            {
                ang = 0f;
            }
            else
            {
                // -totalSpread/2 ~ +totalSpread/2 사이를 균등 분할
                ang = -totalSpread * 0.5f + totalSpread * ((float)i / (count - 1));
            }

            Vector2 d = Quaternion.Euler(0f, 0f, ang) * baseDir;

            var proj = pool.Get<ArrowProjectile2D>(origin, Quaternion.identity);
            ApplyProjectileSorting(proj.gameObject);

            proj.Init(enemyMask, P.damage, P.projectileSpeed, P.lifeSeconds, d, owner);
        }
    }
}