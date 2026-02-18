using UnityEngine;

[DisallowMultipleComponent]
public sealed class BalsiWeapon2D : CommonSkillWeapon2D
{
    [SerializeField] private ProjectilePool2D pool;
    [SerializeField] private Transform spawnPoint;

    [Header("발시 옵션")]
    [SerializeField, Tooltip("관통 횟수(0=첫 타격에 소멸, 1=1회 관통 후 소멸)")]
    private int pierceCount = 0;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private float _noTargetLogTimer;

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;

        if (pool == null)
            pool = GetComponentInChildren<ProjectilePool2D>(true);
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
            {
                if (debugLog)
                {
                    _noTargetLogTimer -= Time.deltaTime;
                    if (_noTargetLogTimer <= 0f)
                    {
                        _noTargetLogTimer = 1f;
                        Debug.LogWarning("[BalsiWeapon2D] 타겟을 못 찾아서 발사하지 않습니다. (EnemyRegistry2D 등록/레이어/Tag 확인)", this);
                    }
                }
                return;
            }
        }
        else
        {
            TryGetNearest(out target);
        }

        TryBeginFireConsumeCooldown(() => Fire(target));
    }

    private void Fire(EnemyRegistryMember2D target)
    {
        if (pool == null)
            pool = GetComponentInChildren<ProjectilePool2D>(true);

        if (pool == null) return;
        if (owner == null) return;

        Vector2 origin = GetSpawnOrigin(spawnPoint);
        Vector2 dir = (target != null) ? (target.Position - origin).normalized : Vector2.right;

        var proj = pool.Get<BalsiProjectile2D>(origin, Quaternion.identity);
        ApplyProjectileSorting(proj.gameObject);
        proj.Init(enemyMask, P.damage, P.projectileSpeed, P.lifeSeconds, dir, pierceCount);

        if (debugLog)
            Debug.Log($"[BalsiWeapon2D] Fire target={(target != null)} dmg={P.damage} cd={P.cooldown:0.00}", this);
    }
}
