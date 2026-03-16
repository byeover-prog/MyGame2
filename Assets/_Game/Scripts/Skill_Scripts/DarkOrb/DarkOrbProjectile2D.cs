// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 암흑구 투사체.
/// [구현 원리 요약]
/// - 암흑구 렉의 핵심 원인은 투사체 몸통 VFX와 분열 후 폭발 VFX가 한 프레임에 몰리는 구조였다.
/// - 그래서 투사체는 스프라이트만 보이게 하고, 외주 VFX 컴포넌트는 코드에서 강제로 끈다.
/// - 자식 구체는 충돌을 끄고 수명 만료로만 분열하게 유지해서 물리 부하를 최소화한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class DarkOrbProjectile2D : MonoBehaviour
{
    [Header("분열 설정")]
    [SerializeField] private DarkOrbProjectile2D splitSpawnPrefab;
    [Range(1f, 89f)] [SerializeField] private float splitAngleDeg = 40f;
    [Min(0f)] [SerializeField] private float spawnEps = 0.4f;

    [Header("렉 방지")]
    [Tooltip("생성 직후 충돌 무시 시간(초). 연쇄 폭발 방지.")]
    [SerializeField] private float collisionGracePeriod = 0.05f;

    [Header("표현")]
    [Tooltip("암흑구 스프라이트 회전 속도(도/초)")]
    [SerializeField] private float rotateDegPerSec = 180f;

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

    private LayerMask _enemyMask;
    private int _damage;
    private float _speed;
    private float _life;
    private float _age;
    private Vector2 _dir;
    private float _explosionRadius;
    private int _splitCount;
    private float _splitSpeed;
    private float _splitLife;
    private int _splitDamage;
    private ProjectilePool2D _splitPool;
    private float _alpha = 0.55f;

    private int _depth = 1;
    private int _maxDepth = 1;
    private bool _isRoot;
    private FragmentBudget _budget;
    private bool _inited;
    private bool _exploding;

    private SpriteRenderer[] _cachedSprites;
    private Collider2D[] _cachedColliders;
    private bool _componentsCached;
    private readonly Collider2D[] _hits = new Collider2D[32];
    private ContactFilter2D _contactFilter;

    private static readonly Dictionary<int, Queue<DarkOrbProjectile2D>> _pool = new Dictionary<int, Queue<DarkOrbProjectile2D>>();
    private static Transform _poolRoot;
    private static Transform _inactiveRoot;
    private static bool _sceneHookRegistered;

    public static int ActiveCount { get; private set; }

    private int _poolKey;
    private DarkOrbProjectile2D _sourcePrefab;

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
        {
            while (kvp.Value.Count > 0)
            {
                var obj = kvp.Value.Dequeue();
                if (obj != null) Destroy(obj.gameObject);
            }
        }

        _pool.Clear();
        ActiveCount = 0;
    }

    public static DarkOrbProjectile2D Spawn(DarkOrbProjectile2D prefab, Vector2 pos, bool autoActivate = true)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();
        DarkOrbProjectile2D inst = null;

        if (_pool.TryGetValue(key, out var q))
        {
            while (q.Count > 0)
            {
                inst = q.Dequeue();
                if (inst != null) break;
                inst = null;
            }
        }

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
        inst.transform.position = pos;
        inst.transform.rotation = Quaternion.identity;

        if (autoActivate) inst.gameObject.SetActive(true);
        return inst;
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

    private void ReturnToPool()
    {
        _inited = false;
        _exploding = false;
        _age = 0f;
        ActiveCount = Mathf.Max(0, ActiveCount - 1);

        gameObject.SetActive(false);
        transform.SetParent(PoolRoot, false);

        if (!_pool.ContainsKey(_poolKey))
            _pool[_poolKey] = new Queue<DarkOrbProjectile2D>();

        _pool[_poolKey].Enqueue(this);
    }

    private void CacheComponents()
    {
        if (_componentsCached) return;

        _cachedSprites = GetComponentsInChildren<SpriteRenderer>(true);
        _cachedColliders = GetComponentsInChildren<Collider2D>(true);

        // 외주 VFX는 암흑구 몸통/폭발 잔상과 스파이크의 핵심 원인이므로 강제로 끈다.
        var legacyVfx = GetComponent<ProjectileVFXChild>();
        if (legacyVfx != null)
        {
            legacyVfx.SetVFXEnabled(false, false);
            legacyVfx.enabled = false;
        }

        ForceSpritesEnabled(true);
        _componentsCached = true;
    }

    public void Init(
        LayerMask enemyMask,
        int damage,
        float speed,
        float lifeSeconds,
        Vector2 dir,
        float explosionRadius,
        int splitCount,
        float splitSpeed,
        float splitLifeSeconds,
        int splitDamage,
        ProjectilePool2D splitPool,
        float orbAlpha)
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
        _budget = new FragmentBudget { remaining = CalcMaxFragments(_maxDepth) };

        _contactFilter = new ContactFilter2D();
        _contactFilter.SetLayerMask(_enemyMask);
        _contactFilter.useTriggers = true;

        ApplyAlpha(_alpha);
        ForceSpritesEnabled(true);
        SetCollidersEnabled(true);

        _inited = true;
        _exploding = false;
        ActiveCount++;
    }

    private void InitAsChild(
        LayerMask enemyMask,
        int damage,
        float speed,
        float life,
        Vector2 dir,
        float explosionRadius,
        float splitSpeed,
        float splitLife,
        int splitDamage,
        ProjectilePool2D splitPool,
        float alpha,
        int depth,
        int maxDepth,
        FragmentBudget sharedBudget)
    {
        _enemyMask = enemyMask;
        _damage = Mathf.Max(1, damage);
        _speed = Mathf.Max(0.1f, speed);
        _life = Mathf.Max(0.05f, life);
        _age = 0f;
        _dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        _explosionRadius = Mathf.Max(0.05f, explosionRadius);
        _splitCount = 0;
        _splitSpeed = Mathf.Max(0.1f, splitSpeed);
        _splitLife = Mathf.Max(0.05f, splitLife);
        _splitDamage = Mathf.Max(0, splitDamage);
        _splitPool = splitPool;
        _alpha = Mathf.Clamp01(alpha);

        _depth = depth;
        _maxDepth = maxDepth;
        _isRoot = false;
        _budget = sharedBudget;

        _contactFilter = new ContactFilter2D();
        _contactFilter.SetLayerMask(_enemyMask);
        _contactFilter.useTriggers = true;

        ApplyAlpha(_alpha);
        ForceSpritesEnabled(true);

        // 자식은 충돌을 완전히 꺼서 물리 부하를 줄인다.
        SetCollidersEnabled(false);

        _inited = true;
        _exploding = false;
        ActiveCount++;
    }

    private void ForceSpritesEnabled(bool enabled)
    {
        if (_cachedSprites == null) return;

        for (int i = 0; i < _cachedSprites.Length; i++)
        {
            if (_cachedSprites[i] != null)
                _cachedSprites[i].enabled = enabled;
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (!_componentsCached) CacheComponents();

        for (int i = 0; i < _cachedColliders.Length; i++)
        {
            if (_cachedColliders[i] != null)
                _cachedColliders[i].enabled = enabled;
        }
    }

    private void ApplyAlpha(float a)
    {
        if (!_componentsCached) CacheComponents();

        for (int i = 0; i < _cachedSprites.Length; i++)
        {
            var sr = _cachedSprites[i];
            if (sr == null) continue;

            sr.enabled = true;
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }

    private static int SplitCountToMaxDepth(int splitCount)
    {
        if (splitCount <= 0) return 1;
        if (splitCount <= 2) return 2;
        return 3;
    }

    private static int CalcMaxFragments(int maxDepth)
    {
        int total = 0;
        for (int d = 2; d <= maxDepth; d++)
            total += 1 << (d - 1);
        return total;
    }

    private void Update()
    {
        if (!_inited) return;

        float dt = Time.deltaTime;
        _age += dt;
        transform.position += (Vector3)(_dir * (_speed * dt));

        if (rotateDegPerSec != 0f)
            transform.Rotate(0f, 0f, rotateDegPerSec * dt);

        if (_age >= _life)
            Explode(transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_inited || other == null) return;
        if (_age < collisionGracePeriod) return;

        if (((1 << other.gameObject.layer) & _enemyMask.value) != 0)
            Explode(transform.position);
    }

    private void Explode(Vector2 pos)
    {
        if (!_inited || _exploding) return;
        _exploding = true;

        if (_isRoot)
        {
            int count = Physics2D.OverlapCircle(pos, _explosionRadius, _contactFilter, _hits);
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit == null) continue;
                DamageUtil2D.ApplyDamage(hit, _damage);
            }
        }

        if (_depth < _maxDepth && _budget != null && _budget.TryConsume(2))
        {
            Vector2 dirA = Rotate(_dir, +splitAngleDeg).normalized;
            Vector2 dirB = Rotate(_dir, -splitAngleDeg).normalized;
            SpawnChild(pos + dirA * spawnEps, dirA, _depth + 1, _maxDepth);
            SpawnChild(pos + dirB * spawnEps, dirB, _depth + 1, _maxDepth);
        }

        ReturnToPool();
    }

    private void SpawnChild(Vector2 pos, Vector2 dir, int childDepth, int maxDepth)
    {
        var child = Spawn(_sourcePrefab, pos, autoActivate: false);
        if (child == null) return;

        int childDamage = _splitDamage > 0 ? _splitDamage : _damage;

        child.InitAsChild(
            _enemyMask,
            childDamage,
            _splitSpeed,
            _splitLife,
            dir,
            _explosionRadius,
            _splitSpeed,
            _splitLife,
            0,
            _splitPool,
            _alpha,
            childDepth,
            maxDepth,
            _budget);

        child.gameObject.SetActive(true);
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}