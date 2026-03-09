using UnityEngine;
using _Game.Scripts.Core.Session;

/// <summary>
/// [구현 원리 요약]
/// 게임 한 판(런)의 최상위 조율자입니다.
/// - 각 매니저를 직접 제어하지 않고, "시작/종료 신호"만 보냅니다.
/// - 세부 정책(TimeScale, 입력 허용 등)은 SessionGameManager2D가 담당합니다.
/// - 킬 카운트, 게임 결과 등 "런 단위" 데이터를 모아둡니다.
///
/// 씬 배치: @Managers > GameManager 오브젝트에 이 컴포넌트를 붙이세요.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class GameManager2D : MonoBehaviour
{
    // ───────── 싱글턴 ─────────
    public static GameManager2D Instance { get; private set; }

    // ───────── 인스펙터 참조 ─────────
    [Header("매니저 참조 (비면 자동 탐색)")]
    [Tooltip("세션 상태 FSM을 관리하는 매니저입니다.")]
    [SerializeField] private SessionGameManager2D sessionManager;

    [Tooltip("킬 카운트를 집계하는 컴포넌트입니다.")]
    [SerializeField] private KillCountSource killCountSource;

    [Tooltip("스테이지 진행을 관리하는 매니저입니다.")]
    [SerializeField] private StageManager2D stageManager;

    [Header("게임 시작 설정")]
    [Tooltip("Awake 직후 자동으로 게임을 시작할지 여부입니다.\n편성(로비) 씬에서 넘어오는 구조면 false로 두세요.")]
    [SerializeField] private bool autoStartOnAwake = true;

    [Header("디버그")]
    [SerializeField] private bool log = true;

    // ───────── 런타임 상태 ─────────
    private bool _gameStarted;

    /// <summary> 현재 런이 진행 중인지 </summary>
    public bool IsGameRunning => _gameStarted
        && sessionManager != null
        && sessionManager.CurrentState == SessionState.Playing;

    /// <summary> 현재 킬 수 </summary>
    public int KillCount => killCountSource != null ? killCountSource.KillCount : 0;

    // ───────── 생명주기 ─────────

    private void Awake()
    {
        // 싱글턴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 자동 탐색
        if (sessionManager == null) sessionManager = FindFirstObjectByType<SessionGameManager2D>();
        if (killCountSource == null) killCountSource = FindFirstObjectByType<KillCountSource>();
        if (stageManager == null) stageManager = FindFirstObjectByType<StageManager2D>();

        if (sessionManager == null)
            Debug.LogError("[GameManager2D] SessionGameManager2D를 찾지 못했습니다. 씬에 반드시 있어야 합니다.", this);
    }

    private void Start()
    {
        if (autoStartOnAwake)
            StartGame();
    }

    private void OnEnable()
    {
        RunSignals.PlayerDead += OnPlayerDead;
    }

    private void OnDisable()
    {
        RunSignals.PlayerDead -= OnPlayerDead;
    }

    // ───────── 공개 API ─────────

    /// <summary>
    /// 게임(런) 시작. 로비에서 게임 씬 진입 시 1회 호출합니다.
    /// 순서: 킬 카운트 리셋 → 세션 시작 → 스테이지 시작 신호
    /// </summary>
    public void StartGame()
    {
        if (_gameStarted)
        {
            if (log) Debug.LogWarning("[GameManager2D] 이미 게임이 시작된 상태입니다.", this);
            return;
        }

        _gameStarted = true;

        // 1) 킬 카운트 리셋
        if (killCountSource != null)
            killCountSource.ResetKill();

        // 2) 세션 FSM → Playing
        if (sessionManager != null)
            sessionManager.StartSession();

        // 3) 스테이지 시작
        if (stageManager != null)
            stageManager.BeginStage();

        // 4) 전역 신호 발행 (스포너 등이 구독)
        RunSignals.RaiseStageStarted();

        if (log) Debug.Log("[GameManager2D] 게임 시작", this);
    }

    /// <summary>
    /// 게임 종료 (패배). PlayerDead 신호에 의해 자동 호출됩니다.
    /// </summary>
    public void EndGame_Defeat()
    {
        if (!_gameStarted) return;

        if (sessionManager != null)
            sessionManager.GameOver();

        if (stageManager != null)
            stageManager.EndStage();

        _gameStarted = false;

        if (log) Debug.Log($"[GameManager2D] 게임 종료 (패배) — 킬 수: {KillCount}", this);
    }

    /// <summary>
    /// 게임 종료 (승리). 보스 처치 등 외부에서 호출합니다.
    /// </summary>
    public void EndGame_Victory()
    {
        if (!_gameStarted) return;

        if (sessionManager != null)
            sessionManager.Victory();

        if (stageManager != null)
            stageManager.EndStage();

        _gameStarted = false;

        if (log) Debug.Log($"[GameManager2D] 게임 종료 (승리) — 킬 수: {KillCount}", this);
    }

    /// <summary>
    /// 게임 재시작 (같은 씬 리로드).
    /// </summary>
    public void RestartGame()
    {
        _gameStarted = false;

        // 구독 정리 (씬 리로드 시 이벤트 누수 방지)
        RunSignals.ClearAllSubscribers();

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    // ───────── 이벤트 핸들러 ─────────

    private void OnPlayerDead()
    {
        if (log) Debug.Log("[GameManager2D] PlayerDead 신호 수신 → 패배 처리", this);
        EndGame_Defeat();
    }
}