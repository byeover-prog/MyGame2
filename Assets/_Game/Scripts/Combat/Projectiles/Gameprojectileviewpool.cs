// UTF-8
// Assets/_Game/Scripts/Combat/Projectiles/GameProjectileViewPool.cs
// ──────────────────────────────────────────────────────────────────
// [v4 리팩토링] 경량 뷰 프리팹 전용 풀
//
// ★ 이전 버전(v3)과의 차이:
//   - _dormantRoot(비활성 부모) 제거  → 경량 프리팹엔 Awake 차단 불필요
//   - NukeAllLogicComponents 제거     → 경량 프리팹엔 로직 컴포넌트 자체가 없음
//   - DestroyImmediate 해킹 제거      → 위와 동일
//   - DestroyLegacyComponents 제거    → 위와 동일
//   - 코드 절반으로 축소, Missing Script 경고 원천 차단
//
// [경량 뷰 프리팹 만드는 법]
//   1. Unity Hierarchy 우클릭 → Create Empty → 이름 "DarkOrbView"
//   2. Add Component → SpriteRenderer
//   3. Sprite 필드에 기존 DarkOrb 스프라이트 이미지 드래그
//   4. Color, Sorting Layer, Order in Layer를 기존 DarkOrb와 동일하게
//   5. Project 창 Prefabs 폴더로 드래그 → 프리팹화
//   6. Hierarchy에서 씬의 DarkOrbView 삭제 (프리팹만 남김)
//   ※ 절대 로직 컴포넌트를 넣지 말 것. SpriteRenderer만!
//
// [Inspector 설정]
//   Hierarchy: [GameProjectileManager] 오브젝트
//   컴포넌트: GameProjectileViewPool
//     Dark Orb View Prefab  → DarkOrbView 프리팹 (신규 경량 프리팹)
//     Dark Orb Prewarm Count → 20
// ──────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public sealed class GameProjectileViewPool : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // Inspector
    // ══════════════════════════════════════════════════════

    [Header("경량 뷰 프리팹 (SpriteRenderer만 있는 프리팹)")]
    [Tooltip("DarkOrbView 프리팹을 여기에 드래그하세요.\n" +
             "로직 컴포넌트가 없는 순수 비주얼 프리팹이어야 합니다.")]
    [SerializeField] private GameObject darkOrbViewPrefab;

    [Header("풀 설정")]
    [Tooltip("게임 시작 시 미리 생성할 뷰 개수")]
    [SerializeField] private int darkOrbPrewarmCount = 20;

    // ══════════════════════════════════════════════════════
    // 내부 구조
    // ══════════════════════════════════════════════════════

    private struct ViewEntry
    {
        public GameObject Go;
        public Transform  Tr;
        public SpriteRenderer Sr;
        public bool InUse;
    }

    private ViewEntry[] _views;
    private readonly Stack<int> _free = new Stack<int>(64);
    private bool[] _inFreeStack;
    private int _capacity;
    private Transform _poolRoot;

    /// <summary>유효하지 않은 ViewId 상수</summary>
    public const int InvalidId = -1;

    // ══════════════════════════════════════════════════════
    // 초기화
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        // 풀 루트 생성 (반환된 뷰를 여기 아래에 보관)
        var rootGo = new GameObject("[ProjectileViewPool]");
        _poolRoot = rootGo.transform;

        // 배열 초기화
        _capacity = Mathf.Max(32, darkOrbPrewarmCount * 2);
        _views = new ViewEntry[_capacity];
        _inFreeStack = new bool[_capacity];

        // Prewarm
        PrewarmDarkOrb();

        Debug.Log($"<color=cyan>[GameProjectileViewPool] ★★★ v4 리팩토링 완료 ★★★ " +
                  $"prewarm={darkOrbPrewarmCount}, free={_free.Count}</color>", this);
    }

    private void PrewarmDarkOrb()
    {
        if (darkOrbViewPrefab == null)
        {
            Debug.LogError("[GameProjectileViewPool] darkOrbViewPrefab이 비어있습니다! " +
                           "Inspector에서 DarkOrbView 경량 프리팹을 연결하세요.", this);
            return;
        }

        for (int i = 0; i < darkOrbPrewarmCount; i++)
        {
            int id = CreateViewSlot(darkOrbViewPrefab);

            // 직접 free 스택에 등록 (Release 경유 안 함)
            _views[id].InUse = false;
            if (!_inFreeStack[id])
            {
                _free.Push(id);
                _inFreeStack[id] = true;
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════

    /// <summary>풀에서 뷰를 꺼내 활성화. 반환값 = ViewId (Release 시 사용)</summary>
    public int Acquire(GameProjectileKind kind, Vector2 pos, float alpha = 1f)
    {
        int id = InvalidId;

        // 풀에서 유효한 슬롯 Pop
        while (_free.Count > 0)
        {
            int candidate = _free.Pop();
            _inFreeStack[candidate] = false;

            if (candidate < 0 || candidate >= _capacity) continue;
            if (_views[candidate].Go == null) continue;

            id = candidate;
            break;
        }

        // 풀 고갈 → 새로 생성
        if (id == InvalidId)
        {
            GameObject prefab = GetPrefab(kind);
            if (prefab == null) return InvalidId;
            id = CreateViewSlot(prefab);
        }

        ref var v = ref _views[id];
        v.InUse = true;

        // 풀 루트에서 떼어내고 월드에 배치 후 활성화
        v.Tr.SetParent(null, false);
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

    /// <summary>뷰를 비활성화하고 풀로 반환</summary>
    public void Release(int viewId)
    {
        if (viewId < 0 || viewId >= _capacity) return;
        if (_inFreeStack[viewId]) return; // 이미 반환됨

        ref var v = ref _views[viewId];
        if (v.Go == null) return;

        v.InUse = false;
        v.Go.SetActive(false);
        v.Tr.SetParent(_poolRoot, false);

        _free.Push(viewId);
        _inFreeStack[viewId] = true;
    }

    /// <summary>뷰 위치 동기화 (Manager의 SyncViews에서 매 프레임 호출)</summary>
    public void SetPosition(int viewId, Vector2 pos)
    {
        if (viewId < 0 || viewId >= _capacity) return;
        ref var v = ref _views[viewId];
        if (v.Tr != null) v.Tr.position = (Vector3)pos;
    }

    /// <summary>전체 반환 (스테이지 종료 시 등)</summary>
    public void ReleaseAll()
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (_views[i].InUse)
                Release(i);
        }
    }

    // ══════════════════════════════════════════════════════
    // 내부 유틸
    // ══════════════════════════════════════════════════════

    private int CreateViewSlot(GameObject prefab)
    {
        int slot = FindEmptySlot();
        if (slot < 0)
        {
            GrowCapacity();
            slot = FindEmptySlot();
        }
        if (slot < 0) slot = _capacity - 1; // 최후 안전장치

        // ★ v4: 경량 프리팹이므로 그냥 Instantiate. 해킹 불필요.
        var go = Instantiate(prefab, _poolRoot);
        go.name = prefab.name;
        go.SetActive(false);

        ref var v = ref _views[slot];
        v.Go = go;
        v.Tr = go.transform;
        v.Sr = go.GetComponentInChildren<SpriteRenderer>(true);
        v.InUse = false;

        return slot;
    }

    private GameObject GetPrefab(GameProjectileKind kind)
    {
        // 현재는 DarkOrb만 지원. 추후 다른 투사체 종류 추가 시 switch 확장.
        switch (kind)
        {
            case GameProjectileKind.DarkOrb:
                return darkOrbViewPrefab;
            default:
                return darkOrbViewPrefab;
        }
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

        var newViews = new ViewEntry[newCap];
        System.Array.Copy(_views, newViews, _capacity);
        _views = newViews;

        var newFlags = new bool[newCap];
        System.Array.Copy(_inFreeStack, newFlags, _capacity);
        _inFreeStack = newFlags;

        _capacity = newCap;
    }
}