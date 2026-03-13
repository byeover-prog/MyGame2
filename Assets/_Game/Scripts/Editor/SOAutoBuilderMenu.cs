// ──────────────────────────────────────────────
// SOAutoBuilderMenu.cs
// 에디터 전용: 스킬 SO 템플릿 생성 및 검증 도구
// [Tools → 그날이후 → SO] 메뉴에서 접근
// ──────────────────────────────────────────────

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using _Game.Skills;

public static class SOAutoBuilderMenu
{
    private const string Root           = "Assets/_Game/Data";
    private const string SkillFolder    = Root + "/Skills";
    private const string PassiveFolder  = Root + "/Skills/Passives";
    private const string BalanceFolder  = Root + "/Balance";

    // ════════════════════════════════════════════
    //  메뉴: 폴더 생성
    // ════════════════════════════════════════════

    [MenuItem("Tools/그날이후/SO/필수 폴더 만들기")]
    public static void CreateFolders()
    {
        CreateFolderIfMissing("Assets/_Game");
        CreateFolderIfMissing(Root);
        CreateFolderIfMissing(SkillFolder);
        CreateFolderIfMissing(PassiveFolder);
        CreateFolderIfMissing(BalanceFolder);

        AssetDatabase.Refresh();
        Debug.Log("[SO] 폴더 생성 완료");
    }

    // ════════════════════════════════════════════
    //  메뉴: 밸런스 테이블 SO 생성
    // ════════════════════════════════════════════

    [MenuItem("Tools/그날이후/SO/패시브 밸런스 테이블 생성")]
    public static void CreatePassiveBalanceTable()
    {
        CreateFolders();

        var path = $"{BalanceFolder}/PassiveBalanceTable.asset";
        if (File.Exists(path))
        {
            Debug.Log("[SO] PassiveBalanceTable 이미 존재함");
            return;
        }

        var so = ScriptableObject.CreateInstance<PassiveBalanceTableSO>();
        AssetDatabase.CreateAsset(so, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SO] PassiveBalanceTable 생성 완료 → " + path);

        // 생성 후 바로 선택해서 Inspector에 표시
        Selection.activeObject = so;
    }

    // ════════════════════════════════════════════
    //  메뉴: 액티브 스킬 SO 템플릿 생성
    // ════════════════════════════════════════════

    [MenuItem("Tools/그날이후/SO/액티브 스킬 SO 템플릿 생성(예시 5개)")]
    public static void CreateActiveSkillTemplates()
    {
        CreateFolders();

        CreateSkillSO("arrow_shot",      "발시",       SkillType.Active, PassiveStatType.None, "SkillDef_ArrowShot",      SkillFolder);
        CreateSkillSO("boomerang",       "부메랑",     SkillType.Active, PassiveStatType.None, "SkillDef_Boomerang",      SkillFolder);
        CreateSkillSO("spinning_sword",  "회전검",     SkillType.Active, PassiveStatType.None, "SkillDef_SpinningSword",  SkillFolder);
        CreateSkillSO("homing_missile",  "호밍미사일", SkillType.Active, PassiveStatType.None, "SkillDef_HomingMissile",  SkillFolder);
        CreateSkillSO("arrow_rain",      "화살비",     SkillType.Active, PassiveStatType.None, "SkillDef_ArrowRain",      SkillFolder);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SO] 액티브 스킬 템플릿 5개 생성 완료");
    }

    // ════════════════════════════════════════════
    //  메뉴: 패시브 스킬 SO 템플릿 생성 (8종)
    // ════════════════════════════════════════════

    [MenuItem("Tools/그날이후/SO/패시브 스킬 SO 템플릿 생성(8종)")]
    public static void CreatePassiveSkillTemplates()
    {
        CreateFolders();

        CreateSkillSO("passive_attack_power",    "공격력 증가",       SkillType.Passive, PassiveStatType.AttackPowerPercent,   "PassiveDef_AttackPower",    PassiveFolder);
        CreateSkillSO("passive_pickup_range",    "픽업 범위 증가",    SkillType.Passive, PassiveStatType.PickupRangePercent,   "PassiveDef_PickupRange",    PassiveFolder);
        CreateSkillSO("passive_move_speed",      "이동속도 증가",     SkillType.Passive, PassiveStatType.MoveSpeedPercent,     "PassiveDef_MoveSpeed",      PassiveFolder);
        CreateSkillSO("passive_defense",         "방어력 증가",       SkillType.Passive, PassiveStatType.DefensePercent,       "PassiveDef_Defense",        PassiveFolder);
        CreateSkillSO("passive_max_hp",          "최대 체력 증가",    SkillType.Passive, PassiveStatType.MaxHpFlat,            "PassiveDef_MaxHp",          PassiveFolder);
        CreateSkillSO("passive_skill_haste",    "스킬 가속 증가",    SkillType.Passive, PassiveStatType.SkillHastePercent, "PassiveDef_SkillHaste",  PassiveFolder);
        CreateSkillSO("passive_skill_area",     "스킬 범위 증가",  SkillType.Passive, PassiveStatType.SkillAreaPercent,      "PassiveDef_SkillArea",       PassiveFolder);
        CreateSkillSO("passive_exp_gain",        "경험치 획득량 증가",SkillType.Passive, PassiveStatType.ExpGainPercent,       "PassiveDef_ExpGain",        PassiveFolder);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SO] 패시브 스킬 템플릿 8종 생성 완료");
    }

    // ════════════════════════════════════════════
    //  메뉴: 스킬 SO 검증
    // ════════════════════════════════════════════

    [MenuItem("Tools/그날이후/SO/검증: 스킬 SO 누락/빈값 체크")]
    public static void ValidateSkills()
    {
        var guids = AssetDatabase.FindAssets("t:SkillDefinitionSO", new[] { SkillFolder });
        int errorCount = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var so   = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(path);
            if (so == null) continue;

            if (string.IsNullOrWhiteSpace(so.SkillId))
            {
                Debug.LogError($"[SO][스킬] skillId 비어있음: {path}", so);
                errorCount++;
            }

            if (string.IsNullOrWhiteSpace(so.DisplayName))
            {
                Debug.LogError($"[SO][스킬] displayName 비어있음: {path}", so);
                errorCount++;
            }

            if (so.Icon == null)
                Debug.LogWarning($"[SO][스킬] icon 미할당: {path}", so);

            if (so.SkillType == SkillType.Passive && so.PassiveStatType == PassiveStatType.None)
                Debug.LogWarning($"[SO][패시브] PassiveStatType이 None: {path}", so);
        }

        if (errorCount == 0)
            Debug.Log("[SO] 스킬 SO 검증 통과");
        else
            Debug.LogWarning($"[SO] 스킬 SO 검증 에러 {errorCount}개");
    }

    // ════════════════════════════════════════════
    //  내부 유틸
    // ════════════════════════════════════════════

    private static void CreateFolderIfMissing(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        var parent = Path.GetDirectoryName(folderPath);
        var name   = Path.GetFileName(folderPath);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            CreateFolderIfMissing(parent);

        AssetDatabase.CreateFolder(parent, name);
    }

    /// <summary>스킬 SO 생성 (액티브/패시브 공용)</summary>
    private static void CreateSkillSO(
        string          skillId,
        string          displayName,
        SkillType       skillType,
        PassiveStatType passiveStatType,
        string          assetName,
        string          folder)
    {
        var path = $"{folder}/{assetName}.asset";
        if (File.Exists(path)) return;

        var so         = ScriptableObject.CreateInstance<SkillDefinitionSO>();
        var serialized = new SerializedObject(so);

        serialized.FindProperty("skillId").stringValue            = skillId;
        serialized.FindProperty("skillType").enumValueIndex       = (int)skillType;
        serialized.FindProperty("displayName").stringValue        = displayName;
        serialized.FindProperty("maxLevel").intValue              = 8;
        serialized.FindProperty("passiveStatType").enumValueIndex = (int)passiveStatType;

        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(so, path);
    }
}
#endif