// ============================================================================
// JwagyeokYoseLevelAutoFill.cs
// 경로: Assets/_Game/Scripts/Editor/JwagyeokYoseLevelAutoFill.cs
//
// [사용법]
// Unity 메뉴 → Tools → 그날이후 → 좌격요세 레벨 자동 채우기
// CommonSkillConfigSO 에셋을 선택한 상태에서 실행하면
// 8레벨 밸런스 데이터를 자동으로 채워준다.
//
// [CommonSkillLevelParams 필드 매핑]
// damage          → 피해량
// cooldown        → 재사용 대기시간
// explosionRadius → 참격 판정 반경 (AoE 반경 개념 동일)
// lifeSeconds     → 참격 수명 (VFX 재생 시간)
// projectileCount → 1 고정 (참격 1회)
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 좌격요세(하린 기본 스킬) 레벨 밸런스 데이터를 SO에 자동 채우는 에디터 도구.
/// </summary>
public static class JwagyeokYoseLevelAutoFill
{
    // ══════════════════════════════════════════════════════
    //  밸런스 테이블 (8레벨)
    //
    //  [설계 의도]
    //  - 근접 범위 공격이라 투사체 스킬보다 기본 피해량은 낮지만
    //    다수 적을 동시에 맞추므로 총 DPS는 비슷하다.
    //  - 레벨업 시 피해량 + 쿨타임 감소 + 범위 증가 3축으로 성장.
    //  - 발시(30→65)와 비교: 좌격요세는 AoE이므로 개당 데미지가 낮다.
    //
    //  | Lv | 피해량 | 쿨타임 | 참격범위 | 비고           |
    //  |----|--------|--------|----------|----------------|
    //  | 1  | 20     | 1.20   | 2.0      | 기본           |
    //  | 2  | 24     | 1.10   | 2.0      | 피해량↑ 쿨↓   |
    //  | 3  | 28     | 1.00   | 2.3      | 범위 증가      |
    //  | 4  | 32     | 0.95   | 2.3      | 피해량↑ 쿨↓   |
    //  | 5  | 36     | 0.90   | 2.6      | 범위 증가      |
    //  | 6  | 40     | 0.85   | 2.6      | 피해량↑ 쿨↓   |
    //  | 7  | 45     | 0.80   | 3.0      | 범위 최대치    |
    //  | 8  | 50     | 0.70   | 3.0      | 최종           |
    // ══════════════════════════════════════════════════════

    private static readonly int[]   Damages   = { 20, 24, 28, 32, 36, 40, 45, 50 };
    private static readonly float[] Cooldowns = { 1.20f, 1.10f, 1.00f, 0.95f, 0.90f, 0.85f, 0.80f, 0.70f };
    private static readonly float[] Radii     = { 2.0f, 2.0f, 2.3f, 2.3f, 2.6f, 2.6f, 3.0f, 3.0f };
    private static readonly float[] Lifetimes = { 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f };

    [MenuItem("Tools/그날이후/좌격요세 레벨 자동 채우기")]
    private static void Fill()
    {
        var selected = Selection.activeObject as CommonSkillConfigSO;
        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "좌격요세 레벨 자동 채우기",
                "CommonSkillConfigSO 에셋을 Project 창에서 선택한 후 실행하세요.",
                "확인");
            return;
        }

        Undo.RecordObject(selected, "좌격요세 레벨 자동 채우기");

        // levels 배열을 8개로 설정
        selected.levels = new CommonSkillLevelParams[8];

        for (int i = 0; i < 8; i++)
        {
            selected.levels[i] = new CommonSkillLevelParams
            {
                // ── 핵심 3축 ──
                damage          = Damages[i],
                cooldown        = Cooldowns[i],
                explosionRadius = Radii[i],         // 참격 판정 반경

                // ── 보조 ──
                lifeSeconds     = Lifetimes[i],     // 참격 수명
                projectileCount = 1,                // 참격 1회 (의미상)
                projectileSpeed = 0f,               // 투사체 없음
                maxDistance      = 0f,               // 투사체 없음

                // ── 미사용 (기본값 0) ──
                spreadAngleDeg   = 0f,
                bounceCount      = 0,
                chainCount       = 0,
                splitCount       = 0,
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

        Debug.Log($"[좌격요세 AutoFill] {selected.name} SO에 8레벨 데이터 채우기 완료!\n" +
                  $"Lv1: 피해량={Damages[0]}, 쿨타임={Cooldowns[0]:F2}초, 범위={Radii[0]:F1}\n" +
                  $"Lv8: 피해량={Damages[7]}, 쿨타임={Cooldowns[7]:F2}초, 범위={Radii[7]:F1}");

        EditorUtility.DisplayDialog(
            "좌격요세 레벨 자동 채우기",
            $"{selected.name}에 8레벨 밸런스 데이터를 채웠습니다.\n\n" +
            $"Lv1: 피해량 {Damages[0]}, 쿨타임 {Cooldowns[0]:F2}초\n" +
            $"Lv8: 피해량 {Damages[7]}, 쿨타임 {Cooldowns[7]:F2}초",
            "확인");
    }
}
#endif