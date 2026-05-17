#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class GameSceneValidator
{
    private const string MenuPath = "Tools/Honryeom/Validation/Game Scene Validator";
    private const string SceneGamePath = "Assets/Scenes/Scene_Game.unity";

    [MenuItem(MenuPath)]
    public static void RunFromMenu()
    {
        ValidationReport report = Run();
        Debug.Log(report.ToConsoleText());

        if (report.ErrorCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Game Scene Validator",
                $"Failed: {report.ErrorCount} error(s), {report.WarningCount} warning(s).\nSee Console for details.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Game Scene Validator",
                $"Passed: {report.WarningCount} warning(s).",
                "OK");
        }
    }

    public static ValidationReport Run()
    {
        ValidationReport report = new ValidationReport("Game Scene Validator");
        string text = ReadAssetText(SceneGamePath);

        if (string.IsNullOrEmpty(text))
        {
            report.AddError("GSV001", SceneGamePath, 0, "Scene_Game.unity could not be read.");
            return report;
        }

        SceneSnapshot scene = SceneSnapshot.Parse(text);

        ValidateSingleActiveComponent(report, scene, "GameManager2D", "GSV010", "Scene_Game needs exactly one active GameManager2D.");
        ValidateGameSceneContextPolicy(report, scene);
        ValidateBootstrapperPolicy(report, scene);
        ValidateSingleActiveComponent(report, scene, "StageManager2D", "GSV011", "Scene_Game needs exactly one active StageManager2D.");
        ValidateSingleActiveComponent(report, scene, "EnemySpawner2D", "GSV012", "Scene_Game needs exactly one active EnemySpawner2D as the official runtime spawner.");
        ValidateNoActiveComponent(
            report,
            scene,
            "EnemySpawnerTimeline2D",
            "GSV013",
            "EnemySpawnerTimeline2D is a legacy/direct spawner in Scene_Game. Keep it inactive so StageManager2D and EnemySpawner2D own stage spawning.");

        string commonSkillManagerId = ValidateSingleActiveComponent(
            report,
            scene,
            "CommonSkillManager2D",
            "GSV020",
            "Common skill runtime state must have one active owner. Duplicate managers split skill levels, HUD events, and reward application.");

        ValidateSerializedReferencesPointToOwner(
            report,
            text,
            "commonSkillManager:",
            commonSkillManagerId,
            "GSV021",
            "Serialized commonSkillManager references should point to the active CommonSkillManager2D owner.");

        ValidatePrefabOverrideReferencesPointToOwner(
            report,
            text,
            "manager",
            commonSkillManagerId,
            "GSV022",
            "Prefab override manager references for starting common skills should point to the active CommonSkillManager2D owner.");

        ValidateNoReferencesToInactiveComponent(
            report,
            scene,
            text,
            "CommonSkillManager2D",
            "GSV023",
            "Scene_Game still references an inactive CommonSkillManager2D. This can make start skills log as applied without actually spawning a weapon.");

        ValidateMaxOneActiveComponent(
            report,
            scene,
            "TrialSpawnRateScaler",
            "GSV030",
            "Only one active TrialSpawnRateScaler may control the spawn multiplier. Multiple scalers can overwrite each other every frame.");

        ValidateNoActiveNamedComponent(
            report,
            scene,
            "EnemyHealth2D",
            "EnemyHealth2D",
            "GSV040",
            "A standalone active EnemyHealth2D object is present in Scene_Game. Enemies should come from the spawner/pool, not a scene test object.");

        ValidateNoActiveComponent(
            report,
            scene,
            "PlayerStatRuntimeApplier2D",
            "GSV070",
            "Scene_Game should not have a standalone PlayerStatRuntimeApplier2D. The Player prefab owns runtime stat application.");

        ValidateStageCatalogPolicy(report, text);
        ValidateGameplayCollisionPolicy(report);

        report.Sort();
        return report;
    }

    private static string ValidateSingleActiveComponent(
        ValidationReport report,
        SceneSnapshot scene,
        string componentName,
        string ruleId,
        string message)
    {
        List<SceneGameObject> owners = scene.FindActiveObjectsWithComponent(componentName);
        if (owners.Count == 1)
            return owners[0].ComponentIdByName[componentName];

        report.AddError(ruleId, SceneGamePath, owners.Count > 0 ? owners[0].Line : 0, $"{message} Active count: {owners.Count}.");
        return null;
    }

    private static void ValidateMaxOneActiveComponent(
        ValidationReport report,
        SceneSnapshot scene,
        string componentName,
        string ruleId,
        string message)
    {
        List<SceneGameObject> owners = scene.FindActiveObjectsWithComponent(componentName);
        if (owners.Count <= 1) return;

        report.AddError(ruleId, SceneGamePath, owners[1].Line, $"{message} Active count: {owners.Count}.");
    }

    private static void ValidateGameSceneContextPolicy(ValidationReport report, SceneSnapshot scene)
    {
        List<SceneGameObject> owners = scene.FindActiveObjectsWithComponent("GameSceneContext");
        if (owners.Count == 1)
            return;

        if (owners.Count == 0)
        {
            report.AddWarning(
                "GSV060",
                SceneGamePath,
                0,
                "Scene_Game should have one active GameSceneContext so scene start dependencies are owned in one place. Runtime fallback still preserves current play behavior.");
            return;
        }

        report.AddError(
            "GSV060",
            SceneGamePath,
            owners[1].Line,
            $"Scene_Game should have exactly one active GameSceneContext. Active count: {owners.Count}.");
    }

    private static void ValidateBootstrapperPolicy(ValidationReport report, SceneSnapshot scene)
    {
        List<SceneGameObject> owners = scene.FindActiveObjectsWithComponent("GameBootstrapper");
        if (owners.Count == 0)
            return;

        foreach (SceneGameObject owner in owners)
        {
            report.AddWarning(
                "GSV061",
                SceneGamePath,
                owner.Line,
                "GameBootstrapper is now a compatibility fallback. Scene_Game start preparation should be owned by GameSceneContext and GameSceneRuntime.");
        }
    }

    private static void ValidateNoActiveNamedComponent(
        ValidationReport report,
        SceneSnapshot scene,
        string objectName,
        string componentName,
        string ruleId,
        string message)
    {
        foreach (SceneGameObject go in scene.GameObjects)
        {
            if (!go.Active) continue;
            if (!string.Equals(go.Name, objectName, StringComparison.Ordinal)) continue;
            if (!go.ComponentIdByName.ContainsKey(componentName)) continue;

            report.AddError(ruleId, SceneGamePath, go.Line, message);
        }
    }

    private static void ValidateNoActiveComponent(
        ValidationReport report,
        SceneSnapshot scene,
        string componentName,
        string ruleId,
        string message)
    {
        List<SceneGameObject> owners = scene.FindActiveObjectsWithComponent(componentName);
        foreach (SceneGameObject owner in owners)
            report.AddError(ruleId, SceneGamePath, owner.Line, message);
    }

    private static void ValidateSerializedReferencesPointToOwner(
        ValidationReport report,
        string sceneText,
        string fieldName,
        string expectedFileId,
        string ruleId,
        string message)
    {
        if (string.IsNullOrWhiteSpace(expectedFileId))
            return;

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        string[] lines = SplitLines(sceneText);
        Regex fieldRegex = new Regex(@"^\s*" + Regex.Escape(fieldName) + @"\s*\{fileID:\s*(?<id>-?\d+)\}", RegexOptions.Compiled);

        for (int i = 0; i < lines.Length; i++)
        {
            Match match = fieldRegex.Match(lines[i]);
            if (!match.Success) continue;

            string id = match.Groups["id"].Value;
            if (id == "0") continue;
            seen.Add(id);

            if (!string.Equals(id, expectedFileId, StringComparison.Ordinal))
            {
                report.AddError(ruleId, SceneGamePath, i + 1, $"{message} Expected fileID {expectedFileId}, found {id}.");
            }
        }

        if (seen.Count > 1)
        {
            report.AddError(ruleId, SceneGamePath, 0, $"{message} Multiple target fileIDs were found: {string.Join(", ", seen)}.");
        }
    }

    private static void ValidatePrefabOverrideReferencesPointToOwner(
        ValidationReport report,
        string sceneText,
        string propertyPath,
        string expectedFileId,
        string ruleId,
        string message)
    {
        if (string.IsNullOrWhiteSpace(expectedFileId))
            return;

        string[] lines = SplitLines(sceneText);
        string propertyNeedle = "propertyPath: " + propertyPath;

        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Trim().Equals(propertyNeedle, StringComparison.Ordinal))
                continue;

            for (int j = i + 1; j < Math.Min(lines.Length, i + 8); j++)
            {
                Match match = Regex.Match(lines[j], @"objectReference: \{fileID:\s*(?<id>-?\d+)\}");
                if (!match.Success) continue;

                string id = match.Groups["id"].Value;
                if (id != "0" && !string.Equals(id, expectedFileId, StringComparison.Ordinal))
                {
                    report.AddError(ruleId, SceneGamePath, j + 1, $"{message} Expected fileID {expectedFileId}, found {id}.");
                }

                break;
            }
        }
    }

    private static void ValidateNoReferencesToInactiveComponent(
        ValidationReport report,
        SceneSnapshot scene,
        string sceneText,
        string componentName,
        string ruleId,
        string message)
    {
        HashSet<string> inactiveComponentIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (SceneGameObject go in scene.GameObjects)
        {
            if (go.Active) continue;
            if (go.ComponentIdByName.TryGetValue(componentName, out string componentId))
                inactiveComponentIds.Add(componentId);
        }

        if (inactiveComponentIds.Count == 0)
            return;

        string[] lines = SplitLines(sceneText);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("- component:", StringComparison.Ordinal))
                continue;

            if (!line.Contains("objectReference:", StringComparison.Ordinal)
                && !line.Contains(": {fileID:", StringComparison.Ordinal))
                continue;

            foreach (string componentId in inactiveComponentIds)
            {
                if (!line.Contains("{fileID: " + componentId + "}", StringComparison.Ordinal))
                    continue;

                report.AddError(ruleId, SceneGamePath, i + 1, message);
            }
        }
    }

    private static void ValidateStageCatalogPolicy(ValidationReport report, string sceneText)
    {
        string[] lines = SplitLines(sceneText);
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("m_EditorClassIdentifier: Assembly-CSharp::StageManager2D"))
                continue;

            for (int j = Math.Max(0, i - 40); j < Math.Min(lines.Length, i + 80); j++)
            {
                if (!lines[j].Contains("stageCatalog:")) continue;
                if (!lines[j].Contains("{fileID: 0}")) return;

                report.AddWarning(
                    "GSV050",
                    SceneGamePath,
                    j + 1,
                    "StageManager2D has no StageCatalogSO assigned. Prototype spawning can still run, but release story stage clear rules should become data-owned.");
                return;
            }
        }
    }

    private static void ValidateGameplayCollisionPolicy(ValidationReport report)
    {
        string tagManagerText = ReadAssetText("ProjectSettings/TagManager.asset");
        if (string.IsNullOrEmpty(tagManagerText))
        {
            report.AddWarning("GSV080", "ProjectSettings/TagManager.asset", 0, "TagManager.asset could not be read. Collision layers were not validated.");
            return;
        }

        if (!tagManagerText.Contains("- PlayerBody", StringComparison.Ordinal))
        {
            report.AddError("GSV080", "ProjectSettings/TagManager.asset", 0, "PlayerBody layer is required so the player's solid body can be separated from player hurtbox triggers.");
        }

        if (!tagManagerText.Contains("- Obstacle", StringComparison.Ordinal))
        {
            report.AddError("GSV081", "ProjectSettings/TagManager.asset", 0, "Obstacle layer is required for trees, walls, pavilions, and tilemap blockers.");
        }

        string playerPrefabText = ReadAssetText("Assets/_Game/Prefabs/Characters/Player.prefab");
        if (string.IsNullOrEmpty(playerPrefabText))
        {
            report.AddWarning("GSV082", "Assets/_Game/Prefabs/Characters/Player.prefab", 0, "Player prefab could not be read. Player collision body was not validated.");
            return;
        }

        if (!playerPrefabText.Contains("m_EditorClassIdentifier: Assembly-CSharp::PlayerCollisionBody2D", StringComparison.Ordinal))
        {
            report.AddWarning(
                "GSV082",
                "Assets/_Game/Prefabs/Characters/Player.prefab",
                0,
                "Player prefab should have PlayerCollisionBody2D so hurtbox triggers and solid movement collisions are owned separately.");
        }
    }

    private static string ReadAssetText(string assetPath)
    {
        string fullPath = Path.GetFullPath(assetPath);
        return File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
    }

    private static string[] SplitLines(string text)
    {
        return Regex.Split(text ?? string.Empty, "\r\n|\r|\n");
    }

    private sealed class SceneSnapshot
    {
        private readonly Dictionary<string, string> _componentClassByFileId;

        private SceneSnapshot(List<SceneGameObject> gameObjects, Dictionary<string, string> componentClassByFileId)
        {
            GameObjects = gameObjects;
            _componentClassByFileId = componentClassByFileId;

            foreach (SceneGameObject go in GameObjects)
            {
                foreach (string componentId in go.ComponentIds)
                {
                    if (!_componentClassByFileId.TryGetValue(componentId, out string className)) continue;
                    if (!go.ComponentIdByName.ContainsKey(className))
                        go.ComponentIdByName.Add(className, componentId);
                }
            }
        }

        public List<SceneGameObject> GameObjects { get; }

        public static SceneSnapshot Parse(string sceneText)
        {
            string[] lines = SplitLines(sceneText);
            List<SceneGameObject> gameObjects = new List<SceneGameObject>(256);
            Dictionary<string, string> componentClassByFileId = new Dictionary<string, string>(StringComparer.Ordinal);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                Match gameObjectMatch = Regex.Match(line, @"^--- !u!1 &(?<id>-?\d+)");
                if (gameObjectMatch.Success)
                {
                    gameObjects.Add(ParseGameObjectBlock(lines, i, gameObjectMatch.Groups["id"].Value));
                    continue;
                }

                Match monoMatch = Regex.Match(line, @"^--- !u!114 &(?<id>-?\d+)");
                if (monoMatch.Success)
                {
                    string className = ParseMonoBehaviourClass(lines, i);
                    if (!string.IsNullOrWhiteSpace(className))
                        componentClassByFileId[monoMatch.Groups["id"].Value] = className;
                }
            }

            return new SceneSnapshot(gameObjects, componentClassByFileId);
        }

        public List<SceneGameObject> FindActiveObjectsWithComponent(string componentName)
        {
            List<SceneGameObject> result = new List<SceneGameObject>();
            foreach (SceneGameObject go in GameObjects)
            {
                if (!go.Active) continue;
                if (go.ComponentIdByName.ContainsKey(componentName))
                    result.Add(go);
            }

            return result;
        }

        private static SceneGameObject ParseGameObjectBlock(string[] lines, int startIndex, string fileId)
        {
            string name = string.Empty;
            bool active = true;
            List<string> componentIds = new List<string>(8);

            for (int i = startIndex + 1; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("--- ", StringComparison.Ordinal)) break;

                string trimmed = lines[i].Trim();
                if (trimmed.StartsWith("m_Name:", StringComparison.Ordinal))
                    name = trimmed.Substring("m_Name:".Length).Trim().Trim('\'');
                else if (trimmed.StartsWith("m_IsActive:", StringComparison.Ordinal))
                    active = trimmed.EndsWith("1", StringComparison.Ordinal);
                else
                {
                    Match componentMatch = Regex.Match(trimmed, @"^- component: \{fileID:\s*(?<id>-?\d+)\}");
                    if (componentMatch.Success)
                        componentIds.Add(componentMatch.Groups["id"].Value);
                }
            }

            return new SceneGameObject(fileId, name, active, startIndex + 1, componentIds);
        }

        private static string ParseMonoBehaviourClass(string[] lines, int startIndex)
        {
            for (int i = startIndex + 1; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("--- ", StringComparison.Ordinal)) break;

                string trimmed = lines[i].Trim();
                if (!trimmed.StartsWith("m_EditorClassIdentifier:", StringComparison.Ordinal))
                    continue;

                string value = trimmed.Substring("m_EditorClassIdentifier:".Length).Trim();
                int marker = value.LastIndexOf("::", StringComparison.Ordinal);
                return marker >= 0 ? value.Substring(marker + 2).Trim() : value;
            }

            return string.Empty;
        }
    }

    private sealed class SceneGameObject
    {
        public SceneGameObject(string fileId, string name, bool active, int line, List<string> componentIds)
        {
            FileId = fileId;
            Name = name ?? string.Empty;
            Active = active;
            Line = line;
            ComponentIds = componentIds ?? new List<string>();
            ComponentIdByName = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public string FileId { get; }
        public string Name { get; }
        public bool Active { get; }
        public int Line { get; }
        public List<string> ComponentIds { get; }
        public Dictionary<string, string> ComponentIdByName { get; }
    }

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class ValidationReport
    {
        private readonly string _title;
        private readonly List<Finding> _findings = new List<Finding>(32);

        public ValidationReport(string title)
        {
            _title = title ?? "Validator";
        }

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int InfoCount { get; private set; }

        public void AddError(string ruleId, string path, int line, string message)
        {
            Add(ValidationSeverity.Error, ruleId, path, line, message);
        }

        public void AddWarning(string ruleId, string path, int line, string message)
        {
            Add(ValidationSeverity.Warning, ruleId, path, line, message);
        }

        public void Add(ValidationSeverity severity, string ruleId, string path, int line, string message)
        {
            _findings.Add(new Finding(severity, ruleId, NormalizePath(path), line, message));

            switch (severity)
            {
                case ValidationSeverity.Error:
                    ErrorCount++;
                    break;
                case ValidationSeverity.Warning:
                    WarningCount++;
                    break;
                case ValidationSeverity.Info:
                    InfoCount++;
                    break;
            }
        }

        public void Sort()
        {
            _findings.Sort((a, b) =>
            {
                int severity = b.Severity.CompareTo(a.Severity);
                if (severity != 0) return severity;

                int path = string.Compare(a.Path, b.Path, StringComparison.Ordinal);
                if (path != 0) return path;

                int line = a.Line.CompareTo(b.Line);
                if (line != 0) return line;

                return string.Compare(a.RuleId, b.RuleId, StringComparison.Ordinal);
            });
        }

        public string ToConsoleText()
        {
            StringBuilder sb = new StringBuilder(4096);
            sb.AppendLine(_title);
            sb.AppendLine(ErrorCount > 0 ? "Result: Failed" : "Result: Passed");
            sb.AppendLine($"Errors: {ErrorCount}, Warnings: {WarningCount}, Info: {InfoCount}");

            if (_findings.Count == 0)
            {
                sb.AppendLine("No findings.");
                return sb.ToString();
            }

            foreach (Finding finding in _findings)
            {
                sb.Append('[').Append(finding.Severity.ToString().ToUpperInvariant()).Append("] ")
                    .Append(finding.RuleId).Append(' ')
                    .Append(finding.Path);

                if (finding.Line > 0)
                    sb.Append(':').Append(finding.Line);

                sb.AppendLine();
                sb.Append("  Reason: ").AppendLine(finding.Message);
            }

            return sb.ToString();
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }
    }

    public readonly struct Finding
    {
        public Finding(ValidationSeverity severity, string ruleId, string path, int line, string message)
        {
            Severity = severity;
            RuleId = ruleId ?? string.Empty;
            Path = path ?? string.Empty;
            Line = line;
            Message = message ?? string.Empty;
        }

        public ValidationSeverity Severity { get; }
        public string RuleId { get; }
        public string Path { get; }
        public int Line { get; }
        public string Message { get; }
    }
}
#endif
