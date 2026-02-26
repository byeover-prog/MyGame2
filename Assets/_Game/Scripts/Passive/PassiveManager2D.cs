using UnityEngine;

[DisallowMultipleComponent]
public sealed class PassiveManager2D : MonoBehaviour
{
    [SerializeField] private PassiveCatalogSO catalog;
    [SerializeField] private Transform owner;

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

    private void Awake()
    {
        if (owner == null) owner = transform;

        int count = System.Enum.GetValues(typeof(PassiveKind)).Length;
        _levels = new int[count];
    }

    public int GetLevel(PassiveKind kind)
    {
        int idx = (int)kind;
        if (_levels == null || idx < 0 || idx >= _levels.Length) return 0;
        return _levels[idx];
    }

    public bool IsMaxLevel(PassiveConfigSO p)
    {
        if (p == null) return true;
        int cur = GetLevel(p.kind);
        int max = Mathf.Max(1, p.maxLevel);
        return cur >= max;
    }

    public void Upgrade(PassiveConfigSO p)
    {
        if (p == null) return;

        int idx = (int)p.kind;
        int cur = _levels[idx];
        int max = Mathf.Max(1, p.maxLevel);

        if (cur >= max) return;

        _levels[idx] = Mathf.Clamp(cur + 1, 1, max);
        RecomputeAndApply();
    }

    private void RecomputeAndApply()
    {
        var stats = owner != null ? owner.GetComponentInParent<PlayerCombatStats2D>() : null;
        if (stats == null && owner != null)
            stats = owner.gameObject.AddComponent<PlayerCombatStats2D>();

        float atkBonus = SumPercent(PassiveKind.AttackDamage);
        float defBonus = SumPercent(PassiveKind.Defense);
        float cdBonus  = SumPercent(PassiveKind.CooldownReduction);
        float msBonus  = SumPercent(PassiveKind.MoveSpeed);
        float prBonus  = SumPercent(PassiveKind.PickupRange);
        float areaBonus = SumPercent(PassiveKind.SkillArea);
        float elemBonus = SumPercent(PassiveKind.ElementDamage);

        // 합연산 → 배율화 (캡은 여기서 걸어버림)
        stats.SetDamageMul(1f + atkBonus);
        stats.SetIncomingDamageMul(1f - Mathf.Clamp(defBonus, 0f, 0.60f)); // 최대 60% 감소까지만
        stats.SetCooldownMul(1f - Mathf.Clamp(cdBonus, 0f, 0.60f));        // 쿨감도 60% 상한
        stats.SetMoveSpeedMul(1f + Mathf.Clamp(msBonus, 0f, 1.00f));
        stats.SetPickupRangeMul(1f + Mathf.Clamp(prBonus, 0f, 2.00f));
        stats.SetAreaMul(1f + Mathf.Clamp(areaBonus, 0f, 1.00f));
        stats.SetElementDamageMul(1f + Mathf.Clamp(elemBonus, 0f, 1.00f));

        // 최대체력(정수)
        int hpAdd = SumInt(PassiveKind.MaxHp);
        var hp = owner != null ? owner.GetComponentInParent<PlayerHealth>() : null;
        if (hp != null)
            hp.SetMaxHpBonus(hpAdd, healToFull: true);
    }

    private float SumPercent(PassiveKind kind)
    {
        if (catalog == null || catalog.passives == null) return 0f;
        var cfg = catalog.passives.Find(x => x != null && x.kind == kind);
        if (cfg == null) return 0f;

        int lv = GetLevel(kind);
        float sum = 0f;
        for (int i = 1; i <= lv; i++)
            sum += cfg.GetLevelParams(i).addPercent;

        return sum;
    }

    private int SumInt(PassiveKind kind)
    {
        if (catalog == null || catalog.passives == null) return 0;
        var cfg = catalog.passives.Find(x => x != null && x.kind == kind);
        if (cfg == null) return 0;

        int lv = GetLevel(kind);
        int sum = 0;
        for (int i = 1; i <= lv; i++)
            sum += cfg.GetLevelParams(i).addInt;

        return sum;
    }
}