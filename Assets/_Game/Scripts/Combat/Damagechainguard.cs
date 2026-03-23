// ──────────────────────────────────────────────
// DamageChainGuard.cs
// 보너스 데미지 이벤트 체인 폭발 방지용 공유 플래그
//
// [문제]
// HayulPassive_Dosa(+20% Electric) → AttributeSynergyManager(+Ice, +Dark)
// → 각각 다시 OnEnemyDamageApplied 발생 → 서로 트리거 → 데미지 폭발
//
// [해결]
// 모든 "추가 보너스 데미지" 시스템이 이 플래그를 확인하고,
// 다른 시스템의 보너스 데미지에는 반응하지 않음.
// 오직 "원본 데미지"에만 반응.
// ──────────────────────────────────────────────

/// <summary>
/// 보너스 데미지 처리 중인지 여부를 공유하는 정적 가드입니다.
/// HayulPassive_Dosa, YoonseolPassive_Hokhan, AttributeSynergyManager2D가 공용으로 사용합니다.
/// </summary>
public static class DamageChainGuard
{
    /// <summary>
    /// 현재 보너스/시너지 데미지 처리 중이면 true.
    /// true인 동안에는 추가 보너스 데미지를 생성하지 않습니다.
    /// </summary>
    public static bool IsProcessingBonus { get; private set; }

    /// <summary>
    /// 보너스 데미지 처리 시작. 반드시 EndBonus()와 쌍으로 호출하세요.
    /// </summary>
    public static void BeginBonus() => IsProcessingBonus = true;

    /// <summary>
    /// 보너스 데미지 처리 종료.
    /// </summary>
    public static void EndBonus() => IsProcessingBonus = false;
}