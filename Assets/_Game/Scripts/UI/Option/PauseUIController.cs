using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PauseUIController : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private InGameOptionPanel2D settingPanel;

    [Header("버튼")]
    [SerializeField] private Button btnResum;
    [SerializeField] private Button btnSetting;
    [SerializeField] private Button btnGoMain;
    [SerializeField] private Button btnEndGame;

    private bool _isOpen;

    private void Awake()
    {
        btnResum.onClick.AddListener(Close);
        btnSetting.onClick.AddListener(OpenSetting);
        btnGoMain.onClick.AddListener(GoMain);
        btnEndGame.onClick.AddListener(EndGame);

        SetOpen(false);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        
        if (!_isOpen && GamePauseGate2D.IsPaused) return;
        
        // 설정 열려있으면 설정만 닫기
        if (settingPanel.gameObject.activeSelf)
        {
            settingPanel.Close();
            return;
        }

        if (_isOpen) Close();
        else Open();
    }

    public void Open()
    {
        SetOpen(true);
        GamePauseGate2D.Acquire(this);
    }

    public void Close()
    {
        SetOpen(false);
        GamePauseGate2D.Release(this);
    }

    private void SetOpen(bool open)
    {
        _isOpen = open;
        
        pauseCanvasGroup.gameObject.SetActive(open);
        
        if (pauseCanvasGroup == null) return;
        pauseCanvasGroup.alpha = open ? 1f : 0f;
        pauseCanvasGroup.blocksRaycasts = open;
        pauseCanvasGroup.interactable = open;
    }

    private void OpenSetting()
    {
        Debug.Log("OpenSetting 호출");
        settingPanel.Open();
    }

    private void GoMain()
    {
        GamePauseGate2D.ClearAll();
        StartCoroutine(LoadScene("Scene_Boot"));
    }

    private void EndGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private System.Collections.IEnumerator LoadScene(string sceneName)
    {
        yield return null;
        SceneManager.LoadScene(sceneName);
    }
}