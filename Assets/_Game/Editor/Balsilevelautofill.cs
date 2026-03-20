// [사용법] Tools → 그날이후 → 발시 레벨 자동 채우기
// CommonSkillConfigSO 에셋(CS_Balsi)을 선택한 상태에서 실행.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 발시(윤설 기본 스킬) 레벨 밸런스 데이터를 SO에 자동 채우는 에디터 도구.
/// </summary>
public static class BalsiLevelAutoFill
{
    //  | Lv | 피해량 | 쿨타임 | 속도 | 수명 | 비고           |
    //  |----|--------|--------|------|------|----------------|
    //  | 1  | 30     | 1.00   | 14   | 2.0  | 기본           |
    //  | 2  | 35     | 0.95   | 14   | 2.0  | 피해량↑ 쿨↓   |
    //  | 3  | 40     | 0.88   | 15   | 2.0  | 속도 증가      |
    //  | 4  | 45     | 0.80   | 15   | 2.0  | 피해량↑ 쿨↓   |
    //  | 5  | 50     | 0.72   | 16   | 2.0  | 속도 증가      |
    //  | 6  | 55     | 0.65   | 16   | 2.0  | 피해량↑ 쿨↓   |
    //  | 7  | 60     | 0.58   | 17   | 2.0  | 속도 증가      |
    //  | 8  | 65     | 0.50   | 17   | 2.0  | 최종           |

    private static readonly int[]   Damages    = { 30, 35, 40, 45, 50, 55, 60, 65 };
    private static readonly float[] Cooldowns  = { 1.00f, 0.95f, 0.88f, 0.80f, 0.72f, 0.65f, 0.58f, 0.50f };
    private static readonly float[] Speeds     = { 14f, 14f, 15f, 15f, 16f, 16f, 17f, 17f };
    private static readonly float[] Lifetimes  = { 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f };

    [MenuItem("Tools/그날이후/발시 레벨 자동 채우기")]
    private static void Fill()
    {
        var selected = Selection.activeObject as CommonSkillConfigSO;
        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "발시 레벨 자동 채우기",
                "CommonSkillConfigSO 에셋을 Project 창에서 선택한 후 실행하세요.",
                "확인");
            return;
        }

        Undo.RecordObject(selected, "발시 레벨 자동 채우기");

        selected.levels = new CommonSkillLevelParams[8];

        for (int i = 0; i < 8; i++)
        {
            selected.levels[i] = new CommonSkillLevelParams
            {
                damage          = Damages[i],
                cooldown        = Cooldowns[i],
                projectileCount = 1,               // 단발 고정
                projectileSpeed = Speeds[i],
                lifeSeconds     = Lifetimes[i],
                maxDistance      = 0f,

                // 미사용 필드
                spreadAngleDeg   = 0f,
                bounceCount      = 0,
                chainCount       = 0,
                splitCount       = 0,
                explosionRadius  = 0f,
                childSpeed       = 0f,
                hitInterval      = 0f,
                orbitRadius      = 0f,
                orbitAngularSpeed = 0f,
                returnSpeed      = 0f,
                turnSpeedDeg     = 0f,
            };
        }

        EditorUtility.SetDirty(selected);
        AssetDatabase.SaveAssets();

        Debug.Log($"[발시 AutoFill] {selected.name} SO에 8레벨 데이터 채우기 완료!\n" +
                  $"Lv1: 피해량={Damages[0]}, 쿨타임={Cooldowns[0]:F2}초\n" +
                  $"Lv8: 피해량={Damages[7]}, 쿨타임={Cooldowns[7]:F2}초");

        EditorUtility.DisplayDialog(
            "발시 레벨 자동 채우기",
            $"{selected.name}에 8레벨 밸런스 데이터를 채웠습니다.\n\n" +
            $"Lv1: 피해량 {Damages[0]}, 쿨타임 {Cooldowns[0]:F2}초\n" +
            $"Lv8: 피해량 {Damages[7]}, 쿨타임 {Cooldowns[7]:F2}초",
            "확인");
    }
}
#endif