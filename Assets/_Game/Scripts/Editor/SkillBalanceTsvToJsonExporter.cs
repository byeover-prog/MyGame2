// UTF-8
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SkillBalanceTsvToJsonExporter
{
    [Serializable]
    private sealed class DB
    {
        public int version = 1;
        public SkillRow[] skills;
    }

    [Serializable]
    private sealed class SkillRow
    {
        public string id;

        public int damage;
        public int damageAddPerLevel;

        public float cooldown;
        public float cooldownAddPerLevel;

        public float speed;
        public float speedAddPerLevel;

        public float life;
        public float lifeAddPerLevel;

        public int count;
        public int countAddPerLevel;

        public float hitInterval;
        public float hitIntervalAddPerLevel;

        public float orbitRadius;
        public float orbitRadiusAddPerLevel;

        public float orbitSpeed;
        public float orbitSpeedAddPerLevel;

        public float active;
        public float activeAddPerLevel;

        public float burstInterval;
        public float burstIntervalAddPerLevel;

        public float spinDps;
        public float spinDpsAddPerLevel;

        public int bounceCount;
        public int bounceAddPerLevel;

        public int chainCount;
        public int chainAddPerLevel;

        public int splitCount;
        public int splitAddPerLevel;

        public float explosionRadius;
        public float explosionRadiusAddPerLevel;

        public float explodeDistance;
        public float explodeDistanceAddPerLevel;

        public float childSpeed;
        public float childSpeedAddPerLevel;

        public float slowRate;
        public float slowRateAddPerLevel;

        public float slowSeconds;
        public float slowSecondsAddPerLevel;
    }

    [MenuItem("Tools/그날이후/Balance/TSV -> SkillBalance JSON 변환")]
    private static void Export()
    {
        var tsvPath = EditorUtility.OpenFilePanel("TSV 선택", Application.dataPath, "txt,tsv");
        if (string.IsNullOrEmpty(tsvPath)) return;

        string outPath = EditorUtility.SaveFilePanel("JSON 저장 위치", Application.dataPath, "skill_balance", "json");
        if (string.IsNullOrEmpty(outPath)) return;

        string raw = File.ReadAllText(tsvPath, Encoding.UTF8);
        raw = Normalize(raw);

        // 줄 분리
        var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            EditorUtility.DisplayDialog("실패", "TSV에 헤더/데이터가 없습니다.", "OK");
            return;
        }

        // 헤더(탭)
        var header = lines[0].Split('\t');
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < header.Length; i++)
        {
            var key = header[i].Trim();
            if (!string.IsNullOrEmpty(key) && !index.ContainsKey(key))
                index.Add(key, i);
        }

        // 데이터 파싱
        var rows = new List<SkillRow>(Mathf.Max(16, lines.Length - 1));
        for (int li = 1; li < lines.Length; li++)
        {
            var cols = lines[li].Split('\t');
            if (cols.Length == 0) continue;

            string id = Get(cols, index, "id");
            if (string.IsNullOrWhiteSpace(id)) continue;

            var r = new SkillRow();
            r.id = id;

            // 숫자 필드들 (없는 컬럼이면 0 -> 의도와 다르면 TSV 쪽에서 -1로 채워두는 게 안전)
            r.damage = GetInt(cols, index, "damage");
            r.damageAddPerLevel = GetInt(cols, index, "damageAddPerLevel");

            r.cooldown = GetFloat(cols, index, "cooldown");
            r.cooldownAddPerLevel = GetFloat(cols, index, "cooldownAddPerLevel");

            r.speed = GetFloat(cols, index, "speed");
            r.speedAddPerLevel = GetFloat(cols, index, "speedAddPerLevel");

            r.life = GetFloat(cols, index, "life");
            r.lifeAddPerLevel = GetFloat(cols, index, "lifeAddPerLevel");

            r.count = GetInt(cols, index, "count");
            r.countAddPerLevel = GetInt(cols, index, "countAddPerLevel");

            r.hitInterval = GetFloat(cols, index, "hitInterval");
            r.hitIntervalAddPerLevel = GetFloat(cols, index, "hitIntervalAddPerLevel");

            r.orbitRadius = GetFloat(cols, index, "orbitRadius");
            r.orbitRadiusAddPerLevel = GetFloat(cols, index, "orbitRadiusAddPerLevel");

            r.orbitSpeed = GetFloat(cols, index, "orbitSpeed");
            r.orbitSpeedAddPerLevel = GetFloat(cols, index, "orbitSpeedAddPerLevel");

            r.active = GetFloat(cols, index, "active");
            r.activeAddPerLevel = GetFloat(cols, index, "activeAddPerLevel");

            r.burstInterval = GetFloat(cols, index, "burstInterval");
            r.burstIntervalAddPerLevel = GetFloat(cols, index, "burstIntervalAddPerLevel");

            r.spinDps = GetFloat(cols, index, "spinDps");
            r.spinDpsAddPerLevel = GetFloat(cols, index, "spinDpsAddPerLevel");

            r.bounceCount = GetInt(cols, index, "bounceCount");
            r.bounceAddPerLevel = GetInt(cols, index, "bounceAddPerLevel");

            r.chainCount = GetInt(cols, index, "chainCount");
            r.chainAddPerLevel = GetInt(cols, index, "chainAddPerLevel");

            r.splitCount = GetInt(cols, index, "splitCount");
            r.splitAddPerLevel = GetInt(cols, index, "splitAddPerLevel");

            r.explosionRadius = GetFloat(cols, index, "explosionRadius");
            r.explosionRadiusAddPerLevel = GetFloat(cols, index, "explosionRadiusAddPerLevel");

            r.explodeDistance = GetFloat(cols, index, "explodeDistance");
            r.explodeDistanceAddPerLevel = GetFloat(cols, index, "explodeDistanceAddPerLevel");

            r.childSpeed = GetFloat(cols, index, "childSpeed");
            r.childSpeedAddPerLevel = GetFloat(cols, index, "childSpeedAddPerLevel");

            r.slowRate = GetFloat(cols, index, "slowRate");
            r.slowRateAddPerLevel = GetFloat(cols, index, "slowRateAddPerLevel");

            r.slowSeconds = GetFloat(cols, index, "slowSeconds");
            r.slowSecondsAddPerLevel = GetFloat(cols, index, "slowSecondsAddPerLevel");

            rows.Add(r);
        }

        var db = new DB { version = 1, skills = rows.ToArray() };
        string json = JsonUtility.ToJson(db, true);

        // UTF-8 (BOM 없이)
        File.WriteAllText(outPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료", $"변환 완료!\n{outPath}\n\n이제 JsonManager2D의 Default Skill Balance Json에 이 JSON을 넣으세요.", "OK");
    }

    private static string Normalize(string raw)
    {
        if (raw == null) return null;
        raw = raw.Trim();
        if (raw.Length > 0 && raw[0] == '\ufeff')
            raw = raw.Substring(1);
        return raw;
    }

    private static string Get(string[] cols, Dictionary<string, int> index, string key)
    {
        if (!index.TryGetValue(key, out int i)) return "";
        if (i < 0 || i >= cols.Length) return "";
        return cols[i].Trim();
    }

    private static int GetInt(string[] cols, Dictionary<string, int> index, string key)
    {
        string s = Get(cols, index, key);
        if (string.IsNullOrEmpty(s)) return 0;
        if (int.TryParse(s, out int v)) return v;
        return 0;
    }

    private static float GetFloat(string[] cols, Dictionary<string, int> index, string key)
    {
        string s = Get(cols, index, key);
        if (string.IsNullOrEmpty(s)) return 0f;
        if (float.TryParse(s, out float v)) return v;
        return 0f;
    }
}