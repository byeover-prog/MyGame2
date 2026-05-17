using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LobbyMenuController : MonoBehaviour
{
    [Header("Title Buttons")]
    [SerializeField] private Button btnStoryMode;
    [SerializeField] private Button btnCasualMode;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnQuit;

    [Header("Story Mode Buttons")]
    [SerializeField] private Button btnContinue;
    [SerializeField] private Button btnNewGame;

    [Header("Scene Routes")]
    [SerializeField] private string formationSceneName = "Scene_Boot";
    [SerializeField] private string storyOpeningSceneName = "Scene_Lobby";
    [SerializeField] private string casualLobbySceneName = "Scene_Boot";

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private void Awake()
    {
        if (btnStoryMode == null)
            btnStoryMode = GameObject.Find("Btn_StoryMode")?.GetComponent<Button>()
                ?? GameObject.Find("Btn_Start")?.GetComponent<Button>();

        if (btnCasualMode == null)
            btnCasualMode = GameObject.Find("Btn_CasualMode")?.GetComponent<Button>();

        if (btnSettings == null)
            btnSettings = GameObject.Find("Btn_Settings")?.GetComponent<Button>();

        if (btnQuit == null)
            btnQuit = GameObject.Find("Btn_Quit")?.GetComponent<Button>();

        if (btnContinue == null)
            btnContinue = GameObject.Find("Btn_Continue")?.GetComponent<Button>();

        if (btnNewGame == null)
            btnNewGame = GameObject.Find("Btn_NewGame")?.GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (btnStoryMode != null) btnStoryMode.onClick.AddListener(OnClickStoryMode);
        if (btnCasualMode != null) btnCasualMode.onClick.AddListener(OnClickCasualMode);
        if (btnSettings != null) btnSettings.onClick.AddListener(OnClickSettings);
        if (btnQuit != null) btnQuit.onClick.AddListener(OnClickQuit);
        if (btnContinue != null) btnContinue.onClick.AddListener(OnClickContinue);
        if (btnNewGame != null) btnNewGame.onClick.AddListener(OnClickNewGame);

        RefreshContinueButton();
    }

    private void OnDisable()
    {
        if (btnStoryMode != null) btnStoryMode.onClick.RemoveListener(OnClickStoryMode);
        if (btnCasualMode != null) btnCasualMode.onClick.RemoveListener(OnClickCasualMode);
        if (btnSettings != null) btnSettings.onClick.RemoveListener(OnClickSettings);
        if (btnQuit != null) btnQuit.onClick.RemoveListener(OnClickQuit);
        if (btnContinue != null) btnContinue.onClick.RemoveListener(OnClickContinue);
        if (btnNewGame != null) btnNewGame.onClick.RemoveListener(OnClickNewGame);
    }

    public void OnClickStoryMode()
    {
        RefreshContinueButton();

        if (btnContinue != null || btnNewGame != null)
        {
            SetStorySubMenuVisible(true);
            return;
        }

        if (StoryContinueCheckpointService.HasContinuePoint())
            OnClickContinue();
        else
            OnClickNewGame();
    }

    public void OnClickContinue()
    {
        if (StoryContinueCheckpointService.TryResumeFromSavedCheckpoint())
            return;

        if (log) GameLogger.LogWarning("[Lobby] Continue requested without a valid checkpoint.", this);
        RefreshContinueButton();
    }

    public void OnClickNewGame()
    {
        RunSetupHolder.Clear();
        RunSetup setup = RunSetupFactory.CreateFromCurrentState(RunSetupMode.Story, StoryClearRouteService.OpeningStageIndex);
        RunSetupHolder.Set(setup);

        string targetScene = !string.IsNullOrWhiteSpace(storyOpeningSceneName)
            ? storyOpeningSceneName
            : formationSceneName;

        LoadScene(targetScene);
    }

    public void OnClickCasualMode()
    {
        RunSetupHolder.Clear();
        RunSetup setup = RunSetupFactory.CreateFromCurrentState(RunSetupMode.Casual);
        RunSetupHolder.Set(setup);

        LoadScene(casualLobbySceneName);
    }

    public void OnClickSettings()
    {
        if (log) GameLogger.Log("[Lobby] Settings requested. Settings UI is not wired yet.", this);
    }

    public void OnClickQuit()
    {
        if (log) GameLogger.Log("[Lobby] Quit", this);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshContinueButton()
    {
        if (btnContinue == null) return;
        btnContinue.interactable = StoryContinueCheckpointService.HasContinuePoint();
    }

    private void SetStorySubMenuVisible(bool visible)
    {
        if (btnContinue != null) btnContinue.gameObject.SetActive(visible);
        if (btnNewGame != null) btnNewGame.gameObject.SetActive(visible);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[Lobby] Target scene name is empty.", this);
            return;
        }

        if (log) GameLogger.Log($"[Lobby] LoadScene: {sceneName}", this);
        SceneManager.LoadScene(sceneName);
    }
}
