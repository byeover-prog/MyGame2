#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class AssetIntegrityValidator
{
    private const string MenuPath = "Tools/Honryeom/Validation/Asset Integrity Validator";
    private const string MissingScriptPattern = "m_Script: {fileID: 0}";

    private static readonly string[] ScanRoots =
    {
        "Assets/_Game",
        "Assets/GameData",
        "Assets/Scenes"
    };

    private static readonly HashSet<string> ScannableExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".asset",
        ".prefab",
        ".unity"
    };

    [MenuItem(MenuPath)]
    public static void RunFromMenu()
    {
        ValidationReport report = Run();
        Debug.Log(report.ToConsoleText());

        if (report.ErrorCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Asset Integrity Validator",
                $"Failed: {report.ErrorCount} error(s), {report.WarningCount} warning(s).\nSee Console for details.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Asset Integrity Validator",
                $"Passed: {report.WarningCount} warning(s).",
                "OK");
        }
    }

    public static ValidationReport Run()
    {
        ValidationReport report = new ValidationReport();
        HashSet<string> enabledBuildScenes = GetEnabledBuildScenes();

        foreach (string assetPath in GetScannableAssetPaths())
        {
            ScanYamlAsset(report, assetPath, enabledBuildScenes);
        }

        report.Sort();
        return report;
    }

    private static void ScanYamlAsset(
        ValidationReport report,
        string assetPath,
        HashSet<string> enabledBuildScenes)
    {
        string normalizedPath = NormalizePath(assetPath);
        string text = ReadAssetText(normalizedPath);
        if (string.IsNullOrEmpty(text)) return;
        if (!ContainsOrdinal(text, MissingScriptPattern)) return;

        string[] lines = SplitLines(text);
        string extension = Path.GetExtension(normalizedPath);
        bool isEnabledBuildScene = enabledBuildScenes.Contains(normalizedPath);

        for (int i = 0; i < lines.Length; i++)
        {
            if (!ContainsOrdinal(lines[i], MissingScriptPattern)) continue;

            int lineNumber = i + 1;
            string objectName = FindNearestObjectName(lines, i);
            string objectSuffix = string.IsNullOrEmpty(objectName) ? string.Empty : $" Object: {objectName}.";

            if (isEnabledBuildScene)
            {
                report.AddError(
                    "AIT001",
                    normalizedPath,
                    lineNumber,
                    $"Enabled build scene contains a missing MonoBehaviour script reference.{objectSuffix}");
                continue;
            }

            if (extension.Equals(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                report.AddError(
                    "AIT002",
                    normalizedPath,
                    lineNumber,
                    $"Prefab contains a missing MonoBehaviour script reference.{objectSuffix}");
                continue;
            }

            if (extension.Equals(".asset", StringComparison.OrdinalIgnoreCase))
            {
                report.AddError(
                    "AIT003",
                    normalizedPath,
                    lineNumber,
                    $"ScriptableObject or serialized asset contains a missing script reference.{objectSuffix}");
                continue;
            }

            if (extension.Equals(".unity", StringComparison.OrdinalIgnoreCase))
            {
                report.AddWarning(
                    "AIT004",
                    normalizedPath,
                    lineNumber,
                    $"Non-build scene contains a missing MonoBehaviour script reference. It becomes release-blocking if the scene is added to Build Settings.{objectSuffix}");
            }
        }
    }

    private static IEnumerable<string> GetScannableAssetPaths()
    {
        string[] guids = AssetDatabase.FindAssets(string.Empty, ScanRoots);
        Array.Sort(guids, StringComparer.Ordinal);

        HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string guid in guids)
        {
            string path = NormalizePath(AssetDatabase.GUIDToAssetPath(guid));
            if (string.IsNullOrEmpty(path)) continue;
            if (!ScannableExtensions.Contains(Path.GetExtension(path))) continue;
            if (!paths.Add(path)) continue;

            yield return path;
        }
    }

    private static HashSet<string> GetEnabledBuildScenes()
    {
        HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene == null || !scene.enabled) continue;
            if (string.IsNullOrWhiteSpace(scene.path)) continue;
            paths.Add(NormalizePath(scene.path));
        }

        return paths;
    }

    private static string FindNearestObjectName(string[] lines, int missingScriptLineIndex)
    {
        for (int i = missingScriptLineIndex; i >= 0; i--)
        {
            string line = lines[i].Trim();
            if (line.StartsWith("--- !u!", StringComparison.Ordinal)) break;

            const string namePrefix = "m_Name:";
            if (!line.StartsWith(namePrefix, StringComparison.Ordinal)) continue;

            return line.Substring(namePrefix.Length).Trim();
        }

        return string.Empty;
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

    private static bool ContainsOrdinal(string text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern)) return false;
        return text.IndexOf(pattern, StringComparison.Ordinal) >= 0;
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

        public void Add(
            ValidationSeverity severity,
            string ruleId,
            string path,
            int line,
            string message)
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
            sb.AppendLine("Asset Integrity Validator");
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
        public Finding(
            ValidationSeverity severity,
            string ruleId,
            string path,
            int line,
            string message)
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
