// UTF-8
// [구현 원리 요약]
// - 구형 ProjectilePool 타입이 남아 있는 파일이 깨지지 않도록 호환 풀을 제공한다.
// - 프리팹별 큐를 따로 두어 재사용하고, IPoolable/IPooledProjectile2D를 함께 지원한다.
// - Release 시 이미 비활성인 오브젝트는 무시하여 중복 반납 버그를 방지한다.
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프리팹별 투사체 오브젝트 풀.
/// Get으로 꺼내고 Release로 반납한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class ProjectilePool : MonoBehaviour
{
    [System.Serializable]
    private sealed class PoolBucket
    {
        public GameObject prefab;
        public readonly Queue<GameObject> queue = new Queue<GameObject>(64);
    }

    [Header("디버그")]
    [Tooltip("풀 동작 로그 출력 여부")]
    [SerializeField] private bool debugLog = false;

    private readonly Dictionary<int, PoolBucket> _buckets = new Dictionary<int, PoolBucket>(16);

    /// <summary>지정한 프리팹을 미리 생성해서 풀에 넣어둔다.</summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null) return;

        count = Mathf.Clamp(count, 0, 2048);
        PoolBucket bucket = GetOrCreateBucket(prefab);

        for (int i = 0; i < count; i++)
        {
            GameObject go = CreateNew(prefab, bucket);
            go.SetActive(false);
            bucket.queue.Enqueue(go);
        }

        if (debugLog)
            Debug.Log($"[ProjectilePool] Prewarm {prefab.name} x{count}", this);
    }

    /// <summary>풀에서 오브젝트를 꺼낸다. 없으면 새로 생성한다.</summary>
    public GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;

        PoolBucket bucket = GetOrCreateBucket(prefab);
        GameObject go = null;

        // 큐에서 유효한 오브젝트를 찾을 때까지 꺼낸다
        while (bucket.queue.Count > 0)
        {
            var candidate = bucket.queue.Dequeue();
            if (candidate != null)
            {
                go = candidate;
                break;
            }
        }

        if (go == null)
            go = CreateNew(prefab, bucket);

        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);

        if (go.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnPoolGet();

        return go;
    }

    /// <summary>
    /// 오브젝트를 풀에 반납한다.
    /// 이미 비활성인 오브젝트는 무시하여 중복 반납을 방지한다.
    /// </summary>
    public void Release(GameObject prefabKey, GameObject instance)
    {
        if (prefabKey == null || instance == null) return;

        // ── 중복 반납 가드 ──
        if (!instance.activeSelf) return;

        PoolBucket bucket = GetOrCreateBucket(prefabKey);

        if (instance.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnPoolRelease();

        instance.SetActive(false);
        bucket.queue.Enqueue(instance);
    }

    private PoolBucket GetOrCreateBucket(GameObject prefab)
    {
        int key = prefab.GetInstanceID();

        if (_buckets.TryGetValue(key, out PoolBucket bucket))
            return bucket;

        bucket = new PoolBucket { prefab = prefab };
        _buckets.Add(key, bucket);
        return bucket;
    }

    private GameObject CreateNew(GameObject prefab, PoolBucket bucket)
    {
        GameObject go = Instantiate(prefab, transform);
        go.name = prefab.name;

        if (go.TryGetComponent<IPoolable>(out var poolable))
            poolable.BindPool(this, prefab);

        if (go.TryGetComponent<IPooledProjectile2D>(out var projectile))
        {
            projectile.SetPool(this);
            projectile.SetOriginPrefab(prefab);
        }

        if (debugLog)
            Debug.Log($"[ProjectilePool] CreateNew {prefab.name}", this);

        return go;
    }
}
