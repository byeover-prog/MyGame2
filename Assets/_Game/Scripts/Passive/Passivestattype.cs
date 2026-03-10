// ──────────────────────────────────────────────
// PassiveStatType.cs
// 패시브 스킬이 적용하는 능력치 종류 (8종)
// ──────────────────────────────────────────────

namespace _Game.Skills
{
    /// <summary>
    /// 패시브 스킬이 영향을 주는 능력치 종류.
    /// PassiveBalanceTableSO에서 밸런스 수치를 관리한다.
    /// </summary>
    public enum PassiveStatType
    {
        /// <summary>미지정 (액티브 스킬용)</summary>
        None = 0,

        /// <summary>공격력 % 증가</summary>
        AttackPowerPercent,

        /// <summary>픽업 범위 % 증가</summary>
        PickupRangePercent,

        /// <summary>이동속도 % 증가</summary>
        MoveSpeedPercent,

        /// <summary>방어력 % 증가</summary>
        DefensePercent,

        /// <summary>최대 체력 정수 증가</summary>
        MaxHpFlat,

        /// <summary>속성 피해 % 증가</summary>
        ElementDamagePercent,

        /// <summary>재화 획득량 % 증가</summary>
        GoldGainPercent,

        /// <summary>경험치 획득량 % 증가</summary>
        ExpGainPercent
    }
}