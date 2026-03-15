// UTF-8
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 암흑구 투사체 (분열 + 폭발)
/// 
/// [최적화 5가지 통합]
/// 1. FragmentBudget: 트리 전체 분열 총량 제한 (class 참조 공유)
/// 2. collisionGracePeriod: 생성 직후 충돌 무시 (연쇄 폭발 렉 방지)
/// 3. 자식 분열은 물리 탐색 생략 (Root만 OverlapCircle)
/// 4. Static 오브젝트 풀 (Instantiate/Destroy → Get/Return)
/// 5. 로그는 UNITY_EDITOR에서만 출력
/// </summary>
[DisallowMultipleComponent]
public sealed class DarkOrbProjectile2D : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // Inspector
    // ══════════════════════════════════════════════════════

    [Header("분열 설정")]
    [SerializeField] private DarkOrbProjectile2D splitSpawnPrefab;

    [Range(1f, 89f)]
    [SerializeField] private float splitAngleDeg = 40f;

    [Min(0f)]
    [SerializeField] private float spawnEps = 0.4f;

    [Header("렉 방지")]
    [Tooltip("생성 직후 이 시간(초) 동안 충돌 무시.\n적 몸 안에서 즉시 연쇄 폭발하는 렉을 방지.")]
    [SerializeField] private float collisionGracePeriod = 0.05f;

    // ══════════════════════════════════════════════════════
    // FragmentBudget (트리 전체 공유)
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 하나의 Root 투사체에서 파생되는 모든 자식이 같은 인스턴스를 공유.
    /// remaining이 0이면 더 이상 분열 불가.
    /// </summary>
    public sealed class FragmentBudget
    {
        public int remaining;

        public bool TryConsume(int amount)
        {
            if (remaining < amount) return false;
            remaining -= amount;
            return true;
        }
    }

    // ══════════════════════════════════════════════════════
    // 런타임 파라미터
    // ══════════════════════════════════════════════════════

    private LayerMask _enemyMask;
    private int _damage;
    private float _speed, _life, _age;
    private Vector2 _dir;
    private float _explosionRadius;
    private int _splitCount;
    private float _splitSpeed, _splitLife;
    private int _splitDamage;
    private ProjectilePool2D _splitPool;
    private float _alpha = 0.55f;

    private int _depth = 1;
    private int _maxDepth = 1;
    private bool _isRoot;           // depth==1 이면 true
    private FragmentBudget _budget;  // 트리 전체 공유

    private bool _inited, _exploding;

    // 캐싱
    private SpriteRenderer[] _cachedSprites;
    private bool _componentsCached;
    private readonly Collider2D[] _hits = new Collider2D[32];
    private ContactFilter2D _contactFilter;

    // ══════════════════════════════════════════════════════
    // Static 풀 + 동시 활성 카운트
    // ══════════════════════════════════════════════════════

    private static readonly Dictionary<int, Queue<DarkOrbProjectile2D>> _pool
        = new Dictionary<int, Queue<DarkOrbProjectile2D>>();
    private static Transform _poolRoot;
    private static Transform _inactiveRoot;
    private static bool _sceneHookRegistered;

    /// <summary>현재 씬에 활성화된 DarkOrb 총 수 (Root + 자식 포함)</summary>
    public static int ActiveCount { get; private set; }

    private int _poolKey;
    private DarkOrbProjectile2D _sourcePrefab;

    // ── 풀 인프라 ─────────────────────────────────────────

    private static Transform PoolRoot
    {
        get
        {
            if (_poolRoot == null)
            {
                var go = new GameObject("[DarkOrbPool]");
                DontDestroyOnLoad(go);
                _poolRoot = go.transform;
            }
            if (!_sceneHookRegistered)
            {
                UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
                _sceneHookRegistered = true;
            }
            return _poolRoot;
        }
    }

    private static Transform InactiveRoot
    {
        get
        {
            if (_inactiveRoot == null)
            {
                var go = new GameObject("[DarkOrbInactiveRoot]");
                go.SetActive(false);
                DontDestroyOnLoad(go);
                _inactiveRoot = go.transform;
            }
            return _inactiveRoot;
        }
    }

    private static void OnSceneUnloaded(UnityEngine.SceneManagement.Scene _)
    {
        foreach (var kvp in _pool)
            while (kvp.Value.Count > 0)
            {
                var obj = kvp.Value.Dequeue();
                if (obj != null) Destroy(obj.gameObject);
            }
        _pool.Clear();
        ActiveCount = 0;
    }

    // ── Spawn / Return / Prewarm ──────────────────────────

    public static DarkOrbProjectile2D Spawn(DarkOrbProjectile2D prefab,
                                             Vector2 pos, bool autoActivate = true)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();
        DarkOrbProjectile2D inst = null;

        if (_pool.TryGetValue(key, out var q))
            while (q.Count > 0) { inst = q.Dequeue(); if (inst != null) break; inst = null; }

        if (inst == null)
        {
            inst = Instantiate(prefab, InactiveRoot);
            inst.gameObject.SetActive(false);
            inst.name = prefab.name;
            inst.CacheComponents();
        }

        inst._poolKey = key;
        inst._sourcePrefab = prefab;
        inst.transform.SetParent(null, false);
        inst.transform.position = (Vector3)pos;
        inst.transform.rotation = Quaternion.identity;

        if (autoActivate)
            inst.gameObject.SetActive(true);

        return inst;
    }

    private void ReturnToPool()
    {
        _inited = false;
        ActiveCount--;
        gameObject.SetActive(false);
        transform.SetParent(PoolRoot, false);

        if (!_pool.ContainsKey(_poolKey))
            _pool[_poolKey] = new Queue<DarkOrbProjectile2D>();
        _pool[_poolKey].Enqueue(this);
    }

    public static void Prewarm(DarkOrbProjectile2D prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        int key = prefab.GetInstanceID();
        if (!_pool.ContainsKey(key)) _pool[key] = new Queue<DarkOrbProjectile2D>();

        for (int i = 0; i < count; i++)
        {
            var inst = Instantiate(prefab, InactiveRoot);
            inst.gameObject.SetActive(false);
            inst.name = prefab.name;
            inst._poolKey = key;
            inst._sourcePrefab = prefab;
            inst.CacheComponents();
            inst.transform.SetParent(PoolRoot, false);
            _pool[key].Enqueue(inst);
        }
    }

    private void CacheComponents()
    {
        if (_componentsCached) return;
        _cachedSprites = GetComponentsInChildren<SpriteRenderer>(true);
        _componentsCached = true;
    }

    // ══════════════════════════════════════════════════════
    // Init (시그니처 동일)
    // ══════════════════════════════════════════════════════

    public void Init(
        LayerMask enemyMask, int damage, float speed, float lifeSeconds,
        Vector2 dir, float explosionRadius, int splitCount,
        float splitSpeed, float splitLifeSeconds, int splitDamage,
        ProjectilePool2D splitPool, float orbAlpha)
    {
        _enemyMask = enemyMask;
        _damage = Mathf.Max(1, damage);
        _speed = Mathf.Max(0.1f, speed);
        _life = Mathf.Max(0.05f, lifeSeconds);
        _age = 0f;
        _dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        _explosionRadius = Mathf.Max(0.05f, explosionRadius);
        _splitCount = Mathf.Max(0, splitCount);
        _splitSpeed = Mathf.Max(0.1f, splitSpeed);
        _splitLife = Mathf.Max(0.05f, splitLifeSeconds);
        _splitDamage = Mathf.Max(0, splitDamage);
        _splitPool = splitPool;
        _alpha = Mathf.Clamp01(orbAlpha);

        _depth = 1;
        _maxDepth = SplitCountToMaxDepth(_splitCount);
        _isRoot = true;

        // ★ Root가 FragmentBudget 생성 (maxDepth 기반 총량 계산)
        //   depth=2 → 최대 2, depth=3 → 최대 6, depth=4 → 최대 14
        int maxFragments = CalcMaxFragments(_maxDepth);
        _budget = new FragmentBudget { remaining = maxFragments };

        // Unity 6 물리 필터
        _contactFilter = new ContactFilter2D();
        _contactFilter.SetLayerMask(_enemyMask);
        _contactFilter.useTriggers = true;

        ApplyAlpha(_alpha);
        _inited = true;
        _exploding = false;
        ActiveCount++;
    }

    /// <summary>트리 깊이에 따른 최대 분열체 수 계산 (Root 제외)</summary>
    private static int CalcMaxFragments(int maxDepth)
    {
        // depth=2: 2개, depth=3: 2+4=6개
        int total = 0;
        for (int d = 2; d <= maxDepth; d++)
            total += 1 << (d - 1); // 2^(d-1)
        return total;
    }

    // ── 자식용 초기화 (외부 호출 금지) ────────────────────

    private void InitAsChild(
        LayerMask enemyMask, int damage, float speed, float life,
        Vector2 dir, float explosionRadius,
        float splitSpeed, float splitLife, int splitDamage,
        ProjectilePool2D splitPool, float alpha,
        int depth, int maxDepth, FragmentBudget sharedBudget)
    {
        _enemyMask = enemyMask;
        _damage = damage;
        _speed = speed;
        _life = life;
        _age = 0f;
        _dir = dir;
        _explosionRadius = explosionRadius;
        _splitCount = 0;
        _splitSpeed = splitSpeed;
        _splitLife = splitLife;
        _splitDamage = splitDamage;
        _splitPool = splitPool;
        _alpha = alpha;

        _depth = depth;
        _maxDepth = maxDepth;
        _isRoot = false;
        _budget = sharedBudget; // ★ 같은 인스턴스 공유

        _contactFilter = new ContactFilter2D();
        _contactFilter.SetLayerMask(_enemyMask);
        _contactFilter.useTriggers = true;

        ApplyAlpha(_alpha);
        _inited = true;
        _exploding = false;
        ActiveCount++;
    }

    private void SetTreeDepth(int d, int md)
    {
        _depth = Mathf.Max(1, d);
        _maxDepth = Mathf.Max(1, md);
    }

    /// <summary>
    /// splitCount → maxDepth 변환.
    /// ★ [GPT §3] maxDepth 상한=3 (1→2→4 = 최대 7개).
    ///   maxDepth=4 (15개)는 연산 폭증의 근본 원인이므로 차단.
    /// </summary>
    private static int SplitCountToMaxDepth(int sc)
    {
        if (sc <= 0) return 1;  // 분열 없음
        if (sc <= 2) return 2;  // 1→2 = 3개
        return 3;               // 1→2→4 = 7개 (상한)
    }

    private void ApplyAlpha(float a)
    {
        if (!_componentsCached) CacheComponents();
        for (int i = 0; i < _cachedSprites.Length; i++)
        {
            var sr = _cachedSprites[i];
            if (sr == null) continue;
            var c = sr.color; c.a = a; sr.color = c;
        }
    }

    // ══════════════════════════════════════════════════════
    // Update / Trigger
    // ══════════════════════════════════════════════════════

    private void Update()
    {
        if (!_inited) return;
        _age += Time.deltaTime;
        transform.position += (Vector3)(_dir * (_speed * Time.deltaTime));
        if (_age >= _life) Explode((Vector2)transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_inited || other == null) return;

        // ★ [Gemini 지적] 생성 직후 유예 시간: 적 몸 안에서 즉시 연쇄 폭발 방지
        if (_age < collisionGracePeriod) return;

        if (((1 << other.gameObject.layer) & _enemyMask.value) != 0)
            Explode((Vector2)transform.position);
    }

    // ══════════════════════════════════════════════════════
    // Explode
    // ══════════════════════════════════════════════════════

    private void Explode(Vector2 pos)
    {
        if (!_inited || _exploding) return;
        _exploding = true;

        // ★ [지시문 §2] Root만 물리 탐색. 자식은 분열만.
        if (_isRoot)
        {
            int count = Physics2D.OverlapCircle(
                pos, _explosionRadius, _contactFilter, _hits);

            for (int i = 0; i < count; i++)
            {
                var h = _hits[i];
                if (h == null) continue;
                DamageUtil2D.ApplyDamage(h, _damage);
            }
        }

        // 분열 (Budget 체크 포함)
        if (_depth < _maxDepth)
        {
            // ★ [지시문 §1] FragmentBudget으로 총량 제한
            if (_budget != null && _budget.TryConsume(2))
            {
                Vector2 dirA = Rotate(_dir, +splitAngleDeg).normalized;
                Vector2 dirB = Rotate(_dir, -splitAngleDeg).normalized;
                SpawnChild(pos + dirA * spawnEps, dirA, _depth + 1, _maxDepth);
                SpawnChild(pos + dirB * spawnEps, dirB, _depth + 1, _maxDepth);
            }
        }

        ReturnToPool();
    }

    // ── 분열 자식 생성 ────────────────────────────────────

    private void SpawnChild(Vector2 pos, Vector2 d, int childDepth, int maxDepth)
    {
        var child = Spawn(_sourcePrefab, pos, autoActivate: false);
        if (child == null) return;

        int childDmg = (_splitDamage > 0) ? _splitDamage : _damage;

        // ★ InitAsChild: 자식 전용 초기화 (Budget 공유, isRoot=false)
        child.InitAsChild(
            _enemyMask, childDmg, _splitSpeed, _splitLife, d,
            _explosionRadius, _splitSpeed, _splitLife, 0,
            _splitPool, _alpha,
            childDepth, maxDepth, _budget);

        child.gameObject.SetActive(true);
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad); float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}