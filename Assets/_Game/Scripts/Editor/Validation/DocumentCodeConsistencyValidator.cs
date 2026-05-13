#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class DocumentCodeConsistencyValidator
{
    private const string MenuPath = "Tools/Honryeom/Validation/Document Code Consistency Validator";
    private const string DocsRoot = "Assets/_Game/Docs";
    private const string ArchitectureDecisionPath = "Assets/_Game/Docs/ArchitectureDecisionMatrix.md";
    private const string DebugSpecPath = "Assets/_Game/Docs/DebugObjectValidatorSpec.md";
    private const string ArchitectureDiagnosisPath = "Assets/_Game/Docs/ArchitectureDiagnosis.md";
    private const string CurrentFailureMatrixPath = "Assets/_Game/Docs/CurrentFailureMatrix.md";
    private const string Phase1ValidatorSpecPath = "Assets/_Game/Docs/Phase1_ValidatorSpec.md";
    private const string BuildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";
    private const string SceneLobbyPath = "Assets/Scenes/Scene_Lobby.unity";
    private const string SceneBootPath = "Assets/Scenes/Scene_Boot.unity";
    private const string SceneGamePath = "Assets/Scenes/Scene_Game.unity";
    private const string CharacterEquipmentSavePath = "Assets/_Game/Scripts/Meta/Save/Characterequipmentsavedata2d.cs";
    private const string MetaProfilePath = "Assets/_Game/Scripts/Meta/Save/MetaProfileSaveData2D.cs";
    private const string StageProgressPath = "Assets/_Game/Scripts/Stage/StageProgressSaveData.cs";
    private const string CharacterDefinitionPath = "Assets/_Game/Scripts/SO/Characters/CharacterDefinitionSO.cs";
    private const string LevelUpCardGeneratorPath = "Assets/_Game/Scripts/LevelUp/Levelupcardgenerator.cs";
    private const string EquipmentDatabaseAssetPath = "Assets/GameData/EquipmentDatabase.asset";
    private const string EquipmentRoot = "Assets/GameData/Equipments";
    private const string ValidatorRoot = "Assets/_Game/Scripts/Editor/Validation";
    private const int ExpectedTalismanSlots = 6;
    private const int ExpectedGachaItemCount = 44;

    [MenuItem(MenuPath)]
    public static void RunFromMenu()
    {
        ValidationReport report = Run();
        Debug.Log(report.ToConsoleText());

        if (report.ErrorCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Document Code Consistency Validator",
                $"Failed: {report.ErrorCount} error(s), {report.WarningCount} warning(s).\nSee Console for details.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Document Code Consistency Validator",
                $"Passed: {report.WarningCount} warning(s).",
                "OK");
        }
    }

    public static ValidationReport Run()
    {
        ValidationReport report = new ValidationReport();

        ValidateRequiredDocs(report);
        ValidateConfirmedRulesDocumented(report);
        ValidateCodeAgainstConfirmedRules(report);
        ValidateDocClaimsAgainstAssets(report);
        ValidateDebugSpecFreshness(report);
        ValidateValidatorSpecCoverage(report);

        report.Sort();
        return report;
    }

    private static void ValidateRequiredDocs(ValidationReport report)
    {
        if (!Directory.Exists(Path.GetFullPath(DocsRoot)))
        {
            report.AddError("DCC001", DocsRoot, 0, "Docs root does not exist.");
            return;
        }

        RequireDoc(report, "DCC002", ArchitectureDecisionPath, "Architecture decision baseline is missing.");
        RequireDoc(report, "DCC003", ArchitectureDiagnosisPath, "ArchitectureDiagnosis.md is missing. The Phase 1 prompt named it as the baseline diagnosis document.");
        RequireDoc(report, "DCC004", CurrentFailureMatrixPath, "CurrentFailureMatrix.md is missing. Validator findings need one human-readable failure matrix.");
        RequireDoc(report, "DCC005", Phase1ValidatorSpecPath, "Phase1_ValidatorSpec.md is missing. Release validators need one spec document.");
    }

    private static void ValidateConfirmedRulesDocumented(ValidationReport report)
    {
        string architectureText = ReadAssetText(ArchitectureDecisionPath);
        if (string.IsNullOrEmpty(architectureText))
            return;

        RequireText(report, "DCC010", ArchitectureDecisionPath, architectureText, "Story Mode", "Confirmed title flow must document Story Mode.");
        RequireText(report, "DCC011", ArchitectureDecisionPath, architectureText, "Casual Mode", "Confirmed title flow must document Casual Mode.");
        RequireText(report, "DCC012", ArchitectureDecisionPath, architectureText, "Continue", "Confirmed story flow must document Continue/checkpoint behavior.");
        RequireText(report, "DCC013", ArchitectureDecisionPath, architectureText, "Stage 0", "Confirmed Story New Game flow must document Stage 0.");
        if (!ContainsAny(architectureText, "Stage 1", "Stage 0/1", "Stage0/1"))
        {
            report.AddWarning("DCC014", ArchitectureDecisionPath, 0, "Confirmed Story New Game flow must document Stage 1.");
        }
        RequireText(report, "DCC015", ArchitectureDecisionPath, architectureText, "Story Lobby", "Confirmed Story Lobby must be documented.");
        RequireText(report, "DCC016", ArchitectureDecisionPath, architectureText, "Casual Lobby", "Confirmed Casual Lobby must be documented.");
        RequireText(report, "DCC017", ArchitectureDecisionPath, architectureText, "6", "Confirmed talisman slot count of 6 must be documented.");
        RequireText(report, "DCC018", ArchitectureDecisionPath, architectureText, "44", "Confirmed gacha item count of 44 must be documented.");
        RequireText(report, "DCC019", ArchitectureDecisionPath, architectureText, "Soul", "Confirmed Soul currency must be documented.");
        RequireText(report, "DCC020", ArchitectureDecisionPath, architectureText, "RunSetup", "RunSetup target ownership must be documented.");
    }

    private static void ValidateCodeAgainstConfirmedRules(ValidationReport report)
    {
        string equipmentSaveText = ReadAssetText(CharacterEquipmentSavePath);
        int maxSlots = ExtractConstInt(equipmentSaveText, "MaxSlots");
        if (maxSlots > 0 && maxSlots != ExpectedTalismanSlots)
        {
            report.AddError(
                "DCC030",
                CharacterEquipmentSavePath,
                FindLineNumber(equipmentSaveText, "MaxSlots"),
                $"Confirmed rule says talisman slots are {ExpectedTalismanSlots}, but code uses MaxSlots={maxSlots}.");
        }

        string metaText = ReadAssetText(MetaProfilePath);
        if (!ContainsAny(metaText, "public int soul", "public int spirit", "public int yeonghon", "public int hon"))
        {
            report.AddError(
                "DCC031",
                MetaProfilePath,
                0,
                "Confirmed economy has Nyang and Soul, but MetaProfileSaveData2D only persists Nyang.");
        }

        string stageText = ReadAssetText(StageProgressPath);
        if (!ContainsAny(stageText, "continueCheckpoint", "ContinueCheckpoint", "StoryCheckpoint", "resumePoint", "ResumePoint"))
        {
            report.AddError(
                "DCC032",
                StageProgressPath,
                0,
                "Confirmed Continue points are save points, but StageProgressSaveData has no explicit Continue checkpoint field.");
        }

        string characterDefinitionText = ReadAssetText(CharacterDefinitionPath);
        if (!ContainsAny(characterDefinitionText, "uniquePassive", "exclusivePassive", "passiveSkill", "CharacterPassive", "PassiveDefinition"))
        {
            report.AddError(
                "DCC033",
                CharacterDefinitionPath,
                0,
                "Confirmed character contract requires one unique starting passive, but CharacterDefinitionSO has no obvious unique passive field.");
        }

        if (!File.Exists(Path.GetFullPath(EquipmentDatabaseAssetPath)))
        {
            report.AddError(
                "DCC034",
                EquipmentDatabaseAssetPath,
                0,
                "Confirmed gacha pool has 44 items, but no EquipmentDatabase.asset exists to own the release-facing pool.");
        }

        int equipmentCount = CountAssetsByClassIdentifier(EquipmentRoot, "Assembly-CSharp::EquipmentDefinitionSO");
        if (equipmentCount != ExpectedGachaItemCount)
        {
            report.AddError(
                "DCC035",
                EquipmentRoot,
                0,
                $"Confirmed gacha pool has {ExpectedGachaItemCount} items, but found {equipmentCount} EquipmentDefinitionSO asset files.");
        }

        string levelUpText = ReadAssetText(LevelUpCardGeneratorPath);
        if (ContainsAll(levelUpText, "CharacterSkillSet[] characterSkillSets", "FindCharacterSkillSet"))
        {
            report.AddWarning(
                "DCC036",
                LevelUpCardGeneratorPath,
                FindLineNumber(levelUpText, "CharacterSkillSet[] characterSkillSets"),
                "Docs say future skill/card updates should be data-owned and validator-driven, but current exclusive skill pool is still scene-inspector owned.");
        }
    }

    private static void ValidateDocClaimsAgainstAssets(ValidationReport report)
    {
        string architectureText = ReadAssetText(ArchitectureDecisionPath);
        if (string.IsNullOrEmpty(architectureText))
            return;

        if (ContainsOrdinal(architectureText, "`EquipmentDatabaseSO` exists") && !File.Exists(Path.GetFullPath(EquipmentDatabaseAssetPath)))
        {
            report.AddError(
                "DCC040",
                ArchitectureDecisionPath,
                FindLineNumber(architectureText, "`EquipmentDatabaseSO` exists"),
                "ArchitectureDecisionMatrix says EquipmentDatabaseSO exists, but no EquipmentDatabase.asset exists under Assets/GameData.");
        }

        List<string> enabledScenes = ReadEnabledBuildScenes();
        bool docsMentionSceneLobby = ContainsOrdinal(architectureText, "`Scene_Lobby`");
        bool docsMentionSceneBoot = ContainsOrdinal(architectureText, "`Scene_Boot`");
        bool docsMentionSceneGame = ContainsOrdinal(architectureText, "`Scene_Game`");

        if (docsMentionSceneLobby && !enabledScenes.Contains(SceneLobbyPath))
            report.AddWarning("DCC041", ArchitectureDecisionPath, 0, "ArchitectureDecisionMatrix mentions Scene_Lobby as a build scene, but it is not currently enabled.");

        if (docsMentionSceneBoot && !enabledScenes.Contains(SceneBootPath))
            report.AddWarning("DCC042", ArchitectureDecisionPath, 0, "ArchitectureDecisionMatrix mentions Scene_Boot as a build scene, but it is not currently enabled.");

        if (docsMentionSceneGame && !enabledScenes.Contains(SceneGamePath))
            report.AddWarning("DCC043", ArchitectureDecisionPath, 0, "ArchitectureDecisionMatrix mentions Scene_Game as a build scene, but it is not currently enabled.");
    }

    private static void ValidateDebugSpecFreshness(ValidationReport report)
    {
        string debugSpecText = ReadAssetText(DebugSpecPath);
        if (string.IsNullOrEmpty(debugSpecText))
            return;

        if (!ContainsOrdinal(debugSpecText, "## Current Confirmed Findings"))
            return;

        int currentSectionStart = debugSpecText.IndexOf("## Current Confirmed Findings", StringComparison.Ordinal);
        int nextSectionStart = debugSpecText.IndexOf("\n## ", currentSectionStart + 1, StringComparison.Ordinal);
        string currentSectionText = nextSectionStart >= 0
            ? debugSpecText.Substring(currentSectionStart, nextSectionStart - currentSectionStart)
            : debugSpecText.Substring(currentSectionStart);

        Dictionary<string, string> staleBuildSceneClaims = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "UIRaycastProbe", SceneBootPath },
            { "UIButtonPointerProbe", SceneBootPath },
            { "DebugRuntimeHUD", SceneGamePath },
            { "LevelUpRuntimeHardResetOnPlay", SceneGamePath }
        };

        foreach (KeyValuePair<string, string> claim in staleBuildSceneClaims)
        {
            if (!ContainsOrdinal(currentSectionText, claim.Key))
                continue;

            string sceneText = ReadAssetText(claim.Value);
            if (!ContainsOrdinal(sceneText, claim.Key))
            {
                report.AddWarning(
                    "DCC050",
                    DebugSpecPath,
                    FindLineNumber(debugSpecText, claim.Key),
                    $"DebugObjectValidatorSpec still lists '{claim.Key}' as a confirmed build-scene finding, but it was not found in {claim.Value}. Update the doc from confirmed finding to historical/resolved finding.");
            }
        }
    }

    private static void ValidateValidatorSpecCoverage(ValidationReport report)
    {
        string[] requiredValidators =
        {
            "DebugObjectValidator.cs",
            "AssetIntegrityValidator.cs",
            "BuildSceneValidator.cs",
            "StoryFlowValidator.cs",
            "ContinueCheckpointValidator.cs",
            "RunSetupValidator.cs",
            "CharacterSquadValidator.cs",
            "SkillPoolValidator.cs",
            "TalismanGachaValidator.cs",
            "SaveCompatibilityValidator.cs",
            "DocumentCodeConsistencyValidator.cs"
        };

        for (int i = 0; i < requiredValidators.Length; i++)
        {
            string path = Path.Combine(ValidatorRoot, requiredValidators[i]).Replace('\\', '/');
            if (!File.Exists(Path.GetFullPath(path)))
            {
                report.AddError("DCC060", path, 0, $"Required Phase 1 validator is missing: {requiredValidators[i]}");
            }
        }

        if (!File.Exists(Path.GetFullPath(Phase1ValidatorSpecPath)))
        {
            report.AddWarning(
                "DCC061",
                Phase1ValidatorSpecPath,
                0,
                "Validator code exists without the Phase1_ValidatorSpec.md summary document. Collaborators need the human-readable validator contract.");
        }
    }

    private static void RequireDoc(ValidationReport report, string ruleId, string path, string message)
    {
        if (!File.Exists(Path.GetFullPath(path)))
            report.AddError(ruleId, path, 0, message);
    }

    private static void RequireText(ValidationReport report, string ruleId, string path, string text, string required, string message)
    {
        if (!ContainsOrdinalIgnoreCase(text, required))
            report.AddWarning(ruleId, path, 0, message);
    }

    private static List<string> ReadEnabledBuildScenes()
    {
        List<string> result = new List<string>();
        string text = ReadAssetText(BuildSettingsPath);
        if (string.IsNullOrEmpty(text))
            return result;

        MatchCollection matches = Regex.Matches(text, @"enabled:\s*1\s*\r?\n\s*path:\s*(.+)");
        foreach (Match match in matches)
        {
            string path = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(path))
                result.Add(NormalizePath(path));
        }

        return result;
    }

    private static int CountAssetsByClassIdentifier(string root, string classIdentifier)
    {
        string fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot))
            return 0;

        int count = 0;
        string[] files = Directory.GetFiles(fullRoot, "*.asset", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string text = File.ReadAllText(files[i]);
            if (ContainsOrdinal(text, "m_EditorClassIdentifier: " + classIdentifier))
                count++;
        }

        return count;
    }

    private static string ReadAssetText(string assetPath)
    {
        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath)) return null;
        return File.ReadAllText(fullPath);
    }

    private static int ExtractConstInt(string text, string constName)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(constName)) return 0;

        Match match = Regex.Match(text, @"\b" + Regex.Escape(constName) + @"\s*=\s*(\d+)");
        if (!match.Success) return 0;

        return int.TryParse(match.Groups[1].Value, out int value) ? value : 0;
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

    private static bool ContainsOrdinal(string text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern)) return false;
        return text.IndexOf(pattern, StringComparison.Ordinal) >= 0;
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
        private readonly List<Finding> _findings = new List<Finding>(64);

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
            StringBuilder sb = new StringBuilder(8192);
            sb.AppendLine("Document Code Consistency Validator");
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
