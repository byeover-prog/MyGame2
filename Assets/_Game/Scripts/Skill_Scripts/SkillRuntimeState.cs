using System;
using System.Collections.Generic;
using UnityEngine;

// 런에서 보유한 스킬/패시브/전용스킬의 레벨을 관리한다.
//  v2 변경사항:
// - OfferKind.CharacterSkill 지원
// - 전용 스킬은 Weapon 슬롯 카운트와 별도로 관리되지만,
//   합산 슬롯 제한(useTotalSlotCap)에는 포함된다.
// - maxCharacterSkillSlots (기본 2): 전용 스킬 최대 보유 수>
[DisallowMultipleComponent]
public sealed class SkillRuntimeState : MonoBehaviour
{
    [Header("슬롯 제한")]
    [SerializeField, Min(0)] private int maxWeaponSlots = 4;
    [SerializeField, Min(0)] private int maxPassiveSlots = 4;

    [Tooltip("캐릭터 전용 스킬 슬롯 최대 개수")]
    [SerializeField, Min(0)] private int maxCharacterSkillSlots = 2;

    [SerializeField] private bool useTotalSlotCap = true;
    [SerializeField, Min(0)] private int maxTotalSlots = 10;

    [Header("디버그/운영")]
    [SerializeField] private bool clearOnAwake = false;
    [SerializeField] private bool enableLogs = true;

    // id -> level
    private readonly Dictionary<string, int> _weaponLevels = new(32);
    private readonly Dictionary<string, int> _passiveLevels = new(32);
    private readonly Dictionary<string, int> _charSkillLevels = new(4);

    public int WeaponCount => _weaponLevels.Count;
    public int PassiveCount => _passiveLevels.Count;
    public int CharacterSkillCount => _charSkillLevels.Count;
    public int TotalCount => _weaponLevels.Count + _passiveLevels.Count + _charSkillLevels.Count;

    private void Awake()
    {
        if (clearOnAwake)
            ClearAll();
    }

    // 해당 kind에 대한 Dictionary를 반환한다.
    private Dictionary<string, int> GetMap(OfferKind kind)
    {
        return kind switch
        {
            OfferKind.Weapon         => _weaponLevels,
            OfferKind.Passive        => _passiveLevels,
            OfferKind.CharacterSkill => _charSkillLevels,
            _                        => null
        };
    }

    public bool Has(OfferKind kind, string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        var map = GetMap(kind);
        return map != null && map.ContainsKey(id);
    }

    public int GetLevel(OfferKind kind, string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return 0;
        var map = GetMap(kind);
        if (map == null) return 0;
        return map.TryGetValue(id, out int lv) ? lv : 0;
    }

    public bool CanAcquire(OfferKind kind)
    {
        // 합산 제한(옵션)
        if (useTotalSlotCap && maxTotalSlots > 0 && TotalCount >= maxTotalSlots)
            return false;

        return kind switch
        {
            OfferKind.Weapon         => maxWeaponSlots > 0 && _weaponLevels.Count < maxWeaponSlots,
            OfferKind.Passive        => maxPassiveSlots > 0 && _passiveLevels.Count < maxPassiveSlots,
            OfferKind.CharacterSkill => maxCharacterSkillSlots > 0 && _charSkillLevels.Count < maxCharacterSkillSlots,
            _                        => false
        };
    }
    
    // 상태 갱신: 획득 또는 레벨업.
    // - 이미 보유면 level++
    // - 미보유면 슬롯 제한을 만족할 때만 level=1로 추가
    
    public int GrantOrLevelUp(OfferKind kind, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return 0;

        var map = GetMap(kind);
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
        _charSkillLevels.Clear();
    }
    
    // Save/Load 지원
    
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

        foreach (var kv in _charSkillLevels)
            list.Add(new Entry { kind = OfferKind.CharacterSkill, id = kv.Key, level = kv.Value });

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
            var map = GetMap(e.kind);
            if (map != null)
                map[e.id] = lv;
        }
    }
}