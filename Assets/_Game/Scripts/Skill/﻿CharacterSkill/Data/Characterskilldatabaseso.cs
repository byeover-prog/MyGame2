using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 전용 스킬 전체 DB입니다.
/// 구현 원리:
///  에디터 메뉴에서 프로젝트 내 CharacterSkillDefinitionSO와 CharacterSkillSetSO를 자동 수집합니다.
///  런타임에서는 ID 조회와 캐릭터별 스킬 조회만 담당합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "CharacterSkillDatabase",
    menuName = "혼령검/스킬/캐릭터 전용 스킬 DB",
    order = 102)]
public sealed class CharacterSkillDatabaseSO : ScriptableObject
{
    [Header("자동 수집된 전용 스킬")]
    [Tooltip("프로젝트 내 모든 캐릭터 전용 스킬 정의입니다. Tool 메뉴에서 자동 갱신합니다.")]
    [SerializeField] private CharacterSkillDefinitionSO[] characterSkills;

    [Header("자동 수집된 캐릭터별 스킬 세트")]
    [Tooltip("캐릭터별 전용 스킬 세트입니다. Tool 메뉴에서 자동 갱신합니다.")]
    [SerializeField] private CharacterSkillSetSO[] characterSkillSets;

    private Dictionary<string, CharacterSkillDefinitionSO> _skillById;
    private Dictionary<string, CharacterSkillSetSO> _setByCharacterId;

    public IReadOnlyList<CharacterSkillDefinitionSO> CharacterSkills => characterSkills;
    public IReadOnlyList<CharacterSkillSetSO> CharacterSkillSets => characterSkillSets;

    private void OnEnable()
    {
        RebuildIndex();
    }

    public void RebuildIndex()
    {
        _skillById = new Dictionary<string, CharacterSkillDefinitionSO>();
        _setByCharacterId = new Dictionary<string, CharacterSkillSetSO>();

        if (characterSkills != null)
        {
            for (int i = 0; i < characterSkills.Length; i++)
            {
                CharacterSkillDefinitionSO skill = characterSkills[i];
                if (skill == null) continue;
                if (string.IsNullOrWhiteSpace(skill.SkillId)) continue;

                _skillById[skill.SkillId] = skill;
            }
        }

        if (characterSkillSets != null)
        {
            for (int i = 0; i < characterSkillSets.Length; i++)
            {
                CharacterSkillSetSO set = characterSkillSets[i];
                if (set == null) continue;
                if (string.IsNullOrWhiteSpace(set.CharacterId)) continue;

                _setByCharacterId[set.CharacterId] = set;
            }
        }
    }

    public bool TryGetSkill(string skillId, out CharacterSkillDefinitionSO skill)
    {
        if (_skillById == null)
            RebuildIndex();

        if (string.IsNullOrWhiteSpace(skillId))
        {
            skill = null;
            return false;
        }

        return _skillById.TryGetValue(skillId, out skill) && skill != null;
    }

    public bool TryGetSkillSet(string characterId, out CharacterSkillSetSO set)
    {
        if (_setByCharacterId == null)
            RebuildIndex();

        if (string.IsNullOrWhiteSpace(characterId))
        {
            set = null;
            return false;
        }

        return _setByCharacterId.TryGetValue(characterId, out set) && set != null;
    }

    public List<CharacterSkillDefinitionSO> GetSkillsByOwner(string characterId)
    {
        List<CharacterSkillDefinitionSO> result = new List<CharacterSkillDefinitionSO>();

        if (string.IsNullOrWhiteSpace(characterId))
            return result;

        if (characterSkills == null)
            return result;

        for (int i = 0; i < characterSkills.Length; i++)
        {
            CharacterSkillDefinitionSO skill = characterSkills[i];
            if (skill == null) continue;

            if (skill.OwnerCharacterId == characterId)
                result.Add(skill);
        }

        return result;
    }
}