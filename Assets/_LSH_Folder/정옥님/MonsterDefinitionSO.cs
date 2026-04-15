using UnityEngine;

/// <summary>
/// 몬스터 1종의 기준 데이터를 담는 ScriptableObject입니다.
///
/// 왜 이 SO를 먼저 고정하는가:
/// - 스포너가 무엇을 생성할지 판단하는 기준이 됩니다.
/// - 런타임 적용기가 어떤 수치를 주입할지 결정하는 기준이 됩니다.
/// - alive count 포함 여부 같은 스폰 규칙도 이 데이터 기준으로 관리하기 위함입니다.
///
/// 이번 단계에서는 기존 SO 에셋 직렬화 호환을 우선하므로,
/// 기존 필드명과 기존 float 타입은 가능한 한 유지합니다.
/// </summary>
[CreateAssetMenu(menuName = "Game/Monster/MonsterDefinition")]
public class MonsterDefinitionSO : ScriptableObject
{
    [Header("====1. 식별 정보====")]
    [SerializeField, Tooltip("몬스터 고유 ID입니다. 카탈로그 조회, 스폰 타임라인 등록, 이후 런타임 식별 기준으로 사용합니다." +
                             "\n공백 없이 snake_case를 사용하세요. 예: normal_cat")]
    private string monsterId;

    [SerializeField, Tooltip("Inspector와 디버그 로그에서 구분할 표시 이름입니다." +
                             "\n실제 스폰 키는 monsterId이므로, 표시용 이름은 읽기 쉽게 작성하면 됩니다.")]
    private string displayName;

    [SerializeField, Tooltip("몬스터의 등급 분류입니다." +
                             "\n일반 / 엘리트 / 보스 구분에 사용합니다." +
                             "\n행동 방식 구분은 behaviorType에서 따로 설정합니다.")]
    private MonsterType monsterType;

    [SerializeField, Tooltip("몬스터의 행동 분류입니다." +
                             "\n추적형인지, 원거리형인지 런타임 분기 기준으로 사용합니다." +
                             "\n현재 1차 단계에서는 Chase, Ranged만 사용합니다.")]
    private MonsterBehaviorType behaviorType = MonsterBehaviorType.Chase;

    [Header("====2. 프리팹 연결====")]
    [SerializeField, Tooltip("실제로 스폰할 몬스터 프리팹입니다." +
                             "\n프리팹에는 비주얼, 콜라이더, 기본 컴포넌트 껍데기만 두고," +
                             "\n실제 수치는 런타임에 이 SO에서 주입할 예정입니다.")]
    private GameObject monsterPrefab;

    [SerializeField, Tooltip("선택용 보조 이미지입니다." +
                             "\n현재 전투 런타임 필수값은 아니며," +
                             "\n도감 / 선택 UI / 관리용 표시 이미지로 사용할 수 있습니다." +
                             "\n없으면 비워둬도 됩니다.")]
    private Sprite portrait;

    [Header("====3. 전투 수치====")]
    [SerializeField, Min(1f), Tooltip("몬스터 최대 체력입니다." +
                                      "\n현재 기존 SO 에셋 직렬화 호환을 위해 float를 유지합니다." +
                                      "\n실제 체력 컴포넌트 적용 시 어떤 기준으로 반올림할지는" +
                                      "\n이후 런타임 주입기 단계에서 한 곳에서 통일합니다.")]
    private float maxHp = 100f;

    [SerializeField, Min(0f), Tooltip("몬스터 이동 속도입니다." +
                                      "\n단위는 world units per second 기준으로 생각하면 됩니다." +
                                      "\n추적형과 원거리형 모두 기본 이동 기준값으로 사용됩니다.")]
    private float moveSpeed = 2f;

    [SerializeField, Min(0f), Tooltip("몬스터의 공격 기본 피해값입니다." +
                                      "\n원거리 공격, 특수 공격 등 '공격 쪽 기준값'으로 사용합니다." +
                                      "\n접촉 데미지는 별도 contactDamage로 분리합니다.")]
    private float attackDamage = 10f;

    [SerializeField, Min(0), Tooltip("플레이어와 접촉했을 때 줄 데미지입니다." +
                                     "\nEnemyContactDamage2D 같은 접촉 피해 컴포넌트에 주입할 값입니다." +
                                     "\n원거리형은 0을 허용합니다.")]
    private int contactDamage = 10;

    // TODO:
    // expReward는 현재 별도 로직 유지.
    // 보상 구조를 SO 기준으로 통합할 때 다시 열어둘 예정.
    // [SerializeField, Min(0), Tooltip("처치 시 지급할 경험치 보상입니다.")]
    // private int expReward = 10;

    [Header("====4. 행동 판단====")]
    [SerializeField, Min(0f), Tooltip("플레이어를 인식하고 전투 상태로 들어가는 거리입니다." +
                                      "\n단위는 world unit입니다." +
                                      "\ndetectRange 밖이면 추적형 / 원거리형 모두 기본적으로" +
                                      "\n전투 추적을 멈추는 기준이 됩니다.")]
    private float detectRange = 5f;

    [SerializeField, Min(0f), Tooltip("공격을 시작할 수 있는 거리입니다." +
                                      "\n단위는 world unit입니다." +
                                      "\n원거리형은 이 거리 안에서 공격을 시도하고," +
                                      "\n추적형은 향후 근접 공격 판정 기준으로도 사용할 수 있습니다.")]
    private float attackRange = 1.5f;

    [SerializeField, Min(0.01f), Tooltip("공격 주기입니다." +
                                         "\n단위는 초(second)입니다." +
                                         "\n0 이하가 되면 의미가 무너지므로 최소 0.01 이상으로 유지합니다.")]
    private float attackCooldown = 1.5f;

    [SerializeField, Min(0f), Tooltip("플레이어가 너무 가까울 때 후퇴를 시작하는 거리입니다." +
                                      "\n단위는 world unit입니다." +
                                      "\n현재는 주로 원거리형에서 사용하며," +
                                      "\n추적형은 0으로 두어도 됩니다.")]
    private float retreatRange = 0f;

    [Header("====5. 런타임 규칙====")]
    [SerializeField, Tooltip("이 몬스터를 alive count에 포함할지 여부입니다." +
                             "\n일반 몬스터 스폰 시스템의 동시 생존 수 관리 기준으로 사용합니다." +
                             "\n하자드 쪽 alive count는 이 값이 아니라" +
                             "\n패턴 프리팹 규칙으로 따로 처리합니다.")]
    private bool countsAsAliveEnemy = true;

    /// <summary>몬스터 고유 ID</summary>
    public string MonsterId => monsterId;

    /// <summary>표시 이름</summary>
    public string DisplayName => displayName;

    /// <summary>몬스터 등급 분류</summary>
    public MonsterType MonsterType => monsterType;

    /// <summary>몬스터 행동 분류</summary>
    public MonsterBehaviorType BehaviorType => behaviorType;

    /// <summary>스폰 대상 프리팹</summary>
    public GameObject MonsterPrefab => monsterPrefab;

    /// <summary>보조 초상화 이미지</summary>
    public Sprite Portrait => portrait;

    /// <summary>최대 체력</summary>
    public float MaxHp => maxHp;

    /// <summary>이동 속도</summary>
    public float MoveSpeed => moveSpeed;

    /// <summary>공격 기본 피해값</summary>
    public float AttackDamage => attackDamage;

    /// <summary>접촉 데미지</summary>
    public int ContactDamage => contactDamage;

    /// <summary>감지 범위</summary>
    public float DetectRange => detectRange;

    /// <summary>공격 범위</summary>
    public float AttackRange => attackRange;

    /// <summary>공격 주기</summary>
    public float AttackCooldown => attackCooldown;

    /// <summary>후퇴 시작 범위</summary>
    public float RetreatRange => retreatRange;

    /// <summary>alive count 포함 여부</summary>
    public bool CountsAsAliveEnemy => countsAsAliveEnemy;
}