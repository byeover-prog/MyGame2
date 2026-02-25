// UTF-8
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

        TryBeginFireConsumeCooldown(() => Fire(target));
    }

    private void Fire(EnemyRegistryMember2D target)
    {
        if (pool == null || owner == null) return;

        // 최종 수치는 CommonSkillWeapon2D 베이스가 JSON/SO/레벨을 합쳐 P에 준비해둠
        Vector2 origin = GetSpawnOrigin(spawnPoint);
        Vector2 dir = (target != null) ? (target.Position - origin).normalized : Vector2.right;

        int damage = P.damage;
        float speed = P.projectileSpeed;
        float life = P.lifeSeconds;
        int count = Mathf.Max(1, P.projectileCount);

        float spread = Mathf.Max(0f, P.spreadAngleDeg);

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0f : (i - (count - 1) * 0.5f);
            float ang = spread * t;
            Vector2 d = Quaternion.Euler(0f, 0f, ang) * dir;

            var proj = pool.Get<PiercingBulletProjectile2D>(origin, Quaternion.identity);
            ApplyProjectileSorting(proj.gameObject);

            proj.Init(enemyMask, damage, speed, life, d);
        }
    }
}
