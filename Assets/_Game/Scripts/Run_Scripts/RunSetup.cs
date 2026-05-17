using System;
using System.Collections.Generic;

public enum RunSetupMode
{
    Story = 0,
    Casual = 1
}

[Serializable]
public sealed class RunSetup
{
    public RunSetupMode mode = RunSetupMode.Casual;
    public int stageIndex;
    public string mapId;
    public string mainId;
    public string support1Id;
    public string support2Id;
    public List<string> talismanItemIds = new List<string>(CharacterEquipmentSaveData.MaxSlots);
    public StoryContinueCheckpointSaveData continueCheckpoint = StoryContinueCheckpointSaveData.None();
    public RunConfigSO runConfig;

    public bool IsValid(out string reason)
    {
        if (string.IsNullOrWhiteSpace(mainId))
        {
            reason = "RunSetup requires a mainId before Scene_Game starts.";
            return false;
        }

        if (stageIndex < 0)
        {
            reason = "RunSetup requires a non-negative stage index.";
            return false;
        }

        if (talismanItemIds == null)
        {
            reason = "RunSetup talisman list is null.";
            return false;
        }

        if (talismanItemIds.Count != CharacterEquipmentSaveData.MaxSlots)
        {
            reason = $"RunSetup requires exactly {CharacterEquipmentSaveData.MaxSlots} talisman slots.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public FormationSaveData2D ToFormationSaveData()
    {
        return new FormationSaveData2D
        {
            mainId = mainId,
            support1Id = support1Id,
            support2Id = support2Id
        };
    }
}

public static class RunSetupHolder
{
    public static RunSetup Current { get; private set; }

    public static bool HasCurrent => Current != null;

    public static void Set(RunSetup setup)
    {
        Current = setup;
    }

    public static RunSetup GetOrCreateFromCurrentState()
    {
        if (Current == null)
            Current = RunSetupFactory.CreateFromCurrentState();

        return Current;
    }

    public static void Clear()
    {
        Current = null;
    }

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        Current = null;
    }
}

public static class RunSetupFactory
{
    public static RunSetup CreateFromCurrentState(
        RunSetupMode mode = RunSetupMode.Casual,
        int? stageIndexOverride = null,
        RunConfigSO runConfig = null)
    {
        if (string.IsNullOrWhiteSpace(SquadLoadoutRuntime.MainId))
            SquadLoadoutRuntime.LoadFromSave();

        SquadLoadoutRuntime.Loadout loadout = SquadLoadoutRuntime.Current;
        SaveManager2D saveManager = SaveManager2D.Instance;
        MetaProfileSaveData2D meta = null;

        if (saveManager != null && saveManager.Data != null)
        {
            saveManager.Data.EnsureDefaults();
            meta = saveManager.Data.metaProfile;
        }

        int stageIndex = stageIndexOverride ?? ResolveStageIndex(meta);

        RunSetup setup = new RunSetup
        {
            mode = mode,
            stageIndex = stageIndex,
            mapId = $"stage_{stageIndex}",
            mainId = loadout.mainId,
            support1Id = loadout.support1Id,
            support2Id = loadout.support2Id,
            runConfig = runConfig != null ? runConfig : RunConfigHolder.FindSceneConfig(),
            continueCheckpoint = CloneCheckpoint(meta != null ? meta.stageProgress?.continueCheckpoint : null),
            talismanItemIds = BuildTalismanSnapshot(meta, loadout.mainId)
        };

        return setup;
    }

    private static int ResolveStageIndex(MetaProfileSaveData2D meta)
    {
        StoryContinueCheckpointSaveData checkpoint = meta != null ? meta.stageProgress?.continueCheckpoint : null;
        if (checkpoint != null && checkpoint.checkpointKind == StoryContinueCheckpointKind.StageStart && checkpoint.stageIndex >= 0)
            return checkpoint.stageIndex;

        return 0;
    }

    private static List<string> BuildTalismanSnapshot(MetaProfileSaveData2D meta, string mainId)
    {
        List<string> result = new List<string>(CharacterEquipmentSaveData.MaxSlots);

        CharacterEquipmentSaveData equipment = meta != null && meta.equipment != null && !string.IsNullOrWhiteSpace(mainId)
            ? meta.equipment.GetOrCreate(mainId)
            : null;

        if (equipment != null)
        {
            equipment.EnsureSlots();
            for (int i = 0; i < CharacterEquipmentSaveData.MaxSlots; i++)
                result.Add(i < equipment.slotItemIds.Count ? equipment.slotItemIds[i] ?? string.Empty : string.Empty);
        }

        while (result.Count < CharacterEquipmentSaveData.MaxSlots)
            result.Add(string.Empty);

        if (result.Count > CharacterEquipmentSaveData.MaxSlots)
            result.RemoveRange(CharacterEquipmentSaveData.MaxSlots, result.Count - CharacterEquipmentSaveData.MaxSlots);

        return result;
    }

    private static StoryContinueCheckpointSaveData CloneCheckpoint(StoryContinueCheckpointSaveData source)
    {
        if (source == null)
            return StoryContinueCheckpointSaveData.None();

        return new StoryContinueCheckpointSaveData
        {
            checkpointKind = source.checkpointKind,
            stageIndex = source.stageIndex,
            sceneName = source.sceneName,
            storyId = source.storyId
        };
    }
}
