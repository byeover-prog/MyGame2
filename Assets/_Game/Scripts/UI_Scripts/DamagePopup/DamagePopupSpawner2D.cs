// UTF-8
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamagePopupSpawner2D : MonoBehaviour
{
    [Header("프리팹")]
    [SerializeField, InspectorName("팝업 프리팹")]
    private DamagePopup2D popupPrefab;

    [Header("풀링")]
    [SerializeField, InspectorName("미리 생성 개수")]
    private int prewarmCount = 30;

    [SerializeField, InspectorName("풀 루트(선택)")]
    [Tooltip("비워두면 이 오브젝트 아래에 생성됩니다.")]
    private Transform poolRoot;

    private readonly Queue<DamagePopup2D> _pool = new Queue<DamagePopup2D>();

    private void Awake()
    {
        if (poolRoot == null) poolRoot = transform;
        Prewarm();
    }

    private void OnEnable()
    {
        DamageEvents2D.OnDamagePopupRequested += HandlePopupRequest;
    }

    private void OnDisable()
    {
        DamageEvents2D.OnDamagePopupRequested -= HandlePopupRequest;
    }

    private void Prewarm()
    {
        if (popupPrefab == null) return;

        int n = Mathf.Max(0, prewarmCount);
        for (int i = 0; i < n; i++)
        {
            var p = CreateInstanceToPool();
            p.gameObject.SetActive(false);
            _pool.Enqueue(p);
        }
    }

    private DamagePopup2D Get()
    {
        if (_pool.Count > 0) return _pool.Dequeue();

        var p = CreateInstanceToPool();
        p.gameObject.SetActive(false);
        return p;
    }

    private void Return(DamagePopup2D p)
    {
        if (p == null) return;
        p.gameObject.SetActive(false);
        _pool.Enqueue(p);
    }

    private DamagePopup2D CreateInstanceToPool()
    {
        // 경고 제거 핵심:
        // - persistent(DontDestroyOnLoad) 부모를 Instantiate(parent)로 넘기면 Unity가 부모를 무시하며 경고를 띄운다.
        // - 해결: parent 없이 Instantiate → 그 다음 SetParent로 붙인다.
        var p = Instantiate(popupPrefab);

        if (poolRoot != null)
            p.transform.SetParent(poolRoot, false);

        return p;
    }

    private void HandlePopupRequest(DamageEvents2D.DamagePopupRequest req)
    {
        if (popupPrefab == null) return;

        float opacity = 1f;
        if (GameSettingsRuntime.HasInstance)
            opacity = GameSettingsRuntime.Instance.DamageNumberOpacity;

        var popup = Get();
        popup.Play(req.Amount, req.Element, opacity, req.WorldPos, Return);
    }
}