// UTF-8
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - 시작 시(획득 시) 검을 "최대치"만큼 미리 만들어두고, 레벨에 맞게 활성/비활성만 한다.
// - 그래서 레벨업 때 Destroy/Instantiate가 발생하지 않아 멈춤 현상이 사라진다.
// - 검의 위치는 매 프레임 플레이어 주위를 원형으로 돌도록 갱신한다.
[DisallowMultipleComponent]
public sealed class OrbitingSwordSkill2D : MonoBehaviour, ISkill2D
{
    [Header("Prefab")]
    [Tooltip("회전검 프리팹(OrbitingSwordBlade2D + Trigger Collider 권장)")]
    [SerializeField] private OrbitingSwordBlade2D swordPrefab;

    [Tooltip("검이 생성될 부모(비우면 자동으로 자기 밑에 SwordContainer 생성)")]
    [SerializeField] private Transform container;

    [Header("Combat")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private int damage = 8;

    [Header("Motion")]
    [Tooltip("플레이어로부터 거리")]
    [SerializeField] private float radius = 1.2f;

    [Tooltip("회전 속도(도/초)")]
    [SerializeField] private float rotateSpeedDeg = 180f;

    [Tooltip("최대 검 개수(프로토타입: 8 권장)")]
    [SerializeField, Range(1, 16)] private int maxSwords = 8;

    [Header("획득/장착")]
    [SerializeField] private bool startOwned = false;
    [SerializeField] private bool startEquipped = false;

    [Header("Level")]
    [Range(1, 8)]
    [SerializeField] private int level = 1;

    private bool _owned;
    private bool _equipped;

    private readonly List<OrbitingSwordBlade2D> _swords = new List<OrbitingSwordBlade2D>(16);
    private float _angle;

    public bool IsOwned => _owned;
    public bool IsEquipped => _equipped;
    public int Level => level;

    private void Awake()
    {
        _owned = startOwned;
        _equipped = startEquipped;
        if (_equipped) _owned = true;

        if (container == null)
        {
            var go = new GameObject("SwordContainer");
            go.transform.SetParent(transform, false);
            container = go.transform;
        }

        if (_owned)
            EnsurePool();
        ApplyActiveCount();
    }

    private void Update()
    {
        if (!_owned || !_equipped) return;
        if (Time.timeScale <= 0f) return;

        EnsurePool();

        int count = GetSwordCount(level);
        if (count <= 0) return;

        _angle += rotateSpeedDeg * Time.deltaTime;

        Vector2 center = transform.position;
        float step = 360f / count;

        for (int i = 0; i < _swords.Count; i++)
        {
            bool active = i < count;
            var s = _swords[i];
            if (s == null) continue;

            if (s.gameObject.activeSelf != active)
                s.gameObject.SetActive(active);

            if (!active) continue;

            float a = _angle + step * i;
            float rad = a * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

            s.transform.position = center + offset;
            s.transform.rotation = Quaternion.Euler(0f, 0f, a);
        }
    }

    public void Grant(int startLevelValue, bool equip)
    {
        _owned = true;
        _equipped = equip;
        SetLevel(startLevelValue);

        EnsurePool();
        ApplyActiveCount();
    }

    public void Upgrade(int delta)
    {
        if (!_owned)
        {
            Grant(1, true);
            return;
        }

        SetLevel(level + delta);
        _equipped = true;
        ApplyActiveCount();
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Clamp(newLevel, 1, 8);
    }

    public void SetEquipped(bool equip)
    {
        if (!_owned && equip) _owned = true;
        _equipped = equip;

        if (_owned) EnsurePool();
        ApplyActiveCount();
    }

    private void EnsurePool()
    {
        if (swordPrefab == null) return;

        while (_swords.Count < maxSwords)
        {
            var s = Instantiate(swordPrefab, container);
            s.gameObject.SetActive(false);
            s.Bind(enemyMask, damage);
            _swords.Add(s);
        }
    }

    private void ApplyActiveCount()
    {
        int count = (_owned && _equipped) ? GetSwordCount(level) : 0;

        for (int i = 0; i < _swords.Count; i++)
        {
            if (_swords[i] == null) continue;
            _swords[i].gameObject.SetActive(i < count);
        }
    }

    private int GetSwordCount(int lv)
    {
        // 기획: 2레벨부터 레벨당 검 개수 증가 → 사실상 "레벨=검개수"로 두면 직관적
        return Mathf.Clamp(lv, 1, maxSwords);
    }
}