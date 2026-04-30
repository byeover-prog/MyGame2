#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class HonryeomCharacterSkillTools
{
    private const string ExportPath = "Assets/_Game/Balance/character_skill_balance.generated.tsv";
    private static readonly string[] Headers =
    {
        "skillId","displayName","ownerCharacterId","level","damage","cooldown","count","speed","lifetime","radius","duration","interval","delay","spreadAngle","extraCount",
        "custom_hitRadius","custom_explosionRadius","custom_frostDuration","custom_frostSlowMultiplier","custom_attachDelay","description","addInfo"
    };

    [MenuItem("Tools/혼령검/Skill/캐릭터 전용 스킬 TSV 내보내기")]
    public static void ExportTsv()
    {
        var guids = AssetDatabase.FindAssets("t:CharacterSkillDefinitionSO");
        var sb = new StringBuilder();
        sb.AppendLine(string.Join("\t", Headers));
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
            string skillId = GetStr(so, "skillId");
            string displayName = GetStr(so, "displayName");
            string owner = GetStr(so, "ownerCharacterId");
            string desc = GetStr(so, "description");
            string addInfo = GetStr(so, "addInfo");
            var levels = so.FindProperty("levels") ?? so.FindProperty("levelBalances");
            if (levels == null || !levels.isArray)
            {
                sb.AppendLine(Row(skillId, displayName, owner, "1", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", desc, addInfo));
                continue;
            }

            for (int i = 0; i < levels.arraySize; i++)
            {
                var lv = levels.GetArrayElementAtIndex(i);
                sb.AppendLine(Row(skillId, displayName, owner, (i + 1).ToString(),
                    GetNum(lv, "damage"), GetNum(lv, "cooldown"), GetNum(lv, "count"), GetNum(lv, "speed"), GetNum(lv, "lifetime"), GetNum(lv, "radius"),
                    GetNum(lv, "duration"), GetNum(lv, "interval"), GetNum(lv, "delay"), GetNum(lv, "spreadAngle"), GetNum(lv, "extraCount"),
                    GetCustom(lv, "hitRadius"), GetCustom(lv, "explosionRadius"), GetCustom(lv, "frostDuration"), GetCustom(lv, "frostSlowMultiplier"), GetCustom(lv, "attachDelay"), desc, addInfo));
            }
        }
        File.WriteAllText(ExportPath, sb.ToString(), new UTF8Encoding(true));
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("혼령검", $"TSV 내보내기 완료\n{ExportPath}", "확인");
    }

    [MenuItem("Tools/혼령검/Skill/캐릭터 전용 스킬 TSV 가져오기")]
    public static void ImportTsv()
    {
        if (!File.Exists(ExportPath))
        {
            EditorUtility.DisplayDialog("혼령검", $"TSV 파일이 없습니다.\n{ExportPath}", "확인");
            return;
        }
        var map = new Dictionary<string, ScriptableObject>(StringComparer.OrdinalIgnoreCase);
        foreach (string guid in AssetDatabase.FindAssets("t:CharacterSkillDefinitionSO"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            var so = new SerializedObject(asset);
            string id = GetStr(so, "skillId");
            if (!string.IsNullOrWhiteSpace(id)) map[id] = asset;
        }

        var lines = File.ReadAllLines(ExportPath);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] cols = lines[i].Split('\t');
            if (cols.Length < 4) continue;
            string skillId = cols[0].Trim();
            if (!map.TryGetValue(skillId, out var asset)) continue; // 없는 skillId 자동 생성은 현재 미지원
            var so = new SerializedObject(asset);
            SetStr(so, "description", Col(cols, 20));
            SetStr(so, "addInfo", Col(cols, 21));
            int level = Mathf.Max(1, ParseInt(Col(cols, 3), 1));
            var levels = so.FindProperty("levels") ?? so.FindProperty("levelBalances");
            if (levels != null && levels.isArray)
            {
                while (levels.arraySize < level) levels.InsertArrayElementAtIndex(levels.arraySize);
                var lv = levels.GetArrayElementAtIndex(level - 1);
                SetNum(lv, "damage", Col(cols, 4)); SetNum(lv, "cooldown", Col(cols, 5)); SetNum(lv, "count", Col(cols, 6)); SetNum(lv, "speed", Col(cols, 7));
                SetNum(lv, "lifetime", Col(cols, 8)); SetNum(lv, "radius", Col(cols, 9)); SetNum(lv, "duration", Col(cols, 10)); SetNum(lv, "interval", Col(cols, 11));
                SetNum(lv, "delay", Col(cols, 12)); SetNum(lv, "spreadAngle", Col(cols, 13)); SetNum(lv, "extraCount", Col(cols, 14));
                SetCustom(lv, "hitRadius", Col(cols, 15)); SetCustom(lv, "explosionRadius", Col(cols, 16)); SetCustom(lv, "frostDuration", Col(cols, 17));
                SetCustom(lv, "frostSlowMultiplier", Col(cols, 18)); SetCustom(lv, "attachDelay", Col(cols, 19));
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("혼령검", "TSV 가져오기 완료\n(없는 skillId 자동 생성은 이번 작업에서 미지원)", "확인");
    }

    static string Col(string[] cols, int idx) => idx >= 0 && idx < cols.Length ? cols[idx] : "";
    static string Row(params string[] cols) => string.Join("\t", cols.Select(Escape));
    static string Escape(string s) => (s ?? string.Empty).Replace("\t", " ").Replace("\r", " ").Replace("\n", "\\n");
    static string GetStr(SerializedObject so, string key) => so.FindProperty(key)?.stringValue ?? string.Empty;
    static void SetStr(SerializedObject so, string key, string v) { var p = so.FindProperty(key); if (p != null) p.stringValue = v; }
    static string GetNum(SerializedProperty p, string key) { var n = p.FindPropertyRelative(key); return n == null ? "" : n.propertyType == SerializedPropertyType.Integer ? n.intValue.ToString() : n.floatValue.ToString("0.###"); }
    static void SetNum(SerializedProperty p, string key, string raw) { if (!TryParseNum(raw, out var f, out var isInt)) return; var n = p.FindPropertyRelative(key); if (n == null) return; if (n.propertyType == SerializedPropertyType.Integer || isInt) n.intValue = Mathf.RoundToInt(f); else n.floatValue = f; }
    static string GetCustom(SerializedProperty level, string name) { var arr = level.FindPropertyRelative("customValues"); if (arr == null || !arr.isArray) return ""; for (int i=0;i<arr.arraySize;i++){var e=arr.GetArrayElementAtIndex(i); if ((e.FindPropertyRelative("key")?.stringValue ?? "") == name) return GetNum(e, "value");} return ""; }
    static void SetCustom(SerializedProperty level, string key, string raw) { if (!TryParseNum(raw, out var f, out _)) return; var arr = level.FindPropertyRelative("customValues"); if (arr == null || !arr.isArray) return; for (int i=0;i<arr.arraySize;i++){var e=arr.GetArrayElementAtIndex(i); if ((e.FindPropertyRelative("key")?.stringValue ?? "") == key){var v=e.FindPropertyRelative("value"); if (v!=null) v.floatValue=f; return;}} int idx=arr.arraySize; arr.InsertArrayElementAtIndex(idx); var ne=arr.GetArrayElementAtIndex(idx); var k=ne.FindPropertyRelative("key"); var v2=ne.FindPropertyRelative("value"); if(k!=null)k.stringValue=key; if(v2!=null)v2.floatValue=f; }
    static bool TryParseNum(string raw, out float f, out bool isInt) { f = 0; isInt = false; raw = (raw ?? "").Trim(); if (string.IsNullOrEmpty(raw)) return false; if (raw == "-1") return false; if (int.TryParse(raw, out var i)) { f = i; isInt = true; return true; } return float.TryParse(raw, out f); }
    static int ParseInt(string raw, int d) => int.TryParse((raw ?? "").Trim(), out var v) ? v : d;
}
#endif
