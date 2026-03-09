#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WeaponSkillDeckAutoFillMenu
{
    [MenuItem("Tools/그날이후/업그레이드/무기 업그레이드 풀 자동 채우기(선택한 Deck)")]
    public static void FillSelectedDeck()
    {
        var deck = Selection.objects.OfType<WeaponSkillDeckSO>().FirstOrDefault();
        if (deck == null)
        {
            Debug.LogError("[WeaponSkillDeckAutoFill] Project 창에서 WeaponSkillDeckSO 에셋을 하나 선택한 뒤 실행하세요.");
            return;
        }

        FillDeck(deck);
    }

    [MenuItem("Tools/그날이후/업그레이드/무기 업그레이드 풀 자동 채우기(프로젝트 내 모든 Deck)")]
    public static void FillAllDecks()
    {
        var deckGuids = AssetDatabase.FindAssets("t:WeaponSkillDeckSO");
        if (deckGuids == null || deckGuids.Length == 0)
        {
            Debug.LogError("[WeaponSkillDeckAutoFill] WeaponSkillDeckSO 에셋이 없습니다.");
            return;
        }

        int total = 0;
        foreach (var g in deckGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var deck = AssetDatabase.LoadAssetAtPath<WeaponSkillDeckSO>(path);
            if (deck == null) continue;

            FillDeck(deck);
            total++;
        }

        Debug.Log($"[WeaponSkillDeckAutoFill] 완료. Deck {total}개 갱신.");
    }

    private static void FillDeck(WeaponSkillDeckSO deck)
    {
        // 프로젝트의 WeaponDefinitionSO 전부 수집
        var defGuids = AssetDatabase.FindAssets("t:WeaponDefinitionSO");
        var defs = defGuids
            .Select(g => AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(d => d != null)
            // “풀에 넣을지”는 WeaponDefinitionSO.includeInPrototype 로 통제(원하는 3개만 true로 두면 정확히 3개만 들어감)
            .Where(d => d.includeInPrototype)
            // 안전 필터
            .Where(d => d.weight > 0)
            .Where(d => d.projectilePrefab != null)
            .OrderBy(d => d.weaponId)
            .ToArray();

        var so = new SerializedObject(deck);
        var weaponsProp = so.FindProperty("weapons");
        if (weaponsProp == null || !weaponsProp.isArray)
        {
            Debug.LogError("[WeaponSkillDeckAutoFill] WeaponSkillDeckSO 내부 필드명이 weapons 가 아닙니다.");
            return;
        }

        weaponsProp.arraySize = defs.Length;
        for (int i = 0; i < defs.Length; i++)
        {
            weaponsProp.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(deck);

        AssetDatabase.SaveAssets();

        Debug.Log($"[WeaponSkillDeckAutoFill] '{deck.name}' 채움: {defs.Length}개 (includeInPrototype=true 만)");
    }
}
#endif