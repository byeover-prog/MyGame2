using UnityEngine;

[DisallowMultipleComponent]
public sealed class HomingMissileWeapon2D : CommonSkillWeapon2D
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
            if (!TryGetFarthest(out target))
                return;
        }
        else
        {
            TryGetFarthest(out target);
        }

        // 쿨다운 소비는 TryBeginFireConsumeCooldown 내부에서 "실제 발사 시점"에 처리한다.
        TryBeginFireConsumeCooldown(() => Fire(target));
    }

    private void Fire(EnemyRegistryMember2D target)
    {
        if (pool == null || owner == null) return;

        Vector2 origin = GetSpawnOrigin(spawnPoint);
        Vector2 dir = (target != null) ? (target.Position - origin).normalized : Vector2.right;

        var proj = pool.Get<HomingMissileProjectile2D>(origin, Quaternion.identity);
        ApplyProjectileSorting(proj.gameObject);

        proj.Init(
            enemyMask,
            P.damage,
            P.projectileSpeed,
            P.lifeSeconds,
            Mathf.Max(30f, P.turnSpeedDeg),
            P.chainCount,
            dir,
            target
        );
    }
}