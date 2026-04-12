using System;
using UnityEngine;
using _Game.Scripts.Core.Session;

public sealed class StageManager2D : MonoBehaviour
{
    [Header("데이터")]
    [Tooltip("전체 스테이지 카탈로그입니다. 없으면 기본 타이머 모드로 동작합니다.")]
    [SerializeField] private StageCatalogSO stageCatalog;

    [Header("현재 스테이지")]
    [Tooltip("현재 플레이 중인 스테이지 인덱스입니다.")]
    [SerializeField] private int currentStageIndex;

    [Header("참조")]
    [Tooltip("적 스포너입니다.")]
    [SerializeField] private EnemySpawnerTimeline2D spawner;

    [Tooltip("세션 매니저입니다.")]
    [SerializeField] private SessionGameManager2D sessionManager;

    [Tooltip("비워두면 SaveManager2D.Instance를 자동 사용합니다.")]
    [SerializeField] private SaveManager2D saveManager;

    [Header("보스")]
    [Tooltip("현재 스폰된 보스입니다. (런타임)")]
    [SerializeField] private GameObject currentBossInstance;

    [Header("디버그")]
    [SerializeField] private bool debugLog = true;

    // ─── 이벤트 (기존 API 호환) ───

    /// <summary>스테이지 클리어 시 발생합니다. (ClearBridge 등 구독)</summary>
    public event Action OnStageCleared;

    /// <summary>스테이지 시작 시 발생합니다.</summary>
    public event Action<StageDefinitionSO> OnStageStartedWithDef;

    /// <summary>보스 등장 시 발생합니다.</summary>
    public event Action<StageDefinitionSO> OnBossSpawned;

    /// <summary>보스 경고 시 발생합니다.</summary>
    public event Action<string> OnBossWarning;

    // ─── 싱글톤 ───
    public static StageManager2D Instance { get; private set; }

    // ─── 런타임 ───
    private StageDefinitionSO _currentStage;
    private float _elapsed;
    private bool _bossSpawned;
    private bool _cleared;
    private bool _bossWarningShown;
    private bool _stageRunning;
    private GameObject _mapInstance;

    /// <summary>현재 스테이지 정의입니다.</summary>
    public StageDefinitionSO CurrentStage => _currentStage;

    /// <summary>현재 스테이지 인덱스입니다.</summary>
    public int CurrentStageIndex => currentStageIndex;

    /// <summary>스테이지 경과 시간(초)입니다. (HudConnector에서 사용)</summary>
    public float ElapsedTime => _elapsed;

    /// <summary>클리어 여부입니다.</summary>
    public bool IsCleared => _cleared;

    /// <summary>스테이지가 진행 중인지 여부입니다.</summary>
    public bool IsRunning => _stageRunning;

    // ═══════════════════════════════════════════
    //  생명주기
    // ═══════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (saveManager == null)
            saveManager = SaveManager2D.Instance;

        if (sessionManager == null)
            sessionManager = FindFirstObjectByType<SessionGameManager2D>();

        if (spawner == null)
            spawner = FindFirstObjectByType<EnemySpawnerTimeline2D>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!_stageRunning || _cleared) return;

        // 세션 상태 체크 (Playing일 때만 진행)
        if (sessionManager != null && sessionManager.CurrentState != SessionState.Playing)
            return;

        _elapsed += Time.deltaTime;

        // 스테이지 정의가 없으면 단순 타이머 모드
        if (_currentStage == null) return;

        // 보스 경고 (등장 5초 전)
        if (_currentStage.HasBoss && !_bossWarningShown && !_bossSpawned)
        {
            float warningTime = _currentStage.BossSpawnTime - 5f;
            if (warningTime > 0f && _elapsed >= warningTime)
            {
                _bossWarningShown = true;
                string warning = string.IsNullOrWhiteSpace(_currentStage.BossWarningText)
                    ? "강력한 존재가 다가옵니다..."
                    : _currentStage.BossWarningText;
                OnBossWarning?.Invoke(warning);
                Log($"보스 경고: {warning}");
            }
        }

        // 보스 스폰
        if (_currentStage.HasBoss && !_bossSpawned && _elapsed >= _currentStage.BossSpawnTime)
        {
            SpawnBoss();
        }

        // 클리어 조건 체크
        CheckClearCondition();
    }

    // ═══════════════════════════════════════════
    //  공개 API (기존 GameManager2D 호환)
    // ═══════════════════════════════════════════

    /// <summary>
    /// 스테이지를 시작합니다. GameManager2D.StartGame()에서 호출됩니다.
    /// </summary>
    public void BeginStage()
    {
        // 로비에서 설정한 스테이지 인덱스 확인
        if (StageSelectBridge.HasSelection)
        {
            currentStageIndex = StageSelectBridge.SelectedStageIndex;
            StageSelectBridge.Clear();
        }

        _elapsed = 0f;
        _bossSpawned = false;
        _cleared = false;
        _bossWarningShown = false;
        _stageRunning = true;

        // 카탈로그가 있으면 스테이지 정의 로드
        if (stageCatalog != null)
        {
            _currentStage = stageCatalog.GetByIndex(currentStageIndex);
            if (_currentStage != null)
            {
                LoadMap();
                ConfigureSpawner();

                if (_currentStage.IsTutorial)
                    ApplyTutorialSettings();

                OnStageStartedWithDef?.Invoke(_currentStage);
                Log($"스테이지 {currentStageIndex} ({_currentStage.DisplayName}) 시작");
            }
            else
            {
                Log($"스테이지 {currentStageIndex} 정의를 찾을 수 없습니다. 기본 모드로 동작합니다.");
            }
        }
        else
        {
            Log("StageCatalog 없음 — 기본 타이머 모드로 동작합니다.");
        }
    }

    /// <summary>
    /// 스테이지를 종료합니다. GameManager2D.EndGame_Defeat/Victory에서 호출됩니다.
    /// </summary>
    public void EndStage()
    {
        _stageRunning = false;
        Log($"스테이지 {currentStageIndex} 종료 (경과: {_elapsed:F1}초)");
    }

    /// <summary>보스 사망 시 외부에서 호출합니다.</summary>
    public void ReportBossKilled()
    {
        if (_currentStage == null || _cleared) return;

        if (_currentStage.ClearCondition == StageClearCondition.BossKill)
        {
            Log("보스 처치 → 클리어");
            OnClear();
        }
    }

    /// <summary>보스 HP 변경 시 외부에서 호출합니다. (두억시니 1% 연출 등)</summary>
    public void ReportBossHP(float hpRatio)
    {
        if (_currentStage == null || _cleared) return;

        if (_currentStage.ClearCondition == StageClearCondition.BossHPThreshold
            && hpRatio <= _currentStage.BossHPThreshold)
        {
            Log($"보스 HP {hpRatio:P0} → 임계치 도달 → 연출 진입");
            OnClear();
        }
    }

    /// <summary>스테이지를 강제 클리어합니다. (디버그용)</summary>
    [ContextMenu("강제 클리어")]
    public void ForceClear()
    {
        if (_stageRunning && !_cleared)
            OnClear();
    }

    // ═══════════════════════════════════════════
    //  내부
    // ═══════════════════════════════════════════

    private void LoadMap()
    {
        if (_mapInstance != null)
            Destroy(_mapInstance);

        if (_currentStage.MapPrefab != null)
        {
            _mapInstance = Instantiate(_currentStage.MapPrefab);
            _mapInstance.name = $"Map_Stage{_currentStage.StageIndex}";
        }
    }

    private void ConfigureSpawner()
    {
        if (spawner == null || _currentStage == null) return;

        // TODO: EnemySpawnerTimeline2D에 SetStageData() 메서드 추가 후 연결
        // spawner.SetStageData(_currentStage.SpawnTimeline, _currentStage.EnemyRoot,
        //                      _currentStage.BaseSpawnInterval, _currentStage.MaxEnemies);

        Log("스포너 설정 — TODO: SetStageData() 연결 필요");
    }

    private void SpawnBoss()
    {
        _bossSpawned = true;

        if (_currentStage.BossPrefab == null)
        {
            Log("보스 프리팹이 없습니다.");
            return;
        }

        Vector3 spawnPos = Vector3.zero;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            spawnPos = player.transform.position + new Vector3(0f, 8f, 0f);

        currentBossInstance = Instantiate(_currentStage.BossPrefab, spawnPos, Quaternion.identity);
        currentBossInstance.name = $"Boss_{_currentStage.DisplayName}";

        OnBossSpawned?.Invoke(_currentStage);
        Log($"보스 스폰: {currentBossInstance.name}");
    }

    private void CheckClearCondition()
    {
        switch (_currentStage.ClearCondition)
        {
            case StageClearCondition.SurviveTime:
                if (_elapsed >= _currentStage.StageDuration)
                {
                    Log("생존 시간 달성 → 클리어");
                    OnClear();
                }
                break;

            case StageClearCondition.BossKill:
            case StageClearCondition.BossHPThreshold:
                // 외부에서 ReportBossKilled() / ReportBossHP() 호출
                break;

            case StageClearCondition.ClearAllWaves:
                // TODO: 웨이브 완료 체크
                break;
        }
    }

    private void OnClear()
    {
        if (_cleared) return;
        _cleared = true;

        ApplyClearRewards();
        SaveProgress();

        // 기존 API 호환 — ClearBridge가 이 이벤트를 구독
        OnStageCleared?.Invoke();

        Log($"스테이지 {currentStageIndex} 클리어!");
    }

    private void ApplyClearRewards()
    {
        if (_currentStage == null) return;

        if (_currentStage.ClearRewardNyang > 0 && saveManager != null)
        {
            MetaWalletService2D wallet = new MetaWalletService2D(saveManager);
            wallet.AddNyang(_currentStage.ClearRewardNyang, autoSave: false);
        }

        if (_currentStage.HasUnlockCharacter)
        {
            StageProgressSaveData progress = GetProgress();
            if (progress != null)
                progress.UnlockCharacter(_currentStage.UnlockCharacterId);

            Log($"캐릭터 해금: {_currentStage.UnlockCharacterId}");
        }

        if (_currentStage.UnlocksUpgradeSystem)
        {
            StageProgressSaveData progress = GetProgress();
            if (progress != null)
                progress.upgradeSystemUnlocked = true;

            Log("강화 시스템 해금!");
        }
    }

    private void SaveProgress()
    {
        StageProgressSaveData progress = GetProgress();
        if (progress == null) return;

        progress.MarkCleared(currentStageIndex);

        if (saveManager != null)
            saveManager.Save();
    }

    private void ApplyTutorialSettings()
    {
        Log("튜토리얼 모드 활성화 — 사망 불가 등");

        // TODO: PlayerHealth에 무적 모드 추가
        // TODO: UltimateController에 쿨타임 즉시 해제 연결
    }

    private StageProgressSaveData GetProgress()
    {
        if (saveManager == null || saveManager.Data == null) return null;
        saveManager.Data.EnsureDefaults();
        return saveManager.Data.metaProfile.stageProgress;
    }

    private void Log(string msg)
    {
        if (debugLog)
            GameLogger.Log($"[StageManager] {msg}");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        Instance = null;
    }
}

/// <summary>
/// 로비 → 인게임 씬 전환 시 선택한 스테이지 인덱스를 전달하는 브릿지입니다.
/// </summary>
public static class StageSelectBridge
{
    public static int SelectedStageIndex { get; private set; }
    public static bool HasSelection { get; private set; }

    public static void Select(int stageIndex)
    {
        SelectedStageIndex = stageIndex;
        HasSelection = true;
    }

    public static void Clear()
    {
        HasSelection = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        SelectedStageIndex = 0;
        HasSelection = false;
    }
}
