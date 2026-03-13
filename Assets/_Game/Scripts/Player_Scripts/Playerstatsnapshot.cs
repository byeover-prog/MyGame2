// ──────────────────────────────────────────────
// PlayerStatSnapshot.cs
// 패시브 스킬 합산 결과를 담는 구조체
// PlayerSkillLoadout.BuildStatSnapshot()이 반환한다.
// ──────────────────────────────────────────────

namespace _Game.Player
{
    /// <summary>
    /// 패시브 스킬이 반영된 플레이어 최종 보너스 스탯.
    /// 기본 스탯에 이 값을 더해서 최종 수치를 산출한다.
    /// </summary>
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

        /// <summary>속성 피해 % 보너스</summary>
        public float ElementDamagePercent;

        /// <summary>재화 획득량 % 보너스</summary>
        public float GoldGainPercent;

        /// <summary>경험치 획득량 % 보너스</summary>
        public float ExpGainPercent;

        /// <summary>디버그용 문자열 출력</summary>
        public override string ToString()
        {
            return $"[Snapshot] " +
                   $"ATK%={AttackPowerPercent}, " +
                   $"Pickup%={PickupRangePercent}, " +
                   $"SPD%={MoveSpeedPercent}, " +
                   $"DEF%={DefensePercent}, " +
                   $"HP+{MaxHpFlat}, " +
                   $"Elem%={ElementDamagePercent}, " +
                   $"Gold%={GoldGainPercent}, " +
                   $"EXP%={ExpGainPercent}";
        }
    }
}