#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 공통스킬 8종(스킬설정 8 + 레벨업카드 8 + 카탈로그 1 + 카드풀 1)을 한번에 생성/연결.
/// - 이미 존재하는 에셋은 재사용(덮어쓰기 없음)
/// - Root_Skill.asset이 있으면 commonSkillCatalog/commonSkillCardPool 자동 연결
/// </summary>
public static class CommonSkillBatchTools
{
    private const string Base = "Assets/_Game/Data";
    private const string Defs = Base + "/Defs/CommonSkills";
    private const string Cards = Base + "/Cards/CommonSkills";
    private const string Catalogs = Base + "/Catalogs";
    private const string Roots = Base + "/Roots";

    private static readonly string[] SkillNames =
    {
        "회전검","부메랑","총알","호밍미사일","다크오브","수리검","화살발사","화살비"
    };

    [MenuItem("Tools/그날이후/데이터/3) 공통스킬 8종 세트 생성(설정+카드+카탈로그+풀)")]
    public static void CreateCommonSkillSet()
    {
        EnsureFolder(Defs);
        EnsureFolder(Cards);
        EnsureFolder(Catalogs);
        EnsureFolder(Roots);

        // 1) 카탈로그/풀 생성 or 로드
        var catalog = LoadOrCreateAsset<CommonSkillCatalogSO>(Catalogs, "Catalog_CommonSkills.asset");
        var pool = LoadOrCreateAsset<CommonSkillCardPoolSO>(Catalogs, "Pool_CommonSkillLevelUpCards.asset");

        // 2) 스킬 설정 8개 생성/확보
        var skillConfigs = new List<CommonSkillConfigSO>(8);
        var skillConfigObjs = new List<UnityEngine.Object>(8);

        for (int i = 0; i < SkillNames.Length; i++)
        {
            string n = SkillNames[i];
            var cfg = LoadOrCreateAsset<CommonSkillConfigSO>(Defs, $"CS_{n}.asset");

            // 필드가 존재할 때만 세팅됨(없으면 무시)
            TrySetStringField(cfg, "displayName", n);
            TrySetStringField(cfg, "visualDescriptionKr", $"{n}: (레벨1 동작 설명을 여기에 작성)");

            skillConfigs.Add(cfg);
            skillConfigObjs.Add(cfg);
        }

        // 3) 카탈로그 리스트 채우기(필드명 후보)
        TryFillObjectRefList(
            catalog,
            new[] { "skills", "configs", "list", "items" },
            skillConfigObjs);

        // 4) 레벨업 카드 8개 생성/확보 + pool에 넣기
        var cardObjs = new List<UnityEngine.Object>(8);

        for (int i = 0; i < SkillNames.Length; i++)
        {
            string n = SkillNames[i];

            // 실제 타입: CommonSkillCardSO (너 프로젝트 기준)
            var card = LoadOrCreateAsset<CommonSkillCardSO>(Cards, $"CARD_CS_{n}.asset");

            TrySetObjectField(card, "skill", skillConfigs[i]);
            TrySetIntField(card, "weight", 10);

            cardObjs.Add(card);
        }

        // pool 리스트 채우기(필드명 후보)
        TryFillObjectRefList(
            pool,
            new[] { "cards", "list", "items" },
            cardObjs);

        // 5) Root_Skill 자동 연결(있으면)
        var root = AssetDatabase.LoadAssetAtPath<SkillRootSO>($"{Roots}/Root_Skill.asset");
        if (root != null)
        {
            TrySetObjectField(root, "commonSkillCatalog", catalog);
            TrySetObjectField(root, "commonSkillCardPool", pool);
            EditorUtility.SetDirty(root);
        }

        EditorUtility.SetDirty(catalog);
        EditorUtility.SetDirty(pool);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CommonSkillBatchTools] 공통스킬 8종 세트 생성/연결 완료");
    }

    // ----------------- helpers -----------------

    private static T LoadOrCreateAsset<T>(string folder, string fileName) where T : ScriptableObject
    {
        string path = $"{folder}/{fileName}";
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a != null) return a;

        a = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(a, path);
        EditorUtility.SetDirty(a);
        return a;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        string name = Path.GetFileName(folder);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }

    private static void TrySetStringField(UnityEngine.Object obj, string fieldName, string value)
    {
        if (obj == null) return;

        var so = new SerializedObject(obj);
        var p = so.FindProperty(fieldName);

        if (p != null && p.propertyType == SerializedPropertyType.String)
        {
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void TrySetIntField(UnityEngine.Object obj, string fieldName, int value)
    {
        if (obj == null) return;

        var so = new SerializedObject(obj);
        var p = so.FindProperty(fieldName);

        if (p != null && p.propertyType == SerializedPropertyType.Integer)
        {
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void TrySetObjectField(UnityEngine.Object obj, string fieldName, UnityEngine.Object value)
    {
        if (obj == null) return;

        var so = new SerializedObject(obj);
        var p = so.FindProperty(fieldName);

        if (p != null && p.propertyType == SerializedPropertyType.ObjectReference)
        {
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void TryFillObjectRefList(UnityEngine.Object obj, string[] possibleFieldNames, IList<UnityEngine.Object> items)
    {
        if (obj == null) return;

        var so = new SerializedObject(obj);
        SerializedProperty listProp = null;

        for (int i = 0; i < possibleFieldNames.Length; i++)
        {
            var p = so.FindProperty(possibleFieldNames[i]);
            if (p != null && p.isArray)
            {
                listProp = p;
                break;
            }
        }

        if (listProp == null) return;

        listProp.ClearArray();
        for (int i = 0; i < items.Count; i++)
        {
            listProp.InsertArrayElementAtIndex(i);
            var elem = listProp.GetArrayElementAtIndex(i);

            if (elem.propertyType == SerializedPropertyType.ObjectReference)
                elem.objectReferenceValue = items[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif