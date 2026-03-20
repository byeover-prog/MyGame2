using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 캐릭터 정의(CharacterDefinitionSO)를 한 곳에 모아 관리하는 카탈로그입니다.
/// 프로젝트에 하나만 만들어 두면 됩니다.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/메타/캐릭터 카탈로그", fileName = "CharacterCatalog")]
public sealed class CharacterCatalogSO : ScriptableObject
{
    [Header("캐릭터 목록")]
    [Tooltip("등록된 캐릭터 정의 목록입니다. 윤설, 하율, 하린 순서로 넣으세요.")]
    [SerializeField] private List<CharacterDefinitionSO> characters = new List<CharacterDefinitionSO>(3);

    /// <summary>등록된 캐릭터 목록 (읽기 전용)입니다.</summary>
    public IReadOnlyList<CharacterDefinitionSO> Characters => characters;

    /// <summary>
    /// characterId로 캐릭터 정의를 검색합니다.
    /// </summary>
    public bool TryFindById(string characterId, out CharacterDefinitionSO found)
    {
        found = null;
        if (string.IsNullOrWhiteSpace(characterId) || characters == null) return false;

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterDefinitionSO def = characters[i];
            if (def == null) continue;
            if (def.CharacterId == characterId)
            {
                found = def;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 인덱스로 캐릭터 정의를 가져옵니다.
    /// </summary>
    public CharacterDefinitionSO GetByIndex(int index)
    {
        if (characters == null || index < 0 || index >= characters.Count) return null;
        return characters[index];
    }
}