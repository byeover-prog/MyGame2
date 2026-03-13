// ──────────────────────────────────────────────
// PassiveStatType.cs
// 패시브 스킬이 적용하는 능력치 종류 (8종)
// ★ 설계 문서 기준으로 수정
// ──────────────────────────────────────────────

namespace _Game.Skills
{
    public enum PassiveStatType
    {
        /// <summary>미지정 (액티브 스킬용)</summary>
        None = 0,

        /// <summary>1. 공격력 % 증가 (레벨당 10%)</summary>
        AttackPowerPercent = 1,

        /// <summary>3. 픽업 범위 % 증가 (레벨당 20%)</summary>
        PickupRangePercent = 2,

        /// <summary>5. 이동속도 % 증가 (레벨당 5%)</summary>
        MoveSpeedPercent = 3,

        /// <summary>2. 방어력 % 증가 (레벨당 10%, LoL 유효체력 공식)</summary>
        DefensePercent = 4,

        /// <summary>4. 최대 체력 정수 증가 (레벨당 +20)</summary>
        MaxHpFlat = 5,

        /// <summary>8. 스킬 가속 증가 (레벨당 10%, 쿨타임=기본×100/(100+가속))</summary>
        SkillHastePercent = 6,

        /// <summary>6. 스킬 범위 % 증가 (레벨당 5%)</summary>
        SkillAreaPercent = 7,

        /// <summary>7. 경험치 획득량 % 증가 (레벨당 10%)</summary>
        ExpGainPercent = 8
    }
}