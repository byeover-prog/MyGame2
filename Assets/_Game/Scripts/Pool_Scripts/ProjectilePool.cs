using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProjectilePool : MonoBehaviour
{
    [Header("기본 프리팹(선택)")]
    [SerializeField] private GameObject defaultPrefab;

    [Header("프리웜(기본 프리팹용)")]
    [Min(0)]
    [SerializeField] private int prewarmCount = 32;

    [Tooltip("풀에 반납된 오브젝트를 정리할 부모(비우면 자기 Transform)")]
    [SerializeField] private Transform poolRoot;

    [Header("프리팹별 최대 보관 개수(0=무제한)")]
    [Min(0)]
    [SerializeField] private int maxPerPrefab = 0;

    private sealed class PoolBucket
    {
        public readonly Queue<GameObject> queue = new Queue<GameObject>(32);
        public int totalCreated;
    }

    private readonly Dictionary<int, PoolBucket> _buckets = new Dictionary<int, PoolBucket>(32);

    private void Awake()
    {
        if (poolRoot == null) poolRoot = transform;

        if (defaultPrefab != null && prewarmCount > 0)
        {
            Prewarm(defaultPrefab, prewarmCount);
        }
    }

    public void Prewarm(int count)
    {
        if (defaultPrefab == null) return;
        Prewarm(defaultPrefab, count);
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        var bucket = GetOrCreateBucket(prefab);
        for (int i = 0; i < count; i++)
        {
            var inst = CreateNew(prefab, bucket);
            ReleaseInternal(prefab, inst);
        }
    }

    public GameObject Get()
    {
        if (defaultPrefab == null)
        {
            Debug.LogError("[ProjectilePool] defaultPrefab이 비어있습니다.", this);
            return null;
        }

        return Get(defaultPrefab, Vector3.zero, Quaternion.identity);
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        var bucket = GetOrCreateBucket(prefab);
        GameObject inst = (bucket.queue.Count > 0) ? bucket.queue.Dequeue() : CreateNew(prefab, bucket);
        if (inst == null) return null;

        var t = inst.transform;
        t.SetParent(null, false);
        t.SetPositionAndRotation(position, rotation);

        // 풀 상태 마킹
        if (inst.TryGetComponent(out PooledObject po))
        {
            po.MarkOutOfPool();
        }

        // 2D 투사체 호환용 바인딩 (활성화 전에 주입)
        if (inst.TryGetComponent(out IPooledProjectile2D pooled2d))
        {
            pooled2d.SetPool(this);
            pooled2d.SetOriginPrefab(prefab);
        }

        if (inst.TryGetComponent(out Projectile2D proj2d))
        {
            proj2d.BindPool(this, prefab);
        }

        inst.SetActive(true);

        // 꺼낼 때 콜백
        if (inst.TryGetComponent(out IPoolable poolable))
        {
            poolable.OnPoolGet();
        }

        return inst;
    }

    // 호환용 공개 API: 기존 투사체 코드(_pool.Release(prefabKey, gameObject))를 살리기 위해 제공
    public void Release(GameObject prefabKey, GameObject instance)
    {
        ReleaseInternal(prefabKey, instance);
    }

    // 외부는 절대 이걸 호출하지 말고, IPoolable.ReleaseToPool()만 사용하도록 설계 (기존 설계 유지)
    internal void ReleaseInternal(GameObject prefabKey, GameObject instance)
    {
        if (prefabKey == null || instance == null) return;

        var bucket = GetOrCreateBucket(prefabKey);

        // 상한이 있고 초과면 파괴(무한 증가 방지)
        if (maxPerPrefab > 0 && bucket.queue.Count >= maxPerPrefab)
        {
            Destroy(instance);
            return;
        }

        // 반납 콜백
        if (instance.TryGetComponent(out IPoolable poolable))
        {
            poolable.OnPoolRelease();
        }

        // 풀 상태 마킹
        if (instance.TryGetComponent(out PooledObject po))
        {
            po.MarkInPool();
        }

        instance.SetActive(false);

        var t = instance.transform;
        t.SetParent(poolRoot, false);

        bucket.queue.Enqueue(instance);
    }

    private PoolBucket GetOrCreateBucket(GameObject prefab)
    {
        int id = prefab.GetInstanceID();
        if (!_buckets.TryGetValue(id, out var bucket))
        {
            bucket = new PoolBucket();
            _buckets.Add(id, bucket);
        }
        return bucket;
    }

    private GameObject CreateNew(GameObject prefab, PoolBucket bucket)
    {
        // 경고: Weapon_XXX 프리팹을 잘못 넣어서 Weapon_Arrow(Clone) 같은 게 계속 생기는 사고 방지용
        if (prefab.name.StartsWith("Weapon_"))
        {
            Debug.LogWarning($"[ProjectilePool] 투사체 프리팹에 무기 프리팹이 들어간 것 같습니다: {prefab.name}", prefab);
        }

        var inst = Instantiate(prefab, poolRoot);
        inst.SetActive(false);
        bucket.totalCreated++;

        // 바인딩(기존 설계 유지): IPoolable 기반이면 생성 시점에 풀/키 주입
        if (inst.TryGetComponent(out IPoolable poolable))
        {
            poolable.BindPool(this, prefab);
        }

        // PooledObject 기반이면 상태 마킹
        if (inst.TryGetComponent(out PooledObject po))
        {
            po.MarkInPool();
        }

        return inst;
    }
}
