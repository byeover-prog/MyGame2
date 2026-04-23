// ──────────────────────────────────────────────
// YoonseolPassive_Hokhan.cs  (v2 — DamageChainGuard 적용)
// 윤설 고유 패시브 — "혹한의 궁사"
//
// [v2 변경사항]
// - DamageChainGuard를 사용하여 보너스/시너지 데미지에 반응하지 않음
// - 혹한 중첩 부여는 Ice 속성이면 항상 (보너스 포함)
// - 추가 데미지 적용은 원본 데미지에만
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 윤설 고유 패시브 "혹한의 궁사" 구현입니다.
/// 빙결 속성 스킬 적중 시 "혹한" 중첩을 부여하고,
/// 중첩 수에 비례하여 추가 데미지를 가합니다.
/// </summary>
public sealed class YoonseolPassive_Hokhan : CharacterPassiveBase
{
    [Header("혹한의 궁사 설정")]
    [Tooltip("중첩당 추가 데미지 비율입니다. 0.01 = 1%")]
    [SerializeField] private float bonusDamagePerStack = 0.01f;

    [Tooltip("최대 혹한 중첩 수입니다.")]
    [SerializeField] private int maxStacks = 100;

    [Tooltip("빙결 적중 1회당 부여되는 중첩 수입니다.")]
    [SerializeField] private int stacksPerHit = 1;

    public override string PassiveName => "혹한의 궁사";
    public override string Description =>
        $"빙결 속성 스킬 적중 시 '혹한' 중첩 부여. " +
        $"중첩당 {bonusDamagePerStack * 100f:F0}% 추가 데미지. 최대 {maxStacks}중첩.";

    /// <summary>적 rootId → 혹한 중첩 수</summary>
    private readonly Dictionary<int, int> _stacks = new Dictionary<int, int>(64);

    /// <summary>특정 적의 현재 혹한 중첩 수를 반환합니다.</summary>
    public int GetStacks(GameObject enemy)
    {
        if (enemy == null) return 0;
        int rootId = DamageUtil2D.GetRootId(enemy);
        return _stacks.TryGetValue(rootId, out int val) ? val : 0;
    }

    /// <summary>특정 적의 혹한 중첩을 수동으로 추가합니다. (궁극기 등에서 사용)</summary>
    public void AddStacks(GameObject enemy, int amount)
    {
        if (enemy == null || amount <= 0) return;
        int rootId = DamageUtil2D.GetRootId(enemy);
        int current = _stacks.TryGetValue(rootId, out int val) ? val : 0;
        _stacks[rootId] = Mathf.Min(current + amount, maxStacks);
    }

    protected override void OnActivate()
    {
        _stacks.Clear();
        DamageEvents2D.OnEnemyDamageApplied += HandleDamageApplied;
    }

    protected override void OnDeactivate()
    {
        DamageEvents2D.OnEnemyDamageApplied -= HandleDamageApplied;
        _stacks.Clear();
    }

    private void HandleDamageApplied(DamageEvents2D.EnemyDamageAppliedInfo info)
    {
        if (!IsActive) return;
        if (info.Target == null) return;

        int rootId = DamageUtil2D.GetRootId(info.Target);

        // ── 1단계: 빙결 속성이면 혹한 중첩 추가 (보너스 데미지 포함) ──
        if (info.Element == DamageElement2D.Ice)
        {
            int current = _stacks.TryGetValue(rootId, out int val) ? val : 0;
            _stacks[rootId] = Mathf.Min(current + stacksPerHit, maxStacks);
        }

        // ── 2단계: 추가 데미지는 원본 데미지에만 적용 (체인 방지) ──
        if (DamageChainGuard.IsProcessingBonus) return;

        if (_stacks.TryGetValue(rootId, out int stacks) && stacks > 0)
        {
            float bonusRate = stacks * bonusDamagePerStack;
            int bonusDamage = Mathf.Max(1, Mathf.RoundToInt(info.Amount * bonusRate));

            DamageChainGuard.BeginBonus();
            DamageUtil2D.TryApplyDamage(info.Target, bonusDamage, info.Element);
            DamageChainGuard.EndBonus();
        }

        // ── 3단계: 적 사망 시 중첩 정리 ──
        var damageable = info.Target.GetComponentInParent<IDamageable2D>();
        if (damageable != null && damageable.IsDead)
            _stacks.Remove(rootId);
    }

    public void ClearAllStacks() => _stacks.Clear();
}