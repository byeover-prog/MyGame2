#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class ContinueCheckpointValidator
{
    private const string MenuPath = "Tools/Honryeom/Validation/Continue Checkpoint Validator";
    private const string ScriptRoot = "Assets/_Game/Scripts";
    private const string StageManagerPath = "Assets/_Game/Scripts/Stage/StageManager.cs";
    private const string StageProgressPath = "Assets/_Game/Scripts/Stage/StageProgressSaveData.cs";
    private const string MetaProfilePath = "Assets/_Game/Scripts/Meta/Save/MetaProfileSaveData2D.cs";
    private const string DefeatUiPath = "Assets/_Game/Scripts/UI/Defeat/DefeatUIController2D.cs";
    private const string ClearButtonPath = "Assets/_Game/Scripts/UI/ClearUI/ClearUIButtonHandler.cs";

    private static readonly string[] CheckpointOwnershipTokens =
    {
        "ContinueCheckpoint",
        "StoryCheckpoint",
        "CheckpointSaveData",
        "continuePoint",
        "checkpointPoint",
        "resumeScene",
        "resumeStage",
        "lastCheckpoint"
    };

    private static readonly string[] StageStartSaveTokens =
    {
        "SaveCheckpoint",
        "SetCheckpoint",
        "SaveContinue",
        "BeginStageCheckpoint",
        "MarkStageStarted"
    };

    [MenuItem(MenuPath)]
    public static void RunFromMenu()
    {
        ValidationReport report = Run();
        Debug.Log(report.ToConsoleText());

        if (report.ErrorCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Continue Checkpoint Validator",
                $"Failed: {report.ErrorCount} error(s), {report.WarningCount} warning(s).\nSee Console for details.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Continue Checkpoint Validator",
                $"Passed: {report.WarningCount} warning(s).",
                "OK");
        }
    }

    public static ValidationReport Run()
    {
        ValidationReport report = new ValidationReport();

        string allRuntimeText = ReadAllRuntimeScriptText();
        string stageManagerText = ReadAssetText(StageManagerPath);
        string stageProgressText = ReadAssetText(StageProgressPath);
        string metaProfileText = ReadAssetText(MetaProfilePath);
        string defeatUiText = ReadAssetText(DefeatUiPath);
        string clearButtonText = ReadAssetText(ClearButtonPath);

        ValidateCheckpointDataOwnership(report, allRuntimeText, stageProgressText, metaProfileText);
        ValidateStageStartCheckpointSave(report, stageManagerText);
        ValidateDefeatResumeRoute(report, defeatUiText);
        ValidateClearRoute(report, clearButtonText);

        report.Sort();
        return report;
    }

    private static void ValidateCheckpointDataOwnership(
        ValidationReport report,
        string allRuntimeText,
        string stageProgressText,
        string metaProfileText)
    {
        if (ContainsAny(allRuntimeText, CheckpointOwnershipTokens)) return;

        report.AddError(
            "CCV001",
            ScriptRoot,
            0,
            "No explicit Continue checkpoint data owner was found. Target flow needs saved points for Stage 0 start, Stage 1 start, and Story Lobby entry.");

        if (!ContainsAny(stageProgressText, "currentStage", "resume", "checkpoint", "Continue"))
        {
            report.AddWarning(
                "CCV002",
                StageProgressPath,
                0,
                "StageProgressSaveData appears to track cleared/max reached stages only, not the current Continue point.");
        }

        if (!ContainsAny(metaProfileText, "checkpoint", "continue", "resume"))
        {
            report.AddWarning(
                "CCV003",
                MetaProfilePath,
                0,
                "MetaProfileSaveData has no obvious field for a Story Continue checkpoint.");
        }
    }

    private static void ValidateStageStartCheckpointSave(ValidationReport report, string stageManagerText)
    {
        if (string.IsNullOrEmpty(stageManagerText))
        {
            report.AddError("CCV004", StageManagerPath, 0, "StageManager text could not be read.");
            return;
        }

        string beginStageBody = ExtractMethodBody(stageManagerText, "BeginStage");
        bool savesCheckpointAtStageStart = ContainsAny(beginStageBody, StageStartSaveTokens)
            || ContainsAll(beginStageBody, "SaveManager2D", "Save(")
            || ContainsAll(
                stageManagerText,
                "SaveContinueCheckpointAtStageStart(runSetup)",
                "StoryContinueCheckpointService.SaveStageStartCheckpoint");

        if (!savesCheckpointAtStageStart)
        {
            report.AddError(
                "CCV005",
                StageManagerPath,
                FindLineNumber(stageManagerText, "public void BeginStage()"),
                "BeginStage does not appear to save a Continue checkpoint at stage start. Target flow requires Stage 0 and Stage 1 start to become Continue points.");
        }

        string saveProgressBody = ExtractMethodBody(stageManagerText, "SaveProgress");
        if (!savesCheckpointAtStageStart && ContainsAll(saveProgressBody, "MarkCleared", "Save("))
        {
            report.AddWarning(
                "CCV006",
                StageManagerPath,
                FindLineNumber(stageManagerText, "private void SaveProgress()"),
                "Current stage save signal is tied to clear progress. This is not enough for Continue points that must exist before clear.");
        }
    }

    private static void ValidateDefeatResumeRoute(ValidationReport report, string defeatUiText)
    {
        if (string.IsNullOrEmpty(defeatUiText)) return;

        if (ContainsAll(defeatUiText, "OnClickRetry", "GetActiveScene", "LoadScene(active.name)"))
        {
            report.AddWarning(
                "CCV007",
                DefeatUiPath,
                FindLineNumber(defeatUiText, "private void OnClickRetry()"),
                "Defeat retry reloads the active scene. Target flow says death before clear should restart from the saved Continue checkpoint for that stage.");
        }
    }

    private static void ValidateClearRoute(ValidationReport report, string clearButtonText)
    {
        if (string.IsNullOrEmpty(clearButtonText)) return;

        if (ContainsAny(clearButtonText, "SceneManager.LoadScene(\"Scene_Boot\")", "homeSceneName = \"Scene_Boot\""))
        {
            report.AddWarning(
                "CCV008",
                ClearButtonPath,
                FindFirstLineNumber(clearButtonText, "SceneManager.LoadScene(\"Scene_Boot\")", "homeSceneName = \"Scene_Boot\""),
                "Clear UI routes back to Scene_Boot. Story clear flow still needs explicit Stage 0 -> Story Scene -> Stage 1 -> Story Lobby checkpoints.");
        }
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

    private static string ExtractMethodBody(string text, string methodName)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(methodName)) return string.Empty;

        int methodIndex = text.IndexOf(methodName, StringComparison.Ordinal);
        if (methodIndex < 0) return string.Empty;

        int openBraceIndex = text.IndexOf('{', methodIndex);
        if (openBraceIndex < 0) return string.Empty;

        int depth = 0;
        for (int i = openBraceIndex; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                depth++;
            }
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return text.Substring(openBraceIndex, i - openBraceIndex + 1);
            }
        }

        return string.Empty;
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
            sb.AppendLine("Continue Checkpoint Validator");
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
