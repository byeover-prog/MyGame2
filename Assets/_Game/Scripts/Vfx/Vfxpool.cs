using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// VFX 전용 오브젝트 풀. static 유틸이므로 씬 배치 불필요.
/// </summary>
public static class VFXPool
{
    private static readonly Dictionary<int, Queue<GameObject>> _pool
        = new Dictionary<int, Queue<GameObject>>();

    private static Transform _poolRoot;
    private static bool _initialized;

    private static Transform PoolRoot
    {
        get
        {
            if (_poolRoot == null)
            {
                var go = new GameObject("[VFXPool]");
                Object.DontDestroyOnLoad(go);
                _poolRoot = go.transform;
                if (!_initialized)
                {
                    SceneManager.sceneUnloaded += _ => ClearAll();
                    _initialized = true;
                }
            }
            return _poolRoot;
        }
    }

    public static GameObject Get(GameObject prefab, Vector3 position,
                                  Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;
        int key = prefab.GetInstanceID();
        GameObject inst = null;

        if (_pool.TryGetValue(key, out var q))
            while (q.Count > 0) { inst = q.Dequeue(); if (inst != null) break; inst = null; }

        if (inst == null) { inst = Object.Instantiate(prefab); inst.name = prefab.name + "_vfx"; }

        if (parent != null)
        { inst.transform.SetParent(parent, false); inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.identity; }
        else
        { inst.transform.SetParent(null); inst.transform.position = position; inst.transform.rotation = rotation; }

        inst.SetActive(true);
        return inst;
    }

    public static void Return(GameObject prefab, GameObject inst)
    {
        if (inst == null || prefab == null) return;
        int key = prefab.GetInstanceID();
        inst.SetActive(false);
        inst.transform.SetParent(PoolRoot, false);
        if (!_pool.ContainsKey(key)) _pool[key] = new Queue<GameObject>();
        _pool[key].Enqueue(inst);
    }

    public static void ClearAll()
    {
        foreach (var kvp in _pool)
            while (kvp.Value.Count > 0) { var go = kvp.Value.Dequeue(); if (go != null) Object.Destroy(go); }
        _pool.Clear();
    }
}