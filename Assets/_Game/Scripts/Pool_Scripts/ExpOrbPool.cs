using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ExpOrb 전용 풀
/// - Release(prefab, instance) 방식(ProjectilePool과 동일 컨셉)
/// - ExpOrb2D가 SetPool(this), SetOriginPrefab(prefabKey)로 반납에 필요한 정보를 가짐
/// </summary>
[DisallowMultipleComponent]
public sealed class ExpOrbPool : MonoBehaviour
{
    [System.Serializable]
    private sealed class PoolBucket
    {
        public GameObject prefab;
        public readonly Queue<GameObject> queue = new Queue<GameObject>(64);
    }

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    // prefab instanceID -> bucket
    private readonly Dictionary<int, PoolBucket> _buckets = new Dictionary<int, PoolBucket>(16);

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
            GameLogger.Log($"[ExpOrbPool] Prewarm {prefab.name} x{count}", this);
    }

    public GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;

        PoolBucket bucket = GetOrCreateBucket(prefab);

        GameObject go = (bucket.queue.Count > 0) ? bucket.queue.Dequeue() : CreateNew(prefab, bucket);

        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return go;
    }

    public void Release(GameObject prefabKey, GameObject instance)
    {
        if (prefabKey == null || instance == null) return;

        PoolBucket bucket = GetOrCreateBucket(prefabKey);

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
        go.name = prefab.name; // (Clone) 붙는 것 방지(가독성)

        // 풀 세팅 주입
        if (go.TryGetComponent(out ExpOrb2D orb))
        {
            orb.SetPool(this);
            orb.SetOriginPrefab(prefab);
        }

        if (debugLog)
            GameLogger.Log($"[ExpOrbPool] CreateNew {prefab.name}", this);

        return go;
    }
}
