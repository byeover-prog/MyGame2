#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HonryeomCharacterSkillTools
{
    private const string DataFolder = "Assets/_Game/Data/Skills/Character";
    private const string BalanceFolder = "Assets/_Game/Balance";
    private const string DatabasePath = "Assets/_Game/Data/Skills/Character/CharacterSkillDatabase.asset";
    private const string JsonOutputPath = "Assets/_Game/Balance/character_skill_balance.generated.json";

    [MenuItem("Tool/혼령검/SO/필수 폴더 만들기")]
    public static void CreateRequiredFolders()
    {
        EnsureFolder("Assets/_Game");
        EnsureFolder("Assets/_Game/Data");
        EnsureFolder("Assets/_Game/Data/Skills");
        EnsureFolder(DataFolder);
        EnsureFolder(BalanceFolder);

        AssetDatabase.Refresh();
        Debug.Log("[혼령검 스킬 툴] 필수 폴더 생성 완료");
    }

    [MenuItem("Tool/혼령검/Skill/캐릭터 전용 스킬 DB 자동 갱신")]
    public static void RefreshCharacterSkillDatabase()
    {
        CreateRequiredFolders();

        CharacterSkillDatabaseSO database = LoadOrCreateDatabase();

        CharacterSkillDefinitionSO[] skills = FindAssets<CharacterSkillDefinitionSO>();
        CharacterSkillSetSO[] sets = FindAssets<CharacterSkillSetSO>();

        Array.Sort(skills, (a, b) => string.Compare(a.SkillId, b.SkillId, StringComparison.Ordinal));
        Array.Sort(sets, (a, b) => string.Compare(a.CharacterId, b.CharacterId, StringComparison.Ordinal));

        SerializedObject so = new SerializedObject(database);

        SetObjectArray(so.FindProperty("characterSkills"), skills);
        SetObjectArray(so.FindProperty("characterSkillSets"), sets);

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        database.RebuildIndex();

        Debug.Log($"[혼령검 스킬 툴] 캐릭터 전용 스킬 DB 자동 갱신 완료 | 스킬 {skills.Length}개 | 캐릭터 세트 {sets.Length}개", database);
    }

    [MenuItem("Tool/혼령검/Skill/캐릭터 전용 스킬 JSON 내보내기")]
    public static void ExportCharacterSkillJson()
    {
        CreateRequiredFolders();

        CharacterSkillDefinitionSO[] skills = FindAssets<CharacterSkillDefinitionSO>();
        Array.Sort(skills, (a, b) => string.Compare(a.SkillId, b.SkillId, StringComparison.Ordinal));

        GeneratedCharacterSkillJsonRoot root = new GeneratedCharacterSkillJsonRoot
        {
            notice = "자동 생성 파일입니다. 직접 수정하지 마세요. Unity의 CharacterSkillDefinitionSO를 수정한 뒤 다시 내보내세요.",
            version = 1,
            generatedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            skills = ConvertSkills(skills)
        };

        string json = JsonUtility.ToJson(root, true);
        string fullPath = Path.GetFullPath(JsonOutputPath);

        File.WriteAllText(fullPath, json);
        AssetDatabase.Refresh();

        Debug.Log($"[혼령검 스킬 툴] 캐릭터 전용 스킬 JSON 생성 완료: {JsonOutputPath}");
    }

    [MenuItem("Tool/혼령검/Skill/신규 전용 스킬 SO 생성")]
    public static void CreateCharacterSkillDefinition()
    {
        CreateRequiredFolders();

        CharacterSkillDefinitionSO asset = ScriptableObject.CreateInstance<CharacterSkillDefinitionSO>();

        string path = AssetDatabase.GenerateUniqueAssetPath($"{DataFolder}/Skill_NewCharacterSkill.asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);

        Debug.Log($"[혼령검 스킬 툴] 신규 전용 스킬 SO 생성: {path}", asset);
    }

    private static CharacterSkillDatabaseSO LoadOrCreateDatabase()
    {
        CharacterSkillDatabaseSO database = AssetDatabase.LoadAssetAtPath<CharacterSkillDatabaseSO>(DatabasePath);
        if (database != null)
            return database;

        database = ScriptableObject.CreateInstance<CharacterSkillDatabaseSO>();
        AssetDatabase.CreateAsset(database, DatabasePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[혼령검 스킬 툴] 캐릭터 전용 스킬 DB 생성: {DatabasePath}", database);
        return database;
    }

    private static T[] FindAssets<T>() where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        List<T> results = new List<T>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null)
                results.Add(asset);
        }

        return results.ToArray();
    }

    private static void SetObjectArray<T>(SerializedProperty property, T[] values) where T : UnityEngine.Object
    {
        if (property == null)
            return;

        property.arraySize = values != null ? values.Length : 0;

        if (values == null)
            return;

        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        string parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(assetPath);

        if (string.IsNullOrEmpty(parent))
            return;

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static GeneratedCharacterSkillJsonEntry[] ConvertSkills(CharacterSkillDefinitionSO[] skills)
    {
        if (skills == null)
            return Array.Empty<GeneratedCharacterSkillJsonEntry>();

        List<GeneratedCharacterSkillJsonEntry> entries = new List<GeneratedCharacterSkillJsonEntry>();

        for (int i = 0; i < skills.Length; i++)
        {
            CharacterSkillDefinitionSO skill = skills[i];
            if (skill == null) continue;
            if (string.IsNullOrWhiteSpace(skill.SkillId)) continue;

            GeneratedCharacterSkillJsonEntry entry = new GeneratedCharacterSkillJsonEntry
            {
                id = skill.SkillId,
                displayName = skill.DisplayName,
                ownerCharacterId = skill.OwnerCharacterId,
                element = skill.Element.ToString(),
                maxLevel = skill.MaxLevel,
                levels = ConvertLevels(skill.LevelBalances)
            };

            entries.Add(entry);
        }

        return entries.ToArray();
    }

    private static SkillLevelBalanceData2D[] ConvertLevels(SkillLevelBalanceData2D[] levels)
    {
        if (levels == null)
            return Array.Empty<SkillLevelBalanceData2D>();

        List<SkillLevelBalanceData2D> result = new List<SkillLevelBalanceData2D>();

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null)
                result.Add(levels[i]);
        }

        return result.ToArray();
    }

    [Serializable]
    private sealed class GeneratedCharacterSkillJsonRoot
    {
        public string notice;
        public int version;
        public string generatedAtUtc;
        public GeneratedCharacterSkillJsonEntry[] skills;
    }

    [Serializable]
    private sealed class GeneratedCharacterSkillJsonEntry
    {
        public string id;
        public string displayName;
        public string ownerCharacterId;
        public string element;
        public int maxLevel;
        public SkillLevelBalanceData2D[] levels;
    }
}
#endif