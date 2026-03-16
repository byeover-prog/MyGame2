using UnityEngine;

/// <summary>
/// 다크오브 경량 뷰 프리팹 오브젝트 풀.
/// SpriteRenderer만 달린 경량 프리팹을 풀링한다.
/// 레거시 컴포넌트 제거(NukeAllLogicComponents) 같은 해킹이 전혀 없다.
/// </summary>
public class DarkOrbViewPool : MonoBehaviour
{
    [Header("풀 설정")]
    [Tooltip("SpriteRenderer만 달린 경량 다크오브 뷰 프리팹")]
    [SerializeField] private GameObject _lightViewPrefab;

    [Tooltip("풀 초기 생성 개수")]
    [SerializeField] private int _initialPoolSize = 20;

    // ── 내부 풀 ──
    private Transform[] _views;
    private SpriteRenderer[] _renderers;
    private bool[] _inUse;
    private int _capacity;

    /// <summary>풀 초기화. DarkOrbManager.Awake()에서 호출</summary>
    public void Initialize()
    {
        _capacity = _initialPoolSize;
        _views = new Transform[_capacity];
        _renderers = new SpriteRenderer[_capacity];
        _inUse = new bool[_capacity];

        for (int i = 0; i < _capacity; i++)
        {
            CreateViewAt(i);
        }
    }

    /// <summary>
    /// 뷰 하나를 빌려간다. 활성화 + 위치/스케일 세팅.
    /// 풀이 부족하면 자동 확장한다.
    /// </summary>
    /// <returns>할당된 뷰 인덱스</returns>
    public int Rent(Vector2 position, float scale)
    {
        int idx = FindFreeSlot();
        if (idx < 0)
        {
            Expand();
            idx = FindFreeSlot();
        }

        _inUse[idx] = true;
        Transform t = _views[idx];
        t.position = new Vector3(position.x, position.y, 0f);
        t.localScale = Vector3.one * scale;
        t.gameObject.SetActive(true);
        return idx;
    }

    /// <summary>뷰를 반납한다. 비활성화.</summary>
    public void Return(int index)
    {
        if (index < 0 || index >= _capacity) return;
        _inUse[index] = false;
        _views[index].gameObject.SetActive(false);
    }

    /// <summary>뷰의 위치를 갱신한다. Manager.Update()에서 매 프레임 호출</summary>
    public void UpdatePosition(int index, Vector2 position)
    {
        if (index < 0 || index >= _capacity) return;
        _views[index].position = new Vector3(position.x, position.y, 0f);
    }

    /// <summary>모든 뷰를 즉시 반납한다. 스킬 리셋/씬 전환 시 사용</summary>
    public void ReturnAll()
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (_inUse[i])
            {
                _inUse[i] = false;
                _views[i].gameObject.SetActive(false);
            }
        }
    }

    // ── Private ──

    private int FindFreeSlot()
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (!_inUse[i]) return i;
        }
        return -1;
    }

    private void Expand()
    {
        int oldCapacity = _capacity;
        int newCapacity = oldCapacity * 2;

        var newViews = new Transform[newCapacity];
        var newRenderers = new SpriteRenderer[newCapacity];
        var newInUse = new bool[newCapacity];

        System.Array.Copy(_views, newViews, oldCapacity);
        System.Array.Copy(_renderers, newRenderers, oldCapacity);
        System.Array.Copy(_inUse, newInUse, oldCapacity);

        // 배열 교체를 먼저 수행 (CreateViewAt가 _views/_renderers를 사용하므로)
        _views = newViews;
        _renderers = newRenderers;
        _inUse = newInUse;
        _capacity = newCapacity;

        for (int i = oldCapacity; i < newCapacity; i++)
        {
            CreateViewAt(i);
        }
    }

    private void CreateViewAt(int index)
    {
        GameObject go = Instantiate(_lightViewPrefab, transform);
        go.name = $"DarkOrbView_{index}";
        go.SetActive(false);
        _views[index] = go.transform;
        _renderers[index] = go.GetComponent<SpriteRenderer>();
    }
}