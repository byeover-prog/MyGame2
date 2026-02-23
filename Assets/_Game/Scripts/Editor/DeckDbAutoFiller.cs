#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DeckDbAutoFiller
{
    [MenuItem("Tools/그날이후/레벨업/Deck+DB weapons 자동 채우기(WeaponDefinitionSO)")]
    public static void Fill()
    {
        var sys = Object.FindFirstObjectByType<PlayerSkillUpgradeSystem>();
        if (sys == null)
        {
            Debug.LogError("[DeckDbAutoFiller] 씬에서 PlayerSkillUpgradeSystem을 못 찾음.");
            return;
        }

        // sys.deck / sys.weaponDatabase 가져오기
        var soSys = new SerializedObject(sys);
        var deckObj = soSys.FindProperty("deck")?.objectReferenceValue;
        var dbObj   = soSys.FindProperty("weaponDatabase")?.objectReferenceValue;

        if (deckObj == null || dbObj == null)
        {
            Debug.LogError("[DeckDbAutoFiller] deck 또는 weaponDatabase 참조가 비어있음.");
            return;
        }

        // 프로젝트의 WeaponDefinitionSO 전부 수집
        var defGuids = AssetDatabase.FindAssets("t:WeaponDefinitionSO");
        if (defGuids == null || defGuids.Length == 0)
        {
            Debug.LogError("[DeckDbAutoFiller] WeaponDefinitionSO 에셋이 0개임.");
            return;
        }

        var defs = defGuids
            .Select(g => AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(d => d != null)
            // “후보로 쓸만한 것만” 필터(안전 기본값)
            .Where(d => d.includeInPrototype)
            .Where(d => d.weight > 0)
            .Where(d => d.projectilePrefab != null)
            .ToArray();

        if (defs.Length == 0)
        {
            Debug.LogError("[DeckDbAutoFiller] 필터 결과 0개. (includeInPrototype/weight/projectilePrefab 확인)");
            return;
        }

        // Deck/DB 둘 다 같은 defs로 채우기
        int deckCount = FillWeaponsArray(deckObj, defs);
        int dbCount   = FillWeaponsArray(dbObj, defs);

        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(deckObj);
        EditorUtility.SetDirty(dbObj);

        Debug.Log($"[DeckDbAutoFiller] 완료. Deck={deckCount}개, DB={dbCount}개 채움. 이제 레벨업 카드가 떠야 정상.");
    }

    private static int FillWeaponsArray(Object targetSO, WeaponDefinitionSO[] defs)
    {
        var so = new SerializedObject(targetSO);

        // 필드명이 다르면 여기서 터짐. (현재 코드는 'weapons' 배열을 기대)
        var weapons = so.FindProperty("weapons");
        if (weapons == null || !weapons.isArray)
        {
            Debug.LogError($"[DeckDbAutoFiller] '{targetSO.name}'에 weapons 배열이 없음(필드명 다름).");
            return 0;
        }

        weapons.arraySize = defs.Length;
        for (int i = 0; i < defs.Length; i++)
        {
            var e = weapons.GetArrayElementAtIndex(i);
            e.objectReferenceValue = defs[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        return defs.Length;
    }
}
#endif