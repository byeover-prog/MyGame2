using UnityEngine;

public sealed class ClearBridge : MonoBehaviour
{
    [SerializeField] private StageManager2D stageManager;
    [SerializeField] private ClearUIController clearUI;

    [Header("임시 스테이지 정보 (나중에 SO로 교체)")]
    [SerializeField] private string stageName = "경복궁 외곽 폐허";
    [SerializeField] private int nyangReward = 1250;
    [SerializeField] private int honryeongReward = 80;

    private void Awake()
    {
        if (stageManager == null)
            stageManager = FindFirstObjectByType<StageManager2D>();
        if (clearUI == null)
            clearUI = FindFirstObjectByType<ClearUIController>();
    }

    private void OnEnable()
    {
        if (stageManager != null)
            stageManager.OnStageCleared += HandleStageCleared;
    }

    private void OnDisable()
    {
        if (stageManager != null)
            stageManager.OnStageCleared -= HandleStageCleared;
    }

    private void HandleStageCleared()
    {
        if (clearUI != null)
            clearUI.ShowClearUI(nyangReward, honryeongReward, stageName);
    }
}