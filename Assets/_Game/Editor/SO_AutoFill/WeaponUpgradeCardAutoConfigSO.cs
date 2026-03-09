// UTF-8
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/SO/레벨업/무기 업그레이드 카드 정답지", fileName = "WeaponUpgradeCardAutoConfig")]
public sealed class WeaponUpgradeCardAutoConfigSO : ScriptableObject
{
    [Serializable]
    public sealed class CardSnapshot
    {
        [Header("복원 키(카드 에셋 이름)")]
        public string assetName;

        [Header("대상(슬롯 기반)")]
        public int slotIndex = -1;
        public string weaponNameKr;
        public string weaponId;

        [Header("업그레이드")]
        public WeaponUpgradeType type;

        [Header("수치")]
        public UpgradeValue value;

        [Header("UI")]
        public Sprite icon;
        public string titleKr;
        [TextArea] public string descKr;
        public string tagsKr;
    }

    public List<CardSnapshot> cards = new List<CardSnapshot>(256);

    public CardSnapshot Find(string assetName)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (string.Equals(cards[i].assetName, assetName, StringComparison.OrdinalIgnoreCase))
                return cards[i];
        }
        return null;
    }
}