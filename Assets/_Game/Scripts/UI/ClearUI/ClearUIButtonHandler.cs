using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ClearUIButtonHandler : MonoBehaviour
{
    [Header("씬 이름")]
    [SerializeField] private string homeSceneName = "Scene_Boot";

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null)
        {
            GameLogger.LogWarning("[ClearUIButtonHandler] UIDocument 미연결", this);
            return;
        }

        var root = doc.rootVisualElement;
        if (root == null)
        {
            GameLogger.LogWarning("[ClearUIButtonHandler] rootVisualElement null", this);
            return;
        }

        var btnNext  = root.Q<Button>("btn-next");
        var btnRetry = root.Q<Button>("btn-retry");
        var btnBase  = root.Q<Button>("btn-base");

        if (btnNext  != null) btnNext.clicked  += OnClickNext;
        if (btnRetry != null) btnRetry.clicked += OnClickRetry;
        if (btnBase  != null) btnBase.clicked  += OnClickHome;
    }

    private void OnClickNext()
    {
        SceneManager.LoadScene("Scene_Boot"); // 임시
    }

    private void OnClickRetry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnClickHome()
    {
        SceneManager.LoadScene(homeSceneName);
    }
}