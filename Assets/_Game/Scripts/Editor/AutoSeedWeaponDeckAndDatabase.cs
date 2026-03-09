#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AutoSeedWeaponDeckAndDatabase
{
    [MenuItem("Tools/그날이후/레벨업/Auto SO 채우기(Deck/DB)")]
    public static void Seed()
    {
        // 씬의 시스템 찾기
        var sys = UnityEngine.Object.FindFirstObjectByType<PlayerSkillUpgradeSystem>();
        if (sys == null)
        {
            Debug.LogError("[AutoSeed] 씬에서 PlayerSkillUpgradeSystem을 못 찾음. Scene_Game 열고 실행해.");
            return;
        }

        // sys의 deck/db 참조 가져오기
        var sysSO = new SerializedObject(sys);
        var deckProp = sysSO.FindProperty("deck");
        var dbProp   = sysSO.FindProperty("weaponDatabase");

        if (deckProp == null || dbProp == null || deckProp.objectReferenceValue == null || dbProp.objectReferenceValue == null)
        {
            Debug.LogError("[AutoSeed] PlayerSkillUpgradeSystem의 deck/weaponDatabase 참조가 비었음(또는 필드명 불일치).");
            return;
        }

        var deckObj = deckProp.objectReferenceValue;
        var dbObj   = dbProp.objectReferenceValue;

        // WeaponDefinitionSO 전부 찾기
        var defType = FindTypeBySimpleName("WeaponDefinitionSO");
        if (defType == null)
        {
            Debug.LogError("[AutoSeed] WeaponDefinitionSO 타입을 못 찾음. 실제 타입명이 다르면 그 이름을 알려줘.");
            return;
        }

        var defGuids = AssetDatabase.FindAssets("t:WeaponDefinitionSO");
        if (defGuids == null || defGuids.Length == 0)
        {
            Debug.LogError("[AutoSeed] WeaponDefinitionSO 에셋이 0개임. (무기 정의 SO가 아직 없거나 타입명이 다름)");
            return;
        }

        var defs = defGuids
            .Select(g => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(o => o != null && defType.IsAssignableFrom(o.GetType()))
            .ToArray();

        // 너무 많으면 상위 8개만(프로토타입)
        var pick = defs.Take(8).ToArray();

        int dbWritten = TryWriteObjectRefArray(dbObj, defType, pick);
        int deckWritten = TryWriteDeck(deckObj, defType, pick);

        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(deckObj);
        EditorUtility.SetDirty(dbObj);

        Debug.Log($"[AutoSeed] 완료. DB에 넣은 정의 수={dbWritten}, Deck에 넣은 항목 수={deckWritten}\n" +
                  $"(DB/Deck의 필드 구조가 특이하면 0이 나올 수 있음. 그땐 SO 인스펙터 스샷 1장 주면 필드명 맞춰서 고정해줌.)");
    }

    // DB 쪽: "WeaponDefinitionSO[] / List<WeaponDefinitionSO>" 같은 ObjectReference 배열을 찾아 거기에 넣는다.
    private static int TryWriteObjectRefArray(UnityEngine.Object target, Type elementType, UnityEngine.Object[] values)
    {
        var so = new SerializedObject(target);

        // SerializedProperty를 훑어서 "배열 + ObjectReference + elementType"인 곳을 찾아 첫 곳에 채움
        var it = so.GetIterator();
        bool enterChildren = true;

        while (it.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (!it.isArray) continue;
            if (it.propertyType != SerializedPropertyType.Generic) continue;

            // 배열의 element가 ObjectReference인지 확인
            if (it.arraySize > 0)
            {
                var e0 = it.GetArrayElementAtIndex(0);
                if (e0.propertyType != SerializedPropertyType.ObjectReference) continue;
            }
            else
            {
                // 빈 배열이면 element 타입 체크가 어려우니 "ObjectReference 배열"로 가정하고 시도
            }

            // 채우기
            it.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                var e = it.GetArrayElementAtIndex(i);
                if (e.propertyType != SerializedPropertyType.ObjectReference) break;
                e.objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return values.Length;
        }

        return 0;
    }

    // Deck 쪽은 프로젝트마다 구조가 달라서 3가지 패턴을 순서대로 시도
    // 1) WeaponDefinitionSO 참조 배열
    // 2) string id 배열(WeaponDefinitionSO.name을 id로 넣는 임시)
    private static int TryWriteDeck(UnityEngine.Object deckObj, Type defType, UnityEngine.Object[] defs)
    {
        var so = new SerializedObject(deckObj);

        // 1) ObjectReference 배열 먼저 찾기
        int wrote = TryWriteObjectRefArray(deckObj, defType, defs);
        if (wrote > 0) return wrote;

        // 2) string 배열 찾기
        var it = so.GetIterator();
        bool enterChildren = true;

        while (it.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (!it.isArray) continue;
            if (it.propertyType != SerializedPropertyType.Generic) continue;

            // string 배열인지 확인
            if (it.arraySize > 0)
            {
                var e0 = it.GetArrayElementAtIndex(0);
                if (e0.propertyType != SerializedPropertyType.String) continue;
            }
            else
            {
                // 비어있으면 string 배열일 수도 있으니 시도
            }

            it.arraySize = defs.Length;
            for (int i = 0; i < defs.Length; i++)
            {
                var e = it.GetArrayElementAtIndex(i);
                if (e.propertyType != SerializedPropertyType.String) break;
                e.stringValue = defs[i].name; // 임시 id
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return defs.Length;
        }

        return 0;
    }

    private static Type FindTypeBySimpleName(string simpleName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch { continue; }

            foreach (var t in types)
            {
                if (t != null && t.Name == simpleName) return t;
            }
        }
        return null;
    }
}
#endif