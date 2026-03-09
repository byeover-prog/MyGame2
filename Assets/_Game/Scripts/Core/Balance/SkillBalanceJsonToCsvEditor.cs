#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// [구현 원리 요약]
// - Unity AssetDatabase 기준으로 JSON(TextAsset)을 찾는다(IO 경로 문제 방지).
// - SkillBalanceDB2D의 "실제 존재 필드"만 CSV로 뽑는다(없는 필드 접근 금지).
// - 빈 칸(미지정)은 공백으로 출력해서 엑셀 편집이 편하게 한다.
public static class SkillBalanceJsonToCsvEditor
{
    private const string DefaultJsonPath = "Assets/_Game/Balance/skill_balance.json";
    private const string DefaultCsvPath  = "Assets/_Game/Balance/skill_balance.csv";

    [MenuItem("Tools/그날이후/밸런스/JSON → CSV 생성(엑셀용)")]
    public static void GenerateCsvFromJson()
    {
        // 1) 고정 경로에서 먼저 로드
        TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultJsonPath);

        // 2) 없으면 프로젝트 전체에서 이름으로 검색
        if (jsonAsset == null)
        {
            string[] guids = AssetDatabase.FindAssets("skill_balance t:TextAsset");
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                    p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                    p.EndsWith(".json.txt", StringComparison.OrdinalIgnoreCase))
                {
                    var cand = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
                    if (cand != null)
                    {
                        jsonAsset = cand;
                        break;
                    }
                }
            }
        }

        // 3) 그래도 없으면 파일 선택창
        if (jsonAsset == null)
        {
            string abs = EditorUtility.OpenFilePanel("skill_balance.json 선택", Application.dataPath, "json");
            if (string.IsNullOrEmpty(abs))
            {
                EditorUtility.DisplayDialog("JSON 없음",
                    $"JSON 파일을 찾지 못했습니다.\n\n기본 경로:\n{DefaultJsonPath}\n\n또는 파일 선택으로 지정해 주세요.",
                    "확인");
                return;
            }

            abs = abs.Replace("\\", "/");
            string dataPath = Application.dataPath.Replace("\\", "/");
            if (!abs.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("경로 오류",
                    "프로젝트 Assets 폴더 안의 JSON만 선택할 수 있습니다.",
                    "확인");
                return;
            }

            string rel = "Assets" + abs.Substring(dataPath.Length);
            jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(rel);
        }

        if (jsonAsset == null)
        {
            EditorUtility.DisplayDialog("JSON 없음",
                $"Unity가 인식한 TextAsset JSON을 찾지 못했습니다.\n\n기본 경로:\n{DefaultJsonPath}",
                "확인");
            return;
        }

        // JSON 파싱
        var db = JsonUtility.FromJson<SkillBalanceDB2D>(jsonAsset.text);
        if (db == null || db.skills == null)
        {
            EditorUtility.DisplayDialog("JSON 파싱 실패",
                "skill_balance.json 형식이 SkillBalanceDB2D와 맞지 않습니다.\n(중괄호/콤마/따옴표 확인)",
                "확인");
            return;
        }

        // CSV 생성(확장 스펙 헤더)
        // ※ splitAngle 같은 '존재하지 않는 필드'는 제거했다.
        var sb = new StringBuilder(8192);
        sb.AppendLine(
            "id," +
            "damage,damageAddPerLevel," +
            "cooldown,cooldownAddPerLevel," +
            "speed,speedAddPerLevel," +
            "life,lifeAddPerLevel," +
            "count,countAddPerLevel," +
            "" +
            "hitInterval,hitIntervalAddPerLevel," +
            "orbitRadius,orbitRadiusAddPerLevel," +
            "orbitSpeed,orbitSpeedAddPerLevel," +
            "active,activeAddPerLevel," +
            "burstInterval,burstIntervalAddPerLevel," +
            "spinDps,spinDpsAddPerLevel," +
            "bounceCount,bounceAddPerLevel," +
            "chainCount,chainAddPerLevel," +
            "splitCount,splitAddPerLevel," +
            "explosionRadius,explosionRadiusAddPerLevel," +
            "explodeDistance,explodeDistanceAddPerLevel," +
            "childSpeed,childSpeedAddPerLevel," +
            "slowRate,slowRateAddPerLevel," +
            "slowSeconds,slowSecondsAddPerLevel"
        );

        foreach (var r in db.skills)
        {
            if (r == null || string.IsNullOrEmpty(r.id)) continue;

            sb.Append(r.id).Append(',');

            AppendInt(sb, r.damage).Append(',');
            AppendIntRaw(sb, r.damageAddPerLevel).Append(',');

            AppendFloat(sb, r.cooldown).Append(',');
            AppendFloatRaw(sb, r.cooldownAddPerLevel).Append(',');

            AppendFloat(sb, r.speed).Append(',');
            AppendFloatRaw(sb, r.speedAddPerLevel).Append(',');

            AppendFloat(sb, r.life).Append(',');
            AppendFloatRaw(sb, r.lifeAddPerLevel).Append(',');

            AppendInt(sb, r.count).Append(',');
            AppendIntRaw(sb, r.countAddPerLevel).Append(',');

            AppendFloat(sb, r.hitInterval).Append(',');
            AppendFloatRaw(sb, r.hitIntervalAddPerLevel).Append(',');

            AppendFloat(sb, r.orbitRadius).Append(',');
            AppendFloatRaw(sb, r.orbitRadiusAddPerLevel).Append(',');

            AppendFloat(sb, r.orbitSpeed).Append(',');
            AppendFloatRaw(sb, r.orbitSpeedAddPerLevel).Append(',');

            AppendFloat(sb, r.active).Append(',');
            AppendFloatRaw(sb, r.activeAddPerLevel).Append(',');

            AppendFloat(sb, r.burstInterval).Append(',');
            AppendFloatRaw(sb, r.burstIntervalAddPerLevel).Append(',');

            AppendFloat(sb, r.spinDps).Append(',');
            AppendFloatRaw(sb, r.spinDpsAddPerLevel).Append(',');

            AppendInt(sb, r.bounceCount).Append(',');
            AppendIntRaw(sb, r.bounceAddPerLevel).Append(',');

            AppendInt(sb, r.chainCount).Append(',');
            AppendIntRaw(sb, r.chainAddPerLevel).Append(',');

            AppendInt(sb, r.splitCount).Append(',');
            AppendIntRaw(sb, r.splitAddPerLevel).Append(',');

            AppendFloat(sb, r.explosionRadius).Append(',');
            AppendFloatRaw(sb, r.explosionRadiusAddPerLevel).Append(',');

            AppendFloat(sb, r.explodeDistance).Append(',');
            AppendFloatRaw(sb, r.explodeDistanceAddPerLevel).Append(',');

            AppendFloat(sb, r.childSpeed).Append(',');
            AppendFloatRaw(sb, r.childSpeedAddPerLevel).Append(',');

            AppendFloat(sb, r.slowRate).Append(',');
            AppendFloatRaw(sb, r.slowRateAddPerLevel).Append(',');

            AppendFloat(sb, r.slowSeconds).Append(',');
            AppendFloatRaw(sb, r.slowSecondsAddPerLevel);

            sb.AppendLine();
        }

        File.WriteAllText(DefaultCsvPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        Debug.Log($"[Balance] JSON → CSV 생성 완료\nJSON(에셋): {AssetDatabase.GetAssetPath(jsonAsset)}\nCSV: {DefaultCsvPath}");
        EditorUtility.DisplayDialog("완료", $"CSV 생성 완료:\n{DefaultCsvPath}", "확인");
    }

    // ===== 출력 규칙 =====
    // - "기본값 필드"는 -1일 때 공백(미지정)
    private static StringBuilder AppendInt(StringBuilder sb, int v)
    {
        if (v >= 0) sb.Append(v);
        return sb;
    }
    private static StringBuilder AppendFloat(StringBuilder sb, float v)
    {
        if (v >= 0f) sb.Append(v.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return sb;
    }

    // - AddPerLevel 계열은 0이 "의미 있음(증가 없음)"이라서, 0도 출력(명시)
    private static StringBuilder AppendIntRaw(StringBuilder sb, int v)
    {
        sb.Append(v);
        return sb;
    }
    private static StringBuilder AppendFloatRaw(StringBuilder sb, float v)
    {
        sb.Append(v.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return sb;
    }
}
#endif