using UnityEngine;

/// <summary>
/// 몬스터 1종의 기준 데이터를 담는 ScriptableObject입니다.
///
/// 이 SO의 역할:
/// - 스포너가 무엇을 생성할지 판단하는 기준이 됩니다.
/// - 런타임 적용기가 어떤 수치를 주입할지 결정하는 기준이 됩니다.
/// - 몬스터 등급, 행동 타입, alive count 포함 여부 같은
///   핵심 런타임 규칙도 이 데이터 기준으로 관리합니다.
///
/// 설계 원칙:
/// - 등급 차이(Normal / Elite / Boss)는 SO 데이터로 관리합니다.
/// - 행동 차이(Chase / Ranged / Dash)는 behaviorType으로 분기하고,
///   행동 전용 수치는 이 SO 안의 전용 블록으로 관리합니다.
/// - 실제 행동 실행은 행동 컴포넌트가 담당하고,
///   이 SO는 "설계값의 기준점" 역할만 담당합니다.
///
/// 직렬화 호환 원칙:
/// - 이미 만들어둔 자산이 많으므로,
///   기존 필드명과 기존 타입은 최대한 유지합니다.
/// - 새로 필요한 행동 전용 수치만 뒤에 확장합니다.
/// </summary>
[CreateAssetMenu(menuName = "Game/Monster/MonsterDefinition")]
public sealed class MonsterDefinitionSO : ScriptableObject
{
    [Header("1. 기본 정보")]
    [SerializeField, Tooltip("몬스터 고유 ID입니다.\n"
                             + "카탈로그 조회, 스폰 타임라인 등록,\n"
                             + "런타임 식별 기준으로 사용합니다.\n"
                             + "공백 없이 snake_case를 사용하세요.\n"
                             + "예: normal_cat, elite_hog")]
    private string monsterId;

    [SerializeField, Tooltip("Inspector와 디버그 로그에서 구분할 표시 이름입니다.\n"
                             + "실제 스폰 키는 monsterId이므로,\n"
                             + "이 값은 읽기 쉬운 이름으로 작성하면 됩니다.")]
    private string displayName;

    [SerializeField, Tooltip("몬스터의 등급 분류입니다.\n"
                             + "Normal / Elite / Boss 구분에 사용합니다.\n"
                             + "행동 방식은 behaviorType에서 따로 설정합니다.")]
    private MonsterType monsterType = MonsterType.Normal;

    [SerializeField, Tooltip("몬스터의 행동 분류입니다.\n"
                             + "Chase / Ranged / Dash 같은 행동 축을 구분합니다.\n"
                             + "런타임 적용기와 행동 컴포넌트의 분기 기준으로 사용합니다.")]
    private MonsterBehaviorType behaviorType = MonsterBehaviorType.Chase;

    [Header("2. 프리팹 연결")]
    [SerializeField, Tooltip("실제로 스폰할 몬스터 프리팹입니다.\n"
                             + "프리팹에는 비주얼, 콜라이더, 기본 컴포넌트 껍데기를 두고,\n"
                             + "실제 수치는 런타임에 이 SO에서 주입합니다.")]
    private GameObject monsterPrefab;

    [SerializeField, Tooltip("선택용 보조 이미지입니다.\n"
                             + "현재 전투 런타임 필수값은 아니며,\n"
                             + "도감 / 선택 UI / 관리용 표시 이미지로 사용할 수 있습니다.\n"
                             + "없으면 비워둬도 됩니다.")]
    private Sprite portrait;

    [Header("3. 공통 전투 수치")]
    [SerializeField, Min(1f), Tooltip("몬스터 최대 체력입니다.\n"
                                      + "기존 SO 자산 직렬화 호환을 위해 float를 유지합니다.\n"
                                      + "실제 체력 컴포넌트 적용 시 반올림 기준은\n"
                                      + "런타임 적용기에서 한 곳에서 통일합니다.")]
    private float maxHp = 100f;

    [SerializeField, Min(0f), Tooltip("몬스터 기본 이동 속도입니다.\n"
                                      + "단위는 world unit / second 기준입니다.\n"
                                      + "Chase / Ranged / Dash 모두의 기본 이동값으로 사용합니다.")]
    private float moveSpeed = 3.5f;

    [SerializeField, Min(0f), Tooltip("플레이어를 인식하고 전투 상태로 들어가는 거리입니다.\n"
                                      + "단위는 world unit입니다.\n"
                                      + "행동 타입과 관계없이 전투 진입 기준으로 사용합니다.")]
    private float detectRange = 15f;

    [SerializeField, Min(0), Tooltip("플레이어와 접촉했을 때 줄 데미지입니다.\n"
                                     + "EnemyContactDamage2D 같은 접촉 피해 컴포넌트에 주입합니다.\n"
                                     + "원거리형은 0을 허용합니다.")]
    private int contactDamage = 10;

    [SerializeField, Tooltip("이 몬스터를 alive count에 포함할지 여부입니다.\n"
                             + "일반 몬스터 스폰 시스템의 동시 생존 수 관리 기준으로 사용합니다.\n"
                             + "하자드 alive count는 이 값이 아니라\n"
                             + "패턴 프리팹 규칙으로 따로 처리합니다.")]
    private bool countsAsAliveEnemy = true;

    [Header("4. 원거리 전용 수치")]
    [SerializeField, Min(0f), Tooltip("원거리 공격 기본 피해값입니다.\n"
                                      + "발사체 피해나 특수 공격 피해의 기준값으로 사용합니다.\n"
                                      + "Chase 몬스터는 이 값을 사용하지 않아도 됩니다.")]
    private float attackDamage = 10f;

    [SerializeField, Min(0f), Tooltip("공격을 시작할 수 있는 거리입니다.\n"
                                      + "단위는 world unit입니다.\n"
                                      + "주로 원거리형에서 실제 사격 시작 거리로 사용합니다.")]
    private float attackRange = 5f;

    [SerializeField, Min(0.01f), Tooltip("공격 주기입니다.\n"
                                         + "단위는 초(second)입니다.\n"
                                         + "원거리형의 발사 간격 기준으로 사용합니다.")]
    private float attackCooldown = 1.5f;

    [SerializeField, Min(0f), Tooltip("플레이어가 너무 가까울 때 후퇴를 시작하는 거리입니다.\n"
                                      + "단위는 world unit입니다.\n"
                                      + "원거리형에서만 의미가 있으며,\n"
                                      + "Chase 몬스터는 보통 0으로 둡니다.")]
    private float retreatRange = 0f;

    [SerializeField, Min(0f), Tooltip("발사 전 차징 시간입니다.\n"
                                      + "단위는 초(second)입니다.\n"
                                      + "원거리형의 위협도와 반응 시간을 결정하는 설계값입니다.\n"
                                      + "차징이 없는 원거리형은 0으로 둘 수 있습니다.")]
    private float chargeDuration = 0f;

    [SerializeField, Min(1), Tooltip("한 번 공격할 때 발사할 투사체 개수입니다.\n"
                                     + "기본형 원거리는 1,\n"
                                     + "산탄형이나 다발형은 2 이상을 사용할 수 있습니다.")]
    private int projectileCount = 1;

    [SerializeField, Min(0f), Tooltip("다발 발사 시 퍼지는 전체 각도입니다.\n"
                                      + "단위는 degree입니다.\n"
                                      + "projectileCount가 1이면 보통 0으로 둡니다.")]
    private float spreadAngle = 0f;

    [SerializeField, Min(0f), Tooltip("투사체 이동 속도입니다.\n"
                                      + "단위는 world unit / second 기준입니다.\n"
                                      + "원거리 공격 체감 난이도를 크게 바꾸는 값입니다.")]
    private float projectileSpeed = 10f;

    [Header("5. 돌진형 전용 수치")]
    [SerializeField, Min(0f), Tooltip("돌진 시작 후 실제 돌진 이동 속도입니다.\n"
                                      + "단위는 world unit / second 기준입니다.\n"
                                      + "현재는 Dash 행동 확장 대비용 필드입니다.")]
    private float dashSpeed = 8f;

    [SerializeField, Min(0f), Tooltip("한 번 돌진할 때 이동할 목표 거리입니다.\n"
                                      + "단위는 world unit입니다.\n"
                                      + "현재는 Dash 행동 확장 대비용 필드입니다.")]
    private float dashDistance = 3f;

    [SerializeField, Min(0.01f), Tooltip("돌진 재사용 대기시간입니다.\n"
                                         + "단위는 초(second)입니다.\n"
                                         + "현재는 Dash 행동 확장 대비용 필드입니다.")]
    private float dashCooldown = 2f;

    [SerializeField, Min(0f), Tooltip("돌진 직전 준비 시간입니다.\n"
                                      + "단위는 초(second)입니다.\n"
                                      + "경고 연출이나 차징형 돌진의 기준으로 사용할 수 있습니다.")]
    private float dashWindupTime = 0f;

    [SerializeField, Min(0f), Tooltip("돌진 종료 후 후딜 시간입니다.\n"
                                      + "단위는 초(second)입니다.\n"
                                      + "현재는 Dash 행동 확장 대비용 필드입니다.")]
    private float dashRecoverTime = 0f;

    // TODO:
    // expReward는 현재 별도 로직 유지.
    // 보상 구조를 SO 기준으로 통합할 때 다시 열어둘 예정.
    // [SerializeField, Min(0), Tooltip("처치 시 지급할 경험치 보상입니다.")]
    // private int expReward = 10;

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

    /// <summary>감지 범위</summary>
    public float DetectRange => detectRange;

    /// <summary>접촉 데미지</summary>
    public int ContactDamage => contactDamage;

    /// <summary>alive count 포함 여부</summary>
    public bool CountsAsAliveEnemy => countsAsAliveEnemy;

    /// <summary>원거리 공격 기본 피해값</summary>
    public float AttackDamage => attackDamage;

    /// <summary>공격 가능 거리</summary>
    public float AttackRange => attackRange;

    /// <summary>공격 주기</summary>
    public float AttackCooldown => attackCooldown;

    /// <summary>후퇴 시작 거리</summary>
    public float RetreatRange => retreatRange;

    /// <summary>차징 시간</summary>
    public float ChargeDuration => chargeDuration;

    /// <summary>투사체 개수</summary>
    public int ProjectileCount => projectileCount;

    /// <summary>탄 퍼짐 각도</summary>
    public float SpreadAngle => spreadAngle;

    /// <summary>투사체 속도</summary>
    public float ProjectileSpeed => projectileSpeed;

    /// <summary>돌진 속도</summary>
    public float DashSpeed => dashSpeed;

    /// <summary>돌진 거리</summary>
    public float DashDistance => dashDistance;

    /// <summary>돌진 쿨타임</summary>
    public float DashCooldown => dashCooldown;

    /// <summary>돌진 준비 시간</summary>
    public float DashWindupTime => dashWindupTime;

    /// <summary>돌진 후딜 시간</summary>
    public float DashRecoverTime => dashRecoverTime;
}