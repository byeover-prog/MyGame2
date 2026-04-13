// ──────────────────────────────────────────────
// ProjectilePool.cs
// 다중 프리팹 지원 오브젝트 풀 (WeaponShooterSystem2D 전용)
//
// 구현 원리:
//   프리팹별로 Queue<GameObject>를 Dictionary에 보관한다.
//   Get(prefab, pos, rot) → 해당 프리팹 큐에서 꺼내거나 새로 생성.
//   투사체가 비활성화되면 ReturnToPool()로 반납한다.
//
// ※ ProjectilePool2D(단일 프리팹 풀)와 역할이 다릅니다.
//   ProjectilePool2D는 CommonSkillWeapon2D 계열이 사용하고,
//   이 풀은 WeaponShooterSystem2D(WeaponDefinitionSO 계열)이 사용합니다.
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 프리팹을 하나의 풀에서 관리하는 다중 프리팹 풀.
/// WeaponShooterSystem2D에서 무기별 투사체 프리팹을 키로 사용한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class ProjectilePool : MonoBehaviour
{
    [Header("풀 설정")]
    [SerializeField, Tooltip("프리팹당 최대 보관 수")]
    private int maxPerPrefab = 200;

    /// <summary>프리팹 InstanceID → 비활성 큐</summary>
    private readonly Dictionary<int, Queue<GameObject>> _pools
        = new Dictionary<int, Queue<GameObject>>(16);

    // ════════════════════════════════════════════
    //  공개 API
    // ════════════════════════════════════════════

    /// <summary>
    /// 지정 프리팹을 count개 미리 생성하여 풀에 넣는다.
    /// </summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        var queue = GetOrCreateQueue(prefab);

        for (int i = 0; i < count; i++)
        {
            if (queue.Count >= maxPerPrefab) break;

            var go = CreateInstance(prefab);
            go.SetActive(false);
            queue.Enqueue(go);
        }
    }

    /// <summary>
    /// 풀에서 꺼내거나 새로 생성하여 위치/회전을 세팅한 뒤 반환한다.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        var queue = GetOrCreateQueue(prefab);
        GameObject go;

        // 풀에 재사용 가능한 오브젝트가 있는지 확인
        while (queue.Count > 0)
        {
            go = queue.Dequeue();

            // 씬 전환 등으로 파괴된 경우 스킵
            if (go == null) continue;

            go.transform.SetParent(null, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
            return go;
        }

        // 풀이 비었으면 새로 생성
        go = CreateInstance(prefab);
        go.transform.SetParent(null, false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        return go;
    }

    /// <summary>
    /// 사용 끝난 오브젝트를 풀로 반환한다.
    /// </summary>
    public void Return(GameObject go, GameObject prefab)
    {
        if (go == null) return;

        go.SetActive(false);
        go.transform.SetParent(transform, false);

        if (prefab == null)
        {
            Destroy(go);
            return;
        }

        var queue = GetOrCreateQueue(prefab);

        if (queue.Count >= maxPerPrefab)
        {
            Destroy(go);
            return;
        }

        queue.Enqueue(go);
    }

    // ════════════════════════════════════════════
    //  내부
    // ════════════════════════════════════════════

    private Queue<GameObject> GetOrCreateQueue(GameObject prefab)
    {
        int key = prefab.GetInstanceID();

        if (!_pools.TryGetValue(key, out var queue))
        {
            queue = new Queue<GameObject>(32);
            _pools[key] = queue;
        }

        return queue;
    }

    private GameObject CreateInstance(GameObject prefab)
    {
        var go = Instantiate(prefab, transform);
        go.name = prefab.name;
        return go;
    }
}