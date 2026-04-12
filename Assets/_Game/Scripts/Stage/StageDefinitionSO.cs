using System;
using System.Collections.Generic;
using UnityEngine;

// 스토리 모드 스테이지 하나의 정의입니다.
// 0~12까지 총 13개의 SO 에셋을 만들어 StageCatalogSO에 등록합니다.

[CreateAssetMenu(menuName = "혼령검/스테이지/스테이지 정의", fileName = "StageDefinition_")]
public sealed class StageDefinitionSO : ScriptableObject
{
    // 식별
    [Header("식별")]
    [Tooltip("스테이지 번호입니다. (0~12)")]
    [SerializeField] private int stageIndex;

    [Tooltip("UI에 표시할 스테이지 이름입니다.")]
    [SerializeField] private string displayName;

    [Tooltip("스테이지 부제목입니다.")]
    [SerializeField] private string subtitle;

    [Tooltip("챕터 표시 텍스트입니다. (예: CHAPTER I)")]
    [SerializeField] private string chapterText;

    // 시간/클리어
    [Header("시간 / 클리어")]
    [Tooltip("스테이지 제한 시간(초)입니다.")]
    [Min(0f)]
    [SerializeField] private float stageDuration = 300f;

    [Tooltip("클리어 조건입니다.")]
    [SerializeField] private StageClearCondition clearCondition = StageClearCondition.SurviveTime;

    [Tooltip("BossHPThreshold 조건일 때 보스 HP 비율 (0~1)입니다. 예: 0.01 = 1%")]
    [Range(0f, 1f)]
    [SerializeField] private float bossHPThreshold = 0.01f;

    // 스폰
    [Header("적 스폰")]
    [Tooltip("이 스테이지의 적 스폰 타임라인입니다.")]
    [SerializeField] private EnemySpawnTimelineSO spawnTimeline;

    [Tooltip("적 데이터 루트입니다.")]
    [SerializeField] private EnemyRootSO enemyRoot;

    [Tooltip("스폰 간격 배율 커브입니다. (없으면 기본 1.0)")]
    [SerializeField] private CasualSpawnCurveConfigSO spawnCurve;

    [Tooltip("기본 스폰 간격(초)입니다.")]
    [Min(0.1f)]
    [SerializeField] private float baseSpawnInterval = 1.0f;

    [Tooltip("동시 최대 적 수입니다.")]
    [Min(1)]
    [SerializeField] private int maxEnemies = 60;

    // 보스
    [Header("보스")]
    [Tooltip("보스 프리팹입니다. (보스 스테이지만)")]
    [SerializeField] private GameObject bossPrefab;

    [Tooltip("보스 등장 시간(초)입니다. 0이면 즉시.")]
    [Min(0f)]
    [SerializeField] private float bossSpawnTime;

    [Tooltip("보스 등장 전 경고 텍스트입니다.")]
    [SerializeField] private string bossWarningText;

    // 맵
    [Header("맵")]
    [Tooltip("맵 프리팹입니다. (현우님 작업물)")]
    [SerializeField] private GameObject mapPrefab;

    [Tooltip("맵 크기 (정사각형 기준 한 변 길이)입니다.")]
    [Min(10f)]
    [SerializeField] private float mapSize = 30f;

    // 방해요소
    [Header("방해요소")]
    [Tooltip("이 스테이지에서 활성화되는 방해요소 목록입니다.")]
    [SerializeField] private List<HazardConfig> hazards = new List<HazardConfig>(6);

    // 캐릭터 해금
    [Header("클리어 보상")]
    [Tooltip("이 스테이지 클리어 시 해금되는 캐릭터 ID입니다. (빈칸이면 없음)")]
    [SerializeField] private string unlockCharacterId;

    [Tooltip("이 스테이지 클리어 시 강화 시스템 해금 여부입니다.")]
    [SerializeField] private bool unlocksUpgradeSystem;

    [Tooltip("클리어 보상 냥입니다.")]
    [Min(0)]
    [SerializeField] private int clearRewardNyang;

    // 스토리
    [Header("스토리 연출")]
    [Tooltip("스테이지 시작 전 스토리 대화 데이터입니다. (비주얼 노벨)")]
    [SerializeField] private TextAsset storyDialogueData;

    [Tooltip("스토리 스킵 가능 여부입니다.")]
    [SerializeField] private bool storySkippable = true;

    // 튜토리얼
    [Header("튜토리얼")]
    [Tooltip("튜토리얼 스테이지 여부입니다. (사망 불가, 궁극기 쿨 즉시 해제 등)")]
    [SerializeField] private bool isTutorial;

    [Tooltip("궁극기 쿨타임 즉시 해제 여부입니다.")]
    [SerializeField] private bool instantUltimateCooldown;

    // 전환 연출
    [Header("전환")]
    [Tooltip("클리어 후 특수 연출 타입입니다.")]
    [SerializeField] private StageTransitionType transitionType = StageTransitionType.FadeOut;

    [Tooltip("클리어 후 연출용 프리팹/타임라인입니다.")]
    [SerializeField] private GameObject transitionPrefab;
    
    //  프로퍼티

    public int StageIndex => stageIndex;
    public string DisplayName => displayName;
    public string Subtitle => subtitle;
    public string ChapterText => chapterText;
    public float StageDuration => stageDuration;
    public StageClearCondition ClearCondition => clearCondition;
    public float BossHPThreshold => bossHPThreshold;
    public EnemySpawnTimelineSO SpawnTimeline => spawnTimeline;
    public EnemyRootSO EnemyRoot => enemyRoot;
    public CasualSpawnCurveConfigSO SpawnCurve => spawnCurve;
    public float BaseSpawnInterval => baseSpawnInterval;
    public int MaxEnemies => maxEnemies;
    public GameObject BossPrefab => bossPrefab;
    public float BossSpawnTime => bossSpawnTime;
    public string BossWarningText => bossWarningText;
    public GameObject MapPrefab => mapPrefab;
    public float MapSize => mapSize;
    public IReadOnlyList<HazardConfig> Hazards => hazards;
    public string UnlockCharacterId => unlockCharacterId;
    public bool UnlocksUpgradeSystem => unlocksUpgradeSystem;
    public int ClearRewardNyang => clearRewardNyang;
    public TextAsset StoryDialogueData => storyDialogueData;
    public bool StorySkippable => storySkippable;
    public bool IsTutorial => isTutorial;
    public bool InstantUltimateCooldown => instantUltimateCooldown;
    public StageTransitionType TransitionType => transitionType;
    public GameObject TransitionPrefab => transitionPrefab;
    public bool HasBoss => bossPrefab != null;
    public bool HasStory => storyDialogueData != null;
    public bool HasUnlockCharacter => !string.IsNullOrWhiteSpace(unlockCharacterId);
}

/// <summary>
/// 스테이지 전환 연출 타입입니다.
/// </summary>
public enum StageTransitionType
{
    /// <summary>단순 페이드 아웃/인</summary>
    FadeOut = 0,

    /// <summary>포탈 진입</summary>
    Portal = 1,

    /// <summary>보스 처치 후 길 열림</summary>
    PathOpen = 2,

    /// <summary>추락 연출 (두억시니 → 심연)</summary>
    FallDown = 3,

    /// <summary>커스텀 타임라인</summary>
    Custom = 4,
}
