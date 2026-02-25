// UTF-8
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// [구현 원리 요약]
// - "처음 1번만" 패널을 꺼서 시작 상태를 보장한다.
// - 외부(UIManager 등)가 켜면, 이 스크립트가 다시 끄지 않는다(가장 흔한 버그 방지).
// - 버튼 Retry/Exit만 확실히 동작시키고, 필요 시 TimeScale 정지까지 담당한다.
[DisallowMultipleComponent]
public sealed class DefeatUIController2D : MonoBehaviour
{
    [Header("루트 패널(처음엔 꺼두기)")]
    [Tooltip("이 패널 루트(보통 자기 자신)를 연결하세요.")]
    [SerializeField] private GameObject panelRoot;

    [Header("버튼")]
    [SerializeField] private Button btnRetry;
    [SerializeField] private Button btnQuit;

    [Header("동작")]
    [Tooltip("패널이 열릴 때 게임을 멈출지 여부")]
    [SerializeField] private bool pauseTimeOnOpen = true;

    [Header("디버그")]
    [SerializeField] private bool enableHotkey = false;

#if ENABLE_INPUT_SYSTEM
    [SerializeField] private bool useNewInputSystemHotkey = true;
#endif

    private bool _didInitHide;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;

        // ✅ 시작 시 1회만 꺼준다 (여기서만!)
        if (!_didInitHide)
        {
            _didInitHide = true;
            panelRoot.SetActive(false);
        }

        if (btnRetry != null) btnRetry.onClick.AddListener(OnClickRetry);
        if (btnQuit != null) btnQuit.onClick.AddListener(OnClickQuit);
    }

    private void Update()
    {
        if (!enableHotkey) return;

        // 필요하면 여기서도 강제 오픈 가능(테스트용)
        if (Input.GetKeyDown(KeyCode.Home))
            Open("Hotkey(OldInput)");

#if ENABLE_INPUT_SYSTEM
        if (useNewInputSystemHotkey && Keyboard.current != null)
        {
            if (Keyboard.current.homeKey.wasPressedThisFrame)
                Open("Hotkey(NewInputSystem)");
        }
#endif
    }

    public void Open(string reason = "")
    {
        if (panelRoot == null) panelRoot = gameObject;

        panelRoot.SetActive(true);

        if (pauseTimeOnOpen)
            Time.timeScale = 0f;

        Debug.Log($"[DefeatUIController2D] Open reason='{reason}' panel='{panelRoot.name}'", this);
    }

    public void Close()
    {
        if (panelRoot == null) panelRoot = gameObject;

        panelRoot.SetActive(false);
        if (Time.timeScale != 1f) Time.timeScale = 1f;
    }

    private void OnClickRetry()
    {
        Close();
        var active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.name);
    }

    private void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}