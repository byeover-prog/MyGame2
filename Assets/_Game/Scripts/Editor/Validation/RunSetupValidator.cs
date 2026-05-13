#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class RunSetupValidator
{
    private const string MenuPath = "Tools/Honryeom/Validation/Run Setup Validator";
    private const string ScriptRoot = "Assets/_Game/Scripts";
    private const string RunConfigPath = "Assets/_Game/Scripts/Run_Scripts/RunConfigSO.cs";
    private const string RunConfigHolderPath = "Assets/_Game/Scripts/Run_Scripts/RunConfigHolder.cs";
    private const string StageManagerPath = "Assets/_Game/Scripts/Stage/StageManager.cs";
    private const string GameManagerPath = "Assets/_Game/Scripts/Core/Gamemanager2d.cs";
    private const string SquadFormationPath = "Assets/_Game/Scripts/UI/Squad/SquadFormationController.cs";
    private const string SquadRuntimeBattlePath = "Assets/_Game/Scripts/Core/Squad/SquadRuntimeBattleBootstrap2D.cs";
    private const string LevelUpCardGeneratorPath = "Assets/_Game/Scripts/LevelUp/Levelupcardgenerator.cs";

    private static readonly string[] RunSetupOwnerTokens =
    {
        "RunSetup",
        "RunStartContext",
        "RunLaunchContext",
        "RunSessionSetup",
        "RunSnapshot"
    };

    private static readonly string[] RequiredRunSetupConceptTokens =
    {
        "Story",
        "Casual",
        "stage",
        "mainId",
        "support1Id",
        "support2Id"
    };

    [MenuItem(MenuPath)]
    public static void RunFromMenu()
    {
        ValidationReport report = Run();
        Debug.Log(report.ToConsoleText());

        if (report.ErrorCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Run Setup Validator",
                $"Failed: {report.ErrorCount} error(s), {report.WarningCount} warning(s).\nSee Console for details.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Run Setup Validator",
                $"Passed: {report.WarningCount} warning(s).",
                "OK");
        }
    }

    public static ValidationReport Run()
    {
        ValidationReport report = new ValidationReport();

        string runtimeText = ReadAllRuntimeScriptText();
        string runConfigText = ReadAssetText(RunConfigPath);
        string runConfigHolderText = ReadAssetText(RunConfigHolderPath);
        string stageManagerText = ReadAssetText(StageManagerPath);
        string gameManagerText = ReadAssetText(GameManagerPath);
        string squadFormationText = ReadAssetText(SquadFormationPath);
        string squadBattleText = ReadAssetText(SquadRuntimeBattlePath);
        string levelUpCardGeneratorText = ReadAssetText(LevelUpCardGeneratorPath);

        ValidateRunSetupOwner(report, runtimeText, runConfigText);
        ValidateRunConfigScope(report, runConfigText, runConfigHolderText);
        ValidateStageSelection(report, stageManagerText);
        ValidateGameStartContract(report, gameManagerText, stageManagerText);
        ValidateFormationStartContract(report, squadFormationText);
        ValidateBattleConsumers(report, squadBattleText, levelUpCardGeneratorText);

        report.Sort();
        return report;
    }

    private static void ValidateRunSetupOwner(ValidationReport report, string runtimeText, string runConfigText)
    {
        if (ContainsAny(runtimeText, RunSetupOwnerTokens))
        {
            if (ContainsAll(runtimeText, RequiredRunSetupConceptTokens))
                return;

            report.AddWarning(
                "RSU001",
                ScriptRoot,
                0,
                "A RunSetup-like type exists, but it does not obviously contain mode, stage, and squad identity concepts.");
            return;
        }

        report.AddError(
            "RSU001",
            ScriptRoot,
            0,
            "No explicit RunSetup owner was found. Target structure needs one snapshot for mode, stage/map, squad, talismans, and Continue linkage.");

        if (!string.IsNullOrEmpty(runConfigText))
        {
            report.AddWarning(
                "RSU002",
                RunConfigPath,
                0,
                "RunConfigSO exists, but it appears to be a narrow config asset rather than a full run-start snapshot.");
        }
    }

    private static void ValidateRunConfigScope(
        ValidationReport report,
        string runConfigText,
        string runConfigHolderText)
    {
        if (string.IsNullOrEmpty(runConfigText))
        {
            report.AddWarning("RSU003", RunConfigPath, 0, "RunConfigSO script was not found.");
            return;
        }

        if (ContainsAll(runConfigText, "mode_id", "casual_spawn_curve")
            && !ContainsAny(runConfigText, "stageIndex", "mainId", "support1Id", "support2Id", "talisman", "checkpoint"))
        {
            report.AddWarning(
                "RSU004",
                RunConfigPath,
                FindLineNumber(runConfigText, "public sealed class RunConfigSO"),
                "RunConfigSO currently covers mode/spawn tuning only. It does not own the selected stage, squad, talismans, or Continue checkpoint.");
        }

        if (ContainsAll(runConfigHolderText, "public static RunConfigSO Current", "DontDestroyOnLoad"))
        {
            report.AddWarning(
                "RSU005",
                RunConfigHolderPath,
                FindLineNumber(runConfigHolderText, "public static RunConfigSO Current"),
                "RunConfigHolder exposes static Current config. Static handoff is fragile unless a validated RunSetup is created before Scene_Game loads.");
        }
    }

    private static void ValidateStageSelection(ValidationReport report, string stageManagerText)
    {
        if (ContainsAll(stageManagerText, "static class StageSelectBridge", "SelectedStageIndex", "HasSelection"))
        {
            report.AddError(
                "RSU006",
                StageManagerPath,
                FindLineNumber(stageManagerText, "public static class StageSelectBridge"),
                "Stage selection is passed through StageSelectBridge static state. Run start data should be owned by an explicit RunSetup instead.");
        }
    }

    private static void ValidateGameStartContract(
        ValidationReport report,
        string gameManagerText,
        string stageManagerText)
    {
        string startGameBody = ExtractMethodBody(gameManagerText, "StartGame");
        if (!ContainsAny(startGameBody, RunSetupOwnerTokens))
        {
            report.AddError(
                "RSU007",
                GameManagerPath,
                FindLineNumber(gameManagerText, "public void StartGame()"),
                "GameManager2D.StartGame starts the run without receiving or validating a RunSetup.");
        }

        string beginStageBody = ExtractMethodBody(stageManagerText, "BeginStage");
        if (!ContainsAny(beginStageBody, RunSetupOwnerTokens))
        {
            report.AddWarning(
                "RSU008",
                StageManagerPath,
                FindLineNumber(stageManagerText, "public void BeginStage()"),
                "StageManager2D.BeginStage resolves stage data internally instead of consuming a validated RunSetup stage target.");
        }
    }

    private static void ValidateFormationStartContract(ValidationReport report, string squadFormationText)
    {
        string clickStartBody = ExtractMethodBody(squadFormationText, "OnClickStart");

        if (ContainsAll(clickStartBody, "SquadLoadoutRuntime.Current.HasMain", "SceneManager.LoadScene(nextSceneName)")
            && !ContainsAny(clickStartBody, RunSetupOwnerTokens))
        {
            report.AddError(
                "RSU009",
                SquadFormationPath,
                FindLineNumber(squadFormationText, "private void OnClickStart()"),
                "Squad formation start loads the game scene directly after checking only the main character. It should create or validate RunSetup first.");
        }
    }

    private static void ValidateBattleConsumers(
        ValidationReport report,
        string squadBattleText,
        string levelUpCardGeneratorText)
    {
        if (ContainsAll(squadBattleText, "SquadLoadoutRuntime.LoadFromSave", "SquadLoadoutRuntime.Current"))
        {
            report.AddWarning(
                "RSU010",
                SquadRuntimeBattlePath,
                FindLineNumber(squadBattleText, "public void ApplyRuntimeLoadout()"),
                "Battle bootstrap reads squad from save/runtime globals. This works today, but RunSetup should be the official game-scene input.");
        }

        if (ContainsAll(levelUpCardGeneratorText, "SquadLoadoutRuntime.MainId", "SquadLoadoutRuntime.Support1Id", "SquadLoadoutRuntime.Support2Id"))
        {
            report.AddWarning(
                "RSU011",
                LevelUpCardGeneratorPath,
                FindLineNumber(levelUpCardGeneratorText, "SquadLoadoutRuntime.MainId"),
                "Skill card pool reads squad IDs from global runtime state. Future card grade/support rules will be easier to validate from RunSetup.");
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
            sb.AppendLine("Run Setup Validator");
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
