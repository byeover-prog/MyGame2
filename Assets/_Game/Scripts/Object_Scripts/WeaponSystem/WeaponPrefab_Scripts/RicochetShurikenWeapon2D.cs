using UnityEngine;

[DisallowMultipleComponent]
public sealed class RicochetShurikenWeapon2D : CommonSkillWeapon2D
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

        var proj = pool.Get<RicochetShurikenProjectile2D>(origin, Quaternion.identity);
        ApplyProjectileSorting(proj.gameObject);

        proj.Init(enemyMask, P.damage, P.projectileSpeed, P.lifeSeconds, Mathf.Max(0, P.bounceCount), target);
    }
}