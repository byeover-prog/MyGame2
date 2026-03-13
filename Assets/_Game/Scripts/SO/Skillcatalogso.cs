// ──────────────────────────────────────────────
// SkillCatalogSO.cs
// 공통 스킬 + 패시브를 모두 등록하는 카탈로그 SO
// LevelUpCardGenerator가 카드 후보를 뽑을 때 사용한다.
// ──────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;

namespace _Game.Skills
{
    [CreateAssetMenu(
        fileName = "SkillCatalog",
        menuName = "Game/Skill/SkillCatalog",
        order = 2)]
    public sealed class SkillCatalogSO : ScriptableObject
    {
        [Header("=== 전체 스킬 목록 ===")]
        [SerializeField, Tooltip("공통 스킬(Active)과 패시브(Passive)를 모두 등록")]
        private SkillDefinitionSO[] allSkills;

        /// <summary>등록된 전체 스킬 목록 (읽기 전용)</summary>
        public IReadOnlyList<SkillDefinitionSO> AllSkills => allSkills;

        /// <summary>
        /// 지정 SkillType에 해당하는 스킬만 필터링하여 반환한다.
        /// </summary>
        public List<SkillDefinitionSO> GetByType(SkillType skillType)
        {
            List<SkillDefinitionSO> result = new List<SkillDefinitionSO>();

            if (allSkills == null)
                return result;

            for (int i = 0; i < allSkills.Length; i++)
            {
                SkillDefinitionSO skill = allSkills[i];

                if (skill == null)
                    continue;

                if (skill.SkillType != skillType)
                    continue;

                result.Add(skill);
            }

            return result;
        }
    }
}