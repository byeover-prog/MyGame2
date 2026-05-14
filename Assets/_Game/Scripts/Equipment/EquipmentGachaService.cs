using System.Collections.Generic;
using UnityEngine;

public sealed class EquipmentGachaService
{
    private readonly GachaConfigSO _config;
    private readonly EquipmentDatabaseSO _database;
    private readonly SaveManager2D _saveManager;
    private readonly MetaWalletService2D _walletService;

    public EquipmentGachaService(
        GachaConfigSO config = null,
        EquipmentDatabaseSO database = null,
        SaveManager2D saveManager = null)
    {
        _config = config != null ? config : GachaConfigSO.RuntimeInstance;
        _database = database != null ? database : EquipmentDatabaseSO.RuntimeInstance;
        _saveManager = saveManager != null ? saveManager : SaveManager2D.Instance;
        _walletService = new MetaWalletService2D(_saveManager);
    }

    public int SinglePullCost => _config != null ? Mathf.Max(0, _config.singlePullCost) : 0;

    public int TenPullCost => _config != null ? Mathf.Max(0, _config.tenPullCost) : 0;

    public bool CanPullSingle(out string reason)
    {
        return CanSpend(SinglePullCost, out reason);
    }

    public bool CanPullTen(out string reason)
    {
        return CanSpend(TenPullCost, out reason);
    }

    public bool TryPullSingle(string characterId, out EquipmentGachaResult result, out string reason)
    {
        result = default;
        if (!CanPullSingle(out reason))
            return false;

        if (!_walletService.SpendNyang(SinglePullCost, autoSave: false))
        {
            reason = "Nyang is not enough.";
            return false;
        }

        result = DrawOne(characterId, null);
        Save();
        reason = string.Empty;
        return true;
    }

    public bool TryPullTen(string characterId, List<EquipmentGachaResult> results, out string reason)
    {
        if (results == null)
        {
            reason = "Result list is null.";
            return false;
        }

        results.Clear();
        if (!CanPullTen(out reason))
            return false;

        if (!_walletService.SpendNyang(TenPullCost, autoSave: false))
        {
            reason = "Nyang is not enough.";
            return false;
        }

        bool hasGuaranteedRarity = false;
        EquipmentRarity minRarity = _config != null ? _config.tenPullMinGuarantee : EquipmentRarity.Rare;

        for (int i = 0; i < 10; i++)
        {
            EquipmentRarity? minimum = i == 9 && !hasGuaranteedRarity ? minRarity : (EquipmentRarity?)null;
            EquipmentGachaResult pull = DrawOne(characterId, minimum);
            if (IsAtLeast(pull.rarity, minRarity))
                hasGuaranteedRarity = true;

            results.Add(pull);
        }

        Save();
        reason = string.Empty;
        return true;
    }

    private bool CanSpend(int cost, out string reason)
    {
        if (_config == null)
        {
            reason = "GachaConfigSO is not registered.";
            return false;
        }

        if (_database == null)
        {
            reason = "EquipmentDatabaseSO is not registered.";
            return false;
        }

        if (_saveManager == null || _saveManager.Data == null)
        {
            reason = "SaveManager2D is not ready.";
            return false;
        }

        if (!_walletService.CanSpendNyang(cost))
        {
            reason = "Nyang is not enough.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private EquipmentGachaResult DrawOne(string characterId, EquipmentRarity? minimumRarity)
    {
        MetaProfileSaveData2D meta = EnsureMeta();
        EquipmentRarity rarity = RollRarity(meta);
        if (minimumRarity.HasValue && !IsAtLeast(rarity, minimumRarity.Value))
            rarity = minimumRarity.Value;

        EquipmentDefinitionSO equipment = PickEquipment(rarity);
        EquipmentGachaResult result = new EquipmentGachaResult
        {
            equipment = equipment,
            equipmentId = equipment != null ? equipment.equipmentId : string.Empty,
            rarity = rarity
        };

        if (equipment != null)
            ApplyOwnership(characterId, equipment, ref result);

        if (meta != null && meta.equipmentGacha != null)
            meta.equipmentGacha.pullsSinceEpic = rarity == EquipmentRarity.Epic ? 0 : meta.equipmentGacha.pullsSinceEpic + 1;

        return result;
    }

    private EquipmentRarity RollRarity(MetaProfileSaveData2D meta)
    {
        if (_config != null && meta != null && meta.equipmentGacha != null && _config.epicPityCount > 0)
        {
            if (meta.equipmentGacha.pullsSinceEpic + 1 >= _config.epicPityCount)
                return EquipmentRarity.Epic;
        }

        float roll = Random.value;
        float epic = _config != null ? Mathf.Max(0f, _config.epicRate) : 0f;
        float rare = _config != null ? Mathf.Max(0f, _config.rareRate) : 0f;
        float uncommon = _config != null ? Mathf.Max(0f, _config.uncommonRate) : 0f;

        if (roll < epic) return EquipmentRarity.Epic;
        if (roll < epic + rare) return EquipmentRarity.Rare;
        if (roll < epic + rare + uncommon) return EquipmentRarity.Uncommon;
        return EquipmentRarity.Common;
    }

    private EquipmentDefinitionSO PickEquipment(EquipmentRarity rarity)
    {
        if (_database == null) return null;

        List<EquipmentDefinitionSO> pool = _database.GetPool(rarity);
        if (pool == null || pool.Count == 0) return null;

        int index = Random.Range(0, pool.Count);
        return pool[index];
    }

    private void ApplyOwnership(string characterId, EquipmentDefinitionSO equipment, ref EquipmentGachaResult result)
    {
        if (equipment == null || string.IsNullOrWhiteSpace(equipment.equipmentId))
            return;

        MetaProfileSaveData2D meta = EnsureMeta();
        CharacterEquipmentSaveData equipmentState = meta != null && meta.equipment != null
            ? meta.equipment.GetOrCreate(characterId)
            : null;

        if (equipmentState == null)
            return;

        int owned = equipmentState.GetOwnedCount(equipment.equipmentId);
        if (owned <= 0)
        {
            equipmentState.SetOwnedCount(equipment.equipmentId, 1);
            return;
        }

        result.isDuplicate = true;
        result.refundNyang = _config != null ? Mathf.Max(0, _config.GetRefundAmount(equipment.rarity)) : 0;
        if (result.refundNyang > 0)
            _walletService.AddNyang(result.refundNyang, autoSave: false);
    }

    private MetaProfileSaveData2D EnsureMeta()
    {
        if (_saveManager == null || _saveManager.Data == null) return null;
        _saveManager.Data.EnsureDefaults();
        return _saveManager.Data.metaProfile;
    }

    private void Save()
    {
        if (_saveManager != null)
            _saveManager.Save();
    }

    private static bool IsAtLeast(EquipmentRarity value, EquipmentRarity minimum)
    {
        return (int)value >= (int)minimum;
    }
}

public struct EquipmentGachaResult
{
    public EquipmentDefinitionSO equipment;
    public string equipmentId;
    public EquipmentRarity rarity;
    public bool isDuplicate;
    public int refundNyang;
}
