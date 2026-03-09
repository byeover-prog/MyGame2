using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/캐릭터/캐릭터 카탈로그", fileName = "SO_CharacterCatalog")]
public sealed class CharacterCatalogSO : ScriptableObject
{
    [Header("보유/등장 가능한 캐릭터 목록")]
    [SerializeField] private List<CharacterDefinitionSO> characters = new List<CharacterDefinitionSO>(16);

    public IReadOnlyList<CharacterDefinitionSO> Characters => characters;

    public bool TryFindById(string id, out CharacterDefinitionSO found)
    {
        found = null;
        if (string.IsNullOrWhiteSpace(id)) return false;

        for (int i = 0; i < characters.Count; i++)
        {
            var c = characters[i];
            if (c == null) continue;
            if (c.CharacterId == id)
            {
                found = c;
                return true;
            }
        }
        return false;
    }
}
