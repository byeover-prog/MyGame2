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

    /// <summary>현재 편성 기준으로 스쿼드 전체 스냅샷을 빌드합니다.</summary>
    public SquadMetaBattleSnapshot2D BuildCurrentSquadSnapshot()
    {
        SquadLoadoutRuntime.Loadout loadout = SquadLoadoutRuntime.Current;
        return BuildSquadSnapshot(loadout.ToSaveData());
    }

    /// <summary>편성 데이터 기준으로 스쿼드 전체 스냅샷을 빌드합니다.</summary>
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

    /// <summary>캐릭터 ID로 개별 보너스 스냅샷을 빌드합니다.</summary>
    public MetaCharacterBonusSnapshot2D BuildForCharacter(string characterId)
    {
        if (_catalog == null || string.IsNullOrWhiteSpace(characterId))
            return MetaCharacterBonusSnapshot2D.Empty;

        return _catalog.TryFindById(characterId, out CharacterDefinitionSO definition)
            ? BuildForCharacter(definition)
            : MetaCharacterBonusSnapshot2D.Empty;
    }

    /// <summary>캐릭터 정의 SO로 개별 보너스 스냅샷을 빌드합니다.</summary>
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

        // ─── 1) 스킬 트리 강화 합산 ───
        CharacterUpgradeStateSaveData2D upgradeState = meta.upgrades.GetOrCreate(definition.CharacterId);
        CharacterUpgradeTreeSO tree = definition.UpgradeTree != null
            ? definition.UpgradeTree
            : CharacterUpgradeTreeSO.GetOrCreateRuntimeFallback(definition);

        if (tree != null && upgradeState != null && upgradeState.purchasedNodes != null)
        {
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
        }

        // ─── 2) 상점 장착 아이템 합산 ───
        if (meta.equipment != null)
        {
            CharacterEquipmentSaveData equipState = meta.equipment.GetOrCreate(definition.CharacterId);
            if (equipState != null && equipState.slotItemIds != null)
            {
                ShopDatabaseSO shopDb = ShopDatabaseSO.RuntimeInstance;
                if (shopDb != null)
                {
                    for (int s = 0; s < equipState.slotItemIds.Count; s++)
                    {
                        string itemId = equipState.slotItemIds[s];
                        if (string.IsNullOrWhiteSpace(itemId)) continue;
                        if (!shopDb.TryFindById(itemId, out ShopItemSO item) || item == null) continue;

                        ApplyModifier(ref snapshot, item.ModifierKind, item.ModifierValue);
                    }
                }
            }
        }

        return snapshot;
    }

    /// <summary>모디파이어를 스냅샷에 합산합니다.</summary>
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

            // ─── 상점 아이템용 ───
            case OutgameModifierKind2D.CastCountFlat:
                snapshot.castCountFlat += Mathf.RoundToInt(value);
                break;
            case OutgameModifierKind2D.SkillAreaPercent:
                snapshot.coreStats.SkillAreaPercent += value;
                break;
            case OutgameModifierKind2D.SkillAccelerationFlat:
                snapshot.skillAccelerationFlat += value;
                break;
            case OutgameModifierKind2D.DefenseFlat:
                snapshot.defenseFlat += Mathf.RoundToInt(value);
                break;
            case OutgameModifierKind2D.HpRegenFlat:
                snapshot.hpRegenFlat += Mathf.RoundToInt(value);
                break;
            case OutgameModifierKind2D.CritChancePercent:
                snapshot.critChancePercent += value;
                break;
            case OutgameModifierKind2D.CritDamagePercent:
                snapshot.critDamagePercent += value;
                break;
            case OutgameModifierKind2D.PickupRangePercent:
                snapshot.coreStats.PickupRangePercent += value;
                break;
            case OutgameModifierKind2D.MaxHpPercent:
                // MaxHpFlat과 별도로 % 계산은 런타임에서 처리
                // 여기서는 coreStats에 합산
                snapshot.coreStats.AttackPowerPercent += 0f; // placeholder
                break;
            case OutgameModifierKind2D.CooldownReductionPercent:
                snapshot.cooldownReductionPercent += value;
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