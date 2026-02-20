using UnityEngine;

[DisallowMultipleComponent]
public sealed class BoomerangWeapon2D : CommonSkillWeapon2D
{
    [Header("풀")]
    [SerializeField] private ProjectilePool2D pool;

    [Header("스폰")]
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
            if (!TryGetFarthest(out target) || target == null)
                return;
        }
        else
        {
            TryGetFarthest(out target);
        }

        Fire(target);
        cooldownTimer = Mathf.Max(0.01f, P.cooldown);
    }

    private void Fire(EnemyRegistryMember2D target)
    {
        if (pool == null || owner == null) return;

        Vector2 origin = spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)owner.position;
        Vector2 dir = (target != null) ? (target.Position - origin).normalized : Vector2.right;

        int count = Mathf.Max(1, P.projectileCount);
        float spread = Mathf.Max(0f, P.spreadAngleDeg);

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0f : (i - (count - 1) * 0.5f);
            float ang = spread * t;
            Vector2 d = Quaternion.Euler(0f, 0f, ang) * dir;

            var proj = pool.Get<BoomerangProjectile2D>(origin, Quaternion.identity);
            proj.Init(owner, d, enemyMask, P.damage, P.projectileSpeed, Mathf.Max(0.1f, P.returnSpeed), P.maxDistance, P.lifeSeconds);
        }
    }
}