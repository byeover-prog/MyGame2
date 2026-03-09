using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 회전검 무기 (구버전 / WeaponSystem 경로)
/// ★ 오브젝트 풀링 방식으로 전환:
///   - Rebuild() 시 Destroy/Instantiate 대신 활성/비활성 전환만 수행
///   - Awake에서 최대 개수만큼 미리 생성 후 재사용
/// </summary>
[DisallowMultipleComponent]
public sealed class OrbitSwordWeapon2D : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform owner; // 보통 플레이어
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private Transform worldRoot; // 비우면 null(parent 없음)

    [Header("회전 설정")]
    [SerializeField] private float radius = 1.2f;
    [SerializeField] private float angularSpeedDeg = 240f;

    [Header("검 개수")]
    [SerializeField, Min(1)] private int swordCount = 1;

    [Header("풀링")]
    [Tooltip("미리 생성할 최대 검 수. 이후 SetSwordCount에서 이 수를 초과하지 않음.")]
    [SerializeField, Min(1)] private int maxPool = 8;

    // ★ 풀링: 미리 생성된 검 리스트 (절대 런타임 중 Destroy하지 않음)
    private readonly List<Transform> _swords = new List<Transform>(8);
    private float _angle;
    private bool _poolReady;

    private void Awake()
    {
        if (owner == null) owner = transform; // 임시: 붙어있는 오브젝트를 오너로
        EnsurePool();
        ApplyActiveCount();
    }

    private void Update()
    {
        if (owner == null) return;

        // 활성화된 검만 카운트
        int active = Mathf.Min(swordCount, _swords.Count);
        if (active <= 0) return;

        _angle += angularSpeedDeg * Time.deltaTime;

        float step = 360f / Mathf.Max(1, active);
        for (int i = 0; i < _swords.Count; i++)
        {
            if (_swords[i] == null) continue;

            if (i >= active)
            {
                // 비활성 검은 위치 갱신 불필요
                continue;
            }

            float a = (_angle + step * i) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            _swords[i].position = owner.position + offset;
        }
    }

    public void SetSwordCount(int count)
    {
        swordCount = Mathf.Clamp(count, 1, maxPool);
        EnsurePool();
        ApplyActiveCount();
    }

    /// <summary>
    /// ★ 풀링 핵심: 최대 개수만큼 미리 생성, 이후에는 Instantiate하지 않음
    /// </summary>
    private void EnsurePool()
    {
        if (swordPrefab == null) return;

        int want = Mathf.Max(swordCount, maxPool);

        while (_swords.Count < want)
        {
            var go = Instantiate(swordPrefab);
            go.name = $"{swordPrefab.name}_Pool_{_swords.Count}";

            if (worldRoot != null) go.transform.SetParent(worldRoot, true);
            else go.transform.SetParent(null, true);

            go.SetActive(false);
            _swords.Add(go.transform);
        }

        _poolReady = true;
    }

    /// <summary>
    /// ★ 풀링: swordCount에 맞게 활성/비활성만 전환 (Destroy/Instantiate 없음)
    /// </summary>
    private void ApplyActiveCount()
    {
        int active = Mathf.Clamp(swordCount, 0, _swords.Count);
        for (int i = 0; i < _swords.Count; i++)
        {
            if (_swords[i] == null) continue;
            _swords[i].gameObject.SetActive(i < active);
        }
    }

    /// <summary>
    /// 씬 전환 시에만 정리
    /// </summary>
    private void OnDestroy()
    {
        for (int i = 0; i < _swords.Count; i++)
        {
            if (_swords[i] != null)
                Destroy(_swords[i].gameObject);
        }
        _swords.Clear();
    }
}