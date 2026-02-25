// UTF-8
using System;
using UnityEngine;

namespace _Game.Scripts.Core.Session
{
    // [구현 원리 요약]
    // - 세션(한 판)의 상태를 enum FSM으로 단일 관리한다.
    // - TimeScale, 입력 허용, 스폰 허용 같은 "전역 정책"은 오직 여기서만 결정한다.
    // - 다른 시스템은 "현재 상태 조회" 또는 "상태 변경 요청"만 한다(직접 TimeScale 만지지 않는다).
    public enum SessionState
    {
        Boot,           // 초기화 중
        Playing,        // 전투 진행
        LevelUpChoice,  // 레벨업 카드 선택(일시정지)
        Paused,         // 수동 일시정지(메뉴)
        GameOver,       // 패배
        Victory         // 승리
    }

    [DisallowMultipleComponent]
    public sealed class SessionGameManager2D : MonoBehaviour
    {
        public static SessionGameManager2D Instance { get; private set; }

        [Header("세션 상태(읽기 전용)")]
        [Tooltip("현재 세션 상태입니다.\n다른 시스템은 이 값을 보고 동작/정지를 결정하세요.")]
        [SerializeField] private SessionState currentState = SessionState.Boot;

        [Header("타이머")]
        [Tooltip("세션 진행 시간(초)입니다. Playing에서만 증가합니다.")]
        [SerializeField] private float sessionTime;

        [Tooltip("보스 등장 시간(초). 예: 20분=1200")]
        [Min(0f)]
        [SerializeField] private float bossSpawnTime = 1200f;

        [Header("전역 정책(상태에 의해 자동 계산)")]
        [Tooltip("입력 허용 여부(자동 계산).")]
        [SerializeField] private bool allowPlayerInput = true;

        [Tooltip("적 스폰 허용 여부(자동 계산).")]
        [SerializeField] private bool allowEnemySpawn = true;

        [Tooltip("전투 로직(무기/투사체 등) 업데이트 허용 여부(자동 계산).")]
        [SerializeField] private bool allowCombatUpdate = true;

        // 상태 변경 이벤트(1:N 반응용)
        public event Action<SessionState, SessionState> OnStateChanged;

        // 보스 타이밍 도달 이벤트(스폰 시스템이 구독)
        public event Action OnBossSpawnTimeReached;

        private bool _bossEventFired;

        public SessionState CurrentState => currentState;
        public float SessionTime => sessionTime;
        public bool AllowPlayerInput => allowPlayerInput;
        public bool AllowEnemySpawn => allowEnemySpawn;
        public bool AllowCombatUpdate => allowCombatUpdate;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 초기 상태 적용
            ApplyPoliciesForState(currentState);
        }

        private void Update()
        {
            // Playing에서만 시간 진행
            if (currentState == SessionState.Playing)
            {
                sessionTime += Time.deltaTime;

                if (!_bossEventFired && sessionTime >= bossSpawnTime)
                {
                    _bossEventFired = true;
                    OnBossSpawnTimeReached?.Invoke();
                }
            }
        }

        // -------------------------
        // 상태 전환 API (외부 호출)
        // -------------------------

        public void StartSession()
        {
            sessionTime = 0f;
            _bossEventFired = false;
            SetState(SessionState.Playing);
        }

        public void EnterLevelUpChoice()
        {
            // 전투 중에만 진입 허용(안전장치)
            if (currentState != SessionState.Playing) return;
            SetState(SessionState.LevelUpChoice);
        }

        public void ExitLevelUpChoice()
        {
            // 선택 UI에서만 복귀 허용(안전장치)
            if (currentState != SessionState.LevelUpChoice) return;
            SetState(SessionState.Playing);
        }

        public void TogglePause()
        {
            // GameOver/Victory에서는 일시정지 토글 금지
            if (currentState == SessionState.GameOver || currentState == SessionState.Victory) return;

            if (currentState == SessionState.Paused) SetState(SessionState.Playing);
            else if (currentState == SessionState.Playing) SetState(SessionState.Paused);
            // LevelUpChoice 중에는 수동 Pause 토글 무시(정책 충돌 방지)
        }

        public void GameOver()
        {
            SetState(SessionState.GameOver);
        }

        public void Victory()
        {
            SetState(SessionState.Victory);
        }

        // -------------------------
        // 내부 구현
        // -------------------------

        private void SetState(SessionState next)
        {
            if (currentState == next) return;

            var prev = currentState;
            currentState = next;

            ApplyPoliciesForState(currentState);
            OnStateChanged?.Invoke(prev, currentState);
        }

        private void ApplyPoliciesForState(SessionState state)
        {
            // 상태별 전역 정책: "여기서만" 결정한다.
            // 다른 시스템이 Time.timeScale을 직접 만지면 나중에 먹통/충돌의 원인이 된다.

            switch (state)
            {
                case SessionState.Boot:
                    Time.timeScale = 1f;
                    allowPlayerInput = false;
                    allowEnemySpawn = false;
                    allowCombatUpdate = false;
                    break;

                case SessionState.Playing:
                    Time.timeScale = 1f;
                    allowPlayerInput = true;
                    allowEnemySpawn = true;
                    allowCombatUpdate = true;
                    break;

                case SessionState.LevelUpChoice:
                    // 카드 선택 중에는 시간 정지(바서류 방식)
                    Time.timeScale = 0f;
                    allowPlayerInput = false;
                    allowEnemySpawn = false;
                    allowCombatUpdate = false;
                    break;

                case SessionState.Paused:
                    Time.timeScale = 0f;
                    allowPlayerInput = false;
                    allowEnemySpawn = false;
                    allowCombatUpdate = false;
                    break;

                case SessionState.GameOver:
                case SessionState.Victory:
                    Time.timeScale = 0f;
                    allowPlayerInput = false;
                    allowEnemySpawn = false;
                    allowCombatUpdate = false;
                    break;
            }
        }
    }
}