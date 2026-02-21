#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 프로젝트 내 '데이터 진입점(Root SO 3개)'를 자동으로 생성하는 최소 Editor 도구.
/// 
/// 왜 하나만 남기나?
/// - 자동 생성기가 여러 개면, 서로 다른 폴더/스펙/에셋을 만들어 "어제 됐는데 오늘 안 됨"이 반복된다.
/// - 따라서 "Root 생성 + 폴더 기준" 같은 핵심 도구 1개만 유지한다.
/// 
/// 주의
/// - 이 도구는 "새 에셋 생성"만 한다.
/// - 기존 에셋을 덮어쓰지 않는다.
/// </summary>
public static class GameDataRootTools
{
    private const string GameRoot = "Assets/_Game";
    private const string DataRoot = GameRoot + "/Data";
    private const string RootsFolder = DataRoot + "/Roots";

    [MenuItem("Tools/그날이후/데이터/1) 표준 폴더 생성(최소)")]
    public static void EnsureFolders()
    {
        EnsureFolder(GameRoot);
        EnsureFolder(DataRoot);
        EnsureFolder(RootsFolder);

        // 팀 규칙: SO는 _Game/Data, Prefab은 _Game/Prefabs, Script는 _Game/Scripts, Editor는 _Game/Editor
        EnsureFolder(GameRoot + "/Prefabs");
        EnsureFolder(GameRoot + "/Scripts");
        EnsureFolder(GameRoot + "/Editor");

        // 권장 데이터 하위 폴더
        EnsureFolder(DataRoot + "/Character");
        EnsureFolder(DataRoot + "/Skills");

        AssetDatabase.Refresh();
        Debug.Log("[GameDataRootTools] 폴더 생성 완료");
    }

    [MenuItem("Tools/그날이후/데이터/2) Roots 3개 생성(Assets/_Game/Data/Roots)")]
    public static void CreateRootAssets()
    {
        EnsureFolders();

        CreateAssetIfMissing<CharacterRootSO>(RootsFolder, "Root_Character.asset");
        CreateAssetIfMissing<SkillRootSO>(RootsFolder, "Root_Skill.asset");
        CreateAssetIfMissing<LevelUpRootSO>(RootsFolder, "Root_LevelUp.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[GameDataRootTools] Root SO 3개 생성/확인 완료");
    }

    private static void CreateAssetIfMissing<T>(string folder, string fileName) where T : ScriptableObject
    {
        string path = folder + "/" + fileName;
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return;

        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        EditorUtility.SetDirty(asset);
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
}
#endif
