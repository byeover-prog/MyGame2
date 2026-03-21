using UnityEngine;
using _Game.Player;

public sealed class CharacterMetaResolver2D
{
    private readonly CharacterCatalogSO _catalog;
    private readonly SaveManager2D _saveManager;
    private readonly CharacterProgressionService2D _progressionService;

    public CharacterMetaResolver2D(CharacterCatalogSO catalog, SaveManager2D saveManager = null)
    {
        _catalog = catalog;
        _saveManager = saveManager != null ? saveManager : SaveManager2D.Instance;
        _progressionService = new CharacterProgressionService2D(catalog, _saveManager);
    }

    public SquadMetaBattleSnapshot2D BuildCurrentSquadSnapshot()
    {
        SquadLoadoutRuntime.Loadout loadout = SquadLoadoutRuntime.Current;
        return BuildSquadSnapshot(loadout.ToSaveData());
    }

    public SquadMetaBattleSnapshot2D BuildSquadSnapshot(FormationSaveData2D formation)
    {
        SquadMetaBattleSnapshot2D snapshot = default;
        if (_catalog == null || formation == null) return snapshot;

        snapshot.support1Id = formation.support1Id;
        snapshot.mainId = formation.mainId;
        snapshot.support2Id = formation.support2Id;

        if (_catalog.TryFindById(formation.support1Id, out CharacterDefinitionSO support1))
        {
            snapshot.support1Definition = support1;
            snapshot.support1Bonus = BuildForCharacter(support1);
        }

        if (_catalog.TryFindById(formation.mainId, out CharacterDefinitionSO main))
        {
            snapshot.mainDefinition = main;
            snapshot.mainBonus = BuildForCharacter(main);
        }

        if (_catalog.TryFindById(formation.support2Id, out CharacterDefinitionSO support2))
        {
            snapshot.support2Definition = support2;
            snapshot.support2Bonus = BuildForCharacter(support2);
        }

        return snapshot;
    }

    public MetaCharacterBonusSnapshot2D BuildForCharacter(string characterId)
    {
        if (_catalog == null || string.IsNullOrWhiteSpace(characterId))
            return MetaCharacterBonusSnapshot2D.Empty;

        return _catalog.TryFindById(characterId, out CharacterDefinitionSO definition)
            ? BuildForCharacter(definition)
            : MetaCharacterBonusSnapshot2D.Empty;
    }

    public MetaCharacterBonusSnapshot2D BuildForCharacter(CharacterDefinitionSO definition)
    {
        if (definition == null)
            return MetaCharacterBonusSnapshot2D.Empty;

        MetaCharacterBonusSnapshot2D snapshot = MetaCharacterBonusSnapshot2D.Empty;
        snapshot.characterLevel = _progressionService.GetLevel(definition.CharacterId);
        snapshot.coreStats = _progressionService.BuildLevelBonusSnapshot(definition.CharacterId);

        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null)
            return snapshot;

        CharacterUpgradeStateSaveData2D upgradeState = meta.upgrades.GetOrCreate(definition.CharacterId);
        CharacterUpgradeTreeSO tree = definition.UpgradeTree != null
            ? definition.UpgradeTree
            : CharacterUpgradeTreeSO.GetOrCreateRuntimeFallback(definition);

        if (tree == null || upgradeState == null || upgradeState.purchasedNodes == null)
            return snapshot;

        snapshot.purchasedNodeCount = upgradeState.purchasedNodes.Count;

        for (int i = 0; i < upgradeState.purchasedNodes.Count; i++)
        {
            CharacterPurchasedUpgradeNodeSaveData2D purchased = upgradeState.purchasedNodes[i];
            if (purchased == null || purchased.rank <= 0) continue;
            if (!tree.TryFindNode(purchased.nodeId, out CharacterUpgradeNodeData2D node) || node == null) continue;
            if (node.modifiers == null) continue;

            for (int m = 0; m < node.modifiers.Count; m++)
            {
                CharacterUpgradeModifierEntry2D modifier = node.modifiers[m];
                ApplyModifier(ref snapshot, modifier.kind, modifier.valuePerRank * purchased.rank);
            }
        }

        return snapshot;
    }

    private static void ApplyModifier(ref MetaCharacterBonusSnapshot2D snapshot, OutgameModifierKind2D kind, float value)
    {
        switch (kind)
        {
            case OutgameModifierKind2D.AttackPowerPercent:
                snapshot.coreStats.AttackPowerPercent += value;
                break;
            case OutgameModifierKind2D.DefensePercent:
                snapshot.coreStats.DefensePercent += value;
                break;
            case OutgameModifierKind2D.MaxHpFlat:
                snapshot.coreStats.MaxHpFlat += Mathf.RoundToInt(value);
                break;
            case OutgameModifierKind2D.BasicSkillDamagePercent:
                snapshot.basicSkillDamagePercent += value;
                break;
            case OutgameModifierKind2D.BasicSkillCooldownPercent:
                snapshot.basicSkillCooldownPercent += value;
                break;
            case OutgameModifierKind2D.UltimateDamagePercent:
                snapshot.ultimateDamagePercent += value;
                break;
            case OutgameModifierKind2D.UltimateCooldownPercent:
                snapshot.ultimateCooldownPercent += value;
                break;
            case OutgameModifierKind2D.PassivePowerPercent:
                snapshot.passivePowerPercent += value;
                break;
            case OutgameModifierKind2D.StoryExpGainPercent:
                snapshot.storyExpGainPercent += value;
                break;
            case OutgameModifierKind2D.CasualExpGainPercent:
                snapshot.casualExpGainPercent += value;
                break;
            case OutgameModifierKind2D.NyangGainPercent:
                snapshot.nyangGainPercent += value;
                break;
        }
    }

    private MetaProfileSaveData2D EnsureMeta()
    {
        if (_saveManager == null || _saveManager.Data == null) return null;
        _saveManager.Data.EnsureDefaults();
        return _saveManager.Data.metaProfile;
    }
}
