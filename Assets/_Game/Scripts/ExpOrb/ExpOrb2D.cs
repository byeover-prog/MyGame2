using UnityEngine;

[DisallowMultipleComponent]
public sealed class ExpOrb2D : MonoBehaviour
{
    [Header("EXP")]
    [Min(0)]
    [SerializeField] private int expValue = 3;

    [Header("수거")]
    [Min(0.01f)]
    [SerializeField] private float pickupDistance = 0.3f;

    [Header("Visual")]
    [SerializeField] private int sortingOrder = 60;

    // 매니저가 읽고 쓰는 런타임 상태
    // public field = 매니저가 직접 접근 (프로퍼티 오버헤드 제거)
    [System.NonSerialized] public float SpawnTime;
    [System.NonSerialized] public bool Attracting; // 자석 범위 내 진입 여부
    [System.NonSerialized] public bool Collected;

    // 풀 반환용
    private ExpOrbPool _pool;
    private GameObject _prefabKey;
    private SpriteRenderer[] _renderers;

    // 읽기 전용
    public int ExpValue => expValue;
    public float PickupDistSqr { get; private set; }

    private void Awake()
    {
        PickupDistSqr = pickupDistance * pickupDistance;
        CacheRenderers();
        ApplySorting();
    }

    private void OnEnable()
    {
        Collected = false;
        Attracting = false;
        SpawnTime = Time.time;
        ApplySorting();

        // 매니저에 자동 등록
        ExpOrbManager.Register(this);
    }

    private void OnDisable()
    {
        Attracting = false;
        ExpOrbManager.Unregister(this);
    }

    // Update / FixedUpdate 없음

    public void SetExp(int value) => expValue = Mathf.Max(0, value);
    public void SetPool(ExpOrbPool pool) => _pool = pool;
    public void SetOriginPrefab(GameObject prefabKey) => _prefabKey = prefabKey;

    public void ApplySorting()
    {
        CacheRenderers();

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            _renderers[i].sortingOrder = sortingOrder;
        }
    }

    // 매니저가 호출. 경험치 지급 후 풀 반환.
    public void DoCollect(PlayerExp playerExp, float expGainMul)
    {
        if (Collected) return;
        Collected = true;

        if (expValue > 0 && playerExp != null)
            playerExp.AddExp(Mathf.Max(1, Mathf.RoundToInt(expValue * expGainMul)));

        Return();
    }

    // 매니저가 호출. 자동 소멸 (경험치 미지급).
    public void DoDespawn()
    {
        if (Collected) return;
        Collected = true;
        Return();
    }

    private void Return()
    {
        gameObject.SetActive(false);
        if (_pool != null && _prefabKey != null)
            _pool.Release(_prefabKey, gameObject);
    }

    private void CacheRenderers()
    {
        if (_renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }
}
