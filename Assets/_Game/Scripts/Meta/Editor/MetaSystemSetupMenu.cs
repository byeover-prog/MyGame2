#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MetaSystemSetupMenu
{
    private const string ProjectRoot = "Assets/_Game";
    private const string MetaRoot = ProjectRoot + "/Data/Meta";
    private const string ProgressionRoot = MetaRoot + "/Progression";
    private const string UpgradeRoot = MetaRoot + "/Upgrades";
    private const string RewardRoot = MetaRoot + "/Rewards";

    [MenuItem("Tools/혼령검/메타/SO/필수 폴더 만들기")]
    public static void CreateRequiredFolders()
    {
        EnsureFolder("Assets", "_Game");
        EnsureFolder(ProjectRoot, "Data");
        EnsureFolder(ProjectRoot + "/Data", "Meta");
        EnsureFolder(MetaRoot, "Progression");
        EnsureFolder(MetaRoot, "Upgrades");
        EnsureFolder(MetaRoot, "Rewards");

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("혼령검", "메타 필수 폴더 생성을 완료했습니다.\nAssets/_Game/Data/Meta", "확인");
    }

    [MenuItem("Tools/혼령검/메타/SO/기본 자산 자동 생성")]
    public static void CreateDefaultMetaAssets()
    {
        CreateRequiredFolders();

        CharacterCatalogSO catalog = FindCatalog();
        if (catalog == null)
        {
            EditorUtility.DisplayDialog("혼령검", "CharacterCatalogSO를 찾지 못했습니다.\n먼저 캐릭터 카탈로그를 준비하세요.", "확인");
            return;
        }

        CharacterLevelCurveSO defaultCurve = LoadOrCreateAsset<CharacterLevelCurveSO>(
            ProgressionRoot + "/CharacterLevelCurve_Default.asset",
            curve => SeedDefaultCurve(curve));

        LoadOrCreateAsset<CharacterRunRewardConfigSO>(
            RewardRoot + "/CharacterRunRewardConfig_Default.asset",
            _ => { });

        SerializedObject catalogSo = new SerializedObject(catalog);
        SerializedProperty charactersProp = catalogSo.FindProperty("characters");

        if (charactersProp == null || !charactersProp.isArray)
        {
            EditorUtility.DisplayDialog("혼령검", "카탈로그 캐릭터 목록을 읽지 못했습니다.", "확인");
            return;
        }

        for (int i = 0; i < charactersProp.arraySize; i++)
        {
            CharacterDefinitionSO definition = charactersProp.GetArrayElementAtIndex(i).objectReferenceValue as CharacterDefinitionSO;
            if (definition == null) continue;

            SerializedObject definitionSo = new SerializedObject(definition);

            SerializedProperty levelCurveProp = definitionSo.FindProperty("levelCurve");
            if (levelCurveProp != null && levelCurveProp.objectReferenceValue == null)
                levelCurveProp.objectReferenceValue = defaultCurve;

            SerializedProperty upgradeTreeProp = definitionSo.FindProperty("upgradeTree");
            if (upgradeTreeProp != null && upgradeTreeProp.objectReferenceValue == null)
            {
                string safeName = MakeSafeFileName(string.IsNullOrWhiteSpace(definition.CharacterId) ? definition.name : definition.CharacterId);
                string assetPath = UpgradeRoot + $"/{safeName}_UpgradeTree.asset";
                CharacterUpgradeTreeSO tree = AssetDatabase.LoadAssetAtPath<CharacterUpgradeTreeSO>(assetPath);
                if (tree == null)
                {
                    tree = ScriptableObject.CreateInstance<CharacterUpgradeTreeSO>();
                    CharacterUpgradeTreeSO.OverwriteWithRuntimeDefault(tree, definition);
                    AssetDatabase.CreateAsset(tree, assetPath);
                }

                upgradeTreeProp.objectReferenceValue = tree;
            }

            definitionSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(definition);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("혼령검", "기본 메타 자산 자동 생성을 완료했습니다.\n- 기본 레벨 곡선\n- 런 보상 설정\n- 캐릭터별 강화 트리", "확인");
    }

    private static CharacterCatalogSO FindCatalog()
    {
        string[] selectedGuids = Selection.assetGUIDs;
        for (int i = 0; i < selectedGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(selectedGuids[i]);
            CharacterCatalogSO selected = AssetDatabase.LoadAssetAtPath<CharacterCatalogSO>(path);
            if (selected != null) return selected;
        }

        string[] guids = AssetDatabase.FindAssets("t:CharacterCatalogSO");
        if (guids == null || guids.Length == 0) return null;

        string firstPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<CharacterCatalogSO>(firstPath);
    }

    private static T LoadOrCreateAsset<T>(string assetPath, System.Action<T> onCreate) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset != null) return asset;

        string directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        if (!string.IsNullOrWhiteSpace(directory))
            EnsureFolderRecursive(directory);

        asset = ScriptableObject.CreateInstance<T>();
        onCreate?.Invoke(asset);
        AssetDatabase.CreateAsset(asset, assetPath);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static void SeedDefaultCurve(CharacterLevelCurveSO curve)
    {
        if (curve == null) return;

        SerializedObject so = new SerializedObject(curve);
        SerializedProperty unlocksProp = so.FindProperty("unlocks");
        if (unlocksProp != null && unlocksProp.arraySize == 0)
        {
            AddUnlock(unlocksProp, 5, "unlock_basic_5", "기본기 보강", "기본 스킬 추가 강화 분기가 열립니다.", CharacterLevelUnlockKind2D.BasicSkillAugment);
            AddUnlock(unlocksProp, 10, "unlock_ult_10", "궁극기 보강", "궁극기 강화 분기가 열립니다.", CharacterLevelUnlockKind2D.UltimateAugment);
            AddUnlock(unlocksProp, 15, "unlock_passive_15", "패시브 증폭", "패시브 강화 분기가 열립니다.", CharacterLevelUnlockKind2D.PassiveAugment);
            AddUnlock(unlocksProp, 20, "unlock_support_20", "지원 시너지", "지원 캐릭터 운용 해금이 열립니다.", CharacterLevelUnlockKind2D.SupportAura);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(curve);
    }

    private static void AddUnlock(SerializedProperty arrayProp, int level, string unlockId, string title, string description, CharacterLevelUnlockKind2D kind)
    {
        int index = arrayProp.arraySize;
        arrayProp.InsertArrayElementAtIndex(index);
        SerializedProperty element = arrayProp.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("level").intValue = level;
        element.FindPropertyRelative("unlockId").stringValue = unlockId;
        element.FindPropertyRelative("titleKr").stringValue = title;
        element.FindPropertyRelative("descriptionKr").stringValue = description;
        element.FindPropertyRelative("kind").enumValueIndex = (int)kind;
    }

    private static void EnsureFolderRecursive(string fullPath)
    {
        string[] parts = fullPath.Split('/');
        if (parts.Length == 0) return;

        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            EnsureFolder(current, parts[i]);
            current += "/" + parts[i];
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (AssetDatabase.IsValidFolder(path)) return;
        AssetDatabase.CreateFolder(parent, child);
    }

    private static string MakeSafeFileName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Character";

        foreach (char c in Path.GetInvalidFileNameChars())
            raw = raw.Replace(c.ToString(), string.Empty);

        return raw.Replace(' ', '_');
    }
}
#endif
