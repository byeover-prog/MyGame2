using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ClearUIButtonHandler : MonoBehaviour
{
    [Header("씬 이름")]
    [SerializeField] private string homeSceneName = "Scene_Boot";

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        root.Q<Button>("btn-next").clicked  += OnClickNext;
        root.Q<Button>("btn-retry").clicked += OnClickRetry;
        root.Q<Button>("btn-base").clicked  += OnClickHome;
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