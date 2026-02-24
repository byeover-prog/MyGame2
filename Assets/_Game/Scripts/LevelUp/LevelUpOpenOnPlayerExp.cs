using UnityEngine;

/// <summary>
/// PlayerExp 레벨업 이벤트를 GameSignals로 브릿지.
/// - PlayerExp는 "레벨업이 났다"까지만 책임
/// - 레벨업 UI/오퍼/시간정지 등은 LevelUp 시스템이 책임
/// </summary>
[DisallowMultipleComponent]
public sealed class LevelUpOpenOnPlayerExp : MonoBehaviour
{
    [SerializeField] private PlayerExp playerExp;

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

        GameSignals.RaiseLevelUpOpenRequested();
    }
}
