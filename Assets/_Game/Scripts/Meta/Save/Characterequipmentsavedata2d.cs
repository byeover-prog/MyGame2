using System;
using System.Collections.Generic;

// 모든 캐릭터의 장비 장착 상태를 관리하는 컬렉션입니다.
// MetaProfileSaveData2D.equipment에 포함됩니다.

[Serializable]
public sealed class CharacterEquipmentCollectionSaveData
{
    public List<CharacterEquipmentSaveData> entries
        = new List<CharacterEquipmentSaveData>(3);

    /// <summary>해당 캐릭터의 장비 상태를 찾거나, 없으면 새로 만듭니다.</summary>
    public CharacterEquipmentSaveData GetOrCreate(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return null;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].characterId == characterId)
                return entries[i];
        }

        CharacterEquipmentSaveData data = new CharacterEquipmentSaveData
        {
            characterId = characterId
        };
        data.EnsureSlots();
        entries.Add(data);
        return data;
    }
}

/// <summary>
/// 한 캐릭터의 장비 슬롯 상태입니다.
/// 8슬롯에 상점 아이템 ID가 장착됩니다.
/// </summary>
[Serializable]
public sealed class CharacterEquipmentSaveData
{
    /// <summary>캐릭터 고유 ID입니다.</summary>
    public string characterId;

    /// <summary>8슬롯에 장착된 상점 아이템 ID 목록입니다. 빈 슬롯은 빈 문자열입니다.</summary>
    public List<string> slotItemIds = new List<string>(8);

    /// <summary>보유 중인 아이템 ID → 수량 매핑입니다.</summary>
    public List<OwnedShopItemEntry> ownedItems = new List<OwnedShopItemEntry>(13);

    public const int MaxSlots = 8;

    /// <summary>슬롯 목록이 8칸인지 보장합니다.</summary>
    public void EnsureSlots()
    {
        while (slotItemIds.Count < MaxSlots)
            slotItemIds.Add(string.Empty);
    }

    /// <summary>해당 슬롯에 아이템을 장착합니다.</summary>
    public bool Equip(int slotIndex, string itemId)
    {
        EnsureSlots();
        if (slotIndex < 0 || slotIndex >= MaxSlots) return false;
        slotItemIds[slotIndex] = itemId ?? string.Empty;
        return true;
    }

    /// <summary>해당 슬롯의 아이템을 해제합니다.</summary>
    public bool Unequip(int slotIndex)
    {
        EnsureSlots();
        if (slotIndex < 0 || slotIndex >= MaxSlots) return false;
        slotItemIds[slotIndex] = string.Empty;
        return true;
    }

    /// <summary>해당 아이템의 보유 수량을 반환합니다.</summary>
    public int GetOwnedCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return 0;
        for (int i = 0; i < ownedItems.Count; i++)
        {
            if (ownedItems[i] != null && ownedItems[i].itemId == itemId)
                return ownedItems[i].count;
        }
        return 0;
    }

    /// <summary>아이템 보유 수량을 설정합니다.</summary>
    public void SetOwnedCount(string itemId, int count)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return;
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

/// <summary>보유 아이템 엔트리입니다.</summary>
[Serializable]
public sealed class OwnedShopItemEntry
{
    public string itemId;
    public int count;
}