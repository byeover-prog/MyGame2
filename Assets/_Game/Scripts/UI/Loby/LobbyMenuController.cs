using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LobbyMenuController : MonoBehaviour
{
    [Header("버튼 연결")]
    [SerializeField] private UnityEngine.UI.Button btnStart;
    [SerializeField] private UnityEngine.UI.Button btnQuit;

    [Header("시작할 씬 이름")]
    [Tooltip("Build Settings에 등록된 씬 이름과 정확히 일치해야 합니다.")]
    [SerializeField] private string gameSceneName = "Scene_Game";

    [Header("디버그")]
    [SerializeField] private bool log = true;

    private void OnEnable()
    {
        if (btnStart != null) btnStart.onClick.AddListener(OnClickStart);
        if (btnQuit != null) btnQuit.onClick.AddListener(OnClickQuit);
    }

    private void OnDisable()
    {
        if (btnStart != null) btnStart.onClick.RemoveListener(OnClickStart);
        if (btnQuit != null) btnQuit.onClick.RemoveListener(OnClickQuit);
    }

    private void OnClickStart()
    {
        if (log) GameLogger.Log($"[Lobby] Start -> LoadScene: {gameSceneName}");

        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("[Lobby] gameSceneName이 비어있음. 인스펙터에서 Scene_Game 같은 이름을 넣어주세요.");
            return;
        }

        // 씬 이름이 Build Settings에 등록되어 있어야 로드됩니다.
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnClickQuit()
    {
        if (log) GameLogger.Log("[Lobby] Quit");

        // 에디터에서는 게임이 종료되지 않는 게 정상입니다.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}