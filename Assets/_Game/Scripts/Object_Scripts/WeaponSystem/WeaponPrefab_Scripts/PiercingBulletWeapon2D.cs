using UnityEngine;

[DisallowMultipleComponent]
public sealed class PiercingBulletWeapon2D : CommonSkillWeapon2D
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

        // 쿨다운 소비는 TryBeginFireConsumeCooldown 내부에서 "실제 발사 시점"에 처리한다.
        TryBeginFireConsumeCooldown(() => Fire(target));
    }

    private void Fire(EnemyRegistryMember2D target)
    {
        if (pool == null || owner == null) return;

        Vector2 origin = GetSpawnOrigin(spawnPoint);
        Vector2 dir = (target != null) ? (target.Position - origin).normalized : Vector2.right;

        int count = Mathf.Max(1, P.projectileCount);
        float spread = Mathf.Max(0f, P.spreadAngleDeg);

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0f : (i - (count - 1) * 0.5f);
            float ang = spread * t;
            Vector2 d = Quaternion.Euler(0f, 0f, ang) * dir;

            var proj = pool.Get<PiercingBulletProjectile2D>(origin, Quaternion.identity);
            ApplyProjectileSorting(proj.gameObject);
            proj.Init(enemyMask, P.damage, P.projectileSpeed, P.lifeSeconds, d);
        }
    }
}