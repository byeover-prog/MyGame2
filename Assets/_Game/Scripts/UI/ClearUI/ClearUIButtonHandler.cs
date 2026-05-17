using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public sealed class ClearUIButtonHandler : MonoBehaviour
{
    [Header("Scene Routes")]
    [SerializeField] private string titleSceneName = "Scene_Lobby";
    [SerializeField] private string storySceneName = "Scene_Lobby";
    [SerializeField] private string storyLobbySceneName = "Scene_Lobby";
    [SerializeField] private string casualLobbySceneName = "Scene_Lobby";

    private void OnEnable()
    {
        UIDocument doc = GetComponent<UIDocument>();
        if (doc == null)
        {
            GameLogger.LogWarning("[ClearUIButtonHandler] UIDocument is missing.", this);
            return;
        }

        VisualElement root = doc.rootVisualElement;
        if (root == null)
        {
            GameLogger.LogWarning("[ClearUIButtonHandler] rootVisualElement is null.", this);
            return;
        }

        Button btnNext = root.Q<Button>("btn-next");
        Button btnRetry = root.Q<Button>("btn-retry");
        Button btnBase = root.Q<Button>("btn-base");

        if (btnNext != null) btnNext.clicked += OnClickNext;
        if (btnRetry != null) btnRetry.clicked += OnClickRetry;
        if (btnBase != null) btnBase.clicked += OnClickHome;
    }

    private void OnDisable()
    {
        UIDocument doc = GetComponent<UIDocument>();
        VisualElement root = doc != null ? doc.rootVisualElement : null;
        if (root == null) return;

        Button btnNext = root.Q<Button>("btn-next");
        Button btnRetry = root.Q<Button>("btn-retry");
        Button btnBase = root.Q<Button>("btn-base");

        if (btnNext != null) btnNext.clicked -= OnClickNext;
        if (btnRetry != null) btnRetry.clicked -= OnClickRetry;
        if (btnBase != null) btnBase.clicked -= OnClickHome;
    }

    private void OnClickNext()
    {
        RunSetup runSetup = RunSetupHolder.HasCurrent ? RunSetupHolder.Current : null;
        StoryClearRoute route = StoryClearRouteService.ResolveAfterStageClear(
            runSetup,
            storySceneName,
            storyLobbySceneName,
            casualLobbySceneName);

        if (StoryClearRouteService.LoadRoute(route))
            return;

        GameLogger.LogWarning($"[ClearUIButtonHandler] Clear route failed. reason='{route.Reason}'", this);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnClickRetry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnClickHome()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}
