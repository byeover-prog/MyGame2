// UTF-8
// Assets/_Game/Scripts/Pool_Scripts/ProjectilePool2D.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 범용 투사체 오브젝트 풀.
/// 모든 무기(Arrow, Boomerang, Musket, Shuriken, Homing 등)가 공유한다.
/// 프리팹별 Dictionary + Queue 기반 풀링.
///
/// PooledObject2D와 연동:
///   - CreateNew() → PooledObject2D.BindPool(this)
///   - Get()       → PooledObject2D.ClearReturningFlag()
///   - Return()    → 비활성화 + 큐 반납
/// </summary>
public class ProjectilePool2D : MonoBehaviour
{
    [Header("풀 설정")]
    [Tooltip("풀링할 투사체 프리팹")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("초기 프리웜 개수")]
    [SerializeField] private int prewarmCount = 10;

    [Tooltip("디버그 로그 출력 여부")]
    [SerializeField] private bool debugLog = false;

    // ── 내부 풀 ──────────────────────────────────────────

    private class PoolBucket
    {
        public GameObject prefab;
        public readonly Queue<GameObject> queue = new Queue<GameObject>();
    }

    private readonly Dictionary<int, PoolBucket> _buckets = new Dictionary<int, PoolBucket>();

    // ═══════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════

    private void Start()
    {
        if (projectilePrefab != null && prewarmCount > 0)
        {
            Prewarm(projectilePrefab, prewarmCount);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  공개 API — Get
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 풀에서 투사체를 꺼낸다. 없으면 새로 생성한다.
    /// </summary>
    public GameObject Get(Vector3 pos, Quaternion rot)
    {
        return Get(projectilePrefab, pos, rot);
    }

    /// <summary>
    /// 특정 프리팹 기준으로 풀에서 투사체를 꺼낸다.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;

        PoolBucket bucket = GetOrCreateBucket(prefab);
        GameObject go = null;

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

        // PooledObject2D 재사용 플래그 초기화
        if (go.TryGetComponent<PooledObject2D>(out var pooled))
            pooled.ClearReturningFlag();

        go.SetActive(true);

        return go;
    }

    /// <summary>
    /// 제네릭 Get. 특정 컴포넌트 타입으로 바로 반환한다.
    /// </summary>
    public T Get<T>(Vector3 pos, Quaternion rot) where T : Component
    {
        GameObject go = Get(pos, rot);
        if (go == null) return null;
        return go.GetComponent<T>();
    }

    /// <summary>
    /// 특정 프리팹 기준 제네릭 Get.
    /// </summary>
    public T Get<T>(GameObject prefab, Vector3 pos, Quaternion rot) where T : Component
    {
        GameObject go = Get(prefab, pos, rot);
        if (go == null) return null;
        return go.GetComponent<T>();
    }

    // ═══════════════════════════════════════════════════════
    //  공개 API — Return / Release
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// PooledObject2D에서 호출하는 반납 메서드.
    /// PooledObject2D.ReturnToPool() → _pool.Return(this) 경로.
    /// </summary>
    public void Return(PooledObject2D obj)
    {
        if (obj == null) return;

        GameObject go = obj.gameObject;

        // 이미 비활성이면 중복 반납 방지
        if (!go.activeSelf) return;

        go.SetActive(false);

        // 기본 프리팹 키로 반납
        PoolBucket bucket = GetOrCreateBucket(projectilePrefab);
        bucket.queue.Enqueue(go);
    }

    /// <summary>
    /// GameObject를 직접 반납한다. (기본 프리팹 키 사용)
    /// </summary>
    public void Release(GameObject instance)
    {
        Release(projectilePrefab, instance);
    }

    /// <summary>
    /// 특정 프리팹 키 기준으로 반납한다.
    /// </summary>
    public void Release(GameObject prefabKey, GameObject instance)
    {
        if (prefabKey == null || instance == null) return;
        if (!instance.activeSelf) return;

        PoolBucket bucket = GetOrCreateBucket(prefabKey);
        instance.SetActive(false);
        bucket.queue.Enqueue(instance);
    }

    // ═══════════════════════════════════════════════════════
    //  공개 API — Prewarm / 프리팹 접근
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 지정된 프리팹을 count만큼 미리 생성한다.
    /// </summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        PoolBucket bucket = GetOrCreateBucket(prefab);

        for (int i = 0; i < count; i++)
        {
            GameObject go = CreateNew(prefab, bucket);
            go.SetActive(false);
            bucket.queue.Enqueue(go);
        }

        if (debugLog)
            Debug.Log($"[ProjectilePool2D] Prewarm {prefab.name} x{count}", this);
    }

    /// <summary>기본 프리팹 참조를 반환한다.</summary>
    public GameObject Prefab => projectilePrefab;

    /// <summary>
    /// 기본 프리팹 참조를 외부에서 교체한다.
    /// WeaponVFXBinder 등에서 VFX가 주입된 프리팹으로 교체 시 사용.
    /// </summary>
    public void SetPrefab(GameObject newPrefab)
    {
        projectilePrefab = newPrefab;
    }

    // ═══════════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════════

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

        // PooledObject2D 바인딩 (ReturnToPool() → this.Return() 경로 연결)
        if (go.TryGetComponent<PooledObject2D>(out var pooled))
            pooled.BindPool(this);

        if (debugLog)
            Debug.Log($"[ProjectilePool2D] CreateNew {prefab.name}", this);

        return go;
    }
}