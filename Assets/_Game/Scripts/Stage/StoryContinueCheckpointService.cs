using UnityEngine.SceneManagement;

public static class StoryContinueCheckpointService
{
    public static bool HasContinuePoint(SaveManager2D saveManager = null)
    {
        StoryContinueCheckpointSaveData checkpoint = GetCheckpoint(saveManager);
        return checkpoint != null && checkpoint.checkpointKind != StoryContinueCheckpointKind.None;
    }

    public static void SaveStageStartCheckpoint(int stageIndex, string sceneName, SaveManager2D saveManager = null)
    {
        StageProgressSaveData progress = GetProgress(saveManager);
        if (progress == null) return;

        progress.SaveContinueStageStart(stageIndex, sceneName);
        ResolveSaveManager(saveManager)?.Save();
    }

    public static void SaveStoryLobbyCheckpoint(string sceneName, SaveManager2D saveManager = null)
    {
        StageProgressSaveData progress = GetProgress(saveManager);
        if (progress == null) return;

        progress.SaveContinueStoryLobby(sceneName);
        ResolveSaveManager(saveManager)?.Save();
    }

    public static bool TryResumeFromSavedCheckpoint(SaveManager2D saveManager = null)
    {
        StoryContinueCheckpointSaveData checkpoint = GetCheckpoint(saveManager);
        if (checkpoint == null || checkpoint.checkpointKind == StoryContinueCheckpointKind.None)
            return false;

        if (checkpoint.checkpointKind == StoryContinueCheckpointKind.StageStart && checkpoint.stageIndex >= 0)
        {
            RunSetup setup = RunSetupFactory.CreateFromCurrentState(
                RunSetupMode.Story,
                checkpoint.stageIndex);

            setup.continueCheckpoint = CloneCheckpoint(checkpoint);
            RunSetupHolder.Set(setup);
        }

        string sceneName = !string.IsNullOrWhiteSpace(checkpoint.sceneName)
            ? checkpoint.sceneName
            : SceneManager.GetActiveScene().name;

        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        SceneManager.LoadScene(sceneName);
        return true;
    }

    private static StageProgressSaveData GetProgress(SaveManager2D saveManager)
    {
        SaveManager2D resolved = ResolveSaveManager(saveManager);
        if (resolved == null || resolved.Data == null) return null;

        resolved.Data.EnsureDefaults();
        return resolved.Data.metaProfile.stageProgress;
    }

    private static StoryContinueCheckpointSaveData GetCheckpoint(SaveManager2D saveManager)
    {
        StageProgressSaveData progress = GetProgress(saveManager);
        if (progress == null) return null;

        progress.EnsureDefaults();
        return progress.continueCheckpoint;
    }

    private static SaveManager2D ResolveSaveManager(SaveManager2D saveManager)
    {
        return saveManager != null ? saveManager : SaveManager2D.Instance;
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
