#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class SaveCompatibilityValidator
{
    private const string MenuPath = "Tools/Honryeom/Validation/Save Compatibility Validator";
    private const string ScriptRoot = "Assets/_Game/Scripts";
    private const string PlayerSaveDataPath = "Assets/_Game/Scripts/Core/Save/PlayerSaveData2D.cs";
    private const string SaveManagerPath = "Assets/_Game/Scripts/Core/Save/SaveManager2D.cs";
    private const string SaveKeysPath = "Assets/_Game/Scripts/Core/Save/SaveKeys2D.cs";
    private const string JsonIOPath = "Assets/_Game/Scripts/Core/IO/JsonIO2D.cs";
    private const string MetaProfilePath = "Assets/_Game/Scripts/Meta/Save/MetaProfileSaveData2D.cs";
    private const string EquipmentSavePath = "Assets/_Game/Scripts/Meta/Save/Characterequipmentsavedata2d.cs";
    private const string StageProgressPath = "Assets/_Game/Scripts/Stage/StageProgressSaveData.cs";
    private const string FormationSavePath = "Assets/_Game/Scripts/Meta/Formation/FormationSaveData2D.cs";
    private const string CurrencyManagerPath = "Assets/_Game/Scripts/UI/ClearUI/CurrencyManager.cs";
    private const string PlayerSpiritPath = "Assets/_Game/Scripts/UI/HUD/PlayerSpirit2D.cs";
    private const string MetaWalletPath = "Assets/_Game/Scripts/Meta/Runtime/MetaWalletService2D.cs";
    private const string WeaponSaveSystemPath = "Assets/_Game/Scripts/Skill/WeaponSaveSystem.cs";
    private const int ExpectedTalismanSlots = 6;

    [MenuItem(MenuPath)]
    public static void RunFromMenu()
    {
        ValidationReport report = Run();
        Debug.Log(report.ToConsoleText());

        if (report.ErrorCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Save Compatibility Validator",
                $"Failed: {report.ErrorCount} error(s), {report.WarningCount} warning(s).\nSee Console for details.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Save Compatibility Validator",
                $"Passed: {report.WarningCount} warning(s).",
                "OK");
        }
    }

    public static ValidationReport Run()
    {
        ValidationReport report = new ValidationReport();

        ValidateMainSaveSchema(report);
        ValidateMigrationPolicy(report);
        ValidateMetaProfileSchema(report);
        ValidateStoryContinueCompatibility(report);
        ValidateTalismanSlotMigrationRisk(report);
        ValidateCurrencyPersistence(report);
        ValidateMultipleSaveStores(report);
        ValidateIOPolicy(report);

        report.Sort();
        return report;
    }

    private static void ValidateMainSaveSchema(ValidationReport report)
    {
        string playerSaveText = ReadAssetText(PlayerSaveDataPath);
        string saveKeysText = ReadAssetText(SaveKeysPath);

        if (string.IsNullOrEmpty(playerSaveText))
        {
            report.AddError("SCV001", PlayerSaveDataPath, 0, "PlayerSaveData2D script could not be read.");
            return;
        }

        if (!ContainsOrdinal(playerSaveText, "public int version"))
        {
            report.AddError("SCV002", PlayerSaveDataPath, 0, "PlayerSaveData2D has no version field. Future save migrations cannot be tracked safely.");
        }

        if (!ContainsOrdinal(playerSaveText, "metaProfile"))
        {
            report.AddError("SCV003", PlayerSaveDataPath, 0, "PlayerSaveData2D has no metaProfile field. Story/Casual shared progression needs one official root.");
        }

        if (ContainsOrdinal(playerSaveText, "version = 2") && !ContainsAny(playerSaveText, "CurrentVersion", "SaveVersion", "SchemaVersion"))
        {
            report.AddWarning(
                "SCV004",
                PlayerSaveDataPath,
                FindLineNumber(playerSaveText, "version = 2"),
                "Save version is hardcoded in multiple places instead of one named current-version constant.");
        }

        if (string.IsNullOrEmpty(saveKeysText) || !ContainsOrdinal(saveKeysText, "PlayerSaveFile"))
        {
            report.AddError("SCV005", SaveKeysPath, 0, "SaveKeys2D has no PlayerSaveFile constant.");
        }
    }

    private static void ValidateMigrationPolicy(ValidationReport report)
    {
        string playerSaveText = ReadAssetText(PlayerSaveDataPath);
        string saveManagerText = ReadAssetText(SaveManagerPath);

        if (string.IsNullOrEmpty(saveManagerText))
        {
            report.AddError("SCV010", SaveManagerPath, 0, "SaveManager2D script could not be read.");
            return;
        }

        bool hasMigrationSignal = ContainsAny(
            playerSaveText + "\n" + saveManagerText,
            "Migrate",
            "Migration",
            "UpgradeSave",
            "switch (version",
            "switch(version",
            "fromVersion",
            "toVersion");

        if (!hasMigrationSignal)
        {
            report.AddError(
                "SCV011",
                SaveManagerPath,
                FindLineNumber(saveManagerText, "Data.EnsureDefaults"),
                "No explicit save migration policy was found. Adding Soul, Continue checkpoints, or shrinking talisman slots can silently rewrite old saves without controlled migration.");
        }

        if (ContainsOrdinal(playerSaveText, "if (version < 2) version = 2") && !hasMigrationSignal)
        {
            report.AddWarning(
                "SCV012",
                PlayerSaveDataPath,
                FindLineNumber(playerSaveText, "version < 2"),
                "EnsureDefaults bumps old saves to version 2 without recording which migrations actually ran.");
        }

        if (!ContainsOrdinal(saveManagerText, "Data.EnsureDefaults()"))
        {
            report.AddError("SCV013", SaveManagerPath, 0, "SaveManager2D does not clearly call EnsureDefaults after load and before save.");
        }
    }

    private static void ValidateMetaProfileSchema(ValidationReport report)
    {
        string metaText = ReadAssetText(MetaProfilePath);
        if (string.IsNullOrEmpty(metaText))
        {
            report.AddError("SCV020", MetaProfilePath, 0, "MetaProfileSaveData2D script could not be read.");
            return;
        }

        if (!ContainsOrdinal(metaText, "public int nyang"))
            report.AddError("SCV021", MetaProfilePath, 0, "MetaProfileSaveData2D has no persistent Nyang field.");

        if (!ContainsAny(metaText, "public int soul", "public int spirit", "public int yeonghon", "public int hon"))
        {
            report.AddError(
                "SCV022",
                MetaProfilePath,
                0,
                "MetaProfileSaveData2D has no persistent Soul/Spirit field. Target economy requires Nyang and Soul to be shared by Story/Casual.");
        }

        if (!ContainsOrdinal(metaText, "public StageProgressSaveData stageProgress"))
        {
            report.AddError("SCV023", MetaProfilePath, 0, "MetaProfileSaveData2D has no stageProgress field.");
        }

        if (!ContainsOrdinal(metaText, "public FormationSaveData2D formation"))
        {
            report.AddError("SCV024", MetaProfilePath, 0, "MetaProfileSaveData2D has no formation field for main/support squad choices.");
        }

        if (!ContainsOrdinal(metaText, "public CharacterEquipmentCollectionSaveData equipment"))
        {
            report.AddError("SCV025", MetaProfilePath, 0, "MetaProfileSaveData2D has no equipment/talisman save field.");
        }

        if (!ContainsAny(metaText, "public int version", "schemaVersion", "metaVersion"))
        {
            report.AddWarning(
                "SCV026",
                MetaProfilePath,
                0,
                "MetaProfileSaveData2D has no nested version. Large meta-only migrations must rely on the root save version.");
        }
    }

    private static void ValidateStoryContinueCompatibility(ValidationReport report)
    {
        string metaText = ReadAssetText(MetaProfilePath);
        string stageText = ReadAssetText(StageProgressPath);

        if (string.IsNullOrEmpty(stageText))
        {
            report.AddError("SCV030", StageProgressPath, 0, "StageProgressSaveData script could not be read.");
            return;
        }

        bool hasContinueSignal = ContainsAny(
            metaText + "\n" + stageText,
            "continueCheckpoint",
            "ContinueCheckpoint",
            "checkpointScene",
            "checkpointStage",
            "StoryCheckpoint",
            "StoryContinue",
            "resumePoint",
            "ResumePoint");

        if (!hasContinueSignal)
        {
            report.AddError(
                "SCV031",
                StageProgressPath,
                0,
                "No explicit Story Continue checkpoint save field was found. Target flow requires Stage 0 start, Stage 1 start, and Story Lobby entry to be resumable.");
        }

        if (ContainsAll(stageText, "clearedStages", "maxReachedStage") && !hasContinueSignal)
        {
            report.AddWarning(
                "SCV032",
                StageProgressPath,
                FindLineNumber(stageText, "maxReachedStage"),
                "StageProgressSaveData tracks cleared/max reached stages, but not the current Continue point.");
        }
    }

    private static void ValidateTalismanSlotMigrationRisk(ValidationReport report)
    {
        string equipmentText = ReadAssetText(EquipmentSavePath);
        if (string.IsNullOrEmpty(equipmentText))
        {
            report.AddError("SCV040", EquipmentSavePath, 0, "Character equipment save data script could not be read.");
            return;
        }

        int maxSlots = ExtractConstInt(equipmentText, "MaxSlots");
        if (maxSlots > 0 && maxSlots != ExpectedTalismanSlots)
        {
            report.AddError(
                "SCV041",
                EquipmentSavePath,
                FindLineNumber(equipmentText, "MaxSlots"),
                $"Target talisman slot count is {ExpectedTalismanSlots}, but saved equipment MaxSlots is {maxSlots}. This needs an explicit migration decision for old 8-slot saves.");
        }

        if (ContainsOrdinal(equipmentText, "new List<string>(8)") || ContainsOrdinal(equipmentText, "MaxSlots = 8"))
        {
            report.AddError(
                "SCV042",
                EquipmentSavePath,
                FindLineNumber(equipmentText, "slotItemIds"),
                "Saved equipment slots still encode an 8-slot layout. Reducing to 6 slots without migration can lose or orphan equipped items.");
        }

        if (!ContainsAny(equipmentText, "Trim", "Migrate", "Normalize", "RemoveRange"))
        {
            report.AddWarning(
                "SCV043",
                EquipmentSavePath,
                0,
                "No slot normalization or migration helper was found for equipment/talisman save data.");
        }
    }

    private static void ValidateCurrencyPersistence(ValidationReport report)
    {
        string currencyManagerText = ReadAssetText(CurrencyManagerPath);
        string playerSpiritText = ReadAssetText(PlayerSpiritPath);
        string metaWalletText = ReadAssetText(MetaWalletPath);

        if (ContainsAll(currencyManagerText, "PlayerPrefs.GetInt", "Currency_Nyang", "Currency_Spirit"))
        {
            report.AddError(
                "SCV050",
                CurrencyManagerPath,
                FindLineNumber(currencyManagerText, "Currency_Nyang"),
                "CurrencyManager persists Nyang/Spirit through PlayerPrefs, separate from SaveManager2D. Story/Casual shared currency should have one owner.");
        }

        if (ContainsAll(playerSpiritText, "currentSpirit", "AddSpirit", "SpendSpirit")
            && !ContainsAny(playerSpiritText, "SaveManager2D", "MetaProfileSaveData2D", "PlayerPrefs"))
        {
            report.AddWarning(
                "SCV051",
                PlayerSpiritPath,
                FindLineNumber(playerSpiritText, "currentSpirit"),
                "PlayerSpirit2D appears runtime-only. If this represents Soul, it is not persistent through the official save root.");
        }

        if (ContainsOrdinal(metaWalletText, "meta.nyang") && !ContainsAny(metaWalletText, "soul", "spirit", "yeonghon", "hon"))
        {
            report.AddWarning(
                "SCV052",
                MetaWalletPath,
                FindLineNumber(metaWalletText, "meta.nyang"),
                "MetaWalletService2D owns Nyang only. There is no matching official Soul wallet service yet.");
        }
    }

    private static void ValidateMultipleSaveStores(ValidationReport report)
    {
        List<string> scriptFiles = FindScriptFiles(ScriptRoot);
        List<string> playerPrefsWriters = new List<string>();
        List<string> persistentJsonWriters = new List<string>();

        foreach (string file in scriptFiles)
        {
            string normalized = NormalizePath(file);
            if (normalized.Contains("/Editor/")) continue;

            string text = File.ReadAllText(file);
            if (ContainsAny(text, "PlayerPrefs.SetInt", "PlayerPrefs.SetFloat", "PlayerPrefs.SetString"))
                playerPrefsWriters.Add(ToAssetPath(file));

            if (ContainsAny(text, "File.WriteAllText", "TrySaveToPersistent"))
                persistentJsonWriters.Add(ToAssetPath(file));
        }

        foreach (string path in playerPrefsWriters)
        {
            if (path.EndsWith("GameSettingsRuntime.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            report.AddWarning(
                "SCV060",
                path,
                0,
                "Runtime PlayerPrefs writer found outside settings. Verify this is not progression/currency data that should live in player_save.json.");
        }

        foreach (string path in persistentJsonWriters)
        {
            if (path.EndsWith("JsonIO2D.cs", StringComparison.OrdinalIgnoreCase)) continue;
            if (path.EndsWith("JsonManager2D.cs", StringComparison.OrdinalIgnoreCase)) continue;
            if (path.EndsWith("SaveManager2D.cs", StringComparison.OrdinalIgnoreCase)) continue;

            report.AddWarning(
                "SCV061",
                path,
                0,
                "Additional persistent JSON writer found. Multiple save files are acceptable only if ownership and migration are documented.");
        }

        if (File.Exists(Path.GetFullPath(WeaponSaveSystemPath)))
        {
            string weaponSaveText = ReadAssetText(WeaponSaveSystemPath);
            if (ContainsOrdinal(weaponSaveText, "weapon_save.json"))
            {
                report.AddWarning(
                    "SCV062",
                    WeaponSaveSystemPath,
                    FindLineNumber(weaponSaveText, "weapon_save.json"),
                    "WeaponSaveSystem writes a separate weapon_save.json outside SaveManager2D. Verify whether this is legacy or still part of release save compatibility.");
            }
        }
    }

    private static void ValidateIOPolicy(ValidationReport report)
    {
        string jsonIOText = ReadAssetText(JsonIOPath);
        string formationText = ReadAssetText(FormationSavePath);

        if (string.IsNullOrEmpty(jsonIOText))
        {
            report.AddError("SCV070", JsonIOPath, 0, "JsonIO2D script could not be read.");
            return;
        }

        if (ContainsOrdinal(jsonIOText, "File.WriteAllText") && !ContainsAny(jsonIOText, ".bak", "backup", "Replace(", "Move("))
        {
            report.AddWarning(
                "SCV071",
                JsonIOPath,
                FindLineNumber(jsonIOText, "File.WriteAllText"),
                "Save writes are not obviously atomic and no backup file policy was found. A crash during save can corrupt player_save.json.");
        }

        if (ContainsAll(formationText, "support1Id", "mainId", "support2Id") && ContainsOrdinal(formationText, "CreateDefault()"))
        {
            report.AddWarning(
                "SCV072",
                FormationSavePath,
                FindLineNumber(formationText, "CreateDefault"),
                "Formation default allows empty squad IDs. This is fine for first boot, but run start must validate it before creating RunSetup.");
        }
    }

    private static List<string> FindScriptFiles(string root)
    {
        List<string> result = new List<string>();
        string fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot))
            return result;

        string[] files = Directory.GetFiles(fullRoot, "*.cs", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.Ordinal);
        result.AddRange(files);
        return result;
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

    private static string ToAssetPath(string fullPath)
    {
        string root = NormalizePath(Path.GetFullPath("."));
        string normalized = NormalizePath(fullPath);
        if (normalized.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
            return normalized.Substring(root.Length + 1);

        return normalized;
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
            sb.AppendLine("Save Compatibility Validator");
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
