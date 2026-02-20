#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SOAutoBuilderMenu
{
    private const string Root = "Assets/_Game/Data";
    private const string SkillFolder = Root + "/Skills";

    [MenuItem("Tools/그날이후/SO/필수 폴더 만들기")]
    public static void CreateFolders()
    {
        CreateFolderIfMissing("Assets/_Game");
        CreateFolderIfMissing("Assets/_Game/Data");
        CreateFolderIfMissing(SkillFolder);

        AssetDatabase.Refresh();
        Debug.Log("[SO] 폴더 생성 완료");
    }

    [MenuItem("Tools/그날이후/SO/스킬 SO 템플릿 생성(예시 3개)")]
    public static void CreateSkillTemplates()
    {
        CreateFolders();

        CreateSkillIfMissing("skill_arrow", "발시", SkillKind.Weapon, "SO_Skill_Arrow");
        CreateSkillIfMissing("skill_darkorb", "다크 오브", SkillKind.CommonSkill, "SO_Skill_DarkOrb");
        CreateSkillIfMissing("skill_shuriken", "수리검", SkillKind.CommonSkill, "SO_Skill_Shuriken");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SO] 스킬 템플릿 3개 생성 완료");
    }

    [MenuItem("Tools/그날이후/SO/검증: 스킬 SO 누락/빈값 체크")]
    public static void ValidateSkills()
    {
        var guids = AssetDatabase.FindAssets("t:SkillDefinitionSO", new[] { SkillFolder });
        int errorCount = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(path);
            if (so == null) continue;

            if (string.IsNullOrWhiteSpace(so.SkillId))
            {
                Debug.LogError($"[SO][스킬] SkillId 비어있음: {path}", so);
                errorCount++;
            }

            if (string.IsNullOrWhiteSpace(so.DisplayName))
            {
                Debug.LogError($"[SO][스킬] DisplayName 비어있음: {path}", so);
                errorCount++;
            }
        }

        if (errorCount == 0) Debug.Log("[SO] 스킬 SO 검증 통과");
        else Debug.LogWarning($"[SO] 스킬 SO 검증 에러 {errorCount}개");
    }

    private static void CreateFolderIfMissing(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        var parent = Path.GetDirectoryName(folderPath);
        var name = Path.GetFileName(folderPath);
        if (!AssetDatabase.IsValidFolder(parent)) CreateFolderIfMissing(parent);

        AssetDatabase.CreateFolder(parent, name);
    }

    private static void CreateSkillIfMissing(string id, string displayName, SkillKind kind, string assetName)
    {
        var path = $"{SkillFolder}/{assetName}.asset";
        if (File.Exists(path)) return;

        var so = ScriptableObject.CreateInstance<SkillDefinitionSO>();

        // SerializedField이라 직접 접근 못하니 SerializedObject로 세팅
        var serialized = new SerializedObject(so);
        serialized.FindProperty("skillId").stringValue = id;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("kind").enumValueIndex = (int)kind;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(so, path);
    }
}
#endif
