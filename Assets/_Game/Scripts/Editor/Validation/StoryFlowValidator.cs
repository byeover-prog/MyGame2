#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class StoryFlowValidator
{
    private const string MenuPath = "Tools/Honryeom/Validation/Story Flow Validator";
    private const string TitleScenePath = "Assets/Scenes/Scene_Lobby.unity";
    private const string FormationScenePath = "Assets/Scenes/Scene_Boot.unity";
    private const string ScriptRoot = "Assets/_Game/Scripts";

    private static readonly TokenRule[] RequiredTitleOptions =
    {
        new TokenRule("SFV001", "Story Mode", "Story Mode entry is missing from the title scene.", "Story", "\uc2a4\ud1a0\ub9ac"),
        new TokenRule("SFV002", "Casual Mode", "Casual Mode entry is missing from the title scene.", "Casual", "\uce90\uc8fc\uc5bc"),
        new TokenRule("SFV003", "Settings", "Settings entry is missing from the title scene.", "btnSettings", "Btn_Settings", "Btn_Setting", "SettingsText", "Option", "\ud658\uacbd\uc124\uc815", "\uc124\uc815"),
        new TokenRule("SFV004", "Quit", "Quit entry is missing from the title scene.", "Quit", "Exit", "\uc885\ub8cc")
    };

    private static readonly TokenRule[] RequiredStorySubOptions =
    {
        new TokenRule("SFV006", "Continue", "Story Mode needs a Continue option gated by save progress.", "Continue", "\uc774\uc5b4\ud558\uae30"),
        new TokenRule("SFV007", "New Game", "Story Mode needs a New Game option.", "New Game", "NewGame", "\uc0c8\ub85c\ud558\uae30")
    };

    [MenuItem(MenuPath)]
    public static void RunFromMenu()
    {
        ValidationReport report = Run();
        Debug.Log(report.ToConsoleText());

        if (report.ErrorCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Story Flow Validator",
                $"Failed: {report.ErrorCount} error(s), {report.WarningCount} warning(s).\nSee Console for details.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Story Flow Validator",
                $"Passed: {report.WarningCount} warning(s).",
                "OK");
        }
    }

    public static ValidationReport Run()
    {
        ValidationReport report = new ValidationReport();

        string titleText = ReadAssetText(TitleScenePath);
        string formationText = ReadAssetText(FormationScenePath);
        string scriptText = ReadAllRuntimeScriptText();

        ValidateTitleScene(report, titleText);
        ValidateStoryOptions(report, titleText, scriptText);
        ValidateCurrentDirectRoutes(report, titleText, formationText, scriptText);
        ValidateCheckpointOwnershipSignal(report, scriptText);

        report.Sort();
        return report;
    }

    private static void ValidateTitleScene(ValidationReport report, string titleText)
    {
        if (string.IsNullOrEmpty(titleText))
        {
            report.AddError("SFV000", TitleScenePath, 0, "Title scene text could not be read.");
            return;
        }

        foreach (TokenRule rule in RequiredTitleOptions)
        {
            if (ContainsAny(titleText, rule.Tokens)) continue;

            report.AddError(
                rule.RuleId,
                TitleScenePath,
                0,
                $"{rule.Message} Expected title option: {rule.DisplayName}.");
        }
    }

    private static void ValidateStoryOptions(ValidationReport report, string titleText, string scriptText)
    {
        string combinedText = (titleText ?? string.Empty) + "\n" + (scriptText ?? string.Empty);

        foreach (TokenRule rule in RequiredStorySubOptions)
        {
            if (ContainsAny(combinedText, rule.Tokens)) continue;

            report.AddError(
                rule.RuleId,
                TitleScenePath,
                0,
                $"{rule.Message} No matching UI or script ownership token was found.");
        }
    }

    private static void ValidateCurrentDirectRoutes(
        ValidationReport report,
        string titleText,
        string formationText,
        string scriptText)
    {
        if (ContainsAll(titleText, "LobbyMenuController", "Btn_Start", "gameSceneName: Scene_Boot"))
        {
            report.AddError(
                "SFV005",
                TitleScenePath,
                FindLineNumber(titleText, "gameSceneName: Scene_Boot"),
                "Title scene still has a generic Start button route to Scene_Boot. Target flow requires Story Mode and Casual Mode to branch explicitly first.");
        }

        bool storyNewGameRouteIsOwned = ContainsAll(
            scriptText,
            "OnClickNewGame",
            "RunSetupMode.Story",
            "storyOpeningSceneName",
            "StoryClearRouteService");

        if (!storyNewGameRouteIsOwned
            && ContainsAny(formationText, "startSceneName: Scene_Game", "nextSceneName: Scene_Game"))
        {
            report.AddWarning(
                "SFV008",
                FormationScenePath,
                FindFirstLineNumber(formationText, "startSceneName: Scene_Game", "nextSceneName: Scene_Game"),
                "Formation scene routes directly to Scene_Game. This may be valid for a prototype, but Story New Game still needs explicit Opening Story, Stage 0, Stage 1, and Story Lobby routing.");
        }
    }

    private static void ValidateCheckpointOwnershipSignal(ValidationReport report, string scriptText)
    {
        if (ContainsAny(scriptText, "ContinueCheckpoint", "Checkpoint", "\uc774\uc5b4\ud558\uae30")) return;

        report.AddWarning(
            "SFV009",
            ScriptRoot,
            0,
            "No obvious Continue checkpoint ownership signal was found in runtime scripts. Static check only: confirm with the dedicated Continue Checkpoint Validator later.");
    }

    private static string ReadAllRuntimeScriptText()
    {
        StringBuilder sb = new StringBuilder(65536);
        string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { ScriptRoot });
        Array.Sort(guids, StringComparer.Ordinal);

        foreach (string guid in guids)
        {
            string path = NormalizePath(AssetDatabase.GUIDToAssetPath(guid));
            if (string.IsNullOrEmpty(path)) continue;
            if (path.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0) continue;

            string text = ReadAssetText(path);
            if (string.IsNullOrEmpty(text)) continue;

            sb.AppendLine(path);
            sb.AppendLine(text);
        }

        return sb.ToString();
    }

    private static string ReadAssetText(string assetPath)
    {
        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath)) return null;
        return File.ReadAllText(fullPath);
    }

    private static int FindLineNumber(string text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern)) return 0;

        string[] lines = SplitLines(text);
        for (int i = 0; i < lines.Length; i++)
        {
            if (ContainsOrdinalIgnoreCase(lines[i], pattern))
                return i + 1;
        }

        return 0;
    }

    private static int FindFirstLineNumber(string text, params string[] patterns)
    {
        foreach (string pattern in patterns)
        {
            int line = FindLineNumber(text, pattern);
            if (line > 0) return line;
        }

        return 0;
    }

    private static string[] SplitLines(string text)
    {
        return Regex.Split(text ?? string.Empty, "\r\n|\r|\n");
    }

    private static bool ContainsAll(string text, params string[] patterns)
    {
        if (string.IsNullOrEmpty(text) || patterns == null) return false;

        foreach (string pattern in patterns)
        {
            if (!ContainsOrdinalIgnoreCase(text, pattern))
                return false;
        }

        return true;
    }

    private static bool ContainsAny(string text, params string[] patterns)
    {
        if (string.IsNullOrEmpty(text) || patterns == null) return false;

        foreach (string pattern in patterns)
        {
            if (ContainsOrdinalIgnoreCase(text, pattern))
                return true;
        }

        return false;
    }

    private static bool ContainsOrdinalIgnoreCase(string text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern)) return false;
        return text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
    }

    private readonly struct TokenRule
    {
        public TokenRule(string ruleId, string displayName, string message, params string[] tokens)
        {
            RuleId = ruleId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Message = message ?? string.Empty;
            Tokens = tokens ?? Array.Empty<string>();
        }

        public string RuleId { get; }
        public string DisplayName { get; }
        public string Message { get; }
        public string[] Tokens { get; }
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
            sb.AppendLine("Story Flow Validator");
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
