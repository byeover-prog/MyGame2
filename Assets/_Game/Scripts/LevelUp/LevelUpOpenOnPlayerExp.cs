using UnityEngine;

/// <summary>
/// PlayerExp 레벨업 이벤트를 레벨업 시스템으로 브릿지.
/// - useNewLevelUpFlow = true → 새 4장 시스템 (LevelUpFlowCoordinator)
/// - useNewLevelUpFlow = false → 기존 3장 시스템 (GameSignals → LevelUpOrchestrator)
/// </summary>
[DisallowMultipleComponent]
public sealed class LevelUpOpenOnPlayerExp : MonoBehaviour
{
    [SerializeField] private PlayerExp playerExp;

    // 신규 4장 레벨업 시스템 ────────────────────
    [Header("신규 4장 레벨업 시스템")]
    [Tooltip("true면 새 4장 레벨업 시스템을 사용합니다. false면 기존 3장 시스템 유지.")]
    [SerializeField] private bool useNewLevelUpFlow = false;

    [SerializeField, Tooltip("새 레벨업 흐름 코디네이터")]
    private _Game.LevelUp.LevelUpFlowCoordinator newFlowCoordinator;
    // 추가 끝 ─────────────────────────────────

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
            Debug.Log($"[LevelUp] PlayerExp LevelUp => {newLevel}", this);

        // 새 시스템 분기 ───────────────────────
        if (useNewLevelUpFlow && newFlowCoordinator != null)
        {
            newFlowCoordinator.RequestLevelUp();
            return;
        }
        // 분기 끝 ─────────────────────────────

        // 기존 경로 (LevelUpOrchestrator → OfferService → 3장 패널)
        GameSignals.RaiseLevelUpOpenRequested();
    }
}