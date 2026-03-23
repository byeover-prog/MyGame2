// ──────────────────────────────────────────────
// HayulPassive_Dosa.cs  (v3 — 전파 보너스 +3)
// 하율 고유 패시브 — "도사란 무엇인가?"
//
// [v3 변경사항]
// - chainBonusCount: 5 → 3
// - 기본 전파 3회 + 패시브 3회 = 최대 6회
// ──────────────────────────────────────────────

using UnityEngine;

/// <summary>
/// 하율 고유 패시브 "도사란 무엇인가?" 구현입니다.
/// 전기 속성 전파 최대치를 증가시키고 전기 추가 데미지를 부여합니다.
/// </summary>
public sealed class HayulPassive_Dosa : CharacterPassiveBase
{
    [Header("도사란 무엇인가? 설정")]
    [Tooltip("전파 최대치 추가량입니다.")]
    [SerializeField] private int chainBonusCount = 3;

    [Tooltip("전기 속성 추가 데미지 비율입니다. 0.20 = 20%")]
    [SerializeField] private float electricBonusDamageRate = 0.20f;

    /// <summary>
    /// 현재 활성화된 하율 패시브의 전파 보너스 수치입니다.
    /// ElectricChainSystem2D, HayulUltimateResolver 등에서 조회합니다.
    /// </summary>
    public static int ChainBonus { get; private set; } = 0;

    /// <summary>현재 활성화된 하율 패시브의 전기 추가 데미지 비율입니다.</summary>
    public static float ElectricBonusRate { get; private set; } = 0f;

    public override string PassiveName => "도사란 무엇인가?";
    public override string Description =>
        $"전기 속성 전파 최대치 +{chainBonusCount}, " +
        $"전기 속성 스킬 {electricBonusDamageRate * 100f:F0}% 추가 데미지";

    protected override void OnActivate()
    {
        ChainBonus = chainBonusCount;
        ElectricBonusRate = electricBonusDamageRate;
        DamageEvents2D.OnEnemyDamageApplied += HandleDamageApplied;

        Debug.Log($"[하율 패시브] 전파 보너스 +{chainBonusCount}, " +
                  $"전기 추가 데미지 {electricBonusDamageRate * 100f}%");
    }

    protected override void OnDeactivate()
    {
        DamageEvents2D.OnEnemyDamageApplied -= HandleDamageApplied;
        ChainBonus = 0;
        ElectricBonusRate = 0f;
    }

    private void HandleDamageApplied(DamageEvents2D.EnemyDamageAppliedInfo info)
    {
        if (!IsActive) return;
        if (info.Target == null) return;

        // ★ 보너스/시너지/전파 데미지에는 반응하지 않음 (체인 방지)
        if (DamageChainGuard.IsProcessingBonus) return;

        // 전기 속성 데미지에만 추가 데미지 적용
        if (info.Element != DamageElement2D.Electric) return;

        int bonusDamage = Mathf.Max(1, Mathf.RoundToInt(info.Amount * electricBonusDamageRate));

        DamageChainGuard.BeginBonus();
        DamageUtil2D.TryApplyDamage(info.Target, bonusDamage, DamageElement2D.Electric);
        DamageChainGuard.EndBonus();
    }
}