// ============================================================================
// GameProjectileViewPool.cs
// 경로: Assets/_Game/Scripts/Combat/Projectiles/GameProjectileViewPool.cs
// 용도: 다크오브 비주얼(SpriteRenderer) 전용 오브젝트 풀
//
// [설계도 기준 - 리팩토링 핵심]
// - 경량 뷰 프리팹(SpriteRenderer만)을 풀링
// - NukeAllLogicComponents, DestroyImmediate 같은 해킹 코드 완전 제거
// - 레거시 DarkOrb 프리팹을 복제하지 않음 → Missing Script 0
//
// [Inspector 설정]
// 오브젝트: [GameProjectileManager]
// 컴포넌트: GameProjectileViewPool
//   - Dark Orb View Prefab → DarkOrbView (신규 경량 프리팹, SpriteRenderer만)
//   - Dark Orb Prewarm Count → 20
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

public sealed class GameProjectileViewPool : MonoBehaviour
{
    // ── Inspector ──
    [Header("경량 뷰 프리팹 (SpriteRenderer만 있어야 함)")]
    [Tooltip("DarkOrbView 프리팹을 드래그하세요.\nSpriteRenderer 외 다른 컴포넌트가 없어야 합니다.")]
    [SerializeField] private GameObject darkOrbViewPrefab;

    [Header("풀 설정")]
    [Tooltip("게임 시작 시 미리 생성할 뷰 개수")]
    [SerializeField] private int darkOrbPrewarmCount = 20;

    // ── 내부 풀 ──
    private struct ViewSlot
    {
        public GameObject Go;
        public Transform  Tr;
        public SpriteRenderer Sr;
        public bool InUse;
    }

    private ViewSlot[]       _slots;
    private readonly Stack<int> _freeStack = new Stack<int>(64);
    private int              _capacity;
    private Transform        _poolRoot;

    /// <summary>유효하지 않은 ViewId</summary>
    public const int InvalidId = -1;

    // ══════════════════════════════════════════════════════════════
    // 초기화
    // ══════════════════════════════════════════════════════════════

    private void Awake()
    {
        // 풀 루트 오브젝트 생성 (정리용)
        var rootGo = new GameObject("[DarkOrbViewPool]");
        rootGo.transform.SetParent(transform);
        _poolRoot = rootGo.transform;

        // 배열 초기화
        _capacity = Mathf.Max(32, darkOrbPrewarmCount * 2);
        _slots = new ViewSlot[_capacity];

        // Prewarm
        Prewarm();

        Debug.Log($"<color=green>[ViewPool] Prewarm 완료. count={darkOrbPrewarmCount}, capacity={_capacity}</color>");
    }

    private void Prewarm()
    {
        if (darkOrbViewPrefab == null)
        {
            Debug.LogError("[ViewPool] darkOrbViewPrefab이 비어있습니다! Inspector에서 DarkOrbView 프리팹을 연결하세요.");
            return;
        }

        for (int i = 0; i < darkOrbPrewarmCount; i++)
        {
            int id = CreateSlot();
            // 비활성 + free 스택에 직접 등록
            _slots[id].Go.SetActive(false);
            _slots[id].InUse = false;
            _freeStack.Push(id);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 풀에서 뷰를 꺼내 활성화하고, 지정 위치에 배치.
    /// 반환값: ViewId (나중에 Release/SetPosition에 사용)
    /// </summary>
    public int Acquire(Vector2 position, float alpha = 1f)
    {
        int id;

        if (_freeStack.Count > 0)
        {
            id = _freeStack.Pop();
        }
        else
        {
            // 풀 부족 → 동적 확장
            id = CreateSlot();
        }

        ref var slot = ref _slots[id];
        slot.InUse = true;
        slot.Tr.position = new Vector3(position.x, position.y, 0f);

        // 알파 적용
        if (slot.Sr != null)
        {
            var c = slot.Sr.color;
            c.a = alpha;
            slot.Sr.color = c;
        }

        slot.Go.SetActive(true);
        return id;
    }

    /// <summary>
    /// 뷰를 비활성화하고 풀에 반환.
    /// </summary>
    public void Release(int viewId)
    {
        if (viewId < 0 || viewId >= _slots.Length) return;

        ref var slot = ref _slots[viewId];
        if (!slot.InUse) return; // 이미 반환됨

        slot.InUse = false;
        slot.Go.SetActive(false);
        _freeStack.Push(viewId);
    }

    /// <summary>
    /// 뷰 위치를 갱신 (매 프레임 Manager가 호출).
    /// </summary>
    public void SetPosition(int viewId, Vector2 position)
    {
        if (viewId < 0 || viewId >= _slots.Length) return;
        ref var slot = ref _slots[viewId];
        if (!slot.InUse) return;

        slot.Tr.position = new Vector3(position.x, position.y, 0f);
    }

    // ══════════════════════════════════════════════════════════════
    // 내부 메서드
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 새 뷰 슬롯을 생성. 배열이 꽉 차면 자동 확장.
    /// </summary>
    private int CreateSlot()
    {
        // 배열 확장 필요 여부 확인
        int id = FindEmptySlotIndex();
        if (id < 0)
        {
            ExpandArray();
            id = FindEmptySlotIndex();
        }

        var go = Instantiate(darkOrbViewPrefab, _poolRoot);
        go.name = $"DarkOrbView_{id}";
        go.SetActive(false);

        _slots[id] = new ViewSlot
        {
            Go = go,
            Tr = go.transform,
            Sr = go.GetComponent<SpriteRenderer>(),
            InUse = false
        };

        return id;
    }

    private int FindEmptySlotIndex()
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (_slots[i].Go == null)
                return i;
        }
        return -1;
    }

    private void ExpandArray()
    {
        int newCapacity = _capacity * 2;
        var newSlots = new ViewSlot[newCapacity];
        System.Array.Copy(_slots, newSlots, _capacity);
        _slots = newSlots;
        _capacity = newCapacity;

        Debug.LogWarning($"[ViewPool] 풀 확장: {_capacity / 2} → {_capacity}. 너무 자주 발생하면 PrewarmCount를 늘리세요.");
    }
}