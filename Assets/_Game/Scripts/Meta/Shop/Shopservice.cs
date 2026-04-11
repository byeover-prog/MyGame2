using UnityEngine;

public sealed class ShopService
{
    private readonly ShopDatabaseSO _shopDb;
    private readonly CharacterCatalogSO _catalog;
    private readonly SaveManager2D _saveManager;
    private readonly MetaWalletService2D _walletService;

    public ShopService(ShopDatabaseSO shopDb, CharacterCatalogSO catalog, SaveManager2D saveManager = null)
    {
        _shopDb = shopDb;
        _catalog = catalog;
        _saveManager = saveManager != null ? saveManager : SaveManager2D.Instance;
        _walletService = new MetaWalletService2D(_saveManager);
    }

    /// <summary>현재 보유 냥입니다.</summary>
    public int Nyang => _walletService.Nyang;

    // ─── 구매 ───

    /// <summary>해당 캐릭터가 해당 아이템을 구매할 수 있는지 확인합니다.</summary>
    public bool CanPurchase(string characterId, string itemId, out string reason)
    {
        reason = string.Empty;

        if (_shopDb == null || !_shopDb.TryFindById(itemId, out ShopItemSO item) || item == null)
        {
            reason = "아이템을 찾을 수 없습니다.";
            return false;
        }

        CharacterEquipmentSaveData equipState = GetOrCreateEquipState(characterId);
        if (equipState == null)
        {
            reason = "캐릭터 정보를 찾을 수 없습니다.";
            return false;
        }

        int owned = equipState.GetOwnedCount(itemId);
        if (owned >= item.MaxPerCharacter)
        {
            reason = $"이미 최대 수량({item.MaxPerCharacter})을 보유하고 있습니다.";
            return false;
        }

        if (!_walletService.CanSpendNyang(item.Cost))
        {
            reason = "냥이 부족합니다.";
            return false;
        }

        return true;
    }

    /// <summary>아이템을 구매합니다.</summary>
    public bool TryPurchase(string characterId, string itemId, out string reason)
    {
        if (!CanPurchase(characterId, itemId, out reason))
            return false;

        ShopItemSO item = null;
        _shopDb.TryFindById(itemId, out item);

        if (!_walletService.SpendNyang(item.Cost, autoSave: false))
        {
            reason = "냥이 부족합니다.";
            return false;
        }

        CharacterEquipmentSaveData equipState = GetOrCreateEquipState(characterId);
        int newCount = equipState.GetOwnedCount(itemId) + 1;
        equipState.SetOwnedCount(itemId, newCount);

        Save();
        reason = string.Empty;
        return true;
    }

    // ─── 장착 / 해제 ───

    /// <summary>아이템을 특정 슬롯에 장착합니다.</summary>
    public bool TryEquip(string characterId, int slotIndex, string itemId, out string reason)
    {
        reason = string.Empty;

        CharacterEquipmentSaveData equipState = GetOrCreateEquipState(characterId);
        if (equipState == null)
        {
            reason = "캐릭터 정보를 찾을 수 없습니다.";
            return false;
        }

        if (slotIndex < 0 || slotIndex >= CharacterEquipmentSaveData.MaxSlots)
        {
            reason = "잘못된 슬롯 번호입니다.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(itemId))
        {
            int owned = equipState.GetOwnedCount(itemId);
            if (owned <= 0)
            {
                reason = "해당 아이템을 보유하고 있지 않습니다.";
                return false;
            }

            // 이미 다른 슬롯에 같은 아이템이 장착되어 있는지 확인
            equipState.EnsureSlots();
            int equippedCount = 0;
            for (int i = 0; i < equipState.slotItemIds.Count; i++)
            {
                if (equipState.slotItemIds[i] == itemId)
                    equippedCount++;
            }
            if (equippedCount >= owned)
            {
                reason = "보유한 모든 아이템이 이미 장착되어 있습니다.";
                return false;
            }
        }

        equipState.Equip(slotIndex, itemId);
        Save();
        MetaAutoBootstrap2D.RebuildBattleSnapshotIfPossible();
        return true;
    }

    /// <summary>특정 슬롯의 아이템을 해제합니다.</summary>
    public bool TryUnequip(string characterId, int slotIndex, out string reason)
    {
        reason = string.Empty;

        CharacterEquipmentSaveData equipState = GetOrCreateEquipState(characterId);
        if (equipState == null)
        {
            reason = "캐릭터 정보를 찾을 수 없습니다.";
            return false;
        }

        equipState.Unequip(slotIndex);
        Save();
        MetaAutoBootstrap2D.RebuildBattleSnapshotIfPossible();
        return true;
    }

    // ─── 조회 ───

    /// <summary>해당 캐릭터의 특정 슬롯에 장착된 아이템 ID를 반환합니다.</summary>
    public string GetEquippedItemId(string characterId, int slotIndex)
    {
        CharacterEquipmentSaveData equipState = GetOrCreateEquipState(characterId);
        if (equipState == null) return string.Empty;
        equipState.EnsureSlots();
        if (slotIndex < 0 || slotIndex >= CharacterEquipmentSaveData.MaxSlots) return string.Empty;
        return equipState.slotItemIds[slotIndex] ?? string.Empty;
    }

    /// <summary>해당 캐릭터의 특정 아이템 보유 수량을 반환합니다.</summary>
    public int GetOwnedCount(string characterId, string itemId)
    {
        CharacterEquipmentSaveData equipState = GetOrCreateEquipState(characterId);
        if (equipState == null) return 0;
        return equipState.GetOwnedCount(itemId);
    }

    // ─── 내부 ───

    private CharacterEquipmentSaveData GetOrCreateEquipState(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return null;
        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null) return null;
        return meta.equipment.GetOrCreate(characterId);
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
}