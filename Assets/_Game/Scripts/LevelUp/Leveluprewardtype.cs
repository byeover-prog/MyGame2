// ──────────────────────────────────────────────
// LevelUpRewardType.cs
// 레벨업 카드 보상 종류
// ──────────────────────────────────────────────

namespace _Game.LevelUp
{
    /// <summary>
    /// 레벨업 카드가 제공하는 보상 타입.
    /// Skill이면 SO 기반 스킬/패시브, 나머지는 즉시 보상.
    /// </summary>
    public enum LevelUpRewardType
    {
        /// <summary>스킬/패시브 SO 기반 선택지</summary>
        Skill = 0,

        /// <summary>체력 회복</summary>
        Heal = 1,

        /// <summary>재화 획득</summary>
        Gold = 2,

        /// <summary>일시 무적</summary>
        Invincible = 3,

        /// <summary>경험치 즉시 획득</summary>
        BonusExp = 4
    }
}