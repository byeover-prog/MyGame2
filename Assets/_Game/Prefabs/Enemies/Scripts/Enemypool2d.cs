// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 전용 오브젝트 풀. 프리팹별로 Queue를 관리한다.
/// EnemySpawner2D에서 Instantiate 대신 이 풀을 사용한다.
/// 
/// ★ 씬에 배치 불필요. EnemySpawner2D가 자동 생성/관리한다.
/// </summary>
public sealed class EnemyPool2D
{
    private readonly Dictionary<int, Queue<GameObject>> _pool
        = new Dictionary<int, Queue<GameObject>>();

    private readonly Transform _poolRoot;

    public EnemyPool2D(string rootName = "[EnemyPool]")
    {
        var go = new GameObject(rootName);
        go.SetActive(true);
        _poolRoot = go.transform;
    }

    /// <summary>풀에서 적을 꺼내거나 새로 생성한다.</summary>
    public GameObject Get(GameObject prefab, Vector2 pos, Quaternion rot, Transform parent)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();
        GameObject inst = null;

        if (_pool.TryGetValue(key, out var q))
            while (q.Count > 0)
            {
                inst = q.Dequeue();
                if (inst != null) break;
                inst = null;
            }

        if (inst == null)
        {
            inst = Object.Instantiate(prefab, pos, rot, parent);
            // 풀 키를 EnemyPoolTag로 저장
            var tag = inst.GetComponent<EnemyPoolTag>();
            if (tag == null) tag = inst.AddComponent<EnemyPoolTag>();
            tag.PoolKey = key;
            tag.Pool = this;
        }
        else
        {
            inst.transform.SetParent(parent, false);
            inst.transform.position = (Vector3)pos;
            inst.transform.rotation = rot;
            inst.SetActive(true);
        }

        return inst;
    }

    /// <summary>적을 풀로 반환한다. Destroy 대신 호출.</summary>
    public void Return(GameObject inst)
    {
        if (inst == null) return;

        var tag = inst.GetComponent<EnemyPoolTag>();
        if (tag == null)
        {
            // 풀 태그가 없으면 Destroy 폴백
            Object.Destroy(inst);
            return;
        }

        int key = tag.PoolKey;
        inst.SetActive(false);
        inst.transform.SetParent(_poolRoot, false);

        if (!_pool.ContainsKey(key))
            _pool[key] = new Queue<GameObject>();
        _pool[key].Enqueue(inst);
    }

    /// <summary>미리 생성.</summary>
    public void Prewarm(GameObject prefab, int count, Transform parent)
    {
        if (prefab == null || count <= 0) return;
        int key = prefab.GetInstanceID();
        if (!_pool.ContainsKey(key)) _pool[key] = new Queue<GameObject>();

        for (int i = 0; i < count; i++)
        {
            var inst = Object.Instantiate(prefab, _poolRoot);
            var tag = inst.GetComponent<EnemyPoolTag>();
            if (tag == null) tag = inst.AddComponent<EnemyPoolTag>();
            tag.PoolKey = key;
            tag.Pool = this;
            inst.SetActive(false);
            _pool[key].Enqueue(inst);
        }
    }

    public void ClearAll()
    {
        foreach (var kvp in _pool)
            while (kvp.Value.Count > 0)
            {
                var go = kvp.Value.Dequeue();
                if (go != null) Object.Destroy(go);
            }
        _pool.Clear();
    }
}