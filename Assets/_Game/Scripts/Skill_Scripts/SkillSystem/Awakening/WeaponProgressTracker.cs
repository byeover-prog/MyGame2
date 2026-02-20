// WeaponProgressTracker.cs
using System;
using UnityEngine;

public sealed class WeaponProgressTracker : MonoBehaviour, IWeaponProgressProvider
{
    [Serializable]
    private class Entry
    {
        public string weaponId;
        public int level;
        public bool awakened;
        public int maxedAtLevelUpIndex;
    }

    [SerializeField] private int maxLevel = 8;
    [SerializeField] private Entry[] entries = new Entry[0];

    public void NotifyWeaponLevelUp(string weaponId, int newLevel, int currentLevelUpIndex)
    {
        if (string.IsNullOrEmpty(weaponId))
            return;

        Entry e = GetOrCreateEntry(weaponId);
        if (newLevel > e.level)
            e.level = newLevel;

        if (e.level >= maxLevel)
        {
            e.level = maxLevel;
            // "8을 찍은 레벨업 인덱스" 저장
            e.maxedAtLevelUpIndex = currentLevelUpIndex;
        }
    }

    public void MarkAwakened(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId))
            return;

        Entry e = GetOrCreateEntry(weaponId);
        e.awakened = true;
    }

    public bool TryGetWeaponLevel(string weaponId, out int level)
    {
        level = 0;
        if (TryGetEntry(weaponId, out Entry e))
        {
            level = e.level;
            return true;
        }
        return false;
    }

    public bool IsWeaponAwakened(string weaponId)
    {
        return TryGetEntry(weaponId, out Entry e) && e.awakened;
    }

    public bool TryGetWeaponMaxedAtLevelUpIndex(string weaponId, out int levelUpIndex)
    {
        levelUpIndex = -1;
        if (TryGetEntry(weaponId, out Entry e))
        {
            levelUpIndex = e.maxedAtLevelUpIndex;
            return levelUpIndex >= 0;
        }
        return false;
    }

    private bool TryGetEntry(string weaponId, out Entry entry)
    {
        entry = null;
        if (entries == null)
            return false;

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e == null)
                continue;

            if (e.weaponId == weaponId)
            {
                entry = e;
                return true;
            }
        }

        return false;
    }

    private Entry GetOrCreateEntry(string weaponId)
    {
        if (TryGetEntry(weaponId, out Entry found))
            return found;

        // 무기 종류가 고정(8개)이면 entries를 인스펙터에 8개 미리 만들어두는 것을 권장
        int oldLen = entries != null ? entries.Length : 0;
        var newArr = new Entry[oldLen + 1];
        for (int i = 0; i < oldLen; i++)
            newArr[i] = entries[i];

        newArr[oldLen] = new Entry
        {
            weaponId = weaponId,
            level = 0,
            awakened = false,
            maxedAtLevelUpIndex = -1
        };

        entries = newArr;
        return entries[oldLen];
    }
}