// UTF-8
// Assets/_Game/Scripts/Combat/Projectiles/GameProjectileViewPool.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 투사체 비주얼 전용 오브젝트 풀.
/// SpriteRenderer/VFX 표시만 담당. 이동/충돌/분열 로직 금지.
/// 
/// GameProjectileManager가 SyncViews()에서 위치를 동기화한다.
/// </summary>
public sealed class GameProjectileViewPool : MonoBehaviour
{
    [Header("뷰 프리팹")]
    [Tooltip("DarkOrb 비주얼 프리팹.\nSpriteRenderer + ProjectileVFXChild만 있으면 됨.\n기존 DarkOrb 프리팹 그대로 사용 가능.")]
    [SerializeField] private GameObject darkOrbViewPrefab;

    [Header("풀 설정")]
    [SerializeField] private int darkOrbPrewarmCount = 20;

    // ── 내부 풀 ───────────────────────────────────────────

    private struct ViewEntry
    {
        public GameObject Go;
        public Transform Tr;
        public SpriteRenderer Sr;
        public bool InUse;
    }

    // ViewId = 배열 인덱스
    private ViewEntry[] _views;
    private readonly Stack<int> _free = new Stack<int>(64);
    private int _capacity;
    private Transform _poolRoot;

    /// <summary>유효하지 않은 ViewId</summary>
    public const int InvalidId = -1;

    private void Awake()
    {
        var rootGo = new GameObject("[ProjectileViewPool]");
        _poolRoot = rootGo.transform;

        _capacity = Mathf.Max(32, darkOrbPrewarmCount * 2);
        _views = new ViewEntry[_capacity];

        PrewarmDarkOrb();
    }

    private void PrewarmDarkOrb()
    {
        if (darkOrbViewPrefab == null) return;

        for (int i = 0; i < darkOrbPrewarmCount; i++)
        {
            int id = CreateViewSlot(darkOrbViewPrefab);
            Release(id);
        }
    }

    // ── Public API ────────────────────────────────────────

    /// <summary>뷰 오브젝트를 풀에서 꺼내 활성화. 반환값은 ViewId.</summary>
    public int Acquire(GameProjectileKind kind, Vector2 pos, float alpha)
    {
        // 풀에 여유가 있으면 꺼냄
        int id = InvalidId;

        if (_free.Count > 0)
        {
            id = _free.Pop();
        }
        else
        {
            // 풀 고갈 → 새로 생성
            GameObject prefab = GetPrefab(kind);
            if (prefab == null) return InvalidId;
            id = CreateViewSlot(prefab);
        }

        ref var v = ref _views[id];
        v.InUse = true;
        v.Tr.position = (Vector3)pos;
        v.Go.SetActive(true);

        // 알파 적용
        if (v.Sr != null)
        {
            var c = v.Sr.color;
            c.a = alpha;
            v.Sr.color = c;
        }

        return id;
    }

    /// <summary>뷰 오브젝트를 비활성화하고 풀로 반환.</summary>
    public void Release(int viewId)
    {
        if (viewId < 0 || viewId >= _capacity) return;
        ref var v = ref _views[viewId];
        if (!v.InUse && v.Go != null && !v.Go.activeSelf)
        {
            // 이미 반환됨
            return;
        }
        v.InUse = false;
        if (v.Go != null)
        {
            v.Go.SetActive(false);
            v.Tr.SetParent(_poolRoot, false);
        }
        _free.Push(viewId);
    }

    /// <summary>뷰 위치 동기화. Manager의 SyncViews에서 호출.</summary>
    public void SetPosition(int viewId, Vector2 pos)
    {
        if (viewId < 0 || viewId >= _capacity) return;
        ref var v = ref _views[viewId];
        if (v.Tr != null) v.Tr.position = (Vector3)pos;
    }

    /// <summary>전체 반환.</summary>
    public void ReleaseAll()
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (_views[i].InUse)
                Release(i);
        }
    }

    // ── 내부 ──────────────────────────────────────────────

    private int CreateViewSlot(GameObject prefab)
    {
        // 용량 초과 시 확장
        if (_free.Count == 0 && FindEmptySlot() < 0)
            GrowCapacity();

        int slot = FindEmptySlot();
        if (slot < 0) slot = _capacity - 1; // 안전장치

        var go = Instantiate(prefab, _poolRoot);
        go.name = prefab.name;
        go.SetActive(false);

        // 레거시 투사체 로직 비활성화 (뷰에는 로직 금지)
        DisableLogicComponents(go);

        ref var v = ref _views[slot];
        v.Go = go;
        v.Tr = go.transform;
        v.Sr = go.GetComponentInChildren<SpriteRenderer>(true);
        v.InUse = false;

        return slot;
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < _capacity; i++)
            if (_views[i].Go == null && !_views[i].InUse)
                return i;
        return -1;
    }

    private void GrowCapacity()
    {
        int newCap = _capacity * 2;
        var newArr = new ViewEntry[newCap];
        System.Array.Copy(_views, newArr, _capacity);
        _views = newArr;
        _capacity = newCap;
    }

    private static void DisableLogicComponents(GameObject go)
    {
        // 레거시 컴포넌트 비활성화 (타입 직접 참조 대신 문자열 탐색으로 안전 처리)
#pragma warning disable CS0618 // Obsolete 경고 억제
        var legacy = go.GetComponent<DarkOrbProjectile2D>();
        if (legacy != null) legacy.enabled = false;
#pragma warning restore CS0618

        var legacySplit = go.GetComponent<DarkOrbSplitProjectile2D>();
        if (legacySplit != null) legacySplit.enabled = false;

        // MonoBehaviour 전체를 순회하며 불필요한 로직 컴포넌트 비활성화
        var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            var b = behaviours[i];
            if (b == null) continue;
            // 뷰에 남겨야 할 것: SpriteRenderer 관련, VFX 관련, Animator
            // 나머지 로직 컴포넌트는 비활성화
            string typeName = b.GetType().Name;
            if (typeName.Contains("Skill") || typeName.Contains("Weapon") || typeName.Contains("Motor"))
                b.enabled = false;
        }

        // Collider 비활성화 (뷰는 충돌 금지)
        var cols = go.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++)
            if (cols[i] != null) cols[i].enabled = false;

        // Rigidbody 비활성화
        var rbs = go.GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < rbs.Length; i++)
            if (rbs[i] != null) rbs[i].simulated = false;
    }

    private GameObject GetPrefab(GameProjectileKind kind)
    {
        switch (kind)
        {
            case GameProjectileKind.DarkOrb: return darkOrbViewPrefab;
            // 향후 확장
            default: return null;
        }
    }
}