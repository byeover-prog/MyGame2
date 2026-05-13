#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using _Game.Skills;
using UnityEditor;
using UnityEngine;

public static class SkillPoolValidator
{
    private const string MenuPath = "Tools/Honryeom/Validation/Skill Pool Validator";
    private const string DataRoot = "Assets/_Game/Data";
    private const string CharacterRoot = "Assets/_Game/Data/Character";
    private const string CharacterSkillRoot = "Assets/_Game/Data/Skills/Character";
    private const string SceneRoot = "Assets/Scenes";
    private const string LevelUpCardGeneratorPath = "Assets/_Game/Scripts/LevelUp/Levelupcardgenerator.cs";
    private const string SkillRootScriptPath = "Assets/_Game/Scripts/SO/Roots/SkillRootSO.cs";
    private const string RootSkillAssetPath = "Assets/_Game/Data/Roots/Root_Skill.asset";

    [MenuItem(MenuPath)]
    public static void RunFromMenu()
    {
        ValidationReport report = Run();
        Debug.Log(report.ToConsoleText());

        if (report.ErrorCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Skill Pool Validator",
                $"Failed: {report.ErrorCount} error(s), {report.WarningCount} warning(s).\nSee Console for details.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Skill Pool Validator",
                $"Passed: {report.WarningCount} warning(s).",
                "OK");
        }
    }

    public static ValidationReport Run()
    {
        ValidationReport report = new ValidationReport();

        List<SkillDefinitionSO> skillDefinitions = LoadAssets<SkillDefinitionSO>(DataRoot);
        List<SkillCatalogSO> skillCatalogs = LoadAssets<SkillCatalogSO>(DataRoot);
        List<CommonSkillCatalogSO> commonCatalogs = LoadAssets<CommonSkillCatalogSO>(DataRoot);
        List<CommonSkillCardPoolSO> cardPools = LoadAssets<CommonSkillCardPoolSO>(DataRoot);
        List<CharacterDefinitionSO> characters = LoadAssets<CharacterDefinitionSO>(CharacterRoot);
        List<CharacterSkillDefinitionSO> characterSkills = LoadAssets<CharacterSkillDefinitionSO>(CharacterSkillRoot);
        List<CharacterSkillSetSO> characterSkillSets = LoadAssets<CharacterSkillSetSO>(CharacterSkillRoot);
        List<CharacterSkillDatabaseSO> characterSkillDatabases = LoadAssets<CharacterSkillDatabaseSO>(CharacterSkillRoot);

        ValidateSkillDefinitions(report, skillDefinitions, skillCatalogs, commonCatalogs);
        ValidateCommonSkillCatalogs(report, commonCatalogs);
        ValidateCommonSkillCardPools(report, commonCatalogs, cardPools);
        ValidateCharacterExclusiveSkills(report, characters, characterSkills, characterSkillSets, characterSkillDatabases);
        ValidateLevelUpCardGeneratorFlow(report);
        ValidateSkillRootDrift(report);

        report.Sort();
        return report;
    }

    private static void ValidateSkillDefinitions(
        ValidationReport report,
        List<SkillDefinitionSO> skillDefinitions,
        List<SkillCatalogSO> skillCatalogs,
        List<CommonSkillCatalogSO> commonCatalogs)
    {
        if (skillDefinitions.Count == 0)
        {
            report.AddError("SPV001", DataRoot, 0, "No SkillDefinitionSO assets were found.");
            return;
        }

        Dictionary<string, SkillDefinitionSO> byId = new Dictionary<string, SkillDefinitionSO>(StringComparer.OrdinalIgnoreCase);
        int activeCount = 0;
        int passiveCount = 0;

        foreach (SkillDefinitionSO skill in skillDefinitions)
        {
            if (skill == null) continue;

            string path = GetAssetPath(skill);
            string label = string.IsNullOrWhiteSpace(skill.SkillId) ? path : skill.SkillId;

            if (string.IsNullOrWhiteSpace(skill.SkillId))
            {
                report.AddError("SPV002", path, 0, "SkillDefinitionSO has an empty SkillId.");
            }
            else if (byId.ContainsKey(skill.SkillId))
            {
                report.AddError("SPV003", path, 0, $"Duplicate SkillId found: {skill.SkillId}");
            }
            else
            {
                byId.Add(skill.SkillId, skill);
            }

            if (string.IsNullOrWhiteSpace(skill.DisplayName))
                report.AddError("SPV004", path, 0, $"Skill '{label}' has no display name.");

            if (skill.MaxLevel <= 0)
                report.AddError("SPV005", path, 0, $"Skill '{label}' has invalid MaxLevel: {skill.MaxLevel}");

            if (skill.SkillType == SkillType.Active)
            {
                activeCount++;
            }
            else if (skill.SkillType == SkillType.Passive)
            {
                passiveCount++;

                if (skill.PassiveStatType == PassiveStatType.None)
                {
                    report.AddWarning(
                        "SPV006",
                        path,
                        0,
                        $"Passive skill '{label}' has PassiveStatType.None, so LevelUpCardGenerator will skip it.");
                }
            }
        }

        if (activeCount == 0)
            report.AddError("SPV007", DataRoot, 0, "No active SkillDefinitionSO assets were found.");

        if (passiveCount == 0)
            report.AddWarning("SPV008", DataRoot, 0, "No passive SkillDefinitionSO assets were found.");

        ValidateSkillCatalogAssets(report, skillCatalogs, commonCatalogs);
    }

    private static void ValidateSkillCatalogAssets(
        ValidationReport report,
        List<SkillCatalogSO> skillCatalogs,
        List<CommonSkillCatalogSO> commonCatalogs)
    {
        if (skillCatalogs.Count == 0)
        {
            report.AddError("SPV010", DataRoot, 0, "No SkillCatalogSO asset was found. LevelUpCardGenerator needs a skill catalog input.");
            return;
        }

        foreach (SkillCatalogSO catalog in skillCatalogs)
        {
            string catalogPath = GetAssetPath(catalog);
            IReadOnlyList<SkillDefinitionSO> allSkills = catalog.AllSkills;

            if (allSkills == null || allSkills.Count == 0)
            {
                report.AddError("SPV011", catalogPath, 0, "SkillCatalogSO has no skills.");
                continue;
            }

            for (int i = 0; i < allSkills.Count; i++)
            {
                SkillDefinitionSO skill = allSkills[i];
                if (skill == null)
                {
                    report.AddError("SPV012", catalogPath, 0, $"SkillCatalogSO has a null skill reference at index {i}.");
                    continue;
                }

                if (skill.SkillType != SkillType.Active)
                    continue;

                if (!CanResolveFromAnyCommonCatalog(commonCatalogs, skill))
                {
                    report.AddWarning(
                        "SPV013",
                        catalogPath,
                        0,
                        $"Active skill '{skill.SkillId}' is in SkillCatalogSO but cannot be resolved by any CommonSkillCatalogSO. It will be skipped as a common card unless it is handled as an exclusive skill.");
                }
            }
        }
    }

    private static void ValidateCommonSkillCatalogs(ValidationReport report, List<CommonSkillCatalogSO> commonCatalogs)
    {
        if (commonCatalogs.Count == 0)
        {
            report.AddError("SPV020", DataRoot, 0, "No CommonSkillCatalogSO asset was found.");
            return;
        }

        foreach (CommonSkillCatalogSO catalog in commonCatalogs)
        {
            string path = GetAssetPath(catalog);

            if (catalog.skills == null || catalog.skills.Count == 0)
            {
                report.AddError("SPV021", path, 0, "CommonSkillCatalogSO has no common skill configs.");
                continue;
            }

            if (catalog.cardPool == null)
            {
                report.AddError("SPV022", path, 0, "CommonSkillCatalogSO has no cardPool reference.");
            }

            HashSet<CommonSkillKind> kinds = new HashSet<CommonSkillKind>();
            for (int i = 0; i < catalog.skills.Count; i++)
            {
                CommonSkillConfigSO config = catalog.skills[i];
                if (config == null)
                {
                    report.AddError("SPV023", path, 0, $"CommonSkillCatalogSO has a null skill config at index {i}.");
                    continue;
                }

                string configPath = GetAssetPath(config);
                if (!kinds.Add(config.kind))
                {
                    report.AddError("SPV024", configPath, 0, $"Duplicate CommonSkillKind in CommonSkillCatalogSO: {config.kind}");
                }

                if (string.IsNullOrWhiteSpace(config.displayName))
                    report.AddError("SPV025", configPath, 0, $"Common skill '{config.kind}' has no display name.");

                if (config.weaponPrefab == null)
                    report.AddError("SPV026", configPath, 0, $"Common skill '{config.kind}' has no weaponPrefab.");

                if (config.maxLevel <= 0)
                    report.AddError("SPV027", configPath, 0, $"Common skill '{config.kind}' has invalid maxLevel: {config.maxLevel}");

                if (config.levels == null || config.levels.Length == 0)
                    report.AddWarning("SPV028", configPath, 0, $"Common skill '{config.kind}' has no level params.");

                if (config.icon == null)
                    report.AddWarning("SPV029", configPath, 0, $"Common skill '{config.kind}' has no icon.");
            }
        }
    }

    private static void ValidateCommonSkillCardPools(
        ValidationReport report,
        List<CommonSkillCatalogSO> commonCatalogs,
        List<CommonSkillCardPoolSO> cardPools)
    {
        if (cardPools.Count == 0)
        {
            report.AddError("SPV030", DataRoot, 0, "No CommonSkillCardPoolSO asset was found.");
            return;
        }

        HashSet<CommonSkillConfigSO> catalogConfigs = new HashSet<CommonSkillConfigSO>();
        foreach (CommonSkillCatalogSO catalog in commonCatalogs)
        {
            if (catalog == null || catalog.skills == null) continue;

            foreach (CommonSkillConfigSO config in catalog.skills)
            {
                if (config != null)
                    catalogConfigs.Add(config);
            }
        }

        foreach (CommonSkillCardPoolSO pool in cardPools)
        {
            if (pool == null) continue;

            string path = GetAssetPath(pool);
            if (pool.cards == null || pool.cards.Count == 0)
            {
                report.AddError("SPV031", path, 0, "CommonSkillCardPoolSO has no cards.");
                continue;
            }

            HashSet<CommonSkillConfigSO> cardSkills = new HashSet<CommonSkillConfigSO>();
            for (int i = 0; i < pool.cards.Count; i++)
            {
                CommonSkillCardSO card = pool.cards[i];
                if (card == null)
                {
                    report.AddError("SPV032", path, 0, $"CommonSkillCardPoolSO has a null card at index {i}.");
                    continue;
                }

                string cardPath = GetAssetPath(card);
                if (card.skill == null)
                {
                    report.AddError("SPV033", cardPath, 0, "CommonSkillCardSO has no skill reference.");
                    continue;
                }

                if (card.weight < 1)
                    report.AddError("SPV034", cardPath, 0, $"CommonSkillCardSO has invalid weight: {card.weight}");

                if (!cardSkills.Add(card.skill))
                    report.AddWarning("SPV035", cardPath, 0, $"Common skill card pool contains a duplicate card for '{card.skill.kind}'.");

                if (!catalogConfigs.Contains(card.skill))
                {
                    report.AddWarning(
                        "SPV036",
                        cardPath,
                        0,
                        $"CommonSkillCardSO references '{card.skill.kind}', but that config was not found in any CommonSkillCatalogSO.");
                }
            }
        }
    }

    private static void ValidateCharacterExclusiveSkills(
        ValidationReport report,
        List<CharacterDefinitionSO> characters,
        List<CharacterSkillDefinitionSO> characterSkills,
        List<CharacterSkillSetSO> characterSkillSets,
        List<CharacterSkillDatabaseSO> characterSkillDatabases)
    {
        Dictionary<string, CharacterDefinitionSO> characterById = BuildCharacterMap(characters);
        Dictionary<string, int> skillCountByOwner = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> skillIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (CharacterSkillDefinitionSO skill in characterSkills)
        {
            if (skill == null) continue;

            string path = GetAssetPath(skill);
            string label = string.IsNullOrWhiteSpace(skill.SkillId) ? path : skill.SkillId;

            if (string.IsNullOrWhiteSpace(skill.SkillId))
            {
                report.AddError("SPV040", path, 0, "CharacterSkillDefinitionSO has an empty SkillId.");
            }
            else if (!skillIds.Add(skill.SkillId))
            {
                report.AddError("SPV041", path, 0, $"Duplicate exclusive SkillId found: {skill.SkillId}");
            }

            if (string.IsNullOrWhiteSpace(skill.DisplayName))
                report.AddError("SPV042", path, 0, $"Exclusive skill '{label}' has no display name.");

            if (string.IsNullOrWhiteSpace(skill.OwnerCharacterId))
            {
                report.AddError("SPV043", path, 0, $"Exclusive skill '{label}' has no ownerCharacterId.");
            }
            else
            {
                if (!characterById.ContainsKey(skill.OwnerCharacterId))
                {
                    report.AddError("SPV044", path, 0, $"Exclusive skill '{label}' ownerCharacterId does not match any CharacterDefinitionSO: {skill.OwnerCharacterId}");
                }

                if (!skillCountByOwner.ContainsKey(skill.OwnerCharacterId))
                    skillCountByOwner.Add(skill.OwnerCharacterId, 0);
                skillCountByOwner[skill.OwnerCharacterId]++;
            }

            if (skill.WeaponPrefab == null)
                report.AddError("SPV045", path, 0, $"Exclusive skill '{label}' has no WeaponPrefab.");

            if (skill.MaxLevel <= 0)
                report.AddError("SPV046", path, 0, $"Exclusive skill '{label}' has invalid MaxLevel: {skill.MaxLevel}");

            if (skill.LevelBalances == null || skill.LevelBalances.Length == 0)
                report.AddWarning("SPV047", path, 0, $"Exclusive skill '{label}' has no level balance entries.");
        }

        foreach (CharacterDefinitionSO character in characters)
        {
            if (character == null || string.IsNullOrWhiteSpace(character.CharacterId)) continue;

            int count = skillCountByOwner.TryGetValue(character.CharacterId, out int value) ? value : 0;
            if (count != 2)
            {
                report.AddError(
                    "SPV048",
                    GetAssetPath(character),
                    0,
                    $"Character '{character.CharacterId}' should contribute exactly 2 exclusive card-pool skills, but found {count} CharacterSkillDefinitionSO assets.");
            }
        }

        ValidateCharacterSkillSets(report, characterById, characterSkillSets);
        ValidateCharacterSkillDatabases(report, characterSkills, characterSkillSets, characterSkillDatabases);
    }

    private static void ValidateCharacterSkillSets(
        ValidationReport report,
        Dictionary<string, CharacterDefinitionSO> characterById,
        List<CharacterSkillSetSO> characterSkillSets)
    {
        Dictionary<string, CharacterSkillSetSO> setByCharacter = new Dictionary<string, CharacterSkillSetSO>(StringComparer.OrdinalIgnoreCase);

        foreach (CharacterSkillSetSO set in characterSkillSets)
        {
            if (set == null) continue;

            string path = GetAssetPath(set);
            if (string.IsNullOrWhiteSpace(set.CharacterId))
            {
                report.AddError("SPV050", path, 0, "CharacterSkillSetSO has an empty CharacterId.");
                continue;
            }

            if (!characterById.ContainsKey(set.CharacterId))
            {
                report.AddError("SPV051", path, 0, $"CharacterSkillSetSO CharacterId does not match any CharacterDefinitionSO: {set.CharacterId}");
            }

            if (setByCharacter.ContainsKey(set.CharacterId))
            {
                report.AddError("SPV052", path, 0, $"Duplicate CharacterSkillSetSO for CharacterId: {set.CharacterId}");
                continue;
            }

            setByCharacter.Add(set.CharacterId, set);

            IReadOnlyList<CharacterSkillDefinitionSO> skills = set.Skills;
            int count = skills != null ? skills.Count : 0;
            if (count != 2)
            {
                report.AddError("SPV053", path, 0, $"CharacterSkillSetSO for '{set.CharacterId}' should contain exactly 2 skills, but contains {count}.");
            }

            if (skills == null) continue;

            for (int i = 0; i < skills.Count; i++)
            {
                CharacterSkillDefinitionSO skill = skills[i];
                if (skill == null)
                {
                    report.AddError("SPV054", path, 0, $"CharacterSkillSetSO for '{set.CharacterId}' has a null skill at index {i}.");
                    continue;
                }

                if (!string.Equals(skill.OwnerCharacterId, set.CharacterId, StringComparison.OrdinalIgnoreCase))
                {
                    report.AddError(
                        "SPV055",
                        path,
                        0,
                        $"CharacterSkillSetSO for '{set.CharacterId}' contains skill '{skill.SkillId}' owned by '{skill.OwnerCharacterId}'.");
                }
            }
        }

        foreach (string characterId in characterById.Keys)
        {
            if (!setByCharacter.ContainsKey(characterId))
            {
                report.AddWarning(
                    "SPV056",
                    GetAssetPath(characterById[characterId]),
                    0,
                    $"Character '{characterId}' has no CharacterSkillSetSO. The current scene-inspector pool can drift from reusable data assets.");
            }
        }
    }

    private static void ValidateCharacterSkillDatabases(
        ValidationReport report,
        List<CharacterSkillDefinitionSO> characterSkills,
        List<CharacterSkillSetSO> characterSkillSets,
        List<CharacterSkillDatabaseSO> characterSkillDatabases)
    {
        if (characterSkillDatabases.Count == 0)
        {
            report.AddWarning("SPV060", CharacterSkillRoot, 0, "No CharacterSkillDatabaseSO asset was found.");
            return;
        }

        foreach (CharacterSkillDatabaseSO database in characterSkillDatabases)
        {
            if (database == null) continue;

            string path = GetAssetPath(database);
            int dbSkillCount = database.CharacterSkills != null ? database.CharacterSkills.Count : 0;
            int dbSetCount = database.CharacterSkillSets != null ? database.CharacterSkillSets.Count : 0;

            if (characterSkills.Count > 0 && dbSkillCount == 0)
                report.AddError("SPV061", path, 0, "CharacterSkillDatabaseSO has no characterSkills even though CharacterSkillDefinitionSO assets exist.");

            if (characterSkillSets.Count > 0 && dbSetCount == 0)
                report.AddError("SPV062", path, 0, "CharacterSkillDatabaseSO has no characterSkillSets even though CharacterSkillSetSO assets exist.");

            if (dbSkillCount != characterSkills.Count)
            {
                report.AddWarning(
                    "SPV063",
                    path,
                    0,
                    $"CharacterSkillDatabaseSO skill count ({dbSkillCount}) does not match CharacterSkillDefinitionSO asset count ({characterSkills.Count}).");
            }

            if (dbSetCount != characterSkillSets.Count)
            {
                report.AddWarning(
                    "SPV064",
                    path,
                    0,
                    $"CharacterSkillDatabaseSO set count ({dbSetCount}) does not match CharacterSkillSetSO asset count ({characterSkillSets.Count}).");
            }
        }
    }

    private static void ValidateLevelUpCardGeneratorFlow(ValidationReport report)
    {
        string generatorText = ReadAssetText(LevelUpCardGeneratorPath);
        if (string.IsNullOrEmpty(generatorText))
        {
            report.AddWarning("SPV070", LevelUpCardGeneratorPath, 0, "LevelUpCardGenerator script could not be read.");
            return;
        }

        if (ContainsAll(generatorText, "CharacterSkillSet[] characterSkillSets", "FindCharacterSkillSet"))
        {
            report.AddWarning(
                "SPV071",
                LevelUpCardGeneratorPath,
                FindLineNumber(generatorText, "CharacterSkillSet[] characterSkillSets"),
                "LevelUpCardGenerator owns exclusive skill pools through a scene inspector array. This is harder to reuse than CharacterSkillDatabaseSO/RunSetup ownership.");
        }

        if (ContainsAll(generatorText, "return characterSkillSets[i]", "FindCharacterSkillSet"))
        {
            report.AddWarning(
                "SPV072",
                LevelUpCardGeneratorPath,
                FindLineNumber(generatorText, "return characterSkillSets[i]"),
                "FindCharacterSkillSet returns the first matching characterId. Duplicate scene entries for one character will hide later exclusive skills.");
        }

        if (!ContainsAll(generatorText, "AppendExclusiveSkillsOf(support1Id", "AppendExclusiveSkillsOf(support2Id"))
        {
            report.AddError(
                "SPV073",
                LevelUpCardGeneratorPath,
                FindLineNumber(generatorText, "AppendExclusiveSkillsOf"),
                "LevelUpCardGenerator does not clearly add support1 and support2 exclusive skills to the card pool.");
        }

        if (ContainsAll(generatorText, "SquadLoadoutRuntime.MainId", "SquadLoadoutRuntime.Support1Id", "SquadLoadoutRuntime.Support2Id"))
        {
            report.AddWarning(
                "SPV074",
                LevelUpCardGeneratorPath,
                FindLineNumber(generatorText, "SquadLoadoutRuntime.MainId"),
                "LevelUpCardGenerator reads squad state directly from SquadLoadoutRuntime. Future card grade/support rules should consume a validated RunSetup.");
        }

        ValidateSceneLevelUpGenerators(report);
    }

    private static void ValidateSceneLevelUpGenerators(ValidationReport report)
    {
        HashSet<string> enabledBuildScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene != null && scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                enabledBuildScenes.Add(NormalizePath(scene.path));
        }

        List<string> scenePaths = FindAssetPaths("t:Scene", SceneRoot);
        foreach (string scenePath in scenePaths)
        {
            string text = ReadAssetText(scenePath);
            if (string.IsNullOrEmpty(text)) continue;
            if (!ContainsOrdinal(text, "Assembly-CSharp::_Game.LevelUp.LevelUpCardGenerator")) continue;

            bool isBuildScene = enabledBuildScenes.Contains(NormalizePath(scenePath));
            List<GeneratorBlock> blocks = ExtractGeneratorBlocks(text);

            for (int i = 0; i < blocks.Count; i++)
            {
                GeneratorBlock block = blocks[i];
                Dictionary<string, int> counts = CountLocalCharacterSkillSets(block.Text);

                if (block.HasEmptyLocalSets)
                {
                    AddSceneFinding(
                        report,
                        isBuildScene,
                        "SPV080",
                        "SPV081",
                        scenePath,
                        block.Line,
                        "LevelUpCardGenerator has an empty characterSkillSets array. Exclusive character skills cannot enter the level-up card pool from this generator.");
                    continue;
                }

                foreach (KeyValuePair<string, int> pair in counts)
                {
                    if (pair.Value <= 1) continue;

                    AddSceneFinding(
                        report,
                        isBuildScene,
                        "SPV082",
                        "SPV083",
                        scenePath,
                        block.Line,
                        $"LevelUpCardGenerator has {pair.Value} separate characterSkillSets entries for '{pair.Key}'. FindCharacterSkillSet returns only the first, so later exclusive skills can be ignored.");
                }

            }
        }
    }

    private static void ValidateSkillRootDrift(ValidationReport report)
    {
        string scriptText = ReadAssetText(SkillRootScriptPath);
        string assetText = ReadAssetText(RootSkillAssetPath);

        if (string.IsNullOrEmpty(scriptText) || string.IsNullOrEmpty(assetText))
            return;

        if (ContainsOrdinal(assetText, "commonSkillCatalog:") && !ContainsOrdinal(scriptText, "commonSkillCatalog"))
        {
            report.AddWarning(
                "SPV090",
                RootSkillAssetPath,
                FindLineNumber(assetText, "commonSkillCatalog:"),
                "Root_Skill.asset still serializes commonSkillCatalog, but SkillRootSO.cs no longer declares that field. This is data ownership drift.");
        }

        if (ContainsOrdinal(assetText, "weaponSkillTracks:") && !ContainsOrdinal(scriptText, "weaponSkillTracks"))
        {
            report.AddWarning(
                "SPV091",
                RootSkillAssetPath,
                FindLineNumber(assetText, "weaponSkillTracks:"),
                "Root_Skill.asset still serializes weaponSkillTracks, but SkillRootSO.cs no longer declares that field. This is stale serialized data unless intentionally preserved.");
        }
    }

    private static bool CanResolveFromAnyCommonCatalog(List<CommonSkillCatalogSO> commonCatalogs, SkillDefinitionSO skill)
    {
        foreach (CommonSkillCatalogSO catalog in commonCatalogs)
        {
            if (catalog == null) continue;
            if (catalog.TryResolve(skill, out CommonSkillConfigSO config) && config != null)
                return true;
        }

        return false;
    }

    private static Dictionary<string, CharacterDefinitionSO> BuildCharacterMap(List<CharacterDefinitionSO> characters)
    {
        Dictionary<string, CharacterDefinitionSO> result = new Dictionary<string, CharacterDefinitionSO>(StringComparer.OrdinalIgnoreCase);
        foreach (CharacterDefinitionSO character in characters)
        {
            if (character == null || string.IsNullOrWhiteSpace(character.CharacterId)) continue;
            if (!result.ContainsKey(character.CharacterId))
                result.Add(character.CharacterId, character);
        }

        return result;
    }

    private static void AddSceneFinding(
        ValidationReport report,
        bool isBuildScene,
        string buildRuleId,
        string nonBuildRuleId,
        string scenePath,
        int line,
        string message)
    {
        if (isBuildScene)
            report.AddError(buildRuleId, scenePath, line, message);
        else
            report.AddWarning(nonBuildRuleId, scenePath, line, message + " This becomes release-blocking if the scene is added to Build Settings.");
    }

    private static List<GeneratorBlock> ExtractGeneratorBlocks(string sceneText)
    {
        List<GeneratorBlock> blocks = new List<GeneratorBlock>();
        string marker = "Assembly-CSharp::_Game.LevelUp.LevelUpCardGenerator";
        int searchIndex = 0;

        while (true)
        {
            int markerIndex = sceneText.IndexOf(marker, searchIndex, StringComparison.Ordinal);
            if (markerIndex < 0) break;

            int start = sceneText.LastIndexOf("--- !u!", markerIndex, StringComparison.Ordinal);
            if (start < 0) start = markerIndex;

            int end = sceneText.IndexOf("--- !u!", markerIndex + marker.Length, StringComparison.Ordinal);
            if (end < 0) end = sceneText.Length;

            string blockText = sceneText.Substring(start, end - start);
            blocks.Add(new GeneratorBlock(blockText, CountLines(sceneText, start) + 1));

            searchIndex = markerIndex + marker.Length;
        }

        return blocks;
    }

    private static Dictionary<string, int> CountLocalCharacterSkillSets(string blockText)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        string[] lines = SplitLines(blockText);
        bool inSets = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmed = line.Trim();

            if (trimmed.StartsWith("characterSkillSets:", StringComparison.Ordinal))
            {
                inSets = true;
                continue;
            }

            if (!inSets) continue;

            if (trimmed.StartsWith("totalCardCount:", StringComparison.Ordinal))
                break;

            Match match = Regex.Match(trimmed, @"^-\s*characterId:\s*(.+)$");
            if (!match.Success) continue;

            string characterId = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(characterId)) continue;

            if (!counts.ContainsKey(characterId))
                counts.Add(characterId, 0);

            counts[characterId]++;
        }

        return counts;
    }

    private readonly struct GeneratorBlock
    {
        public GeneratorBlock(string text, int line)
        {
            Text = text ?? string.Empty;
            Line = line;
        }

        public string Text { get; }
        public int Line { get; }
        public bool HasEmptyLocalSets => ContainsOrdinal(Text, "characterSkillSets: []");
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

    private static List<string> FindAssetPaths(string filter, string root)
    {
        List<string> result = new List<string>();
        string[] guids = AssetDatabase.FindAssets(filter, new[] { root });
        Array.Sort(guids, StringComparer.Ordinal);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrWhiteSpace(path))
                result.Add(NormalizePath(path));
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

    private static int CountLines(string text, int endExclusive)
    {
        int count = 0;
        int limit = Mathf.Clamp(endExclusive, 0, text != null ? text.Length : 0);
        for (int i = 0; i < limit; i++)
        {
            if (text[i] == '\n')
                count++;
        }

        return count;
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
            sb.AppendLine("Skill Pool Validator");
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
