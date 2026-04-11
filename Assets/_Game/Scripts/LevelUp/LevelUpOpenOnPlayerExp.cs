using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class LevelUpOpenOnPlayerExp : MonoBehaviour
{
    [SerializeField] private PlayerExp playerExp;

    [Header("레벨업 시스템")]
    [FormerlySerializedAs("newFlowCoordinator")]
    [SerializeField, Tooltip("레벨업 흐름 코디네이터")]
    private _Game.LevelUp.LevelUpFlowCoordinator flowCoordinator;

    [Header("디버그")]
    [SerializeField] private bool enableLogs = false;

    private void Awake()
    {
        if (playerExp == null)
        {
            playerExp = GetComponent<PlayerExp>();
            if (playerExp == null)
                playerExp = FindFirstObjectByType<PlayerExp>();
        }

        if (flowCoordinator == null)
            flowCoordinator = FindFirstObjectByType<_Game.LevelUp.LevelUpFlowCoordinator>();
    }

    private void OnEnable()
    {
        if (playerExp != null)
            playerExp.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        if (playerExp != null)
            playerExp.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        if (enableLogs)
            GameLogger.Log($"[LevelUp] PlayerExp LevelUp => {newLevel}", this);

        if (flowCoordinator != null)
        {
            flowCoordinator.RequestLevelUp();
        }
        else
        {
            GameLogger.LogWarning("[LevelUp] FlowCoordinator 참조 없음 — 레벨업 UI 열 수 없음", this);
        }
    }
}