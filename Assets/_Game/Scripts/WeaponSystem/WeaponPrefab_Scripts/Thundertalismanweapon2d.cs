using UnityEngine;

/// <summary>
/// 낙뢰부 무기: 부적 투척 → 적 히트 시 적 위치에 번개 범위 공격
///
/// [구조]
/// 1. Update()에서 쿨타임 확인 → Fire()
/// 2. Fire()에서 ProjectilePool2D로 부적 투사체 생성
/// 3. 부적이 적에게 닿으면 OnTalismanHit() 콜백
/// 4. OnTalismanHit()에서 내부 ThunderStrike 풀에서 꺼내서 Strike() 호출
/// 5. ThunderStrikeArea2D.Strike()가 범위 데미지 + VFX 파티클 생성
///
/// [변경 요약 — VFX 전환]
/// - 무기 코드 자체는 변경 없음
/// - thunderPrefab이 참조하는 ThunderStrike 프리팹에서 Animator/SpriteRenderer 제거 필요
/// - ThunderStrikeArea2D가 내부적으로 VFXSpawner를 호출하므로 무기는 관여 안 함
/// </summary>
[DisallowMultipleComponent]
public sealed class ThunderTalismanWeapon2D : CommonSkillWeapon2D
{
    [Header("낙뢰부 전용")]
    [Tooltip("부적 투사체 풀. ThunderTalisman_Weapon 프리팹 하위의 Pool_Talisman 오브젝트를 연결하세요.")]
    [SerializeField] private ProjectilePool2D pool;

    [Tooltip("부적 발사 위치. 비우면 이 오브젝트의 Transform을 사용합니다.")]
    [SerializeField] private Transform spawnPoint;

    [Header("번개(범위 공격)")]
    [Tooltip("ThunderStrike 프리팹.\n※ Animator/SpriteRenderer는 제거하고 ThunderStrikeArea2D만 남겨야 합니다.")]
    [SerializeField] private ThunderStrikeArea2D thunderPrefab;

    [Tooltip("내부 번개 풀 크기. 동시에 존재할 수 있는 번개 이펙트 최대 수.")]
    [SerializeField, Min(1)] private int maxThunders = 5;

    [Tooltip("번개 데미지 반경 배수. 1=원본, 2=2배.\n'범위가 좁다'면 이 값을 올리세요.")]
    [SerializeField, Min(0.1f)] private float thunderRadiusMultiplier = 1.8f;

    [Tooltip("번개 반경 최소값 (너무 작은 값을 방지)")]
    [SerializeField, Min(0.05f)] private float thunderMinRadius = 1.2f;

    [Header("디버그")]
    [SerializeField] private bool debugLog = true;

    private ThunderStrikeArea2D[] _thunderPool;
    private int _thunderIndex;

    /* ───────────── 초기화 ───────────── */

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;
        InitThunderPool();
    }

    /// <summary>
    /// ThunderStrike 인스턴스를 미리 생성해서 내부 풀로 관리합니다.
    /// 런타임 Instantiate를 방지하기 위함입니다.
    /// </summary>
    private void InitThunderPool()
    {
        if (thunderPrefab == null)
        {
            if (debugLog)
                Debug.LogWarning("[ThunderWeapon] thunderPrefab이 null! " +
                                 "Inspector에서 ThunderStrike 프리팹을 연결하세요.");
            return;
        }

        _thunderPool = new ThunderStrikeArea2D[maxThunders];
        for (int i = 0; i < maxThunders; i++)
        {
            var t = Instantiate(thunderPrefab);
            t.gameObject.SetActive(false);
            _thunderPool[i] = t;
        }
        _thunderIndex = 0;

        if (debugLog)
            Debug.Log($"[ThunderWeapon] 번개 풀 초기화 완료 (크기={maxThunders})");
    }

    /* ───────────── 발사 로직 ───────────── */

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
                Debug.Log($"[ThunderWeapon] 부적 발사 dir={dir} " +
                          $"dmg={StatDamage} baseRadius={P.explosionRadius}");
        }
    }

    /* ───────────── 번개 콜백 ───────────── */

    /// <summary>
    /// 부적이 적에게 닿으면 호출됩니다.
    /// 적의 위치에 번개(범위 데미지 + VFX)를 생성합니다.
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
            if (debugLog) Debug.LogWarning("[ThunderWeapon] 번개 풀 모두 사용 중!");
            return;
        }

        // 적 위치에 번개 배치
        thunder.transform.position = enemyPosition;

        // 실제 데미지 반경: (기본 반경 × 배수) 와 최소값 중 더 큰 값
        float baseRadius = Mathf.Max(0.05f, P.explosionRadius);
        float radius = Mathf.Max(thunderMinRadius, baseRadius * thunderRadiusMultiplier);

        if (debugLog)
            Debug.Log($"[ThunderWeapon] 번개 생성 at {enemyPosition} " +
                      $"radius={radius} (base={baseRadius} × mul={thunderRadiusMultiplier}) " +
                      $"dmg={StatDamage} mask={enemyMask.value}");

        thunder.Strike(
            radius: radius,
            damage: StatDamage,
            mask: enemyMask
        );
    }

    /// <summary>
    /// 라운드 로빈 방식으로 번개 풀에서 꺼냅니다.
    /// </summary>
    private ThunderStrikeArea2D GetThunderFromPool()
    {
        if (_thunderPool == null || _thunderPool.Length == 0) return null;

        var t = _thunderPool[_thunderIndex];
        _thunderIndex = (_thunderIndex + 1) % _thunderPool.Length;
        return t;
    }
}