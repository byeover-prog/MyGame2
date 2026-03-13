// ──────────────────────────────────────────────
// PlayerStatSnapshot.cs
// 패시브 스킬 합산 결과를 담는 구조체
// PlayerSkillLoadout.BuildStatSnapshot()이 반환한다.
// ──────────────────────────────────────────────

namespace _Game.Player
{
    public struct PlayerStatSnapshot
    {
        /// <summary>공격력 % 보너스</summary>
        public float AttackPowerPercent;

        /// <summary>픽업 범위 % 보너스</summary>
        public float PickupRangePercent;

        /// <summary>이동속도 % 보너스</summary>
        public float MoveSpeedPercent;

        /// <summary>방어력 % 보너스</summary>
        public float DefensePercent;

        /// <summary>최대 체력 정수 보너스</summary>
        public int MaxHpFlat;

        /// <summary>스킬 가속 % 보너스</summary>
        public float SkillHastePercent;

        /// <summary>스킬 범위 % 보너스</summary>
        public float SkillAreaPercent;

        /// <summary>경험치 획득량 % 보너스</summary>
        public float ExpGainPercent;

        public override string ToString()
        {
            return $"[Snapshot] " +
                   $"ATK%={AttackPowerPercent}, " +
                   $"Pickup%={PickupRangePercent}, " +
                   $"SPD%={MoveSpeedPercent}, " +
                   $"DEF%={DefensePercent}, " +
                   $"HP+{MaxHpFlat}, " +
                   $"Haste%={SkillHastePercent}, " +
                   $"Area%={SkillAreaPercent}, " +
                   $"EXP%={ExpGainPercent}";
        }
    }
}