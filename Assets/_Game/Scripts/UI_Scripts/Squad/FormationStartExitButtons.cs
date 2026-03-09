using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FormationStartExitButtons : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;

    [Header("시작 시 이동할 씬 이름")]
    [SerializeField] private string startSceneName = "Scene_Game";

    [Header("나가기 확인(선택)")]
    [SerializeField] private bool confirmExit = false;

    private void Awake()
    {
        // 인스펙터 미지정 시 안전장치(같은 오브젝트에 붙어있다면 자동)
        if (startButton == null) startButton = GetComponentInChildren<Button>();
    }

    private void OnEnable()
    {
        if (startButton != null) startButton.onClick.AddListener(OnClickStart);
        if (exitButton != null) exitButton.onClick.AddListener(OnClickExit);
    }

    private void OnDisable()
    {
        if (startButton != null) startButton.onClick.RemoveListener(OnClickStart);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnClickExit);
    }

    private void OnClickStart()
    {
        if (string.IsNullOrWhiteSpace(startSceneName))
        {
            Debug.LogError("[FormationStartExitButtons] 시작 씬 이름이 비어있습니다.");
            return;
        }

        // 씬이 Build Settings에 등록되어 있어야 함.
        SceneManager.LoadScene(startSceneName);
    }

    private void OnClickExit()
    {
        if (confirmExit)
        {
            // 프로토타입에서는 UI 확인창 구현 안 함(원하면 다음에 붙이자)
            Debug.Log("[FormationStartExitButtons] confirmExit=true 이지만 확인창은 아직 미구현. 바로 종료합니다.");
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}