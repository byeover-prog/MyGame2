// ──────────────────────────────────────────────
// HayulPassive_Dosa.cs
// 하율 고유 패시브 — "도사란 무엇인가?"
//
// [동작 원리]
// 1. 전기 속성 스킬의 전파 최대치를 +5 증가
// 2. 전기 속성 스킬에 20% 추가 데미지 부여
//
// [적용 방식]
// - 전파 최대치: 정적 프로퍼티로 제공 → HayulUltimateResolver 등에서 조회
// - 추가 데미지: DamageEvents2D.OnEnemyDamageApplied 구독
//   → Electric 속성 피해 시 20% 추가 데미지 적용
//
// [Hierarchy / Inspector]
// Player 오브젝트에 CharacterPassiveManager2D가 관리
// 이 컴포넌트를 직접 부착할 필요 없음 — Manager가 자동 생성
// ──────────────────────────────────────────────

using UnityEngine;

/// <summary>
/// 하율 고유 패시브 "도사란 무엇인가?" 구현입니다.
/// 전기 속성 전파 최대치를 증가시키고 전기 추가 데미지를 부여합니다.
/// </summary>
public sealed class HayulPassive_Dosa : CharacterPassiveBase
{
    // ═══════════════════════════════════════════════════════
    //  설정
    // ═══════════════════════════════════════════════════════

    [Header("도사란 무엇인가? 설정")]
    [Tooltip("전파 최대치 추가량입니다.")]
    [SerializeField] private int chainBonusCount = 5;

    [Tooltip("전기 속성 추가 데미지 비율입니다. 0.20 = 20%")]
    [SerializeField] private float electricBonusDamageRate = 0.20f;

    // ═══════════════════════════════════════════════════════
    //  정적 접근자 (전파 시스템에서 조회용)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 현재 활성화된 하율 패시브의 전파 보너스 수치입니다.
    /// HayulUltimateResolver, HayulUltimateHitResolver2D 등에서
    /// maxChainCount에 이 값을 더해서 사용합니다.
    ///
    /// [사용 예시]
    /// int finalMaxChain = baseMaxChain + HayulPassive_Dosa.ChainBonus;
    /// </summary>
    public static int ChainBonus { get; private set; } = 0;

    /// <summary>
    /// 현재 활성화된 하율 패시브의 전기 추가 데미지 비율입니다.
    /// 0이면 패시브 비활성 상태입니다.
    /// </summary>
    public static float ElectricBonusRate { get; private set; } = 0f;

    // ═══════════════════════════════════════════════════════
    //  프로퍼티
    // ═══════════════════════════════════════════════════════

    public override string PassiveName => "도사란 무엇인가?";
    public override string Description =>
        $"전기 속성 전파 최대치 +{chainBonusCount}, " +
        $"전기 속성 스킬 {electricBonusDamageRate * 100f:F0}% 추가 데미지";

    // ═══════════════════════════════════════════════════════
    //  런타임 상태
    // ═══════════════════════════════════════════════════════

    /// <summary>추가 데미지 → 이벤트 재진입 방지 플래그</summary>
    private bool _applyingBonus;

    // ═══════════════════════════════════════════════════════
    //  활성화 / 비활성화
    // ═══════════════════════════════════════════════════════

    protected override void OnActivate()
    {
        ChainBonus = chainBonusCount;
        ElectricBonusRate = electricBonusDamageRate;
        _applyingBonus = false;
        DamageEvents2D.OnEnemyDamageApplied += HandleDamageApplied;

        Debug.Log($"[하율 패시브] 전파 보너스 +{chainBonusCount}, " +
                  $"전기 추가 데미지 {electricBonusDamageRate * 100f}%");
    }

    protected override void OnDeactivate()
    {
        DamageEvents2D.OnEnemyDamageApplied -= HandleDamageApplied;
        ChainBonus = 0;
        ElectricBonusRate = 0f;
        _applyingBonus = false;
    }

    // ═══════════════════════════════════════════════════════
    //  이벤트 처리
    // ═══════════════════════════════════════════════════════

    private void HandleDamageApplied(DamageEvents2D.EnemyDamageAppliedInfo info)
    {
        if (!IsActive) return;
        if (_applyingBonus) return;
        if (info.Target == null) return;

        // 전기 속성 데미지에만 추가 데미지 적용
        if (info.Element != DamageElement2D.Electric) return;

        int bonusDamage = Mathf.Max(1, Mathf.RoundToInt(info.Amount * electricBonusDamageRate));

        _applyingBonus = true;
        DamageUtil2D.TryApplyDamage(info.Target, bonusDamage, DamageElement2D.Electric);
        _applyingBonus = false;
    }
}