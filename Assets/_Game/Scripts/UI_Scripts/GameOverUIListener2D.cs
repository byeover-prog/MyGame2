using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class GameOverUIListener2D : MonoBehaviour
{
    [Header("필수")]
    [Tooltip("GAME OVER 패널 루트 오브젝트(비활성이어도 됨)")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("다시하기 버튼(첫 선택) - 없어도 동작은 함")]
    [SerializeField] private GameObject firstSelect;

    [Header("옵션")]
    [Tooltip("게임오버 시 게임을 멈출지")]
    [SerializeField] private bool pauseOnGameOver = true;

    private bool _shown;

    private void Awake()
    {
        _shown = false;

        // 시작 시 숨김(씬 저장 상태가 어찌됐든 통일)
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void OnEnable()
    {
        RunSignals.PlayerDead += OnPlayerDead;
    }

    private void OnDisable()
    {
        RunSignals.PlayerDead -= OnPlayerDead;
    }

    private void OnPlayerDead()
    {
        if (_shown) return;
        _shown = true;

        // 1) UI 먼저 켠다 (timescale 0이어도 SetActive는 정상 동작)
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // 2) 버튼 선택(패드/키보드 대응). EventSystem이 씬에 있어야 함.
        if (firstSelect != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelect);

        // 3) 그 다음 멈춘다
        if (pauseOnGameOver)
            GamePauseGate2D.Acquire(this);
    }

    // 재시작 시 호출용
    public void HideAndResume()
    {
        _shown = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        GamePauseGate2D.Release(this);
    }
}