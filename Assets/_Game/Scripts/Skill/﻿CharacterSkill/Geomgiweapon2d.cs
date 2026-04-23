using UnityEngine;

/// <summary>
/// 검기 발사입니다.
/// 가장 가까운 적 방향으로 관통 검기를 발사합니다 (자동 조준).
/// </summary>
[DisallowMultipleComponent]
public sealed class GeomgiWeapon2D : CharacterSkillWeaponBase
{
    [Header("검기 설정")]
    [Tooltip("검기 투사체 풀입니다.")]
    [SerializeField] private ProjectilePool2D projectilePool;

    [Tooltip("발사 시작점입니다. 비우면 owner 위치를 사용합니다.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("검기 이동 속도입니다.")]
    [SerializeField] private float projectileSpeed = 13f;

    [Tooltip("검기 수명입니다.")]
    [SerializeField] private float projectileLifetime = 3f;

    [Tooltip("적 탐색 최대 범위입니다. 0 이하이면 무제한.")]
    [SerializeField] private float detectRange = 10f;

    [Tooltip("적이 없을 때도 발사할지 여부입니다. false면 적이 있어야 발사합니다.")]
    [SerializeField] private bool requireTargetToFire = true;

    [Tooltip("다중 발사 시 벌어지는 각도입니다.")]
    [SerializeField] private float multiShotSpread = 8f;

    [Tooltip("레벨당 피해 증가 비율입니다. (기획: +10%)")]
    [SerializeField] private float damagePerLevel = 0.10f;

    [Header("각성")]
    [Tooltip("이 레벨부터 다중 발사를 적용합니다.")]
    [SerializeField] private int awakeningLevel = 7;

    [Tooltip("각성 시 발사 수 증가량입니다. (기획: 시전 횟수 +2)")]
    [SerializeField] private int awakeningExtraShots = 2;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    protected override void Awake()
    {
        base.Awake();
        element = DamageElement2D.Dark;
        baseDamage = 15;
        baseCooldown = 3.0f;

        if (projectilePool == null)
            projectilePool = GetComponentInChildren<ProjectilePool2D>(true);

        if (spawnPoint == null)
            spawnPoint = transform;
    }

    private void Update()
    {
        if (owner == null) return;
        if (projectilePool == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        // 자동 조준: 가장 가까운 적 방향
        Vector2 aimDir;
        EnemyRegistryMember2D target = null;

        bool found = detectRange > 0f
            ? TryGetNearestEnemy(detectRange, out target)
            : TryGetNearestEnemy(out target);

        if (found && target != null)
        {
            aimDir = (target.Position - (Vector2)owner.position).normalized;
            if (aimDir.sqrMagnitude <= 0.0001f)
                aimDir = Vector2.right;
        }
        else
        {
            if (requireTargetToFire) return;
            aimDir = Vector2.right; // 적이 없으면 기본 오른쪽
        }

        Fire(aimDir);
        cooldownTimer = ScaleCooldown(baseCooldown, 0.1f);
    }

    private void Fire(Vector2 aimDir)
    {
        int shotCount = GetProjectileCount();
        Vector2 origin = spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)owner.position;
        int damage = GetProjectileDamage();

        for (int i = 0; i < shotCount; i++)
        {
            float angleOffset = GetSpreadAngle(shotCount, i);
            Vector2 shotDir = Quaternion.Euler(0f, 0f, angleOffset) * aimDir;

            GeomgiProjectile2D projectile = projectilePool.Get<GeomgiProjectile2D>(origin, Quaternion.identity);
            if (projectile == null)
                continue;

            projectile.Init(
                enemyMask: enemyMask,
                damageElement: element,
                damage: damage,
                speed: projectileSpeed,
                lifetime: projectileLifetime,
                direction: shotDir
            );
        }

        if (debugLog)
            CombatLog.Log($"[검기] 발사 {shotCount}개 | dmg={damage}", this);
    }

    private int GetProjectileDamage()
    {
        float damage = baseDamage * (1f + damagePerLevel * Mathf.Max(0, level - 1));
        return ScaleDamage(damage);
    }

    private int GetProjectileCount()
    {
        int count = 1;
        if (level >= awakeningLevel)
            count += awakeningExtraShots;

        return Mathf.Max(1, count);
    }

    private float GetSpreadAngle(int shotCount, int shotIndex)
    {
        if (shotCount <= 1)
            return 0f;

        float mid = (shotCount - 1) * 0.5f;
        return (shotIndex - mid) * multiShotSpread;
    }
}