// ──────────────────────────────────────────────
// 스킬 분류 열거형 (발동형 / 패시브)
// ──────────────────────────────────────────────

namespace _Game.Skills
{
    /// <summary>
    /// 스킬의 런타임 분류.
    /// PlayerSkillLoadout 내부에서 컨테이너 라우팅에 사용한다.
    /// </summary>
    public enum SkillType
    {
        /// <summary>발동형 스킬 (쿨다운·발사 등)</summary>
        Active,

        /// <summary>패시브 스킬 (상시 효과·스택 등)</summary>
        Passive
    }
}