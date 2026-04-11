using System.Collections.Generic;
using UnityEngine;

// 상점에 표시할 아이템 목록을 관리하는 데이터베이스입니다.
// 기획서 기준 13종 아이템이 등록됩니다.

[CreateAssetMenu(menuName = "혼령검/메타/상점 데이터베이스", fileName = "ShopDatabase")]
public sealed class ShopDatabaseSO : ScriptableObject
{
    [Header("상점 아이템 목록")]
    [Tooltip("상점에 표시할 아이템 목록 (13종)입니다.")]
    [SerializeField] private List<ShopItemSO> items = new List<ShopItemSO>(13);

    /// <summary>등록된 아이템 목록입니다.</summary>
    public IReadOnlyList<ShopItemSO> Items => items;

    /// <summary>런타임에서 접근 가능한 인스턴스입니다. MetaAutoBootstrap2D에서 설정합니다.</summary>
    public static ShopDatabaseSO RuntimeInstance { get; set; }

    /// <summary>ID로 아이템을 찾습니다.</summary>
    public bool TryFindById(string itemId, out ShopItemSO found)
    {
        found = null;
        if (string.IsNullOrWhiteSpace(itemId) || items == null) return false;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].ItemId == itemId)
            {
                found = items[i];
                return true;
            }
        }
        return false;
    }

    /// <summary>인덱스로 아이템을 가져옵니다.</summary>
    public ShopItemSO GetByIndex(int index)
    {
        if (items == null || index < 0 || index >= items.Count) return null;
        return items[index];
    }
}