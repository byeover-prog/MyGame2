// UTF-8
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DefeatUIController2D : MonoBehaviour
{
    [Header("루트 패널(처음엔 꺼두기)")]
    [SerializeField] private GameObject panelRoot;

    [Header("버튼")]
    [SerializeField] private Button btnRetry;
    [SerializeField] private Button btnQuit;

    [Header("동작")]
    [Tooltip("패배 시 시간 정지")]
    [SerializeField] private bool pauseTimeOnOpen = true;

    private bool _opened;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (btnRetry != null) btnRetry.onClick.AddListener(OnRetry);
        if (btnQuit != null) btnQuit.onClick.AddListener(OnQuit);
    }

    public void Open()
    {
        if (_opened) return;
        _opened = true;

        if (panelRoot != null) panelRoot.SetActive(true);
        if (pauseTimeOnOpen) Time.timeScale = 0f;
    }

    private void OnRetry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnQuit()
    {
        Time.timeScale = 1f;
        // 에디터에서는 종료 안 됨
        Application.Quit();
    }
}