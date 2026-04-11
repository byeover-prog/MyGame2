using System;
using UnityEngine;

namespace _Game.Scripts.Core.Session
{
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

        [Tooltip("첫 적 스폰을 지연할 시간(초).\n예) 3이면 게임 시작 후 3초부터 스폰 허용.")]
        [Min(0f)]
        [SerializeField] private float firstSpawnDelay = 3f;

        [Tooltip("보스 등장 시간(초). 예: 20분=1200")]
        [Min(0f)]
        [SerializeField] private float bossSpawnTime = 1200f;

        [Header("전역 정책(상태에 의해 자동 계산)")]
        [Tooltip("입력 허용 여부(자동 계산).")]
        [SerializeField] private bool allowPlayerInput = true;

        [Tooltip("적 스폰 허용 여부(자동 계산).\nPlaying 상태여도 firstSpawnDelay 전에는 false입니다.")]
        [SerializeField] private bool allowEnemySpawn = true;

        [Tooltip("전투 로직(무기/투사체 등) 업데이트 허용 여부(자동 계산).")]
        [SerializeField] private bool allowCombatUpdate = true;
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
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // 초기 상태 적용
            ApplyPoliciesForState(currentState);
        }

        private void Update()
        {
            // Playing에서만 시간 진행 + 동적 정책 갱신
            if (currentState == SessionState.Playing)
            {
                sessionTime += Time.deltaTime;

                // 첫 스폰 지연: sessionTime 기준으로 스폰 허용을 갱신
                allowEnemySpawn = (sessionTime >= firstSpawnDelay);

                if (!_bossEventFired && sessionTime >= bossSpawnTime)
                {
                    _bossEventFired = true;
                    OnBossSpawnTimeReached?.Invoke();
                }
            }
        }
        
        public void StartSession()
        {
            sessionTime = 0f;
            _bossEventFired = false;

            // StartSession 직후에는 firstSpawnDelay가 적용되어야 하므로
            // Playing으로 전환 후 Update에서 allowEnemySpawn이 자동 갱신되게 한다.
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
            // 모든 시간 정지/복구는 GamePauseGate2D를 경유한다.

            switch (state)
            {
                case SessionState.Boot:
                    GamePauseGate2D.Release(this);
                    allowPlayerInput = false;
                    allowEnemySpawn = false;
                    allowCombatUpdate = false;
                    break;

                case SessionState.Playing:
                    GamePauseGate2D.Release(this);
                    allowPlayerInput = true;

                    // Playing이라도 첫 스폰 지연이 있을 수 있으니
                    // 여기서 true로 고정하지 않고, Update에서 sessionTime 기반으로 갱신한다.
                    allowEnemySpawn = (sessionTime >= firstSpawnDelay);

                    allowCombatUpdate = true;
                    break;

                case SessionState.LevelUpChoice:
                    // 카드 선택 중에는 시간 정지
                    GamePauseGate2D.Acquire(this);
                    allowPlayerInput = false;
                    allowEnemySpawn = false;
                    allowCombatUpdate = false;
                    break;

                case SessionState.Paused:
                    GamePauseGate2D.Acquire(this);
                    allowPlayerInput = false;
                    allowEnemySpawn = false;
                    allowCombatUpdate = false;
                    break;

                case SessionState.GameOver:
                case SessionState.Victory:
                    GamePauseGate2D.Acquire(this);
                    allowPlayerInput = false;
                    allowEnemySpawn = false;
                    allowCombatUpdate = false;
                    break;
            }
        }
    }
}