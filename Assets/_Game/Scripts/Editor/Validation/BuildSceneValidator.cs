#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class BuildSceneValidator
{
    private const string MenuPath = "Tools/Honryeom/Validation/Build Scene Validator";
    private const string ExpectedEntryScenePath = "Assets/Scenes/Scene_Lobby.unity";

    private static readonly string[] RequiredBuildScenePaths =
    {
        "Assets/Scenes/Scene_Boot.unity",
        "Assets/Scenes/Scene_Lobby.unity",
        "Assets/Scenes/Scene_Game.unity"
    };

    private static readonly string[] ReleaseBlockedSceneNames =
    {
        "SampleScene",
        "Scene_HJO",
        "Scene_JGM",
        "Scene_UI"
    };

    private static readonly Regex SceneNameFieldRegex =
        new Regex(@"^\s*[A-Za-z0-9_]*SceneName:\s*(?<sceneName>[A-Za-z0-9_\- ]+)\s*$", RegexOptions.Compiled);

    [MenuItem(MenuPath)]
    public static void RunFromMenu()
    {
        ValidationReport report = Run();
        Debug.Log(report.ToConsoleText());

        if (report.ErrorCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Build Scene Validator",
                $"Failed: {report.ErrorCount} error(s), {report.WarningCount} warning(s).\nSee Console for details.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Build Scene Validator",
                $"Passed: {report.WarningCount} warning(s).",
                "OK");
        }
    }

    public static ValidationReport Run()
    {
        ValidationReport report = new ValidationReport();
        List<BuildSceneInfo> enabledScenes = GetEnabledBuildScenes();

        ValidateBuildSceneList(report, enabledScenes);
        ValidateRequiredScenes(report, enabledScenes);
        ValidateSceneReferences(report, enabledScenes);

        report.Sort();
        return report;
    }

    private static void ValidateBuildSceneList(ValidationReport report, List<BuildSceneInfo> enabledScenes)
    {
        if (enabledScenes.Count == 0)
        {
            report.AddError(
                "BSV001",
                "ProjectSettings/EditorBuildSettings.asset",
                0,
                "No enabled build scenes. A release build needs an explicit entry scene.");
            return;
        }

        BuildSceneInfo firstScene = enabledScenes[0];
        if (!PathEquals(firstScene.Path, ExpectedEntryScenePath))
        {
            report.AddError(
                "BSV002",
                "ProjectSettings/EditorBuildSettings.asset",
                0,
                $"First enabled build scene is '{firstScene.Path}'. Expected '{ExpectedEntryScenePath}' so title routing has one stable entry point.");
        }

        HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (BuildSceneInfo scene in enabledScenes)
        {
            if (!File.Exists(scene.Path))
            {
                report.AddError(
                    "BSV003",
                    "ProjectSettings/EditorBuildSettings.asset",
                    0,
                    $"Enabled build scene path does not exist: {scene.Path}");
            }

            if (!seenPaths.Add(scene.Path))
            {
                report.AddError(
                    "BSV004",
                    "ProjectSettings/EditorBuildSettings.asset",
                    0,
                    $"Duplicate enabled build scene: {scene.Path}");
            }

            if (IsReleaseBlockedScene(scene.SceneName))
            {
                report.AddError(
                    "BSV005",
                    "ProjectSettings/EditorBuildSettings.asset",
                    0,
                    $"Development or sandbox scene is enabled for build: {scene.Path}");
            }
        }
    }

    private static void ValidateRequiredScenes(ValidationReport report, List<BuildSceneInfo> enabledScenes)
    {
        HashSet<string> enabledPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (BuildSceneInfo scene in enabledScenes)
        {
            enabledPaths.Add(scene.Path);
        }

        foreach (string requiredPath in RequiredBuildScenePaths)
        {
            if (enabledPaths.Contains(requiredPath)) continue;

            report.AddError(
                "BSV006",
                "ProjectSettings/EditorBuildSettings.asset",
                0,
                $"Required release scene is not enabled in Build Settings: {requiredPath}");
        }
    }

    private static void ValidateSceneReferences(ValidationReport report, List<BuildSceneInfo> enabledScenes)
    {
        Dictionary<string, BuildSceneInfo> enabledByName = new Dictionary<string, BuildSceneInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (BuildSceneInfo scene in enabledScenes)
        {
            enabledByName[scene.SceneName] = scene;
        }

        foreach (BuildSceneInfo scene in enabledScenes)
        {
            string text = ReadAssetText(scene.Path);
            if (string.IsNullOrEmpty(text))
            {
                report.AddWarning("BSV007", scene.Path, 0, "Build scene text could not be read for serialized scene-name reference checks.");
                continue;
            }

            string[] lines = SplitLines(text);
            for (int i = 0; i < lines.Length; i++)
            {
                Match match = SceneNameFieldRegex.Match(lines[i]);
                if (!match.Success) continue;

                string targetSceneName = match.Groups["sceneName"].Value.Trim();
                if (string.IsNullOrEmpty(targetSceneName)) continue;
                if (enabledByName.ContainsKey(targetSceneName)) continue;

                report.AddWarning(
                    "BSV008",
                    scene.Path,
                    i + 1,
                    $"Serialized scene reference '{targetSceneName}' is not an enabled build scene. Field line: {lines[i].Trim()}");
            }
        }
    }

    private static List<BuildSceneInfo> GetEnabledBuildScenes()
    {
        List<BuildSceneInfo> scenes = new List<BuildSceneInfo>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene == null || !scene.enabled) continue;
            if (string.IsNullOrWhiteSpace(scene.path)) continue;

            string normalizedPath = NormalizePath(scene.path);
            scenes.Add(new BuildSceneInfo(normalizedPath, Path.GetFileNameWithoutExtension(normalizedPath)));
        }

        return scenes;
    }

    private static bool IsReleaseBlockedScene(string sceneName)
    {
        foreach (string blockedSceneName in ReleaseBlockedSceneNames)
        {
            if (string.Equals(sceneName, blockedSceneName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool PathEquals(string left, string right)
    {
        return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadAssetText(string assetPath)
    {
        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath)) return null;
        return File.ReadAllText(fullPath);
    }

    private static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
    }

    private static string[] SplitLines(string text)
    {
        return Regex.Split(text ?? string.Empty, "\r\n|\r|\n");
    }

    private readonly struct BuildSceneInfo
    {
        public BuildSceneInfo(string path, string sceneName)
        {
            Path = path ?? string.Empty;
            SceneName = sceneName ?? string.Empty;
        }

        public string Path { get; }
        public string SceneName { get; }
    }

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class ValidationReport
    {
        private readonly List<Finding> _findings = new List<Finding>(32);

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int InfoCount { get; private set; }

        public IReadOnlyList<Finding> Findings => _findings;

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
            sb.AppendLine("Build Scene Validator");
            sb.AppendLine(ErrorCount > 0 ? "Result: Failed" : "Result: Passed");
            sb.AppendLine($"Errors: {ErrorCount}, Warnings: {WarningCount}, Info: {InfoCount}");

            if (_findings.Count == 0)
            {
                sb.AppendLine("No findings.");
                return sb.ToString();
            }

            for (int i = 0; i < _findings.Count; i++)
            {
                Finding finding = _findings[i];
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
