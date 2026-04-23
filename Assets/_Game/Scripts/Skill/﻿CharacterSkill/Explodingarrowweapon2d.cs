using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 폭발 부착형 화살입니다.
/// 가장 가까운 적을 향해 날아가서 붙고, 잠시 뒤 폭발합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class ExplodingArrowWeapon2D : CharacterSkillWeaponBase
{
    [Header("폭발화살 설정")]
    [Tooltip("화살 투사체 풀입니다.")]
    [SerializeField] private ProjectilePool2D projectilePool;

    [Tooltip("발사 시작점입니다.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("화살 속도입니다.")]
    [SerializeField] private float projectileSpeed = 11f;

    [Tooltip("화살 총 수명입니다. 적을 못 만나면 이 시간 후 소멸.")]
    [SerializeField] private float projectileLifetime = 3f;

    [Tooltip("부착 후 폭발까지 대기 시간입니다. (기획: 1.5초)")]
    [SerializeField] private float attachDelay = 1.5f;

    [Tooltip("폭발 반경입니다. (기획: 2)")]
    [SerializeField] private float explosionRadius = 2f;

    [Tooltip("기본 동시 부착 개수입니다.")]
    [SerializeField] private int maxAttachedCount = 2;

    [Tooltip("레벨당 피해 증가 비율입니다. (기획: +10%)")]
    [SerializeField] private float damagePerLevel = 0.10f;

    [Tooltip("레벨당 쿨다운 감소 비율입니다. (기획: -10%)")]
    [SerializeField] private float cooldownReducePerLevel = 0.10f;

    [Header("각성")]
    [Tooltip("이 레벨부터 추가 부착 개수를 적용합니다.")]
    [SerializeField] private int awakeningLevel = 7;

    [Tooltip("각성 시 증가하는 동시 부착 개수입니다. (기획: 시전횟수 +2)")]
    [SerializeField] private int awakeningExtraCount = 2;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private readonly List<ExplodingArrowProjectile2D> _activeArrows = new List<ExplodingArrowProjectile2D>(8);

    protected override void Awake()
    {
        base.Awake();
        element = DamageElement2D.Ice;
        baseDamage = 15;
        baseCooldown = 3.0f;

        if (projectilePool == null)
            projectilePool = GetComponentInChildren<ProjectilePool2D>(true);

        if (spawnPoint == null)
            spawnPoint = transform;
    }

    protected override void OnOwnerBound()
    {
        CleanupActiveArrows();
    }

    private void Update()
    {
        if (owner == null) return;
        if (projectilePool == null) return;

        CleanupActiveArrows();

        if (_activeArrows.Count >= GetMaxAttachedCount())
            return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        if (!TryGetNearestEnemy(out EnemyRegistryMember2D target) || target == null)
            return;

        Fire(target);
        cooldownTimer = GetCurrentCooldown();
    }

    private void Fire(EnemyRegistryMember2D target)
    {
        Vector2 origin = spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)owner.position;
        Vector2 direction = (target.Position - origin).sqrMagnitude > 0.0001f
            ? (target.Position - origin).normalized
            : Vector2.right;

        ExplodingArrowProjectile2D projectile = projectilePool.Get<ExplodingArrowProjectile2D>(origin, Quaternion.identity);
        if (projectile == null)
            return;

        projectile.BindReturnCallback(HandleArrowReturned);
        projectile.Init(
            enemyMask: enemyMask,
            damageElement: element,
            explosionDamage: GetExplosionDamage(),
            speed: projectileSpeed,
            lifetime: projectileLifetime,
            attachDelay: attachDelay,
            explosionRadius: GetExplosionRadius(),
            direction: direction,
            preferredTarget: target.Transform
        );

        _activeArrows.Add(projectile);

        if (debugLog)
            CombatLog.Log($"[폭발화살] 발사 | active={_activeArrows.Count}/{GetMaxAttachedCount()}", this);
    }

    private void HandleArrowReturned(ExplodingArrowProjectile2D returnedArrow)
    {
        for (int i = _activeArrows.Count - 1; i >= 0; i--)
        {
            if (_activeArrows[i] == null || _activeArrows[i] == returnedArrow)
                _activeArrows.RemoveAt(i);
        }
    }

    private void CleanupActiveArrows()
    {
        for (int i = _activeArrows.Count - 1; i >= 0; i--)
        {
            ExplodingArrowProjectile2D arrow = _activeArrows[i];
            if (arrow == null || !arrow.gameObject.activeInHierarchy)
                _activeArrows.RemoveAt(i);
        }
    }

    private int GetExplosionDamage()
    {
        float damage = baseDamage * (1f + damagePerLevel * Mathf.Max(0, level - 1));
        return ScaleDamage(damage);
    }

    private float GetCurrentCooldown()
    {
        float reduce = cooldownReducePerLevel * Mathf.Max(0, level - 1);
        float cooldown = baseCooldown * Mathf.Max(0.2f, 1f - reduce);
        return ScaleCooldown(cooldown, 0.1f);
    }

    private float GetExplosionRadius()
    {
        return ScaleRadius(explosionRadius, 0.2f);
    }

    private int GetMaxAttachedCount()
    {
        int count = maxAttachedCount;
        if (level >= awakeningLevel)
            count += awakeningExtraCount;

        return Mathf.Max(1, count);
    }
}