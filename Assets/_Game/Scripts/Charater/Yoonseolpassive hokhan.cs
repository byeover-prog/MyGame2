// ──────────────────────────────────────────────
// YoonseolPassive_Hokhan.cs
// 윤설 고유 패시브 — "혹한의 궁사"
//
// [동작 원리]
// 1. 빙결(Ice) 속성 데미지가 적에게 적중하면 "혹한" 중첩 1 부여
// 2. 혹한 중첩이 있는 적에게 어떤 데미지든 들어가면
//    → 중첩 수 × 1% 만큼 추가 데미지 적용
// 3. 최대 100중첩
// 4. 적 사망 시 중첩 자동 정리
//
// [예시]
// 적에게 혹한 30중첩 → 100 데미지 적중 시 → 30% 추가 = +30 추가 데미지
//
// [Hierarchy / Inspector]
// Player 오브젝트에 CharacterPassiveManager2D가 관리
// 이 컴포넌트를 직접 부착할 필요 없음 — Manager가 자동 생성
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
    // ═══════════════════════════════════════════════════════
    //  설정
    // ═══════════════════════════════════════════════════════

    [Header("혹한의 궁사 설정")]
    [Tooltip("중첩당 추가 데미지 비율입니다. 0.01 = 1%")]
    [SerializeField] private float bonusDamagePerStack = 0.01f;

    [Tooltip("최대 혹한 중첩 수입니다.")]
    [SerializeField] private int maxStacks = 100;

    [Tooltip("빙결 적중 1회당 부여되는 중첩 수입니다.")]
    [SerializeField] private int stacksPerHit = 1;

    // ═══════════════════════════════════════════════════════
    //  프로퍼티
    // ═══════════════════════════════════════════════════════

    public override string PassiveName => "혹한의 궁사";
    public override string Description =>
        $"빙결 속성 스킬 적중 시 '혹한' 중첩 부여. " +
        $"중첩당 {bonusDamagePerStack * 100f:F0}% 추가 데미지. 최대 {maxStacks}중첩.";

    // ═══════════════════════════════════════════════════════
    //  런타임 상태
    // ═══════════════════════════════════════════════════════

    /// <summary>적 rootId → 혹한 중첩 수</summary>
    private readonly Dictionary<int, int> _stacks = new Dictionary<int, int>(64);

    /// <summary>추가 데미지 → 이벤트 재진입 방지 플래그</summary>
    private bool _applyingBonus;

    // ═══════════════════════════════════════════════════════
    //  공개 API (외부 조회용)
    // ═══════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════
    //  활성화 / 비활성화
    // ═══════════════════════════════════════════════════════

    protected override void OnActivate()
    {
        _stacks.Clear();
        _applyingBonus = false;
        DamageEvents2D.OnEnemyDamageApplied += HandleDamageApplied;
    }

    protected override void OnDeactivate()
    {
        DamageEvents2D.OnEnemyDamageApplied -= HandleDamageApplied;
        _stacks.Clear();
        _applyingBonus = false;
    }

    // ═══════════════════════════════════════════════════════
    //  이벤트 처리
    // ═══════════════════════════════════════════════════════

    private void HandleDamageApplied(DamageEvents2D.EnemyDamageAppliedInfo info)
    {
        if (!IsActive) return;
        if (info.Target == null) return;

        int rootId = DamageUtil2D.GetRootId(info.Target);

        // ── 1단계: 빙결 속성이면 혹한 중첩 추가 ──
        if (info.Element == DamageElement2D.Ice)
        {
            int current = _stacks.TryGetValue(rootId, out int val) ? val : 0;
            int newStacks = Mathf.Min(current + stacksPerHit, maxStacks);
            _stacks[rootId] = newStacks;

            // 중첩 변화 시 디버그 (필요 시 주석 해제)
            // Debug.Log($"[혹한] {info.Target.name} 중첩: {current} → {newStacks}");
        }

        // ── 2단계: 중첩이 있으면 추가 데미지 ──
        // 재진입 방지: 추가 데미지가 또 이벤트를 발생시키므로
        if (_applyingBonus) return;

        if (_stacks.TryGetValue(rootId, out int stacks) && stacks > 0)
        {
            float bonusRate = stacks * bonusDamagePerStack;
            int bonusDamage = Mathf.Max(1, Mathf.RoundToInt(info.Amount * bonusRate));

            _applyingBonus = true;
            DamageUtil2D.TryApplyDamage(info.Target, bonusDamage, info.Element);
            _applyingBonus = false;
        }

        // ── 3단계: 적 사망 시 중첩 정리 ──
        var damageable = info.Target.GetComponentInParent<IDamageable2D>();
        if (damageable != null && damageable.IsDead)
        {
            _stacks.Remove(rootId);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  정리
    // ═══════════════════════════════════════════════════════

    /// <summary>모든 혹한 중첩을 초기화합니다. (스테이지 클리어 등)</summary>
    public void ClearAllStacks()
    {
        _stacks.Clear();
    }
}