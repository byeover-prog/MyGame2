using UnityEngine;

public struct SquadMetaBattleSnapshot2D
{
    public string support1Id;
    public string mainId;
    public string support2Id;

    public CharacterDefinitionSO support1Definition;
    public CharacterDefinitionSO mainDefinition;
    public CharacterDefinitionSO support2Definition;

    public MetaCharacterBonusSnapshot2D support1Bonus;
    public MetaCharacterBonusSnapshot2D mainBonus;
    public MetaCharacterBonusSnapshot2D support2Bonus;
}

public static class MetaBattleSnapshotRuntime2D
{
    private static SquadMetaBattleSnapshot2D _current;

    public static SquadMetaBattleSnapshot2D Current => _current;
    public static bool HasMainCharacter => _current.mainDefinition != null;

    public static void SetCurrent(SquadMetaBattleSnapshot2D snapshot)
    {
        _current = snapshot;
    }

    public static void Clear()
    {
        _current = default;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnDomainReload()
    {
        _current = default;
    }
}
