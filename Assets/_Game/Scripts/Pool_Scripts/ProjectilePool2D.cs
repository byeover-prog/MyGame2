// UTF-8
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - 프리웜(미리 생성)은 성능에 좋지만, 한 프레임에 몰리면 에디터가 먹통처럼 멈춘다.
// - 그래서 프리웜을 "프레임 분산" 코루틴으로 진행한다.
// - 투사체는 PooledObject2D 상속 + ReturnToPool()로 반납.
//
// ★ 수정사항:
// - Get() 시 투사체를 풀 부모에서 분리(SetParent(null))하여
//   부모 스케일/이동 영향 제거
// - Return() 시 풀 아래로 복귀
// - 원본 localScale 캐싱으로 스케일 복원 보장

[DisallowMultipleComponent]
public sealed class ProjectilePool2D : MonoBehaviour
{
    [Header("연결 설정")]
    [Tooltip("풀에서 생성/재사용할 투사체 프리팹\n※ 반드시 PooledObject2D(또는 그 자식)를 붙인 프리팹이어야 합니다.")]
    [SerializeField] private PooledObject2D prefab;

    [Header("프리웜(시작 렉 방지)")]
    [Tooltip("총 프리웜 개수")]
    [Min(0)]
    [SerializeField] private int prewarmCount = 20;

    [Tooltip("프리웜을 한 프레임에 몇 개씩 나눠 만들지(먹통 방지)\n권장: 2~5")]
    [Range(1, 20)]
    [SerializeField] private int prewarmPerFrame = 3;

    [Tooltip("프리웜을 Awake 즉시 하지 않고 Start에서 시작(씬 로딩 안정)\n권장: ON")]
    [SerializeField] private bool prewarmOnStart = true;

    [Header("상한(안전장치)")]
    [Tooltip("풀 내부에 '보관'할 최대 개수")]
    [Min(1)]
    [SerializeField] private int maxCount = 200;

    private readonly Queue<PooledObject2D> _pool = new Queue<PooledObject2D>(256);
    private Coroutine _prewarmCo;

    // ★ 프리팹 원본 localScale 캐싱 (부모 스케일 영향 복원용)
    private Vector3 _prefabLocalScale = Vector3.one;

    private void Awake()
    {
        if (prefab == null) return;

        // 프리팹 원본 스케일 기억
        _prefabLocalScale = prefab.transform.localScale;

        if (!prewarmOnStart)
            StartPrewarm();
    }

    private void Start()
    {
        if (prefab == null) return;

        if (prewarmOnStart)
            StartPrewarm();
    }

    private void StartPrewarm()
    {
        if (prewarmCount <= 0) return;
        if (_prewarmCo != null) StopCoroutine(_prewarmCo);
        _prewarmCo = StartCoroutine(PrewarmRoutine());
    }

    private IEnumerator PrewarmRoutine()
    {
        int target = Mathf.Clamp(prewarmCount, 0, maxCount);

        while (_pool.Count < target)
        {
            int batch = Mathf.Min(prewarmPerFrame, target - _pool.Count);

            for (int i = 0; i < batch; i++)
            {
                var obj = CreateNew();
                Return(obj);
            }

            // ★ 다음 프레임으로 넘겨서 먹통 방지
            yield return null;
        }

        _prewarmCo = null;
    }

    public T Get<T>(Vector3 pos, Quaternion rot) where T : PooledObject2D
    {
        var obj = GetRaw();

        // ★ 핵심 수정: 풀 부모에서 분리하여 월드 공간에서 독립 동작
        // - 부모 스케일(무기 프리팹 0.2~0.28 등) 영향 제거
        // - 부모 이동(플레이어 추적) 영향 제거
        obj.transform.SetParent(null, false);

        // 월드 위치/회전 설정
        obj.transform.SetPositionAndRotation(pos, rot);

        // ★ 스케일 복원: 부모 해제 후 localScale = worldScale이므로 프리팹 원본값으로 복원
        obj.transform.localScale = _prefabLocalScale;

        obj.ClearReturningFlag();
        if (!obj.gameObject.activeSelf) obj.gameObject.SetActive(true);

        if (obj is T t) return t;

        Debug.LogError($"[ProjectilePool2D] Get<{typeof(T).Name}> 타입 불일치. 실제={obj.GetType().Name} prefab={prefab.name}", this);
        return obj.GetComponent<T>();
    }

    public PooledObject2D GetRaw()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();

        return CreateNew();
    }

    private PooledObject2D CreateNew()
    {
        var obj = Instantiate(prefab, transform);
        obj.name = prefab.name;
        obj.BindPool(this);

        if (obj.gameObject.activeSelf) obj.gameObject.SetActive(false);
        return obj;
    }

    public void Return(PooledObject2D obj)
    {
        if (obj == null) return;

        // 풀 아래로 복귀 (worldPositionStays=false → local 좌표계로 깔끔하게 정리)
        obj.transform.SetParent(transform, false);

        if (obj.gameObject.activeSelf) obj.gameObject.SetActive(false);

        if (_pool.Count >= maxCount)
        {
            Destroy(obj.gameObject);
            return;
        }

        _pool.Enqueue(obj);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxCount < 1) maxCount = 1;
        if (prewarmCount < 0) prewarmCount = 0;
        if (prewarmPerFrame < 1) prewarmPerFrame = 1;
    }
#endif
}