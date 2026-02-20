using System.Collections.Generic;
using UnityEngine;

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

    private readonly List<Transform> _swords = new List<Transform>(8);
    private float _angle;

    private void Awake()
    {
        if (owner == null) owner = transform; // 임시: 붙어있는 오브젝트를 오너로
        Rebuild();
    }

    private void Update()
    {
        if (owner == null) return;

        _angle += angularSpeedDeg * Time.deltaTime;

        float step = 360f / Mathf.Max(1, _swords.Count);
        for (int i = 0; i < _swords.Count; i++)
        {
            float a = (_angle + step * i) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            _swords[i].position = owner.position + offset;
        }
    }

    public void SetSwordCount(int count)
    {
        swordCount = Mathf.Max(1, count);
        Rebuild();
    }

    private void Rebuild()
    {
        // 기존 제거
        for (int i = 0; i < _swords.Count; i++)
            if (_swords[i] != null) Destroy(_swords[i].gameObject);
        _swords.Clear();

        if (swordPrefab == null) return;

        for (int i = 0; i < swordCount; i++)
        {
            var go = Instantiate(swordPrefab);
            if (worldRoot != null) go.transform.SetParent(worldRoot, true);
            else go.transform.SetParent(null, true);

            _swords.Add(go.transform);
        }
    }
}