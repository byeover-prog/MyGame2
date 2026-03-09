using System;
using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/공통스킬/프리팹 레지스트리", fileName = "Registry_CommonSkillPrefabs")]
public sealed class CommonSkillPrefabRegistrySO : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public CommonSkillKind kind;
        public GameObject weaponPrefab;
    }

    [Header("Kind -> Weapon Prefab")]
    public Entry[] entries;

    public GameObject Get(CommonSkillKind kind)
    {
        if (entries == null) return null;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].kind == kind)
                return entries[i].weaponPrefab;
        }
        return null;
    }
}