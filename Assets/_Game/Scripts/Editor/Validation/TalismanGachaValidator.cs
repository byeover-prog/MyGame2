#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class TalismanGachaValidator
{
    private const string MenuPath = "Tools/Honryeom/Validation/Talisman Gacha Validator";
    private const string AssetRoot = "Assets";
    private const string GameDataRoot = "Assets/GameData";
    private const string ScriptRoot = "Assets/_Game/Scripts";
    private const string SaveDataPath = "Assets/_Game/Scripts/Meta/Save/MetaProfileSaveData2D.cs";
    private const string EquipmentSaveDataPath = "Assets/_Game/Scripts/Meta/Save/Characterequipmentsavedata2d.cs";
    private const string CharacterMetaResolverPath = "Assets/_Game/Scripts/Meta/Runtime/CharacterMetaResolver2D.cs";
    private const string ShopServicePath = "Assets/_Game/Scripts/Meta/Shop/Shopservice.cs";
    private const string ShopDatabasePath = "Assets/_Game/Scripts/Meta/Shop/Shopdatabaseso.cs";
    private const string ShopItemPath = "Assets/_Game/Scripts/Meta/Shop/Shopitemso.cs";
    private const string GachaConfigPath = "Assets/_Game/Scripts/Equipment/Gachaconfigso.cs";
    private const string EquipmentDatabasePath = "Assets/_Game/Scripts/Equipment/Equipmentdatabaseso.cs";
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
                "Talisman Gacha Validator",
                $"Failed: {report.ErrorCount} error(s), {report.WarningCount} warning(s).\nSee Console for details.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Talisman Gacha Validator",
                $"Passed: {report.WarningCount} warning(s).",
                "OK");
        }
    }

    public static ValidationReport Run()
    {
        ValidationReport report = new ValidationReport();

        List<EquipmentDefinitionSO> equipments = LoadAssets<EquipmentDefinitionSO>(AssetRoot);
        List<EquipmentDatabaseSO> databases = LoadAssets<EquipmentDatabaseSO>(AssetRoot);
        List<GachaConfigSO> gachaConfigs = LoadAssets<GachaConfigSO>(AssetRoot);
        List<ShopDatabaseSO> shopDatabases = LoadAssets<ShopDatabaseSO>(AssetRoot);
        List<ShopItemSO> shopItems = LoadAssets<ShopItemSO>(AssetRoot);

        ValidateEquipmentDefinitions(report, equipments);
        ValidateEquipmentDatabase(report, equipments, databases);
        ValidateGachaConfig(report, gachaConfigs);
        ValidateTalismanSlotsAndSave(report);
        ValidateCurrencyOwnership(report);
        ValidateLegacyShopCoexistence(report, equipments, shopDatabases, shopItems);
        ValidateRuntimeWiring(report);

        report.Sort();
        return report;
    }

    private static void ValidateEquipmentDefinitions(ValidationReport report, List<EquipmentDefinitionSO> equipments)
    {
        if (equipments.Count == 0)
        {
            report.AddError("TGV001", AssetRoot, 0, "No EquipmentDefinitionSO assets were found. Target gacha pool requires 44 talisman/equipment items.");
            return;
        }

        if (equipments.Count != ExpectedGachaItemCount)
        {
            report.AddError(
                "TGV002",
                GameDataRoot,
                0,
                $"EquipmentDefinitionSO count should be {ExpectedGachaItemCount}, but found {equipments.Count}.");
        }

        Dictionary<string, EquipmentDefinitionSO> byId = new Dictionary<string, EquipmentDefinitionSO>(StringComparer.OrdinalIgnoreCase);
        Dictionary<EquipmentRarity, int> rarityCounts = new Dictionary<EquipmentRarity, int>();

        foreach (EquipmentDefinitionSO equipment in equipments)
        {
            if (equipment == null) continue;

            string path = GetAssetPath(equipment);
            string label = string.IsNullOrWhiteSpace(equipment.equipmentId) ? path : equipment.equipmentId;

            if (!path.StartsWith(GameDataRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                report.AddWarning(
                    "TGV003",
                    path,
                    0,
                    "EquipmentDefinitionSO lives outside Assets/GameData. Current generator and database autofill expect Assets/GameData/Equipments.");
            }

            if (string.IsNullOrWhiteSpace(equipment.equipmentId))
            {
                report.AddError("TGV004", path, 0, "EquipmentDefinitionSO has an empty equipmentId.");
            }
            else if (byId.ContainsKey(equipment.equipmentId))
            {
                report.AddError("TGV005", path, 0, $"Duplicate equipmentId found: {equipment.equipmentId}");
            }
            else
            {
                byId.Add(equipment.equipmentId, equipment);
            }

            if (string.IsNullOrWhiteSpace(equipment.equipmentName))
                report.AddError("TGV006", path, 0, $"Equipment '{label}' has no display name.");

            if (equipment.effects == null || equipment.effects.Count == 0)
                report.AddError("TGV007", path, 0, $"Equipment '{label}' has no effects.");

            if (!rarityCounts.ContainsKey(equipment.rarity))
                rarityCounts.Add(equipment.rarity, 0);

            rarityCounts[equipment.rarity]++;
        }

        foreach (EquipmentRarity rarity in Enum.GetValues(typeof(EquipmentRarity)))
        {
            int count = rarityCounts.TryGetValue(rarity, out int value) ? value : 0;
            if (count == 0)
            {
                report.AddError("TGV008", GameDataRoot, 0, $"No EquipmentDefinitionSO assets exist for rarity: {rarity}");
            }
        }
    }

    private static void ValidateEquipmentDatabase(
        ValidationReport report,
        List<EquipmentDefinitionSO> equipments,
        List<EquipmentDatabaseSO> databases)
    {
        if (databases.Count == 0)
        {
            report.AddError(
                "TGV010",
                GameDataRoot,
                0,
                "No EquipmentDatabaseSO asset was found. The 44 gacha items exist, but there is no release-facing database owner.");
            return;
        }

        if (databases.Count > 1)
        {
            report.AddWarning("TGV011", GameDataRoot, 0, $"Multiple EquipmentDatabaseSO assets were found: {databases.Count}. Choose one official owner.");
        }

        foreach (EquipmentDatabaseSO database in databases)
        {
            if (database == null) continue;

            string path = GetAssetPath(database);
            int count = database.allEquipments != null ? database.allEquipments.Count : 0;

            if (count != ExpectedGachaItemCount)
            {
                report.AddError(
                    "TGV012",
                    path,
                    0,
                    $"EquipmentDatabaseSO should contain {ExpectedGachaItemCount} items, but contains {count}.");
            }

            if (count != equipments.Count)
            {
                report.AddWarning(
                    "TGV013",
                    path,
                    0,
                    $"EquipmentDatabaseSO count ({count}) does not match EquipmentDefinitionSO asset count ({equipments.Count}).");
            }

            if (database.allEquipments == null) continue;

            for (int i = 0; i < database.allEquipments.Count; i++)
            {
                if (database.allEquipments[i] == null)
                {
                    report.AddError("TGV014", path, 0, $"EquipmentDatabaseSO has a null equipment reference at index {i}.");
                }
            }
        }
    }

    private static void ValidateGachaConfig(ValidationReport report, List<GachaConfigSO> configs)
    {
        if (configs.Count == 0)
        {
            report.AddError("TGV020", GameDataRoot, 0, "No GachaConfigSO asset was found. Gacha prices and rates must be data-driven.");
            return;
        }

        if (configs.Count > 1)
            report.AddWarning("TGV021", GameDataRoot, 0, $"Multiple GachaConfigSO assets were found: {configs.Count}. Choose one official gacha config.");

        foreach (GachaConfigSO config in configs)
        {
            if (config == null) continue;

            string path = GetAssetPath(config);
            float totalRate = config.epicRate + config.rareRate + config.uncommonRate + config.commonRate;

            if (Mathf.Abs(totalRate - 1f) > 0.001f)
                report.AddError("TGV022", path, 0, $"Gacha rarity rates must sum to 1.0, but current total is {totalRate:F4}.");

            if (config.singlePullCost <= 0)
                report.AddError("TGV023", path, 0, $"singlePullCost must be positive, but is {config.singlePullCost}.");

            if (config.tenPullCost <= 0)
                report.AddError("TGV024", path, 0, $"tenPullCost must be positive, but is {config.tenPullCost}.");

            if (config.tenPullCost > config.singlePullCost * 10)
                report.AddWarning("TGV025", path, 0, "tenPullCost is higher than ten single pulls. Verify this is intentional.");

            if (config.epicPityCount <= 0)
                report.AddError("TGV026", path, 0, $"epicPityCount must be positive, but is {config.epicPityCount}.");

            if (config.commonRefund < 0 || config.uncommonRefund < 0 || config.rareRefund < 0 || config.epicRefund < 0)
                report.AddError("TGV027", path, 0, "Duplicate refund values must not be negative.");
        }
    }

    private static void ValidateTalismanSlotsAndSave(ValidationReport report)
    {
        string equipmentSaveText = ReadAssetText(EquipmentSaveDataPath);
        if (string.IsNullOrEmpty(equipmentSaveText))
        {
            report.AddError("TGV030", EquipmentSaveDataPath, 0, "Character equipment save data script could not be read.");
            return;
        }

        int maxSlots = ExtractConstInt(equipmentSaveText, "MaxSlots");
        if (maxSlots > 0 && maxSlots != ExpectedTalismanSlots)
        {
            report.AddError(
                "TGV031",
                EquipmentSaveDataPath,
                FindLineNumber(equipmentSaveText, "MaxSlots"),
                $"Target talisman slot count is {ExpectedTalismanSlots}, but CharacterEquipmentSaveData.MaxSlots is {maxSlots}.");
        }

        if (ContainsOrdinal(equipmentSaveText, "new List<string>(8)") || ContainsOrdinal(equipmentSaveText, "MaxSlots = 8"))
        {
            report.AddError(
                "TGV032",
                EquipmentSaveDataPath,
                FindLineNumber(equipmentSaveText, "slotItemIds"),
                "Equipment save data still encodes the old 8-slot structure. Target rule is exactly 6 equipped talismans.");
        }

        if (ContainsOrdinal(equipmentSaveText, "OwnedShopItemEntry") || ContainsOrdinal(equipmentSaveText, "ownedItems"))
        {
            report.AddWarning(
                "TGV033",
                EquipmentSaveDataPath,
                FindLineNumber(equipmentSaveText, "ownedItems"),
                "Equipment ownership is still named as shop item ownership. This makes new gacha equipment and legacy shop items easy to mix up.");
        }
    }

    private static void ValidateCurrencyOwnership(ValidationReport report)
    {
        string saveText = ReadAssetText(SaveDataPath);
        if (string.IsNullOrEmpty(saveText))
        {
            report.AddError("TGV040", SaveDataPath, 0, "MetaProfileSaveData2D script could not be read.");
            return;
        }

        if (!ContainsOrdinal(saveText, "public int nyang"))
        {
            report.AddError("TGV041", SaveDataPath, 0, "MetaProfileSaveData2D has no persistent nyang field.");
        }

        if (!ContainsAny(saveText, "public int soul", "public int spirit", "public int yeonghon", "public int hon"))
        {
            report.AddError(
                "TGV042",
                SaveDataPath,
                0,
                "MetaProfileSaveData2D has no obvious persistent Soul/Spirit currency field. Target economy has both Nyang and Soul shared by Story/Casual.");
        }
    }

    private static void ValidateLegacyShopCoexistence(
        ValidationReport report,
        List<EquipmentDefinitionSO> equipments,
        List<ShopDatabaseSO> shopDatabases,
        List<ShopItemSO> shopItems)
    {
        string shopServiceText = ReadAssetText(ShopServicePath);
        string shopDatabaseText = ReadAssetText(ShopDatabasePath);
        string shopItemText = ReadAssetText(ShopItemPath);

        bool legacyShopCodeExists = !string.IsNullOrEmpty(shopServiceText)
            || !string.IsNullOrEmpty(shopDatabaseText)
            || !string.IsNullOrEmpty(shopItemText);

        if (legacyShopCodeExists && equipments.Count > 0)
        {
            report.AddWarning(
                "TGV050",
                ShopServicePath,
                0,
                "Legacy ShopItem/ShopService code coexists with the new Equipment/Gacha system. Decide which system owns talisman purchase, inventory, and equip effects.");
        }

        if (shopDatabases.Count == 0 && shopItems.Count == 0 && legacyShopCodeExists)
        {
            report.AddWarning(
                "TGV051",
                ShopDatabasePath,
                0,
                "Shop runtime code exists, but no ShopDatabaseSO/ShopItemSO assets were found. Runtime shop effects may silently do nothing unless a bootstrap provides a database.");
        }

        string resolverText = ReadAssetText(CharacterMetaResolverPath);
        if (ContainsAll(resolverText, "ShopDatabaseSO.RuntimeInstance", "ShopItemSO", "ApplyModifier"))
        {
            report.AddError(
                "TGV052",
                CharacterMetaResolverPath,
                FindLineNumber(resolverText, "ShopDatabaseSO.RuntimeInstance"),
                "Battle meta resolver applies equipped effects from ShopItemSO, not EquipmentDefinitionSO. New gacha equipment effects are not the official combat input yet.");
        }
    }

    private static void ValidateRuntimeWiring(ValidationReport report)
    {
        List<string> runtimeFiles = FindScriptFiles(ScriptRoot);
        int gachaRuntimeHits = 0;
        int equipmentRuntimeHits = 0;

        foreach (string file in runtimeFiles)
        {
            string normalized = NormalizePath(file);
            if (normalized.EndsWith("/Gachaconfigso.cs", StringComparison.OrdinalIgnoreCase)) continue;
            if (normalized.EndsWith("/Equipmentdatabaseso.cs", StringComparison.OrdinalIgnoreCase)) continue;
            if (normalized.EndsWith("/Equipmentdefinitionso.cs", StringComparison.OrdinalIgnoreCase)) continue;
            if (normalized.EndsWith("/Equipmentassetgenerator.cs", StringComparison.OrdinalIgnoreCase)) continue;
            if (normalized.Contains("/Editor/", StringComparison.OrdinalIgnoreCase)) continue;
            if (normalized.EndsWith("/TalismanGachaValidator.cs", StringComparison.OrdinalIgnoreCase)) continue;

            string text = File.ReadAllText(file);
            if (ContainsAny(text, "GachaConfigSO", "singlePullCost", "tenPullCost", "epicPityCount"))
                gachaRuntimeHits++;

            if (ContainsAny(text, "EquipmentDatabaseSO", "EquipmentDefinitionSO", "EquipmentEffectType"))
                equipmentRuntimeHits++;
        }

        if (gachaRuntimeHits == 0)
        {
            report.AddError(
                "TGV060",
                GachaConfigPath,
                0,
                "No runtime gacha service appears to consume GachaConfigSO. Prices are data-driven as an asset, but the draw flow is not wired to it.");
        }

        if (equipmentRuntimeHits == 0)
        {
            report.AddError(
                "TGV061",
                EquipmentDatabasePath,
                0,
                "No runtime system appears to consume EquipmentDatabaseSO/EquipmentDefinitionSO. The 44-item data set exists, but combat/save flow still uses the legacy shop path.");
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
            sb.AppendLine("Talisman Gacha Validator");
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
