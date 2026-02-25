// UTF-8
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// [구현 원리 요약]
// - 사망 신호/단축키로 GameOver 패널을 "무조건" 켠다.
// - 입력은 Old(Input.GetKeyDown) + New(InputSystem Keyboard) 둘 다 지원한다.
// - 패널이 안 보이는 대표 원인(Canvas 비활성, CanvasGroup alpha=0, ScreenSpaceCamera의 worldCamera 미지정, sortingOrder 뒤)
//   을 강제로 복구해서 "켜졌는데 안 보임"을 막는다.
[DisallowMultipleComponent]
public sealed class UIManager2D : MonoBehaviour
{
    public static UIManager2D Instance { get; private set; }

    [Header("게임오버 패널")]
    [Tooltip("가장 확실한 방법: Hierarchy의 패널 루트를 여기로 드래그해서 직접 연결하세요.\n(예: DefeatPanel 또는 GameOverPanel)")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("직접 연결이 비어있을 때만 사용되는 자동 탐색 이름")]
    [SerializeField] private string gameOverPanelName = "GameOverPanel";

    [Tooltip("패널이 켜질 때 첫 선택(다시하기 버튼 권장)")]
    [SerializeField] private GameObject firstSelect;

    [Header("옵션")]
    [SerializeField] private bool pauseOnGameOver = true;

    [Tooltip("비워두면 모든 씬에서 동작합니다.\n특정 씬에서만 동작시키고 싶으면 그 씬 이름을 입력하세요.")]
    [SerializeField] private string runSceneName = "";

    [Header("단축키(디버그)")]
    [SerializeField] private bool enableHotkey = true;
    [SerializeField] private KeyCode openGameOverHotkey = KeyCode.Home;

    [Tooltip("New Input System에서 Home키를 읽을지 여부(Old Input만 쓰면 꺼도 됨)")]
    [SerializeField] private bool useNewInputSystemHotkey = true;

    private bool _shown;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _shown = false;

        if (Time.timeScale != 1f) Time.timeScale = 1f;
        TryHidePanel();
    }

    private void OnEnable()
    {
        RunSignals.PlayerDead += OnPlayerDead;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        RunSignals.PlayerDead -= OnPlayerDead;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (!enableHotkey) return;

        // 1) Old Input(레거시) 방식
        if (Input.GetKeyDown(openGameOverHotkey))
        {
            ShowGameOver("Hotkey(OldInput)");
            return;
        }

        // 2) New Input System 방식
#if ENABLE_INPUT_SYSTEM
        if (useNewInputSystemHotkey && Keyboard.current != null)
        {
            if (Keyboard.current.homeKey.wasPressedThisFrame)
            {
                ShowGameOver("Hotkey(NewInputSystem)");
                return;
            }
        }
#endif
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.IsNullOrEmpty(runSceneName) && scene.name != runSceneName)
            return;

        _shown = false;
        if (Time.timeScale != 1f) Time.timeScale = 1f;

        // 씬 전환으로 참조가 끊겼을 수 있으니, 없으면 재탐색
        if (gameOverPanel == null)
            gameOverPanel = FindPanelByName(gameOverPanelName);

        TryHidePanel();
    }

    private void OnPlayerDead()
    {
        ShowGameOver("RunSignals.PlayerDead");
    }

    [ContextMenu("테스트: 게임오버 표시")]
    private void Test_ShowGameOver()
    {
        ShowGameOver("ContextMenu(Test)");
    }

    public void ShowGameOver(string reason = "")
    {
        if (_shown) return;
        _shown = true;

        // 1) 패널 확보
        if (gameOverPanel == null)
            gameOverPanel = FindPanelByName(gameOverPanelName);

        EnsureEventSystem();

        if (gameOverPanel != null)
        {
            // 켜기 전에 렌더/정렬 조건부터 강제 복구
            ForceVisibleAndOnTop(gameOverPanel);

            gameOverPanel.SetActive(true);

            // 버튼 포커스
            if (firstSelect != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(firstSelect);
        }
        else
        {
            Debug.LogWarning($"[UIManager2D] GameOverPanel을 찾지 못했습니다. name='{gameOverPanelName}' reason='{reason}'", this);
        }

        if (pauseOnGameOver)
            Time.timeScale = 0f;

        Debug.Log($"[UIManager2D] GameOver 표시 호출 reason='{reason}' panel={(gameOverPanel != null ? gameOverPanel.name : "NULL")}", this);
    }

    public void HideGameOver()
    {
        _shown = false;
        TryHidePanel();
        if (Time.timeScale != 1f) Time.timeScale = 1f;
    }

    public void OnClickRetry()
    {
        HideGameOver();
        var active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.name);
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void TryHidePanel()
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(false);
    }

    private static GameObject FindPanelByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        var all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            var t = all[i];
            if (t == null) continue;

            // 에디터 내부/숨김 오브젝트 제외(과탐색 방지)
            if (t.hideFlags != HideFlags.None) continue;

            if (t.name == name)
                return t.gameObject;
        }
        return null;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();

        // StandaloneInputModule은 Old Input 기반이지만,
        // 버튼 클릭 자체는 마우스/터치로도 되기 때문에 일단 생성해둔다.
        es.AddComponent<StandaloneInputModule>();
    }

    private static void ForceVisibleAndOnTop(GameObject panelRoot)
    {
        // 1) 부모 Canvas/CanvasGroup 강제 복구
        var canvases = panelRoot.GetComponentsInParent<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            if (c == null) continue;

            c.enabled = true;
            c.gameObject.SetActive(true);

            // ScreenSpace-Camera인데 카메라가 비어있으면 UI가 안 보일 수 있음
            if (c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == null)
                c.worldCamera = Camera.main;

            // 맨 위에 오도록 정렬 강제(다른 캔버스에 가려지는 케이스 방지)
            c.overrideSorting = true;
            c.sortingOrder = 9999;
        }

        var groups = panelRoot.GetComponentsInParent<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            var g = groups[i];
            if (g == null) continue;

            g.alpha = 1f;
            g.interactable = true;
            g.blocksRaycasts = true;
        }
    }
}