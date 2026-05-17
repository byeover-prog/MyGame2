using UnityEngine.SceneManagement;

public readonly struct StoryClearRoute
{
    public readonly string SceneName;
    public readonly bool SaveStoryLobbyCheckpoint;
    public readonly string Reason;

    public StoryClearRoute(string sceneName, bool saveStoryLobbyCheckpoint, string reason)
    {
        SceneName = sceneName;
        SaveStoryLobbyCheckpoint = saveStoryLobbyCheckpoint;
        Reason = reason ?? string.Empty;
    }
}

public static class StoryClearRouteService
{
    public const int OpeningStageIndex = 0;
    public const int FirstLobbyStageIndex = 1;

    public static StoryClearRoute ResolveAfterStageClear(
        RunSetup runSetup,
        string storySceneName,
        string storyLobbySceneName,
        string casualLobbySceneName)
    {
        if (runSetup == null || runSetup.mode != RunSetupMode.Story)
        {
            return new StoryClearRoute(
                NormalizeSceneName(casualLobbySceneName),
                false,
                "Casual or unknown run returns to the casual lobby route.");
        }

        if (runSetup.stageIndex == OpeningStageIndex)
        {
            return new StoryClearRoute(
                NormalizeSceneName(storySceneName),
                false,
                "Story Stage 0 clear routes to the next story scene.");
        }

        if (runSetup.stageIndex == FirstLobbyStageIndex)
        {
            return new StoryClearRoute(
                NormalizeSceneName(storyLobbySceneName),
                true,
                "Story Stage 1 clear routes to Story Lobby and saves the lobby Continue point.");
        }

        return new StoryClearRoute(
            NormalizeSceneName(storyLobbySceneName),
            true,
            "Later Story stages return to Story Lobby until a chapter route table is introduced.");
    }

    public static bool LoadRoute(StoryClearRoute route, SaveManager2D saveManager = null)
    {
        if (string.IsNullOrWhiteSpace(route.SceneName))
            return false;

        if (route.SaveStoryLobbyCheckpoint)
            StoryContinueCheckpointService.SaveStoryLobbyCheckpoint(route.SceneName, saveManager);

        SceneManager.LoadScene(route.SceneName);
        return true;
    }

    private static string NormalizeSceneName(string sceneName)
    {
        return string.IsNullOrWhiteSpace(sceneName)
            ? SceneManager.GetActiveScene().name
            : sceneName;
    }
}
