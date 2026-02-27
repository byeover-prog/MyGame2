// UTF-8
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - 무기(Weapon)가 투사체를 Instantiate/Destroy 하지 않도록, 투사체를 미리 만들어 Queue로 재사용한다.
// - 투사체는 반드시 PooledObject2D를 상속해야 하며, ReturnToPool()로 반납한다.
// - "글로벌 풀(ProjectilePool)"과 섞지 않는다. (2D 공통스킬은 이 풀을 표준으로 사용)

[DisallowMultipleComponent]
public sealed class ProjectilePool2D : MonoBehaviour
{
    [Header("연결 설정")]
    [Tooltip("풀에서 생성/재사용할 투사체 프리팹\n" +
             "※ 반드시 PooledObject2D(또는 그 자식)를 붙인 프리팹이어야 합니다.")]
    [SerializeField] private PooledObject2D prefab;

    [Header("프리웜(시작 렉 방지)")]
    [Tooltip("Awake에서 미리 만들어둘 개수")]
    [Min(0)]
    [SerializeField] private int prewarmCount = 20;

    [Header("상한(안전장치)")]
    [Tooltip("풀 내부에 '보관'할 최대 개수\n" +
             "※ 프로토타입 안정성 우선: 한도를 넘어도 필요하면 추가 생성은 합니다(멈춤 방지).")]
    [Min(1)]
    [SerializeField] private int maxCount = 200;

    private readonly Queue<PooledObject2D> _pool = new Queue<PooledObject2D>(256);
    private int _created;

    private void Awake()
    {
        if (prefab == null) return;

        int target = Mathf.Clamp(prewarmCount, 0, maxCount);
        for (int i = 0; i < target; i++)
        {
            var obj = CreateNew();
            Return(obj);
        }
    }

    // 무기에서 쓰는 표준 API
    public T Get<T>(Vector3 pos, Quaternion rot) where T : PooledObject2D
    {
        var obj = GetRaw();
        obj.transform.SetPositionAndRotation(pos, rot);
        obj.gameObject.SetActive(true);
        return (T)obj;
    }

    // 타입을 모르거나, Get 후에 GetComponent로 찾고 싶은 경우
    public PooledObject2D GetRaw()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();

        // maxCount는 "보관 상한" 개념으로만 사용
        // (실전에서는 더 엄격히 막아도 되지만, 지금은 멈춤/누락 방지가 우선)
        return CreateNew();
    }

    private PooledObject2D CreateNew()
    {
        _created++;

        var obj = Instantiate(prefab, transform);
        obj.name = prefab.name; // (Clone) 붙는 것 방지(가독성)

        // ★ 핵심: 반납 경로를 여기로 고정
        obj.BindPool(this);

        obj.gameObject.SetActive(false);
        return obj;
    }

    // PooledObject2D.ReturnToPool()이 호출하면 여기로 들어온다
    public void Return(PooledObject2D obj)
    {
        if (obj == null) return;

        // 씬 정리(풀 루트로 묶어서 보기 좋게)
        obj.transform.SetParent(transform, false);

        obj.gameObject.SetActive(false);

        // 보관 상한을 넘으면 보관하지 않고 파괴(메모리 폭주 방지)
        // 단, maxCount는 "보관"만 제한. 생성은 GetRaw에서 계속 가능.
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
    }
#endif
}