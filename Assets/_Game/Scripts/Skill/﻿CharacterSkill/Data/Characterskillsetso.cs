using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 1명이 보유한 전용 스킬 목록입니다.
/// 구현 원리:
///  레벨업 카드 생성기는 캐릭터 ID로 이 SO를 찾고, 해당 캐릭터의 전용 스킬만 후보로 사용합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "CharacterSkillSet",
    menuName = "혼령검/스킬/캐릭터 전용 스킬 세트",
    order = 101)]
public sealed class CharacterSkillSetSO : ScriptableObject
{
    [Header("캐릭터")]
    [Tooltip("캐릭터 ID입니다. 예: yoonseol, harin, hayul")]
    [SerializeField] private string characterId;

    [Tooltip("인스펙터 확인용 캐릭터 이름입니다.")]
    [SerializeField] private string characterDisplayName;

    [Header("전용 스킬 목록")]
    [Tooltip("이 캐릭터가 레벨업으로 획득할 수 있는 전용 스킬 목록입니다.")]
    [SerializeField] private CharacterSkillDefinitionSO[] skills;

    public string CharacterId => characterId;
    public string CharacterDisplayName => characterDisplayName;
    public IReadOnlyList<CharacterSkillDefinitionSO> Skills => skills;

    public bool ContainsSkill(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return false;

        if (skills == null)
            return false;

        for (int i = 0; i < skills.Length; i++)
        {
            CharacterSkillDefinitionSO skill = skills[i];
            if (skill == null) continue;

            if (skill.SkillId == skillId)
                return true;
        }

        return false;
    }

    public List<CharacterSkillDefinitionSO> GetValidSkills()
    {
        List<CharacterSkillDefinitionSO> result = new List<CharacterSkillDefinitionSO>();

        if (skills == null)
            return result;

        for (int i = 0; i < skills.Length; i++)
        {
            CharacterSkillDefinitionSO skill = skills[i];
            if (skill == null) continue;

            if (string.IsNullOrWhiteSpace(skill.SkillId))
                continue;

            result.Add(skill);
        }

        return result;
    }
}