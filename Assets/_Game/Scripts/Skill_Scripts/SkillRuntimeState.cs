// UTF-8
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillRuntimeState : MonoBehaviour
{
    [Header("슬롯 제한(기본값은 4/4/8)")]
    [Tooltip("무기 슬롯 최대 개수(0이면 무기 획득 금지)")]
    [SerializeField, Min(0)] private int maxWeaponSlots = 4;

    [Tooltip("패시브 슬롯 최대 개수(0이면 패시브 획득 금지)")]
    [SerializeField, Min(0)] private int maxPassiveSlots = 4;

    [Tooltip("무기+패시브 합산 슬롯 제한 사용")]
    [SerializeField] private bool useTotalSlotCap = true;

    [Tooltip("무기+패시브 합산 슬롯 최대 개수(0이면 합산 제한 미사용)")]
    [SerializeField, Min(0)] private int maxTotalSlots = 8;

    [Header("디버그/운영")]
    [Tooltip("플레이 시작(Awake) 때 상태를 자동 초기화")]
    [SerializeField] private bool clearOnAwake = false;

    [Tooltip("슬롯 Full 등 중요한 경고 로그 출력")]
    [SerializeField] private bool enableLogs = true;

    // id -> level
    private readonly Dictionary<string, int> _weaponLevels = new Dictionary<string, int>(32);
    private readonly Dictionary<string, int> _passiveLevels = new Dictionary<string, int>(32);

    public int WeaponCount => _weaponLevels.Count;
    public int PassiveCount => _passiveLevels.Count;
    public int TotalCount => _weaponLevels.Count + _passiveLevels.Count;

    private void Awake()
    {
        if (clearOnAwake)
            ClearAll();
    }

    public bool Has(OfferKind kind, string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        return kind switch
        {
            OfferKind.Weapon => _weaponLevels.ContainsKey(id),
            OfferKind.Passive => _passiveLevels.ContainsKey(id),
            _ => false
        };
    }

    public int GetLevel(OfferKind kind, string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return 0;

        return kind switch
        {
            OfferKind.Weapon => _weaponLevels.TryGetValue(id, out int lvW) ? lvW : 0,
            OfferKind.Passive => _passiveLevels.TryGetValue(id, out int lvP) ? lvP : 0,
            _ => 0
        };
    }

    public bool CanAcquire(OfferKind kind)
    {
        // 합산 제한(옵션)
        if (useTotalSlotCap && maxTotalSlots > 0 && TotalCount >= maxTotalSlots)
            return false;

        return kind switch
        {
            OfferKind.Weapon => maxWeaponSlots > 0 && _weaponLevels.Count < maxWeaponSlots,
            OfferKind.Passive => maxPassiveSlots > 0 && _passiveLevels.Count < maxPassiveSlots,
            _ => false
        };
    }

    /// <summary>
    /// 상태 갱신: 획득 또는 레벨업
    /// - 이미 보유면 level++
    /// - 미보유면 슬롯 제한을 만족할 때만 level=1로 추가
    /// </summary>
    public int GrantOrLevelUp(OfferKind kind, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return 0;

        Dictionary<string, int> map = kind switch
        {
            OfferKind.Passive => _passiveLevels,
            OfferKind.Weapon => _weaponLevels,
            _ => null
        };

        if (map == null)
        {
            if (enableLogs)
                GameLogger.LogWarning($"[SkillRuntimeState] 알 수 없는 kind: {kind} (id={id})", this);
            return 0;
        }

        // 기존 보유 => 레벨업(슬롯 cap 영향 없음)
        if (map.TryGetValue(id, out int lv))
        {
            lv = Mathf.Max(1, lv + 1);
            map[id] = lv;
            return lv;
        }

        // 신규 획득 => 슬롯 제한 체크
        if (!CanAcquire(kind))
        {
            if (enableLogs)
                GameLogger.LogWarning($"[SkillRuntimeState] 슬롯이 가득 차서 '{id}'({kind}) 획득 불가", this);
            return 0;
        }

        map.Add(id, 1);
        return 1;
    }

    public void ClearAll()
    {
        _weaponLevels.Clear();
        _passiveLevels.Clear();
    }

    // ----------------------------
    // Save/Load 지원(선택)
    // ----------------------------
    [Serializable]
    public struct Entry
    {
        public OfferKind kind;
        public string id;
        public int level;
    }

    [Serializable]
    public struct Snapshot
    {
        public Entry[] entries;
    }

    public Snapshot CreateSnapshot()
    {
        var list = new List<Entry>(TotalCount);

        foreach (var kv in _weaponLevels)
            list.Add(new Entry { kind = OfferKind.Weapon, id = kv.Key, level = kv.Value });

        foreach (var kv in _passiveLevels)
            list.Add(new Entry { kind = OfferKind.Passive, id = kv.Key, level = kv.Value });

        return new Snapshot { entries = list.ToArray() };
    }

    public void ApplySnapshot(Snapshot snap, bool clearBeforeApply = true)
    {
        if (clearBeforeApply) ClearAll();
        if (snap.entries == null) return;

        for (int i = 0; i < snap.entries.Length; i++)
        {
            var e = snap.entries[i];
            if (string.IsNullOrWhiteSpace(e.id)) continue;

            int lv = Mathf.Max(1, e.level);

            if (e.kind == OfferKind.Weapon)
                _weaponLevels[e.id] = lv;
            else if (e.kind == OfferKind.Passive)
                _passiveLevels[e.id] = lv;
        }
    }
}