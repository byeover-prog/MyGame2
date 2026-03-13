// ──────────────────────────────────────────────
// PassiveManager2D.cs
// ★ SkillDefinitionSO + PassiveStatType 기반으로 재작성
//    수치는 설계 문서 기준 하드코딩
//    PassiveConfigSO 하위 호환 유지
// ──────────────────────────────────────────────

using UnityEngine;
using _Game.Skills;

[DisallowMultipleComponent]
public sealed class PassiveManager2D : MonoBehaviour
{
    [Header("=== 참조 ===")]

    [SerializeField, Tooltip("패시브 카탈로그 SO (SkillDefinitionSO 목록)")]
    private PassiveCatalogSO catalog;

    [SerializeField, Tooltip("패시브 효과 적용 대상")]
    private Transform owner;

    // ── 런타임 레벨 배열 (PassiveStatType 인덱스) ──

    private int[] _levels;

    public int AcquiredCount
    {
        get
        {
            if (_levels == null) return 0;
            int c = 0;
            for (int i = 0; i < _levels.Length; i++)
                if (_levels[i] > 0) c++;
            return c;
        }
    }

    // ── 초기화 ─────────────────────────────────

    private void Awake()
    {
        if (owner == null) owner = transform;

        int count = System.Enum.GetValues(typeof(PassiveStatType)).Length;
        _levels = new int[count];
    }

    // ── 조회 ───────────────────────────────────

    public int GetLevel(PassiveStatType stat)
    {
        int idx = (int)stat;
        if (_levels == null || idx < 0 || idx >= _levels.Length) return 0;
        return _levels[idx];
    }

    public int GetLevel(PassiveKind kind)
    {
        return GetLevel(MapKindToStat(kind));
    }

    public bool IsMaxLevel(SkillDefinitionSO p)
    {
        if (p == null) return true;
        int cur = GetLevel(p.PassiveStatType);
        int max = Mathf.Max(1, p.MaxLevel);
        return cur >= max;
    }

    public bool IsMaxLevel(PassiveConfigSO p)
    {
        if (p == null) return true;
        int cur = GetLevel(MapKindToStat(p.kind));
        int max = Mathf.Max(1, p.maxLevel);
        return cur >= max;
    }

    // ── 업그레이드 ─────────────────────────────

    public void Upgrade(SkillDefinitionSO p)
    {
        if (p == null) return;
        if (p.PassiveStatType == PassiveStatType.None) return;

        int idx = (int)p.PassiveStatType;
        if (idx < 0 || idx >= _levels.Length) return;

        int cur = _levels[idx];
        int max = Mathf.Max(1, p.MaxLevel);
        if (cur >= max) return;

        _levels[idx] = Mathf.Clamp(cur + 1, 1, max);
        RecomputeAndApply();
    }

    public void Upgrade(PassiveConfigSO p)
    {
        if (p == null) return;

        PassiveStatType mapped = MapKindToStat(p.kind);
        if (mapped == PassiveStatType.None) return;

        int idx = (int)mapped;
        if (idx < 0 || idx >= _levels.Length) return;

        int cur = _levels[idx];
        int max = Mathf.Max(1, p.maxLevel);
        if (cur >= max) return;

        _levels[idx] = Mathf.Clamp(cur + 1, 1, max);
        RecomputeAndApply();
    }

    // ── 스탯 재계산 ───────────────────────────

    private void RecomputeAndApply()
    {
        var stats = owner != null ? owner.GetComponentInParent<PlayerCombatStats2D>() : null;
        if (stats == null && owner != null)
            stats = owner.gameObject.AddComponent<PlayerCombatStats2D>();

        float atkBonus   = SumPercent(PassiveStatType.AttackPowerPercent);
        float defBonus   = SumPercent(PassiveStatType.DefensePercent);
        float hasteBonus = SumPercent(PassiveStatType.SkillHastePercent);
        float msBonus    = SumPercent(PassiveStatType.MoveSpeedPercent);
        float prBonus    = SumPercent(PassiveStatType.PickupRangePercent);
        float areaBonus  = SumPercent(PassiveStatType.SkillAreaPercent);
        float expBonus   = SumPercent(PassiveStatType.ExpGainPercent);

        // 공격력: (기본+고정) × (1 + 증가%/100) — 합연산
        stats.SetDamageMul(1f + atkBonus);

        // 방어력: 받는피해 = 초기피해 × 100/(100+방어력) — LoL 유효체력 공식
        float defStat = defBonus * 100f;
        float incomingMul = 100f / (100f + defStat);
        stats.SetIncomingDamageMul(Mathf.Clamp(incomingMul, 0.1f, 1f));

        // 스킬 가속: 최종쿨 = 기본쿨 × 100/(100+가속) — 무한발사 원천 차단
        float hasteStat = hasteBonus * 100f;
        float cooldownMul = 100f / (100f + hasteStat);
        stats.SetCooldownMul(Mathf.Clamp(cooldownMul, 0.1f, 1f));

        stats.SetMoveSpeedMul(1f + Mathf.Clamp(msBonus, 0f, 1.00f));
        stats.SetPickupRangeMul(1f + Mathf.Clamp(prBonus, 0f, 2.00f));
        stats.SetAreaMul(1f + Mathf.Clamp(areaBonus, 0f, 1.00f));
        stats.SetExpGainMul(1f + Mathf.Clamp(expBonus, 0f, 2.00f));

        // 최대체력
        int hpAdd = SumInt(PassiveStatType.MaxHpFlat);
        var hp = owner != null ? owner.GetComponentInParent<PlayerHealth>() : null;
        if (hp != null)
            hp.SetMaxHpBonus(hpAdd, healToFull: false);
    }

    // ── 수치 합산 (설계 문서 기준) ──────────────

    private float SumPercent(PassiveStatType stat)
    {
        int lv = GetLevel(stat);
        if (lv <= 0) return 0f;
        return GetPercentPerLevel(stat) * lv;
    }

    private int SumInt(PassiveStatType stat)
    {
        int lv = GetLevel(stat);
        if (lv <= 0) return 0;
        return GetIntPerLevel(stat) * lv;
    }

    private static float GetPercentPerLevel(PassiveStatType stat)
    {
        return stat switch
        {
            PassiveStatType.AttackPowerPercent => 0.10f,  // 공격력 레벨당 10%
            PassiveStatType.DefensePercent     => 0.10f,  // 방어력 레벨당 10%
            PassiveStatType.PickupRangePercent => 0.20f,  // 픽업범위 레벨당 20%
            PassiveStatType.MoveSpeedPercent   => 0.05f,  // 이동속도 레벨당 5%
            PassiveStatType.SkillHastePercent  => 0.10f,  // 스킬 가속 레벨당 10%
            PassiveStatType.SkillAreaPercent   => 0.05f,  // 스킬 범위 레벨당 5%
            PassiveStatType.ExpGainPercent     => 0.10f,  // 경험치 레벨당 10%
            _ => 0f
        };
    }

    private static int GetIntPerLevel(PassiveStatType stat)
    {
        return stat switch
        {
            PassiveStatType.MaxHpFlat => 20,  // 최대체력 레벨당 +20
            _ => 0
        };
    }

    // ── PassiveKind → PassiveStatType 매핑 (하위 호환) ──

    private static PassiveStatType MapKindToStat(PassiveKind kind)
    {
        return kind switch
        {
            PassiveKind.AttackDamage      => PassiveStatType.AttackPowerPercent,
            PassiveKind.Defense           => PassiveStatType.DefensePercent,
            PassiveKind.CooldownReduction => PassiveStatType.SkillHastePercent,
            PassiveKind.MoveSpeed         => PassiveStatType.MoveSpeedPercent,
            PassiveKind.PickupRange       => PassiveStatType.PickupRangePercent,
            PassiveKind.MaxHp             => PassiveStatType.MaxHpFlat,
            PassiveKind.ExpGain           => PassiveStatType.ExpGainPercent,
            PassiveKind.SkillArea         => PassiveStatType.SkillAreaPercent,
            _ => PassiveStatType.None
        };
    }
}