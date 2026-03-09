using System;
using UnityEngine;
using _Game.Scripts.Core.Session;

/// <summary>
/// [구현 원리 요약]
/// 스테이지(맵/웨이브) 진행을 관리합니다.
/// - 경과 시간에 따라 난이도 페이즈를 전환합니다.
/// - 보스 등장/스테이지 클리어 조건을 판단합니다.
/// - 적 스폰 자체는 EnemySpawner2D가 담당하며, 이 매니저는 "지금 어떤 페이즈인지"만 알려줍니다.
///
/// 씬 배치: @Managers > StageManager 오브젝트에 이 컴포넌트를 붙이세요.
/// </summary>
[DisallowMultipleComponent]
public sealed class StageManager2D : MonoBehaviour
{
    // ───────── 싱글턴 ─────────
    public static StageManager2D Instance { get; private set; }

    // ───────── 난이도 페이즈 정의 ─────────

    /// <summary> 현재 스테이지 진행 단계 </summary>
    public enum StagePhase
    {
        Idle,       // 시작 전 / 종료 후
        Early,      // 초반 (0 ~ earlyEndSec)
        Mid,        // 중반 (earlyEndSec ~ midEndSec)
        Late,       // 후반 (midEndSec ~ bossSpawnSec)
        Boss,       // 보스 페이즈
        Cleared     // 스테이지 클리어
    }

    // ───────── 인스펙터 ─────────

    [Header("페이즈 시간 구간 (초)")]
    [Tooltip("초반 → 중반 전환 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float earlyEndSec = 60f;

    [Tooltip("중반 → 후반 전환 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float midEndSec = 300f;

    [Tooltip("보스 등장 시간입니다.\nSessionGameManager2D의 bossSpawnTime과 동일하게 맞추세요.")]
    [Min(0f)]
    [SerializeField] private float bossSpawnSec = 1200f;

    [Header("참조 (비면 자동 탐색)")]
    [Tooltip("세션 매니저에서 경과 시간을 읽습니다.")]
    [SerializeField] private SessionGameManager2D sessionManager;

    [Header("디버그")]
    [SerializeField] private bool log = true;

    // ───────── 런타임 상태 ─────────

    [Header("현재 상태 (읽기 전용)")]
    [Tooltip("현재 스테이지 페이즈입니다.")]
    [SerializeField] private StagePhase currentPhase = StagePhase.Idle;

    private bool _stageActive;

    // ───────── 이벤트 ─────────

    /// <summary> 페이즈가 변경될 때 발행 (이전, 다음) </summary>
    public event Action<StagePhase, StagePhase> OnPhaseChanged;

    /// <summary> 보스 페이즈 진입 시 발행 (EnemySpawner 등이 구독) </summary>
    public event Action OnBossPhaseEntered;

    /// <summary> 스테이지 클리어 시 발행 </summary>
    public event Action OnStageCleared;

    // ───────── 프로퍼티 ─────────

    /// <summary> 현재 페이즈 </summary>
    public StagePhase CurrentPhase => currentPhase;

    /// <summary> 스테이지 경과 시간 (SessionGameManager 기준) </summary>
    public float ElapsedTime => sessionManager != null ? sessionManager.SessionTime : 0f;

    /// <summary> 스테이지가 진행 중인지 </summary>
    public bool IsActive => _stageActive;

    // ───────── 생명주기 ─────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (sessionManager == null) sessionManager = FindFirstObjectByType<SessionGameManager2D>();
    }

    private void Update()
    {
        if (!_stageActive) return;
        if (sessionManager == null) return;
        if (sessionManager.CurrentState != SessionState.Playing) return;

        UpdatePhase();
    }

    // ───────── 공개 API (GameManager2D가 호출) ─────────

    /// <summary>
    /// 스테이지 시작. GameManager2D.StartGame()에서 호출됩니다.
    /// </summary>
    public void BeginStage()
    {
        _stageActive = true;
        SetPhase(StagePhase.Early);

        if (log) Debug.Log("[StageManager2D] 스테이지 시작", this);
    }

    /// <summary>
    /// 스테이지 종료. GameManager2D.EndGame_xxx()에서 호출됩니다.
    /// </summary>
    public void EndStage()
    {
        _stageActive = false;
        SetPhase(StagePhase.Idle);

        if (log) Debug.Log("[StageManager2D] 스테이지 종료", this);
    }

    /// <summary>
    /// 보스를 처치했을 때 호출합니다. → 클리어 처리.
    /// </summary>
    public void NotifyBossDefeated()
    {
        if (currentPhase != StagePhase.Boss) return;

        SetPhase(StagePhase.Cleared);
        OnStageCleared?.Invoke();

        if (log) Debug.Log("[StageManager2D] 보스 처치 → 스테이지 클리어!", this);
    }

    // ───────── 내부 로직 ─────────

    private void UpdatePhase()
    {
        float t = ElapsedTime;
        StagePhase target;

        if (t >= bossSpawnSec)
            target = StagePhase.Boss;
        else if (t >= midEndSec)
            target = StagePhase.Late;
        else if (t >= earlyEndSec)
            target = StagePhase.Mid;
        else
            target = StagePhase.Early;

        if (target != currentPhase && currentPhase != StagePhase.Cleared)
            SetPhase(target);
    }

    private void SetPhase(StagePhase next)
    {
        if (currentPhase == next) return;

        var prev = currentPhase;
        currentPhase = next;

        OnPhaseChanged?.Invoke(prev, next);

        if (next == StagePhase.Boss)
        {
            OnBossPhaseEntered?.Invoke();
            if (log) Debug.Log("[StageManager2D] 보스 페이즈 진입!", this);
        }

        if (log && next != StagePhase.Boss)
            Debug.Log($"[StageManager2D] 페이즈 전환: {prev} → {next} (경과 {ElapsedTime:F1}초)", this);
    }
}