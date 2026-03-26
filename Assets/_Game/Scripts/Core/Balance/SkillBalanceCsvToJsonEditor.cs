#if UNITY_EDITOR
// UTF-8
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace _Game.Scripts.Core.Balance
{
    // [구현 원리 요약]
    // - 엑셀 CSV(UTF-8)를 읽어 SkillBalanceDB2D(JSON)를 생성한다.
    // - 빈 칸은 -1(미지정)로 저장한다.
    // - 메뉴 1번으로 Assets/_Game/Balance/skill_balance.json을 갱신한다.
    public static class SkillBalanceCsvToJsonEditor
    {
        private const string DefaultCsvPath = "Assets/_Game/Balance/skill_balance.csv";
        private const string DefaultJsonPath = "Assets/_Game/Balance/skill_balance.json";

        [MenuItem("Tools/그날이후/밸런스/CSV → JSON 생성")]
        public static void GenerateJsonFromCsv()
        {
            string csvPath = DefaultCsvPath;
            if (!File.Exists(csvPath))
            {
                EditorUtility.DisplayDialog(
                    "밸런스 CSV 없음",
                    $"CSV 파일을 찾을 수 없습니다.\n\n경로:\n{csvPath}\n\n엑셀에서 CSV UTF-8로 저장 후 위 경로에 넣어주세요.",
                    "확인"
                );
                return;
            }

            string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
            if (lines.Length < 2)
            {
                EditorUtility.DisplayDialog("CSV 형식 오류", "헤더 1줄 + 데이터 1줄 이상이 필요합니다.", "확인");
                return;
            }

            string[] headers = SplitCsvLine(lines[0]);

            int idx_id = Find(headers, "id");

            int idx_damage = Find(headers, "damage");
            int idx_damageAdd = Find(headers, "damageAddPerLevel");

            int idx_cooldown = Find(headers, "cooldown");
            int idx_cooldownAdd = Find(headers, "cooldownAddPerLevel");

            int idx_speed = Find(headers, "speed");
            int idx_speedAdd = Find(headers, "speedAddPerLevel");

            int idx_life = Find(headers, "life");
            int idx_lifeAdd = Find(headers, "lifeAddPerLevel");

            int idx_count = Find(headers, "count");
            int idx_countAdd = Find(headers, "countAddPerLevel");

            int idx_hitInterval = Find(headers, "hitInterval");
            int idx_hitIntervalAdd = Find(headers, "hitIntervalAddPerLevel");

            int idx_orbitRadius = Find(headers, "orbitRadius");
            int idx_orbitRadiusAdd = Find(headers, "orbitRadiusAddPerLevel");

            int idx_orbitSpeed = Find(headers, "orbitSpeed");
            int idx_orbitSpeedAdd = Find(headers, "orbitSpeedAddPerLevel");

            int idx_active = Find(headers, "active");
            int idx_activeAdd = Find(headers, "activeAddPerLevel");

            int idx_burstInterval = Find(headers, "burstInterval");
            int idx_burstIntervalAdd = Find(headers, "burstIntervalAddPerLevel");

            int idx_spinDps = Find(headers, "spinDps");
            int idx_spinDpsAdd = Find(headers, "spinDpsAddPerLevel");

            int idx_bounceCount = Find(headers, "bounceCount");
            int idx_bounceAdd = Find(headers, "bounceAddPerLevel");

            int idx_chainCount = Find(headers, "chainCount");
            int idx_chainAdd = Find(headers, "chainAddPerLevel");

            int idx_splitCount = Find(headers, "splitCount");
            int idx_splitAdd = Find(headers, "splitAddPerLevel");

            int idx_explosionRadius = Find(headers, "explosionRadius");
            int idx_explosionRadiusAdd = Find(headers, "explosionRadiusAddPerLevel");

            int idx_explodeDistance = Find(headers, "explodeDistance");
            int idx_explodeDistanceAdd = Find(headers, "explodeDistanceAddPerLevel");

            int idx_childSpeed = Find(headers, "childSpeed");
            int idx_childSpeedAdd = Find(headers, "childSpeedAddPerLevel");

            int idx_slowRate = Find(headers, "slowRate");
            int idx_slowRateAdd = Find(headers, "slowRateAddPerLevel");

            int idx_slowSeconds = Find(headers, "slowSeconds");
            int idx_slowSecondsAdd = Find(headers, "slowSecondsAddPerLevel");

            if (idx_id < 0)
            {
                EditorUtility.DisplayDialog("CSV 형식 오류", "헤더에 id 컬럼이 반드시 필요합니다.", "확인");
                return;
            }

            var db = new SkillBalanceDB2D { version = 1 };

            // ✅ 네 프로젝트 스키마에 맞춤: SkillBalanceDB2D.SkillRow2D
            var list = new System.Collections.Generic.List<SkillBalanceDB2D.SkillRow2D>(lines.Length - 1);

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] cells = SplitCsvLine(line);
                string id = GetCell(cells, idx_id).Trim();
                if (string.IsNullOrWhiteSpace(id)) continue;

                var r = new SkillBalanceDB2D.SkillRow2D();
                r.id = id;

                r.damage = ReadInt(cells, idx_damage, -1);
                r.damageAddPerLevel = ReadInt(cells, idx_damageAdd, 0);

                r.cooldown = ReadFloat(cells, idx_cooldown, -1f);
                r.cooldownAddPerLevel = ReadFloat(cells, idx_cooldownAdd, 0f);

                r.speed = ReadFloat(cells, idx_speed, -1f);
                r.speedAddPerLevel = ReadFloat(cells, idx_speedAdd, 0f);

                r.life = ReadFloat(cells, idx_life, -1f);
                r.lifeAddPerLevel = ReadFloat(cells, idx_lifeAdd, 0f);

                r.count = ReadInt(cells, idx_count, -1);
                r.countAddPerLevel = ReadInt(cells, idx_countAdd, 0);

                r.hitInterval = ReadFloat(cells, idx_hitInterval, -1f);
                r.hitIntervalAddPerLevel = ReadFloat(cells, idx_hitIntervalAdd, 0f);

                r.orbitRadius = ReadFloat(cells, idx_orbitRadius, -1f);
                r.orbitRadiusAddPerLevel = ReadFloat(cells, idx_orbitRadiusAdd, 0f);

                r.orbitSpeed = ReadFloat(cells, idx_orbitSpeed, -1f);
                r.orbitSpeedAddPerLevel = ReadFloat(cells, idx_orbitSpeedAdd, 0f);

                r.active = ReadFloat(cells, idx_active, -1f);
                r.activeAddPerLevel = ReadFloat(cells, idx_activeAdd, 0f);

                r.burstInterval = ReadFloat(cells, idx_burstInterval, -1f);
                r.burstIntervalAddPerLevel = ReadFloat(cells, idx_burstIntervalAdd, 0f);

                r.spinDps = ReadFloat(cells, idx_spinDps, -1f);
                r.spinDpsAddPerLevel = ReadFloat(cells, idx_spinDpsAdd, 0f);

                r.bounceCount = ReadInt(cells, idx_bounceCount, -1);
                r.bounceAddPerLevel = ReadInt(cells, idx_bounceAdd, 0);

                r.chainCount = ReadInt(cells, idx_chainCount, -1);
                r.chainAddPerLevel = ReadInt(cells, idx_chainAdd, 0);

                r.splitCount = ReadInt(cells, idx_splitCount, -1);
                r.splitAddPerLevel = ReadInt(cells, idx_splitAdd, 0);

                r.explosionRadius = ReadFloat(cells, idx_explosionRadius, -1f);
                r.explosionRadiusAddPerLevel = ReadFloat(cells, idx_explosionRadiusAdd, 0f);

                r.explodeDistance = ReadFloat(cells, idx_explodeDistance, -1f);
                r.explodeDistanceAddPerLevel = ReadFloat(cells, idx_explodeDistanceAdd, 0f);

                r.childSpeed = ReadFloat(cells, idx_childSpeed, -1f);
                r.childSpeedAddPerLevel = ReadFloat(cells, idx_childSpeedAdd, 0f);

                r.slowRate = ReadFloat(cells, idx_slowRate, -1f);
                r.slowRateAddPerLevel = ReadFloat(cells, idx_slowRateAdd, 0f);

                r.slowSeconds = ReadFloat(cells, idx_slowSeconds, -1f);
                r.slowSecondsAddPerLevel = ReadFloat(cells, idx_slowSecondsAdd, 0f);

                list.Add(r);
            }

            db.skills = list.ToArray();

            string json = JsonUtility.ToJson(db, true);
            File.WriteAllText(DefaultJsonPath, json, Encoding.UTF8);

            AssetDatabase.Refresh();
            GameLogger.Log($"[Balance] CSV → JSON 생성 완료\nCSV: {DefaultCsvPath}\nJSON: {DefaultJsonPath}");
            EditorUtility.DisplayDialog("완료", $"JSON 생성 완료:\n{DefaultJsonPath}", "확인");
        }

        private static int Find(string[] headers, string name)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                if (string.Equals(headers[i]?.Trim(), name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static string[] SplitCsvLine(string line)
        {
            var list = new System.Collections.Generic.List<string>(32);
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') { inQuotes = !inQuotes; continue; }

                if (c == ',' && !inQuotes)
                {
                    list.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                sb.Append(c);
            }
            list.Add(sb.ToString());
            return list.ToArray();
        }

        private static string GetCell(string[] cells, int idx)
        {
            if (idx < 0) return "";
            if (idx >= cells.Length) return "";
            return cells[idx] ?? "";
        }

        private static int ReadInt(string[] cells, int idx, int fallback)
        {
            string s = GetCell(cells, idx).Trim();
            if (string.IsNullOrEmpty(s)) return fallback;

            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                return v;

            s = s.Replace(",", "");
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                return v;

            return fallback;
        }

        private static float ReadFloat(string[] cells, int idx, float fallback)
        {
            string s = GetCell(cells, idx).Trim();
            if (string.IsNullOrEmpty(s)) return fallback;

            string norm = s.Replace(" ", "").Replace(",", ".");
            if (float.TryParse(norm, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                return v;

            return fallback;
        }
    }
}
#endif