// ──────────────────────────────────────────────
// HarinPassive_Bongukkembeop.cs
// 하린 고유 패시브 — "본국검법"
//
// [동작 원리]
// 1. 적이 플레이어를 공격할 때 일정 확률로 "방어" 발동
// 2. 방어 성공 시 해당 피해의 50%만 적용 (절반 감소)
// 3. 방어 실패 시 평소대로 피해 적용
//
// [적용 방식]
// 정적 메서드 TryModifyIncomingDamage()를 제공합니다.
// PlayerHealth.TakeDamage()에서 이 메서드를 호출하여
// 최종 데미지를 수정합니다.
//
// [PlayerHealth 수정 필요]
// ApplyIncomingDamageMultiplier() 안에서
//   finalDamage = HarinPassive_Bongukkembeop.TryModifyIncomingDamage(finalDamage);
// 한 줄만 추가하면 됩니다. (상세 가이드는 아래 주석 참고)
//
// [Hierarchy / Inspector]
// Player 오브젝트에 CharacterPassiveManager2D가 관리
// 이 컴포넌트를 직접 부착할 필요 없음 — Manager가 자동 생성
// ──────────────────────────────────────────────

using UnityEngine;

/// <summary>
/// 하린 고유 패시브 "본국검법" 구현입니다.
/// 일정 확률로 적 공격을 방어하며, 방어 시 피해가 50% 감소합니다.
/// </summary>
public sealed class HarinPassive_Bongukkembeop : CharacterPassiveBase
{
    // ═══════════════════════════════════════════════════════
    //  설정
    // ═══════════════════════════════════════════════════════

    [Header("본국검법 설정")]
    [Tooltip("방어 발동 확률입니다. 0.25 = 25%")]
    [SerializeField] private float blockChance = 0.25f;

    [Tooltip("방어 성공 시 피해 감소 비율입니다. 0.50 = 50% 감소")]
    [SerializeField] private float damageReduction = 0.50f;

    // ═══════════════════════════════════════════════════════
    //  정적 접근자
    // ═══════════════════════════════════════════════════════

    /// <summary>현재 활성화된 하린 패시브 인스턴스입니다. null이면 비활성.</summary>
    private static HarinPassive_Bongukkembeop _instance;

    /// <summary>최근 방어 성공 횟수입니다. (UI 표시용)</summary>
    public static int TotalBlockCount { get; private set; }

    /// <summary>
    /// PlayerHealth에서 호출하는 정적 메서드입니다.
    /// 방어 판정을 수행하고 피해를 감소시킵니다.
    ///
    /// [PlayerHealth.ApplyIncomingDamageMultiplier 수정 예시]
    /// private int ApplyIncomingDamageMultiplier(int rawDamage)
    /// {
    ///     if (combatStats == null) return rawDamage;
    ///     float finalDamage = rawDamage * combatStats.IncomingDamageMul;
    ///     int result = Mathf.Max(1, Mathf.RoundToInt(finalDamage));
    ///     result = HarinPassive_Bongukkembeop.TryModifyIncomingDamage(result); // ← 이 줄 추가
    ///     return result;
    /// }
    /// </summary>
    /// <param name="damage">방어력 계산이 끝난 최종 데미지</param>
    /// <returns>방어 성공 시 감소된 데미지, 실패 시 원래 데미지</returns>
    public static int TryModifyIncomingDamage(int damage)
    {
        if (_instance == null || !_instance.IsActive) return damage;
        if (damage <= 0) return damage;

        float roll = Random.value; // 0.0 ~ 1.0
        if (roll < _instance.blockChance)
        {
            // 방어 성공!
            int reducedDamage = Mathf.Max(1, Mathf.RoundToInt(damage * (1f - _instance.damageReduction)));
            TotalBlockCount++;

            Debug.Log($"[본국검법] 방어 성공! 피해 {damage} → {reducedDamage} " +
                      $"(확률 {roll:F2} < {_instance.blockChance:F2}), " +
                      $"총 방어 횟수: {TotalBlockCount}");

            return reducedDamage;
        }

        return damage;
    }

    // ═══════════════════════════════════════════════════════
    //  프로퍼티
    // ═══════════════════════════════════════════════════════

    public override string PassiveName => "본국검법";
    public override string Description =>
        $"{blockChance * 100f:F0}% 확률로 적 공격 방어. " +
        $"방어 시 피해 {damageReduction * 100f:F0}% 감소.";

    // ═══════════════════════════════════════════════════════
    //  활성화 / 비활성화
    // ═══════════════════════════════════════════════════════

    protected override void OnActivate()
    {
        _instance = this;
        TotalBlockCount = 0;
    }

    protected override void OnDeactivate()
    {
        if (_instance == this)
            _instance = null;
        TotalBlockCount = 0;
    }
}