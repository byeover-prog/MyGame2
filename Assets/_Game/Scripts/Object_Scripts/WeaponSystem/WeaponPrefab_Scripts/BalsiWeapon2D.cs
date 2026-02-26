// UTF-8
using UnityEngine;
using _Game.Scripts.Object_Scripts.WeaponSystem.WeaponPrefab_Scripts;

[DisallowMultipleComponent]
public sealed class BalsiWeapon2D : CommonSkillWeapon2D
{
    [SerializeField] private ProjectilePool2D pool;
    [SerializeField] private Transform spawnPoint;

    [Header("관통(발시 전용)")]
    [Tooltip("발시 투사체 관통 횟수(0=관통 없음). CommonSkillLevelParams에 pierceCount가 없는 프로젝트 호환용.")]
    [Min(0)]
    [SerializeField] private int pierceCount = 0;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private float _noTargetLogTimer;

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;
        if (pool == null) pool = GetComponentInChildren<ProjectilePool2D>(true);
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
                        Debug.LogWarning("[BalsiWeapon2D] 타겟을 못 찾아서 발사하지 않습니다. (EnemyRegistry2D 등록 확인)", this);
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
        if (pool == null) pool = GetComponentInChildren<ProjectilePool2D>(true);
        if (pool == null) return;
        if (owner == null) return;

        Vector2 origin = GetSpawnOrigin(spawnPoint);
        Vector2 dir = (target != null) ? (target.Position - origin).normalized : Vector2.right;

        var proj = pool.Get<BalsiProjectile2D>(origin, Quaternion.identity);
        ApplyProjectileSorting(proj.gameObject);

        proj.Init(enemyMask, P.damage, P.projectileSpeed, P.lifeSeconds, dir, pierceCount);

        if (debugLog)
            Debug.Log($"[BalsiWeapon2D] Fire target={(target != null)} dmg={P.damage} cd={P.cooldown:0.00} pierce={pierceCount}", this);
    }
}