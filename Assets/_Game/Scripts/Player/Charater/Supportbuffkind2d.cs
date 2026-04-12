/// <summary>
/// 지원 궁극기 발동 시 메인 캐릭터에게 부여하는 버프 종류입니다.
/// </summary>
public enum SupportBuffKind2D
{
    /// <summary>없음</summary>
    None = 0,

    /// <summary>윤설 — 공격력 % 증가</summary>
    AttackPowerPercent = 1,

    /// <summary>하율 — 스킬 가속 고정값 추가</summary>
    SkillHasteFlat = 2,

    /// <summary>하린 — 모든 피해 흡혈 %</summary>
    LifestealPercent = 3,
}