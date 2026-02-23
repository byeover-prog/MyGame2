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
        CreateFolderIfMissing(Root);
        CreateFolderIfMissing(SkillFolder);

        AssetDatabase.Refresh();
        Debug.Log("[SO] 폴더 생성 완료");
    }

    [MenuItem("Tools/그날이후/SO/스킬 SO 템플릿 생성(예시 3개)")]
    public static void CreateSkillTemplates()
    {
        CreateFolders();

        // 예시: definitionType만 구분하고, 실제 로직 SO는 linkedAsset에 연결(여기서는 비워둠)
        CreateSkillIfMissing(
            id: "skill_arrow",
            titleKr: "발시",
            defType: SkillDefinitionType.Weapon,
            assetName: "SkillDef_Arrow");

        CreateSkillIfMissing(
            id: "skill_darkorb",
            titleKr: "다크 오브",
            defType: SkillDefinitionType.CommonSkill,
            assetName: "SkillDef_DarkOrb");

        CreateSkillIfMissing(
            id: "skill_shuriken",
            titleKr: "수리검",
            defType: SkillDefinitionType.CommonSkill,
            assetName: "SkillDef_Shuriken");

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

            // SkillDefinitionSO의 필드명에 맞춰 검증
            if (string.IsNullOrWhiteSpace(so.id))
            {
                Debug.LogError($"[SO][스킬] id 비어있음: {path}", so);
                errorCount++;
            }

            if (string.IsNullOrWhiteSpace(so.titleKr))
            {
                Debug.LogError($"[SO][스킬] titleKr 비어있음: {path}", so);
                errorCount++;
            }

            // 실제 로직 SO 연결은 선택사항이지만, 타입이 Weapon/CommonSkill인데 비어있으면 경고로 띄우기
            if ((so.definitionType == SkillDefinitionType.CommonSkill || so.definitionType == SkillDefinitionType.Weapon)
                && so.linkedAsset == null)
            {
                Debug.LogWarning($"[SO][스킬] linkedAsset 미연결(선택): {path}", so);
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

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            CreateFolderIfMissing(parent);

        AssetDatabase.CreateFolder(parent, name);
    }

    private static void CreateSkillIfMissing(string id, string titleKr, SkillDefinitionType defType, string assetName)
    {
        var path = $"{SkillFolder}/{assetName}.asset";
        if (File.Exists(path)) return;

        var so = ScriptableObject.CreateInstance<SkillDefinitionSO>();

        // public 필드라면 직접 세팅 가능하지만, 필드가 SerializeField로 바뀌어도 안전하게 SerializedObject 사용
        var serialized = new SerializedObject(so);

        serialized.FindProperty("definitionType").enumValueIndex = (int)defType;
        serialized.FindProperty("id").stringValue = id;
        serialized.FindProperty("titleKr").stringValue = titleKr;

        // descriptionKr는 템플릿에선 비워둠(카드에 들어갈 "공격 방식" 문구를 나중에 채우기)
        serialized.FindProperty("descriptionKr").stringValue = "";

        // linkedAsset도 템플릿에선 비워둠(나중에 CommonSkillConfigSO/WeaponDefinitionSO 등을 연결)
        serialized.FindProperty("linkedAsset").objectReferenceValue = null;

        serialized.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(so, path);
    }
}
#endif