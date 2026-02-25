// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - RunSignals의 PlayerDead 신호를 받으면 게임오버 패널을 즉시 켠다.
// - Time.timeScale이 0이어도 SetActive는 동작하므로 "UI가 안 뜨는" 문제를 강제로 차단한다.
[DisallowMultipleComponent]
public sealed class GameOverUIListener2D : MonoBehaviour
{
    [Header("게임오버 패널(필수)")]
    [Tooltip("Canvas 아래 GameOverPanel 오브젝트를 넣으세요(비활성 상태여도 됨).")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("옵션")]
    [Tooltip("게임오버 시 게임을 멈출지")]
    [SerializeField] private bool pauseOnGameOver = true;

    private void Awake()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void OnEnable()
    {
        // RunSignals 이벤트 이름이 다르면 여기서 컴파일 에러가 납니다.
        // 그 경우 RunSignals.cs를 열어서 'PlayerDead' 이벤트 이름을 확인 후 아래 줄을 맞추세요.
        RunSignals.PlayerDead += OnPlayerDead;
    }

    private void OnDisable()
    {
        RunSignals.PlayerDead -= OnPlayerDead;
    }

    private void OnPlayerDead()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (pauseOnGameOver)
            Time.timeScale = 0f;
    }
}