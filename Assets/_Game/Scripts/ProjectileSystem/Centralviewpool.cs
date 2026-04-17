using System.Collections.Generic;
using UnityEngine;

public enum ProjectileVisualId : byte
{
    None = 0,

    /// <summary>곡궁 화살</summary>
    Arrow,

    /// <summary>화승총 총알</summary>
    Bullet,

    /// <summary>수리검</summary>
    Shuriken,

    /// <summary>부메랑</summary>
    Boomerang,

    /// <summary>정화구 (호밍 오브)</summary>
    HomingOrb,

    /// <summary>월륜검 칼날</summary>
    Blade,

    /// <summary>낙뢰부 부적</summary>
    Talisman,

    /// <summary>암흑구</summary>
    DarkOrb,

    /// <summary>화승총 발시</summary>
    Balsi,
}

public sealed class CentralViewPool : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════
    //  Inspector
    // ══════════════════════════════════════════════════════════════

    [System.Serializable]
    public struct ViewEntry
    {
        [Tooltip("이 뷰가 사용되는 투사체 비주얼 ID")]
        public ProjectileVisualId Id;

        [Tooltip("경량 뷰 프리팹 (SpriteRenderer + VFX 자식)")]
        public GameObject Prefab;

        [Tooltip("게임 시작 시 미리 생성할 수")]
        public int PrewarmCount;
    }

    [Header("뷰 프리팹 목록")]
    [SerializeField] private ViewEntry[] viewEntries;

    // ══════════════════════════════════════════════════════════════
    //  내부 상태
    // ══════════════════════════════════════════════════════════════

    private struct ViewSlot
    {
        public GameObject Go;
        public Transform Tr;
        public SpriteRenderer Sr;
        public TrailRenderer Trail;
        public ParticleSystem[] Particles; // ★ v2: 재사용 시 Clear용
        public bool InUse;
        public ProjectileVisualId VisualId;
    }

    /// <summary>유효하지 않은 ViewId</summary>
    public const int InvalidId = -1;

    private ViewSlot[] _slots;
    private int _capacity;
    private Transform _poolRoot;

    // VisualId별 Free 스택
    private readonly Dictionary<ProjectileVisualId, Stack<int>> _freeStacks
        = new Dictionary<ProjectileVisualId, Stack<int>>(16);

    // VisualId → Prefab 매핑
    private readonly Dictionary<ProjectileVisualId, GameObject> _prefabMap
        = new Dictionary<ProjectileVisualId, GameObject>(16);

    // ══════════════════════════════════════════════════════════════
    //  초기화
    // ══════════════════════════════════════════════════════════════

    private void Awake()
    {
        var rootGo = new GameObject("[CentralViewPool]");
        rootGo.transform.SetParent(transform);
        _poolRoot = rootGo.transform;

        int totalPrewarm = 0;
        if (viewEntries != null)
        {
            for (int i = 0; i < viewEntries.Length; i++)
                totalPrewarm += viewEntries[i].PrewarmCount;
        }

        _capacity = Mathf.Max(64, totalPrewarm * 2);
        _slots = new ViewSlot[_capacity];

        if (viewEntries != null)
        {
            for (int i = 0; i < viewEntries.Length; i++)
            {
                var entry = viewEntries[i];
                if (entry.Prefab == null || entry.Id == ProjectileVisualId.None) continue;

                _prefabMap[entry.Id] = entry.Prefab;

                if (!_freeStacks.ContainsKey(entry.Id))
                    _freeStacks[entry.Id] = new Stack<int>(entry.PrewarmCount * 2);

                for (int j = 0; j < entry.PrewarmCount; j++)
                {
                    int id = CreateSlot(entry.Prefab, entry.Id);
                    _slots[id].Go.SetActive(false);
                    _slots[id].InUse = false;
                    _freeStacks[entry.Id].Push(id);
                }
            }
        }

        GameLogger.Log($"[CentralViewPool v2] Prewarm 완료. 종류={_prefabMap.Count}, 총={totalPrewarm}");
    }

    // ══════════════════════════════════════════════════════════════
    //  Public API
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 풀에서 뷰를 꺼내 활성화. 반환값: ViewId.
    /// </summary>
    public int Acquire(ProjectileVisualId visualId, Vector2 position)
    {
        if (visualId == ProjectileVisualId.None) return InvalidId;

        int id;

        if (_freeStacks.TryGetValue(visualId, out var stack) && stack.Count > 0)
        {
            id = stack.Pop();
        }
        else
        {
            if (!_prefabMap.TryGetValue(visualId, out var prefab))
            {
                #if UNITY_EDITOR
                GameLogger.LogWarning($"[CentralViewPool] VisualId={visualId}에 매핑된 프리팹이 없습니다.");
                #endif
                return InvalidId;
            }
            id = CreateSlot(prefab, visualId);
        }

        ref var slot = ref _slots[id];
        slot.InUse = true;
        slot.Tr.position = new Vector3(position.x, position.y, 0f);

        // Trail 초기화
        if (slot.Trail != null)
            slot.Trail.Clear();
        
        if (slot.Particles != null)
        {
            for (int i = 0; i < slot.Particles.Length; i++)
            {
                if (slot.Particles[i] == null) continue;
                slot.Particles[i].Clear(true);
            }
        }

        slot.Go.SetActive(true);
        return id;
    }

    // 뷰를 비활성화하고 풀에 반환.
    public void Release(int viewId)
    {
        if (viewId < 0 || viewId >= _slots.Length) return;

        ref var slot = ref _slots[viewId];
        if (!slot.InUse) return;

        slot.InUse = false;
        slot.Go.SetActive(false);

        if (!_freeStacks.ContainsKey(slot.VisualId))
            _freeStacks[slot.VisualId] = new Stack<int>(32);

        _freeStacks[slot.VisualId].Push(viewId);
    }

    /// <summary>뷰 위치 갱신.</summary>
    public void SetPosition(int viewId, Vector2 position)
    {
        if (viewId < 0 || viewId >= _slots.Length) return;
        ref var slot = ref _slots[viewId];
        if (!slot.InUse) return;
        slot.Tr.position = new Vector3(position.x, position.y, 0f);
    }

    /// <summary>뷰 Z축 회전 갱신 (도).</summary>
    public void SetRotation(int viewId, float angleDeg)
    {
        if (viewId < 0 || viewId >= _slots.Length) return;
        ref var slot = ref _slots[viewId];
        if (!slot.InUse) return;
        slot.Tr.rotation = Quaternion.Euler(0f, 0f, angleDeg);
    }

    // ══════════════════════════════════════════════════════════════
    //  내부
    // ══════════════════════════════════════════════════════════════

    private int CreateSlot(GameObject prefab, ProjectileVisualId visualId)
    {
        int id = FindEmptySlotIndex();
        if (id < 0)
        {
            ExpandArray();
            id = FindEmptySlotIndex();
        }

        var go = Instantiate(prefab, _poolRoot);
        go.name = $"{visualId}_View_{id}";
        go.SetActive(false);

        _slots[id] = new ViewSlot
        {
            Go = go,
            Tr = go.transform,
            Sr = go.GetComponent<SpriteRenderer>(),
            Trail = go.GetComponent<TrailRenderer>(),
            Particles = go.GetComponentsInChildren<ParticleSystem>(true),
            InUse = false,
            VisualId = visualId,
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

        #if UNITY_EDITOR
        GameLogger.LogWarning($"[CentralViewPool] 배열 확장: {_capacity / 2} → {_capacity}");
        #endif
    }
}