#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using _Game.LevelUp;
using _Game.Skills;
using UnityEditor;
using UnityEngine;

public static class CharacterSquadValidator
{
    private const string MenuPath = "Tools/Honryeom/Validation/Character Squad Validator";
    private const string CharacterRoot = "Assets/_Game/Data/Character";
    private const string CharacterSkillRoot = "Assets/_Game/Data/Skills/Character";
    private const string ScriptRoot = "Assets/_Game/Scripts";
    private const string CharacterDefinitionPath = "Assets/_Game/Scripts/SO/Characters/CharacterDefinitionSO.cs";
    private const string FormationSaveDataPath = "Assets/_Game/Scripts/Meta/Formation/FormationSaveData2D.cs";
    private const string SquadFormationPath = "Assets/_Game/Scripts/UI/Squad/SquadFormationController.cs";
    private const string LevelUpCardGeneratorPath = "Assets/_Game/Scripts/LevelUp/Levelupcardgenerator.cs";
    private const string SquadRuntimeBattlePath = "Assets/_Game/Scripts/Core/Squad/SquadRuntimeBattleBootstrap2D.cs";
    private const string SupportUltimateControllerPath = "Assets/_Game/Scripts/Ultimate/Supportultimatecontroller2d.cs";

    [MenuItem(MenuPath)]
    public static void RunFromMenu()
    {
        ValidationReport report = Run();
        Debug.Log(report.ToConsoleText());

        if (report.ErrorCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Character Squad Validator",
                $"Failed: {report.ErrorCount} error(s), {report.WarningCount} warning(s).\nSee Console for details.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Character Squad Validator",
                $"Passed: {report.WarningCount} warning(s).",
                "OK");
        }
    }

    public static ValidationReport Run()
    {
        ValidationReport report = new ValidationReport();

        List<CharacterDefinitionSO> characters = LoadAssets<CharacterDefinitionSO>(CharacterRoot);
        List<CharacterCatalogSO> catalogs = LoadAssets<CharacterCatalogSO>(CharacterRoot);
        List<CharacterSkillDefinitionSO> characterSkills = LoadAssets<CharacterSkillDefinitionSO>(CharacterSkillRoot);
        List<CharacterSkillSetSO> characterSkillSets = LoadAssets<CharacterSkillSetSO>(CharacterSkillRoot);

        ValidateCharacterCatalogs(report, characters, catalogs);
        ValidateCharacterDefinitions(report, characters);
        ValidateExclusiveSkills(report, characters, characterSkills, characterSkillSets);
        ValidateUniquePassiveOwnership(report);
        ValidateSquadDataFlow(report);

        report.Sort();
        return report;
    }

    private static void ValidateCharacterCatalogs(
        ValidationReport report,
        List<CharacterDefinitionSO> characters,
        List<CharacterCatalogSO> catalogs)
    {
        if (characters.Count == 0)
        {
            report.AddError("CSV001", CharacterRoot, 0, "No CharacterDefinitionSO assets were found.");
            return;
        }

        Dictionary<string, CharacterDefinitionSO> allById = new Dictionary<string, CharacterDefinitionSO>(StringComparer.OrdinalIgnoreCase);
        foreach (CharacterDefinitionSO character in characters)
        {
            string id = character != null ? character.CharacterId : string.Empty;
            string path = GetAssetPath(character);

            if (string.IsNullOrWhiteSpace(id))
            {
                report.AddError("CSV002", path, 0, "CharacterDefinitionSO has an empty CharacterId.");
                continue;
            }

            if (allById.ContainsKey(id))
            {
                report.AddError("CSV003", path, 0, $"Duplicate CharacterId found: {id}");
                continue;
            }

            allById.Add(id, character);
        }

        if (catalogs.Count == 0)
        {
            report.AddError("CSV004", CharacterRoot, 0, "No CharacterCatalogSO asset was found.");
            return;
        }

        foreach (CharacterCatalogSO catalog in catalogs)
        {
            string catalogPath = GetAssetPath(catalog);
            HashSet<string> catalogIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            IReadOnlyList<CharacterDefinitionSO> catalogCharacters = catalog.Characters;
            if (catalogCharacters == null || catalogCharacters.Count == 0)
            {
                report.AddError("CSV005", catalogPath, 0, "CharacterCatalogSO has no characters.");
                continue;
            }

            for (int i = 0; i < catalogCharacters.Count; i++)
            {
                CharacterDefinitionSO character = catalogCharacters[i];
                if (character == null)
                {
                    report.AddError("CSV006", catalogPath, 0, $"CharacterCatalogSO has a null character reference at index {i}.");
                    continue;
                }

                if (!catalogIds.Add(character.CharacterId))
                {
                    report.AddError("CSV007", catalogPath, 0, $"CharacterCatalogSO contains duplicate CharacterId: {character.CharacterId}");
                }
            }

            foreach (string id in allById.Keys)
            {
                if (catalogIds.Contains(id)) continue;

                report.AddError(
                    "CSV008",
                    catalogPath,
                    0,
                    $"CharacterDefinitionSO exists but is not registered in the catalog: {id}");
            }
        }
    }

    private static void ValidateCharacterDefinitions(
        ValidationReport report,
        List<CharacterDefinitionSO> characters)
    {
        foreach (CharacterDefinitionSO character in characters)
        {
            if (character == null) continue;

            string path = GetAssetPath(character);
            string label = string.IsNullOrWhiteSpace(character.CharacterId) ? path : character.CharacterId;

            if (string.IsNullOrWhiteSpace(character.DisplayName))
                report.AddError("CSV009", path, 0, $"Character '{label}' has no display name.");

            if (character.Attribute == CharacterAttributeKind.None)
                report.AddWarning("CSV010", path, 0, $"Character '{label}' has no attribute. Support attribute effects may not work.");

            if (string.IsNullOrWhiteSpace(character.BasicSkillId))
                report.AddError("CSV011", path, 0, $"Character '{label}' has no BasicSkillId.");

            if (character.StartingSkill == null)
                report.AddError("CSV012", path, 0, $"Character '{label}' has no starting basic skill config.");

            if (character.BasicSkillIcon == null)
                report.AddWarning("CSV013", path, 0, $"Character '{label}' has no basic skill icon.");

            if (string.IsNullOrWhiteSpace(character.UltimateSkillId))
                report.AddError("CSV014", path, 0, $"Character '{label}' has no UltimateSkillId.");

            if (character.UltimateData == null)
                report.AddError("CSV015", path, 0, $"Character '{label}' has no UltimateData.");

            if (character.UltimateResolverPrefab == null)
                report.AddError("CSV016", path, 0, $"Character '{label}' has no ultimate resolver prefab.");

            if (character.UltimateSkillIcon == null)
                report.AddWarning("CSV017", path, 0, $"Character '{label}' has no ultimate icon.");

            if (character.SupportVisualPrefab == null)
                report.AddWarning("CSV018", path, 0, $"Character '{label}' has no support visual prefab for T-key support ultimate.");

            if (character.BaseStatProfile == null)
                report.AddWarning("CSV019", path, 0, $"Character '{label}' has no base stat profile.");
        }
    }

    private static void ValidateExclusiveSkills(
        ValidationReport report,
        List<CharacterDefinitionSO> characters,
        List<CharacterSkillDefinitionSO> characterSkills,
        List<CharacterSkillSetSO> characterSkillSets)
    {
        Dictionary<string, List<CharacterSkillDefinitionSO>> skillsByOwner = new Dictionary<string, List<CharacterSkillDefinitionSO>>(StringComparer.OrdinalIgnoreCase);

        foreach (CharacterSkillDefinitionSO skill in characterSkills)
        {
            if (skill == null) continue;
            string path = GetAssetPath(skill);

            if (string.IsNullOrWhiteSpace(skill.SkillId))
                report.AddError("CSV020", path, 0, "CharacterSkillDefinitionSO has an empty SkillId.");

            if (string.IsNullOrWhiteSpace(skill.OwnerCharacterId))
            {
                report.AddError("CSV021", path, 0, $"Character skill '{skill.SkillId}' has no ownerCharacterId.");
                continue;
            }

            if (!skillsByOwner.TryGetValue(skill.OwnerCharacterId, out List<CharacterSkillDefinitionSO> owned))
            {
                owned = new List<CharacterSkillDefinitionSO>();
                skillsByOwner.Add(skill.OwnerCharacterId, owned);
            }

            owned.Add(skill);
        }

        Dictionary<string, CharacterSkillSetSO> setByCharacterId = new Dictionary<string, CharacterSkillSetSO>(StringComparer.OrdinalIgnoreCase);
        foreach (CharacterSkillSetSO set in characterSkillSets)
        {
            if (set == null) continue;
            string path = GetAssetPath(set);

            if (string.IsNullOrWhiteSpace(set.CharacterId))
            {
                report.AddError("CSV022", path, 0, "CharacterSkillSetSO has an empty CharacterId.");
                continue;
            }

            if (setByCharacterId.ContainsKey(set.CharacterId))
            {
                report.AddError("CSV023", path, 0, $"Duplicate CharacterSkillSetSO for CharacterId: {set.CharacterId}");
                continue;
            }

            setByCharacterId.Add(set.CharacterId, set);
        }

        foreach (CharacterDefinitionSO character in characters)
        {
            if (character == null || string.IsNullOrWhiteSpace(character.CharacterId)) continue;

            string id = character.CharacterId;
            string path = GetAssetPath(character);
            int ownerSkillCount = skillsByOwner.TryGetValue(id, out List<CharacterSkillDefinitionSO> ownedSkills)
                ? ownedSkills.Count
                : 0;

            if (ownerSkillCount != 2)
            {
                report.AddError(
                    "CSV024",
                    path,
                    0,
                    $"Character '{id}' should have exactly 2 exclusive level-up skills, but found {ownerSkillCount} CharacterSkillDefinitionSO assets by ownerCharacterId.");
            }

            if (!setByCharacterId.TryGetValue(id, out CharacterSkillSetSO set))
            {
                report.AddWarning(
                    "CSV025",
                    path,
                    0,
                    $"Character '{id}' has no CharacterSkillSetSO. Exclusive skill ownership may be split between scene inspector arrays and loose skill assets.");
                continue;
            }

            int setSkillCount = set.Skills != null ? set.Skills.Count : 0;
            if (setSkillCount != 2)
            {
                report.AddError(
                    "CSV026",
                    GetAssetPath(set),
                    0,
                    $"CharacterSkillSetSO for '{id}' should contain exactly 2 skills, but contains {setSkillCount}.");
            }
        }
    }

    private static void ValidateUniquePassiveOwnership(ValidationReport report)
    {
        string characterDefinitionText = ReadAssetText(CharacterDefinitionPath);
        if (string.IsNullOrEmpty(characterDefinitionText))
        {
            report.AddWarning("CSV027", CharacterDefinitionPath, 0, "CharacterDefinitionSO script could not be read for unique passive ownership check.");
            return;
        }

        if (!ContainsAny(characterDefinitionText, "uniquePassive", "exclusivePassive", "passiveSkill", "CharacterPassive", "PassiveDefinition"))
        {
            report.AddError(
                "CSV027",
                CharacterDefinitionPath,
                0,
                "CharacterDefinitionSO has no obvious field for each character's unique starting passive. Target rule requires 1 unique passive per character.");
        }
    }

    private static void ValidateSquadDataFlow(ValidationReport report)
    {
        string formationSaveText = ReadAssetText(FormationSaveDataPath);
        string squadFormationText = ReadAssetText(SquadFormationPath);
        string levelUpGeneratorText = ReadAssetText(LevelUpCardGeneratorPath);
        string squadBattleText = ReadAssetText(SquadRuntimeBattlePath);
        string supportUltimateText = ReadAssetText(SupportUltimateControllerPath);

        if (!ContainsAll(formationSaveText, "support1Id", "mainId", "support2Id"))
        {
            report.AddError(
                "CSV028",
                FormationSaveDataPath,
                0,
                "FormationSaveData2D does not clearly store main/support1/support2 IDs.");
        }

        if (ContainsAll(squadFormationText, "requireMainToStart", "SquadLoadoutRuntime.Current.HasMain")
            && !ContainsAny(squadFormationText, "Support1Id", "Support2Id", "support1Id", "support2Id"))
        {
            report.AddWarning(
                "CSV029",
                SquadFormationPath,
                FindLineNumber(squadFormationText, "requireMainToStart"),
                "SquadFormationController appears to require only a main character before start. Target squad rules should define whether support slots are required or intentionally optional.");
        }

        if (ContainsAll(levelUpGeneratorText, "SquadLoadoutRuntime.MainId", "SquadLoadoutRuntime.Support1Id", "SquadLoadoutRuntime.Support2Id"))
        {
            report.AddWarning(
                "CSV030",
                LevelUpCardGeneratorPath,
                FindLineNumber(levelUpGeneratorText, "SquadLoadoutRuntime.MainId"),
                "Exclusive skill card pool reads squad directly from SquadLoadoutRuntime. This should eventually come from validated RunSetup.");
        }

        if (ContainsAll(squadBattleText, "SquadLoadoutRuntime.LoadFromSave", "SquadLoadoutRuntime.Current"))
        {
            report.AddWarning(
                "CSV031",
                SquadRuntimeBattlePath,
                FindLineNumber(squadBattleText, "SquadLoadoutRuntime.LoadFromSave"),
                "Battle squad bootstrap reads save/runtime globals directly. RunSetup should become the official squad input for Scene_Game.");
        }

        if (!ContainsAll(supportUltimateText, "Support1", "Support2", "RunSingleSupportUltimate"))
        {
            report.AddWarning(
                "CSV032",
                SupportUltimateControllerPath,
                0,
                "Support ultimate controller does not obviously execute support1 and support2 ultimates.");
        }
    }

    private static List<T> LoadAssets<T>(string root) where T : UnityEngine.Object
    {
        List<T> result = new List<T>();
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { root });
        Array.Sort(guids, StringComparer.Ordinal);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                result.Add(asset);
        }

        return result;
    }

    private static string GetAssetPath(UnityEngine.Object asset)
    {
        if (asset == null) return string.Empty;
        return NormalizePath(AssetDatabase.GetAssetPath(asset));
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
            sb.AppendLine("Character Squad Validator");
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
