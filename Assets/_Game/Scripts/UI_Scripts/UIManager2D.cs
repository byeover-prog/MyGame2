using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// [구현 원리 요약]
// - PlayerDead 이벤트/단축키로 DefeatUIController2D.Open()을 "확정" 호출한다.
// - Resources.FindObjectsOfTypeAll 같은 에셋 포함 탐색을 피하고, 씬 오브젝트만 대상으로 찾는다.
// - Canvas가 안 보이는 케이스(ScreenSpaceCamera 카메라 미지정/Sorting 뒤)도 최소한 복구한다.
[DisallowMultipleComponent]
public sealed class UIManager2D : MonoBehaviour
{
    public static UIManager2D Instance { get; private set; }

    [Header("Defeat UI")]
    [Tooltip("가장 확실: Hierarchy(씬) 안의 DefeatPanel을 드래그로 연결하세요(프리팹 에셋 X).")]
    [SerializeField] private DefeatUIController2D defeatUI;

    [Tooltip("위 참조가 비었을 때만 이름으로 찾습니다(씬 오브젝트만 탐색).")]
    [SerializeField] private string defeatPanelName = "DefeatPanel";

    [Tooltip("게임오버 시 게임 정지")]
    [SerializeField] private bool pauseOnGameOver = true;

    [Header("Hotkey(Debug)")]
    [SerializeField] private bool enableHotkey = true;
    [SerializeField] private KeyCode openKey = KeyCode.Home;

#if ENABLE_INPUT_SYSTEM
    [SerializeField] private bool useNewInputSystemHotkey = true;
#endif

    private bool _shown;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        _shown = false;
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

        if (Input.GetKeyDown(openKey))
            ShowGameOver("Hotkey(OldInput)");

#if ENABLE_INPUT_SYSTEM
        if (useNewInputSystemHotkey && Keyboard.current != null)
        {
            if (Keyboard.current.homeKey.wasPressedThisFrame)
                ShowGameOver("Hotkey(NewInputSystem)");
        }
#endif
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _shown = false;

        // 씬 바뀌면 참조가 끊겼을 수 있으니 다시 찾기
        if (defeatUI == null)
            defeatUI = FindDefeatUIInScene(defeatPanelName);
    }

    private void OnPlayerDead()
    {
        ShowGameOver("RunSignals.PlayerDead");
    }

    public void ShowGameOver(string reason)
    {
        if (_shown) return;
        _shown = true;

        if (defeatUI == null)
            defeatUI = FindDefeatUIInScene(defeatPanelName);

        if (defeatUI == null)
        {
            Debug.LogWarning($"[UIManager2D] DefeatUIController2D를 찾지 못함 name='{defeatPanelName}' reason='{reason}'", this);
            return;
        }

        // Canvas 표시 문제 최소 복구
        defeatUI.ForceCanvasVisible();

        defeatUI.Open(reason, pauseOnGameOver);

        Debug.Log($"[UIManager2D] GameOver Open reason='{reason}' ui='{defeatUI.name}'", this);
    }

    public void HideGameOver()
    {
        _shown = false;
        if (defeatUI != null)
            defeatUI.Close();
    }

    private static DefeatUIController2D FindDefeatUIInScene(string panelName)
    {
#if UNITY_2023_1_OR_NEWER
        // 씬 오브젝트만(비활성 포함) 탐색 -> 프리팹 에셋 잡는 문제 방지
        var all = Object.FindObjectsByType<DefeatUIController2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var all = Object.FindObjectsOfType<DefeatUIController2D>(true);
#endif
        if (all == null || all.Length == 0) return null;

        if (string.IsNullOrEmpty(panelName))
            return all[0];

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            if (all[i].gameObject.name == panelName)
                return all[i];
        }

        // 이름 매칭 실패 시 첫 번째 반환(그래도 UI 열리게)
        return all[0];
    }
}