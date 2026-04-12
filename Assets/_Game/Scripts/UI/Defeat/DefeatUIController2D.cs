using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DefeatUIController2D : MonoBehaviour
{
    [Header("Panel Root")]
    [Tooltip("패널 루트(보통 자기 자신).")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("플레이 시작 시 1회 숨김")]
    [SerializeField] private bool hideOnAwake = true;

    [Header("Buttons")]
    [SerializeField] private Button btnRetry;
    [SerializeField] private Button btnQuit;

    [Tooltip("패널 오픈 시 첫 선택(게임패드/키보드용)")]
    [SerializeField] private GameObject firstSelect;

    private bool _didInitHide;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;

        if (hideOnAwake && !_didInitHide)
        {
            _didInitHide = true;
            panelRoot.SetActive(false);
        }

        if (btnRetry != null) btnRetry.onClick.AddListener(OnClickRetry);
        if (btnQuit != null) btnQuit.onClick.AddListener(OnClickQuit);
    }

    public void Open(string reason, bool pauseTimeScale)
    {
        if (panelRoot == null) panelRoot = gameObject;

        panelRoot.SetActive(true);

        EnsureEventSystem();

        if (firstSelect != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelect);

        if (pauseTimeScale)
            GamePauseGate2D.Acquire(this);

        GameLogger.Log($"[DefeatUIController2D] Open reason='{reason}'", this);
    }

    public void Close()
    {
        if (panelRoot == null) panelRoot = gameObject;

        panelRoot.SetActive(false);
        GamePauseGate2D.Release(this);
    }

    public void ForceCanvasVisible()
    {
        // ScreenSpace-Camera에서 카메라가 비면 UI가 안 보일 수 있음
        var canvases = GetComponentsInParent<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            if (c == null) continue;

            c.enabled = true;
            c.gameObject.SetActive(true);

            if (c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == null)
                c.worldCamera = Camera.main;

            // 다른 UI에 가려지는 문제 방지(강제 맨 위)
            c.overrideSorting = true;
            c.sortingOrder = 9999;
        }

        var groups = GetComponentsInParent<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            var g = groups[i];
            if (g == null) continue;

            g.alpha = 1f;
            g.interactable = true;
            g.blocksRaycasts = true;
        }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
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