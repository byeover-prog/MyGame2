// ──────────────────────────────────────────────
// RuntimeSkillState.cs
// 런타임 스킬 상태 (레벨 · 획득 여부만 보관)
// UI/연출 책임을 섞지 않는다.
// ──────────────────────────────────────────────

using UnityEngine;

namespace _Game.Skills
{
    /// <summary>
    /// 한 칸의 스킬 슬롯이 런타임에 들고 있는 상태.
    /// 레벨 · 획득 여부만 보관하고, UI/연출/쿨다운은 별도 시스템이 담당한다.
    /// </summary>
    public class RuntimeSkillState
    {
        /// <summary>이 슬롯에 꽂힌 스킬 정의 SO</summary>
        public SkillDefinitionSO Definition { get; private set; }

        public CharacterSkillDefinitionSO CharacterDefinition { get; private set; }

        public string SkillId
        {
            get
            {
                if (Definition != null) return Definition.SkillId;
                return CharacterDefinition != null ? CharacterDefinition.SkillId : string.Empty;
            }
        }

        public int MaxLevel
        {
            get
            {
                if (Definition != null) return Definition.MaxLevel;
                return CharacterDefinition != null ? CharacterDefinition.MaxLevel : 0;
            }
        }

        public Sprite Icon
        {
            get
            {
                if (Definition != null) return Definition.Icon;
                return CharacterDefinition != null ? CharacterDefinition.Icon : null;
            }
        }

        /// <summary>현재 레벨 (1-based)</summary>
        public int Level { get; private set; }

        /// <summary>획득(해금) 여부</summary>
        public bool Unlocked { get; private set; }

        /// <summary>최대 레벨 도달 여부</summary>
        public bool IsMaxLevel => MaxLevel > 0 && Level >= MaxLevel;

        public bool CanLevelUp()
        {
            if (MaxLevel <= 0)
                return false;

            return Level < MaxLevel;
        }

        // ── 생성 ───────────────────────────────────

        public RuntimeSkillState(SkillDefinitionSO def)
        {
            Definition = def;
            CharacterDefinition = null;
            Level      = 1;
            Unlocked   = true;
        }

        public RuntimeSkillState(CharacterSkillDefinitionSO def)
        {
            Definition = null;
            CharacterDefinition = def;
            Level      = 1;
            Unlocked   = true;
        }

        // ── 레벨업 ─────────────────────────────────

        /// <summary>
        /// 레벨을 1 올린다.
        /// 이미 maxLevel이면 false를 반환한다.
        /// </summary>
        public bool TryLevelUp()
        {
            if (IsMaxLevel)
                return false;

            Level++;
            return true;
        }
    }
}
