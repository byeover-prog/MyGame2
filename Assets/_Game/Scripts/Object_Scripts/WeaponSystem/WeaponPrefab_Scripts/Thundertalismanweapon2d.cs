// UTF-8
using UnityEngine;

/// <summary>
/// 낙뢰부 무기: 부적 투척 → 적 히트 시 적 위치에 번개 범위 공격
///
/// [변경 요약]
/// - 번개 데미지 반경을 인스펙터에서 배수로 조절 가능(thunderRadiusMultiplier)
/// - 번개 비주얼 크기는 ThunderStrikeArea2D에서 따로 조절(visualScaleMultiplier)
/// </summary>
[DisallowMultipleComponent]
public sealed class ThunderTalismanWeapon2D : CommonSkillWeapon2D
{
    [Header("낙뢰부 전용")]
    [SerializeField] private ProjectilePool2D pool;
    [SerializeField] private Transform spawnPoint;

    [Header("번개(범위 공격)")]
    [SerializeField] private ThunderStrikeArea2D thunderPrefab;
    [SerializeField, Min(1)] private int maxThunders = 5;

    [Tooltip("번개 데미지 반경 배수. 1=원본, 2=2배\n'범위가 좁다'면 이 값을 올리세요.")]
    [SerializeField, Min(0.1f)] private float thunderRadiusMultiplier = 1.8f;

    [Tooltip("번개 반경 최소값(너무 작은 값을 방지)")]
    [SerializeField, Min(0.05f)] private float thunderMinRadius = 1.2f;

    [Header("디버그")]
    [SerializeField] private bool debugLog = true;

    private ThunderStrikeArea2D[] _thunderPool;
    private int _thunderIndex;

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;
        InitThunderPool();
    }

    private void Update()
    {
        if (config == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        EnemyRegistryMember2D target = null;

        if (requireTargetToFire)
        {
            if (!TryGetNearest(out target) || target == null)
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
        Vector2 dir = (target != null)
            ? (target.Position - origin).normalized
            : Vector2.right;

        int count = Mathf.Max(1, StatProjectileCount);

        for (int i = 0; i < count; i++)
        {
            var proj = pool.Get<ThunderTalismanProjectile2D>(origin, Quaternion.identity);
            ApplyProjectileSorting(proj.gameObject);

            proj.Init(
                mask: enemyMask,
                dmg: StatDamage,
                spd: StatProjectileSpeed,
                life: StatProjectileLife,
                dir: dir,
                thunderCallback: OnTalismanHit
            );

            if (debugLog)
                Debug.Log($"[ThunderWeapon] Fire dir={dir} dmg={StatDamage} baseRadius={P.explosionRadius}");
        }
    }

    /// <summary>
    /// 부적이 적에게 닿으면 호출 → 적 위치에 번개
    /// </summary>
    private void OnTalismanHit(Vector2 enemyPosition)
    {
        if (thunderPrefab == null)
        {
            if (debugLog) Debug.LogWarning("[ThunderWeapon] thunderPrefab이 null!");
            return;
        }

        var thunder = GetThunderFromPool();
        if (thunder == null)
        {
            if (debugLog) Debug.LogWarning("[ThunderWeapon] thunder pool 비어있음!");
            return;
        }

        // 적 위치에 번개 배치
        thunder.transform.position = enemyPosition;

        // 실제 데미지 반경: (기본 반경 * 배수) + 최소값 보장
        float baseRadius = Mathf.Max(0.05f, P.explosionRadius);
        float radius = Mathf.Max(thunderMinRadius, baseRadius * thunderRadiusMultiplier);

        if (debugLog)
            Debug.Log($"[ThunderWeapon] Thunder at {enemyPosition} radius={radius} (base={baseRadius} x mul={thunderRadiusMultiplier}) dmg={StatDamage} mask={enemyMask.value}");

        thunder.Strike(
            radius: radius,
            damage: StatDamage,
            mask: enemyMask
        );
    }

    private void InitThunderPool()
    {
        if (thunderPrefab == null) return;

        _thunderPool = new ThunderStrikeArea2D[maxThunders];
        for (int i = 0; i < maxThunders; i++)
        {
            var t = Instantiate(thunderPrefab);
            t.gameObject.SetActive(false);
            _thunderPool[i] = t;
        }
        _thunderIndex = 0;
    }

    private ThunderStrikeArea2D GetThunderFromPool()
    {
        if (_thunderPool == null || _thunderPool.Length == 0) return null;

        var t = _thunderPool[_thunderIndex];
        _thunderIndex = (_thunderIndex + 1) % _thunderPool.Length;
        return t;
    }
}