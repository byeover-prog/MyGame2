// UTF-8
// Assets/_Game/Scripts/VFX/VFXPool.cs
// ★ VFX v2 — GPT 리뷰 #1 #4 반영: 프리팹별 제한 + 강제회수 제외 + 파티클 Clear
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// VFX 전용 오브젝트 풀.
///
/// [GPT 리뷰 #1] 프리팹별 동시 활성 제한 (SetMaxActive)
/// [GPT 리뷰 #4] 특정 프리팹은 강제 회수 제외 (SetNoForceRecycle)
/// [잔상 수정] 재사용 시 ParticleSystem.Clear() 호출
/// </summary>
public static class VFXPool
{
    /// <summary>기본 동시 활성 제한. SetMaxActive로 프리팹별 오버라이드 가능.</summary>
    public static int DefaultMaxActive = 12;

    private static readonly Dictionary<int, Queue<GameObject>> _pool
        = new Dictionary<int, Queue<GameObject>>();

    private static readonly Dictionary<int, LinkedList<GameObject>> _active
        = new Dictionary<int, LinkedList<GameObject>>();

    // [GPT #1] 프리팹별 제한값
    private static readonly Dictionary<int, int> _maxActiveOverride
        = new Dictionary<int, int>();

    // [GPT #4] 강제 회수 제외 프리팹
    private static readonly HashSet<int> _noForceRecycle = new HashSet<int>();

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

    // ── 설정 API ──

    /// <summary>특정 프리팹의 동시 활성 VFX 제한을 설정.</summary>
    public static void SetMaxActive(GameObject prefab, int max)
    {
        if (prefab == null) return;
        _maxActiveOverride[prefab.GetInstanceID()] = max;
    }

    /// <summary>특정 프리팹을 강제 회수 대상에서 제외. 재생 중 끊김 방지.</summary>
    public static void SetNoForceRecycle(GameObject prefab)
    {
        if (prefab == null) return;
        _noForceRecycle.Add(prefab.GetInstanceID());
    }

    /// <summary>풀을 미리 채움.</summary>
    public static void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        int key = prefab.GetInstanceID();

        if (!_pool.ContainsKey(key))
            _pool[key] = new Queue<GameObject>();

        var q = _pool[key];
        for (int i = 0; i < count; i++)
        {
            var inst = Object.Instantiate(prefab, PoolRoot);
            inst.name = prefab.name + "_vfx";
            inst.SetActive(false);

            var ar = inst.GetComponent<VFXAutoReturn>();
            if (ar == null) ar = inst.AddComponent<VFXAutoReturn>();
            ar.sourcePrefab = prefab;

            q.Enqueue(inst);
        }
    }

    // ── 핵심 API ──

    public static GameObject Get(GameObject prefab, Vector3 position,
                                  Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;
        int key = prefab.GetInstanceID();
        GameObject inst = null;

        // 1. 동시 활성 제한 체크 + 강제 회수
        int maxActive = _maxActiveOverride.ContainsKey(key)
            ? _maxActiveOverride[key]
            : DefaultMaxActive;

        bool canForceRecycle = !_noForceRecycle.Contains(key);

        if (_active.TryGetValue(key, out var activeList))
        {
            while (activeList.Count >= maxActive && activeList.Count > 0 && canForceRecycle)
            {
                var oldest = activeList.First.Value;
                activeList.RemoveFirst();
                if (oldest != null && oldest.activeSelf)
                {
                    oldest.SetActive(false);
                    oldest.transform.SetParent(PoolRoot, false);
                    if (!_pool.ContainsKey(key)) _pool[key] = new Queue<GameObject>();
                    _pool[key].Enqueue(oldest);
                }
            }

            // 강제 회수 불가 + 제한 초과 → 생성 거부
            if (!canForceRecycle && activeList.Count >= maxActive)
                return null;
        }

        // 2. 풀에서 꺼내기
        if (_pool.TryGetValue(key, out var q))
        {
            while (q.Count > 0)
            {
                inst = q.Dequeue();
                if (inst != null) break;
                inst = null;
            }
        }

        // 3. 풀 고갈 → Instantiate
        if (inst == null)
        {
            inst = Object.Instantiate(prefab);
            inst.name = prefab.name + "_vfx";
        }

        // 4. ★ 잔상 수정: 재사용 시 파티클 상태 초기화
        ClearParticles(inst);

        // 5. 배치
        if (parent != null)
        {
            inst.transform.SetParent(parent, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
        }
        else
        {
            inst.transform.SetParent(null);
            inst.transform.position = position;
            inst.transform.rotation = rotation;
        }

        inst.SetActive(true);

        // 6. 활성 추적
        if (!_active.ContainsKey(key))
            _active[key] = new LinkedList<GameObject>();
        _active[key].AddLast(inst);

        return inst;
    }

    /// <summary>[GPT #3] 부모에 붙이되 로컬 오프셋을 보존하는 버전.</summary>
    public static GameObject GetPreservingOffset(GameObject prefab, Transform parent)
    {
        if (prefab == null || parent == null) return null;
        int key = prefab.GetInstanceID();
        GameObject inst = null;

        // 제한 체크 (Get과 동일 로직)
        int maxActive = _maxActiveOverride.ContainsKey(key) ? _maxActiveOverride[key] : DefaultMaxActive;
        bool canForceRecycle = !_noForceRecycle.Contains(key);

        if (_active.TryGetValue(key, out var activeList))
        {
            while (activeList.Count >= maxActive && activeList.Count > 0 && canForceRecycle)
            {
                var oldest = activeList.First.Value;
                activeList.RemoveFirst();
                if (oldest != null && oldest.activeSelf)
                {
                    oldest.SetActive(false);
                    oldest.transform.SetParent(PoolRoot, false);
                    if (!_pool.ContainsKey(key)) _pool[key] = new Queue<GameObject>();
                    _pool[key].Enqueue(oldest);
                }
            }
            if (!canForceRecycle && activeList.Count >= maxActive) return null;
        }

        if (_pool.TryGetValue(key, out var q))
        {
            while (q.Count > 0) { inst = q.Dequeue(); if (inst != null) break; inst = null; }
        }
        if (inst == null) { inst = Object.Instantiate(prefab); inst.name = prefab.name + "_vfx"; }

        ClearParticles(inst);

        // ★ [GPT #3] 로컬 오프셋 보존 — localPosition/localRotation을 리셋하지 않음
        inst.transform.SetParent(parent, false);

        inst.SetActive(true);

        if (!_active.ContainsKey(key)) _active[key] = new LinkedList<GameObject>();
        _active[key].AddLast(inst);

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

        if (_active.TryGetValue(key, out var activeList))
            activeList.Remove(inst);
    }

    public static void ClearAll()
    {
        foreach (var kvp in _pool)
            while (kvp.Value.Count > 0)
            {
                var go = kvp.Value.Dequeue();
                if (go != null) Object.Destroy(go);
            }
        _pool.Clear();
        foreach (var kvp in _active) kvp.Value.Clear();
        _active.Clear();
        _maxActiveOverride.Clear();
        _noForceRecycle.Clear();
    }

    /// <summary>재사용 시 이전 파티클 잔상 제거.</summary>
    private static void ClearParticles(GameObject inst)
    {
        var ps = inst.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < ps.Length; i++)
        {
            ps[i].Clear(true);
            ps[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}