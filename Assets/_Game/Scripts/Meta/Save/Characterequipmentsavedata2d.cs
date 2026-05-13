using System;
using System.Collections.Generic;

// Save data for each character's owned and equipped talisman/equipment state.
// This lives under MetaProfileSaveData2D.equipment.

[Serializable]
public sealed class CharacterEquipmentCollectionSaveData
{
    public List<CharacterEquipmentSaveData> entries
        = new List<CharacterEquipmentSaveData>(3);

    public void EnsureDefaults()
    {
        if (entries == null)
            entries = new List<CharacterEquipmentSaveData>(3);

        for (int i = 0; i < entries.Count; i++)
            entries[i]?.NormalizeSlotsForCurrentSchema();
    }

    public CharacterEquipmentSaveData GetOrCreate(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return null;

        if (entries == null)
            entries = new List<CharacterEquipmentSaveData>(3);

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].characterId == characterId)
                return entries[i];
        }

        CharacterEquipmentSaveData data = new CharacterEquipmentSaveData
        {
            characterId = characterId
        };
        data.NormalizeSlotsForCurrentSchema();
        entries.Add(data);
        return data;
    }
}

[Serializable]
public sealed class CharacterEquipmentSaveData
{
    public string characterId;

    public List<string> slotItemIds = new List<string>(TargetTalismanSlots);

    public List<OwnedShopItemEntry> ownedItems = new List<OwnedShopItemEntry>(13);

    public const int TargetTalismanSlots = 6;
    public const int LegacySlotCapacity = 8;
    public const int MaxSlots = TargetTalismanSlots;

    public void EnsureSlots()
    {
        NormalizeSlotsForCurrentSchema();
    }

    public void NormalizeSlotsForCurrentSchema()
    {
        if (slotItemIds == null)
            slotItemIds = new List<string>(TargetTalismanSlots);

        if (ownedItems == null)
            ownedItems = new List<OwnedShopItemEntry>(13);

        while (slotItemIds.Count < MaxSlots)
            slotItemIds.Add(string.Empty);

        if (slotItemIds.Count <= MaxSlots)
            return;

        MigrateLegacyOverflowSlotsToInventory();
        slotItemIds.RemoveRange(MaxSlots, slotItemIds.Count - MaxSlots);
    }

    private void MigrateLegacyOverflowSlotsToInventory()
    {
        for (int i = MaxSlots; i < slotItemIds.Count; i++)
        {
            string itemId = slotItemIds[i];
            if (string.IsNullOrWhiteSpace(itemId))
                continue;

            if (GetOwnedCount(itemId) <= 0)
                SetOwnedCount(itemId, 1);
        }
    }

    public bool Equip(int slotIndex, string itemId)
    {
        EnsureSlots();
        if (slotIndex < 0 || slotIndex >= MaxSlots) return false;

        slotItemIds[slotIndex] = itemId ?? string.Empty;
        return true;
    }

    public bool Unequip(int slotIndex)
    {
        EnsureSlots();
        if (slotIndex < 0 || slotIndex >= MaxSlots) return false;

        slotItemIds[slotIndex] = string.Empty;
        return true;
    }

    public int GetOwnedCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return 0;
        if (ownedItems == null) return 0;

        for (int i = 0; i < ownedItems.Count; i++)
        {
            if (ownedItems[i] != null && ownedItems[i].itemId == itemId)
                return ownedItems[i].count;
        }

        return 0;
    }

    public void SetOwnedCount(string itemId, int count)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return;

        if (ownedItems == null)
            ownedItems = new List<OwnedShopItemEntry>(13);

        for (int i = 0; i < ownedItems.Count; i++)
        {
            if (ownedItems[i] != null && ownedItems[i].itemId == itemId)
            {
                ownedItems[i].count = count;
                return;
            }
        }

        ownedItems.Add(new OwnedShopItemEntry { itemId = itemId, count = count });
    }
}

[Serializable]
public sealed class OwnedShopItemEntry
{
    public string itemId;
    public int count;
}
