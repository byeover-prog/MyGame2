// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 런타임 상태만 관리.
/// - id -> level (Dictionary)
/// - 스킬/패시브 슬롯 제한
/// - 데이터(SO/JSON)나 프리팹 로딩 책임 없음
/// </summary>
[DisallowMultipleComponent]
public sealed class SkillRuntimeState : MonoBehaviour
{
    [Header("슬롯 제한(기본값은 4/4/8)")]
    [Tooltip("스킬 슬롯 최대 개수(0이면 스킬 획득 금지)")]
    [SerializeField, Min(0)] private int maxSkillSlots = 4;

    [Tooltip("패시브 슬롯 최대 개수(0이면 패시브 획득 금지)")]
    [SerializeField, Min(0)] private int maxPassiveSlots = 4;

    [Tooltip("스킬+패시브 합산 슬롯 제한 사용")]
    [SerializeField] private bool useTotalSlotCap = true;

    [Tooltip("스킬+패시브 합산 슬롯 최대 개수(0이면 합산 제한 미사용)")]
    [SerializeField, Min(0)] private int maxTotalSlots = 8;

    [Header("디버그/운영")]
    [Tooltip("플레이 시작(Awake) 때 상태를 자동 초기화")]
    [SerializeField] private bool clearOnAwake = false;

    // id -> level
    private readonly Dictionary<string, int> _skillLevels = new Dictionary<string, int>(32);
    private readonly Dictionary<string, int> _passiveLevels = new Dictionary<string, int>(32);

    public int SkillCount => _skillLevels.Count;
    public int PassiveCount => _passiveLevels.Count;
    public int TotalCount => _skillLevels.Count + _passiveLevels.Count;

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
            OfferKind.Skill => _skillLevels.ContainsKey(id),
            OfferKind.Passive => _passiveLevels.ContainsKey(id),
            _ => false
        };
    }

    public int GetLevel(OfferKind kind, string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return 0;

        return kind switch
        {
            OfferKind.Skill => _skillLevels.TryGetValue(id, out int lvS) ? lvS : 0,
            OfferKind.Passive => _passiveLevels.TryGetValue(id, out int lvP) ? lvP : 0,
            _ => 0
        };
    }

    public bool CanAcquire(OfferKind kind)
    {
        int total = TotalCount;

        if (useTotalSlotCap && maxTotalSlots > 0 && total >= maxTotalSlots)
            return false;

        return kind switch
        {
            OfferKind.Skill => maxSkillSlots <= 0 ? false : _skillLevels.Count < maxSkillSlots,
            OfferKind.Passive => maxPassiveSlots <= 0 ? false : _passiveLevels.Count < maxPassiveSlots,
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

        var map = (kind == OfferKind.Passive) ? _passiveLevels : _skillLevels;

        if (map.TryGetValue(id, out int lv))
        {
            lv++;
            map[id] = lv;
            return lv;
        }

        if (!CanAcquire(kind))
        {
            Debug.LogWarning($"[SkillRuntimeState] 슬롯이 가득 차서 '{id}'({kind}) 획득 불가");
            return 0;
        }

        map.Add(id, 1);
        return 1;
    }

    public void ClearAll()
    {
        _skillLevels.Clear();
        _passiveLevels.Clear();
    }
}