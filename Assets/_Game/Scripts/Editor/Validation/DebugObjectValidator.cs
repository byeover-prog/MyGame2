#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class DebugObjectValidator
{
    private const string MenuPath = "Tools/Honryeom/Validation/Debug Object Validator";
    private const string RuntimeScriptRoot = "Assets/_Game/Scripts";
    private const string SceneRoot = "Assets/Scenes";

    private static readonly DebugComponentRule[] ForbiddenDebugComponents =
    {
        new DebugComponentRule("DebugRuntimeHUD", "Runtime debug HUD exists in an enabled build scene."),
        new DebugComponentRule("UIRaycastProbe", "UI raycast debug probe exists in an enabled build scene."),
        new DebugComponentRule("UIButtonPointerProbe", "UI button pointer debug probe exists in an enabled build scene."),
        new DebugComponentRule("LevelUpRuntimeHardResetOnPlay", "Runtime hard reset helper exists in an enabled build scene.")
    };

    private static readonly string[] RuntimeDebugHotkeyPatterns =
    {
        "Input.GetKeyDown(KeyCode.F9)",
        "Input.GetKeyDown(KeyCode.F10)"
    };

    private static readonly string[] DeveloperHotkeyComponentNames =
    {
        "WeaponLoadApplier2D",
        "SkillBalanceBootstrap2D"
    };

    private static readonly string[] GuardSymbols =
    {
        "UNITY_EDITOR",
        "DEVELOPMENT_BUILD"
    };

    [MenuItem(MenuPath)]
    public static void RunFromMenu()
    {
        ValidationReport report = Run();
        Debug.Log(report.ToConsoleText());

        if (report.ErrorCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Debug Object Validator",
                $"Failed: {report.ErrorCount} error(s), {report.WarningCount} warning(s).\nSee Console for details.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Debug Object Validator",
                $"Passed: {report.WarningCount} warning(s).",
                "OK");
        }
    }

    public static ValidationReport Run()
    {
        ValidationReport report = new ValidationReport();

        HashSet<string> enabledBuildScenes = GetEnabledBuildScenes();
        ScanBuildScenes(report, enabledBuildScenes);
        ScanNonBuildScenes(report, enabledBuildScenes);
        ScanRuntimeHotkeys(report);

        report.Sort();
        return report;
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

    private static void ScanBuildScenes(ValidationReport report, HashSet<string> enabledBuildScenes)
    {
        foreach (string scenePath in enabledBuildScenes)
        {
            string text = ReadAssetText(scenePath);
            if (string.IsNullOrEmpty(text))
            {
                report.AddWarning("DBG000", scenePath, 0, "Scene text could not be read.");
                continue;
            }

            foreach (DebugComponentRule rule in ForbiddenDebugComponents)
            {
                AddSceneComponentFindings(
                    report,
                    scenePath,
                    text,
                    rule.TypeName,
                    "DBG001",
                    ValidationSeverity.Error,
                    rule.ReleaseRisk);
            }

            foreach (string componentName in DeveloperHotkeyComponentNames)
            {
                AddSceneComponentFindings(
                    report,
                    scenePath,
                    text,
                    componentName,
                    "DBG005",
                    ValidationSeverity.Warning,
                    $"Developer hotkey component '{componentName}' is referenced by an enabled build scene. Verify it is disabled or development-gated.");
            }
        }
    }

    private static void ScanNonBuildScenes(ValidationReport report, HashSet<string> enabledBuildScenes)
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { SceneRoot });
        Array.Sort(sceneGuids, StringComparer.Ordinal);

        foreach (string guid in sceneGuids)
        {
            string scenePath = NormalizePath(AssetDatabase.GUIDToAssetPath(guid));
            if (enabledBuildScenes.Contains(scenePath)) continue;

            string text = ReadAssetText(scenePath);
            if (string.IsNullOrEmpty(text)) continue;

            foreach (DebugComponentRule rule in ForbiddenDebugComponents)
            {
                AddSceneComponentFindings(
                    report,
                    scenePath,
                    text,
                    rule.TypeName,
                    "DBG006",
                    ValidationSeverity.Warning,
                    $"Debug component '{rule.TypeName}' exists in a non-build scene. It becomes release-blocking if the scene is added to Build Settings.");
            }
        }
    }

    private static void ScanRuntimeHotkeys(ValidationReport report)
    {
        string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { RuntimeScriptRoot });
        Array.Sort(scriptGuids, StringComparer.Ordinal);

        foreach (string guid in scriptGuids)
        {
            string scriptPath = NormalizePath(AssetDatabase.GUIDToAssetPath(guid));
            if (string.IsNullOrEmpty(scriptPath)) continue;
            if (IndexOfOrdinalIgnoreCase(scriptPath, "/Editor/") >= 0) continue;

            string text = ReadAssetText(scriptPath);
            if (string.IsNullOrEmpty(text)) continue;

            string[] lines = SplitLines(text);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                foreach (string pattern in RuntimeDebugHotkeyPatterns)
                {
                    if (!ContainsOrdinal(line, pattern)) continue;
                    if (IsLineDevelopmentGuarded(lines, i)) continue;

                    report.AddError(
                        "DBG002",
                        scriptPath,
                        i + 1,
                        $"Unguarded runtime debug hotkey '{pattern}'. Gate it with UNITY_EDITOR, DEVELOPMENT_BUILD, or release-disabled debug settings.");
                }
            }

            if (ContainsOrdinal(text, "private void OnGUI()")
                || ContainsOrdinal(text, "void OnGUI()"))
            {
                bool isKnownDebugComponent = ContainsAny(text, "class DebugRuntimeHUD", "class UIRaycastProbe", "class UIButtonPointerProbe");
                ValidationSeverity severity = isKnownDebugComponent ? ValidationSeverity.Warning : ValidationSeverity.Info;
                report.Add(
                    severity,
                    "DBG003",
                    scriptPath,
                    FindLineNumber(lines, "OnGUI"),
                    "Runtime OnGUI method found. This is acceptable only when not referenced by enabled build scenes or when development-gated.");
            }
        }
    }

    private static void AddSceneComponentFindings(
        ValidationReport report,
        string scenePath,
        string sceneText,
        string typeName,
        string ruleId,
        ValidationSeverity severity,
        string message)
    {
        bool found = false;

        string classIdentifier = $"Assembly-CSharp::{typeName}";
        int classLine = FindLineNumber(sceneText, classIdentifier);
        if (classLine > 0)
        {
            report.Add(severity, ruleId, scenePath, classLine, $"{message} Component: {typeName}.");
            found = true;
        }

        string scriptGuid = FindScriptGuid(typeName);
        if (!string.IsNullOrWhiteSpace(scriptGuid))
        {
            int guidLine = FindLineNumber(sceneText, scriptGuid);
            if (guidLine > 0 && !found)
            {
                report.Add(severity, ruleId, scenePath, guidLine, $"{message} Component: {typeName}.");
                found = true;
            }
        }

        int objectNameLine = FindLineNumber(sceneText, $"m_Name: {typeName}");
        if (objectNameLine > 0 && !found)
        {
            report.Add(severity, ruleId, scenePath, objectNameLine, $"{message} Object name: {typeName}.");
        }
    }

    private static string FindScriptGuid(string typeName)
    {
        string[] guids = AssetDatabase.FindAssets(typeName + " t:MonoScript", new[] { RuntimeScriptRoot });
        Array.Sort(guids, StringComparer.Ordinal);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(fileName, typeName, StringComparison.Ordinal))
                return guid;
        }

        return null;
    }

    private static bool IsLineDevelopmentGuarded(string[] lines, int targetLineIndex)
    {
        int depth = 0;
        bool guarded = false;
        Stack<GuardFrame> stack = new Stack<GuardFrame>();

        for (int i = 0; i <= targetLineIndex && i < lines.Length; i++)
        {
            string trimmed = lines[i].Trim();

            if (trimmed.StartsWith("#if ", StringComparison.Ordinal)
                || trimmed.StartsWith("#elif ", StringComparison.Ordinal))
            {
                bool isGuard = ContainsAny(trimmed, GuardSymbols);

                if (trimmed.StartsWith("#if ", StringComparison.Ordinal))
                {
                    stack.Push(new GuardFrame(depth, isGuard));
                    depth++;
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        GuardFrame frame = stack.Pop();
                        stack.Push(new GuardFrame(frame.Depth, isGuard));
                    }
                }
            }
            else if (trimmed.StartsWith("#else", StringComparison.Ordinal))
            {
                if (stack.Count > 0)
                {
                    GuardFrame frame = stack.Pop();
                    stack.Push(new GuardFrame(frame.Depth, false));
                }
            }
            else if (trimmed.StartsWith("#endif", StringComparison.Ordinal))
            {
                if (stack.Count > 0)
                    stack.Pop();
                if (depth > 0)
                    depth--;
            }
        }

        foreach (GuardFrame frame in stack)
        {
            if (frame.IsDevelopmentOnly)
            {
                guarded = true;
                break;
            }
        }

        return guarded;
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

    private static int FindLineNumber(string text, string pattern)
    {
        string[] lines = SplitLines(text);
        return FindLineNumber(lines, pattern);
    }

    private static int FindLineNumber(string[] lines, string pattern)
    {
        if (lines == null || string.IsNullOrEmpty(pattern)) return 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (ContainsOrdinal(lines[i], pattern))
                return i + 1;
        }

        return 0;
    }

    private static bool ContainsAny(string text, params string[] patterns)
    {
        if (string.IsNullOrEmpty(text) || patterns == null) return false;

        foreach (string pattern in patterns)
        {
            if (!string.IsNullOrEmpty(pattern) && ContainsOrdinal(text, pattern))
                return true;
        }

        return false;
    }

    private static bool ContainsOrdinal(string text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern)) return false;
        return text.IndexOf(pattern, StringComparison.Ordinal) >= 0;
    }

    private static int IndexOfOrdinalIgnoreCase(string text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern)) return -1;
        return text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private readonly struct DebugComponentRule
    {
        public DebugComponentRule(string typeName, string releaseRisk)
        {
            TypeName = typeName;
            ReleaseRisk = releaseRisk;
        }

        public string TypeName { get; }
        public string ReleaseRisk { get; }
    }

    private readonly struct GuardFrame
    {
        public GuardFrame(int depth, bool isDevelopmentOnly)
        {
            Depth = depth;
            IsDevelopmentOnly = isDevelopmentOnly;
        }

        public int Depth { get; }
        public bool IsDevelopmentOnly { get; }
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
            _findings.Add(new Finding(severity, ruleId, DebugObjectValidator.NormalizePath(path), line, message));

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
            sb.AppendLine("Debug Object Validator");
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
