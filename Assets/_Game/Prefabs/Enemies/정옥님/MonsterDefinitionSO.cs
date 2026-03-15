using UnityEngine;

[CreateAssetMenu(menuName = "Game/Monster/MonsterDefinition")]
public class MonsterDefinitionSO : ScriptableObject
{
    [Header("기본 정보")]
    
    [SerializeField, Tooltip("몬스터 고유 ID (저장 및 조회용 키)")]
    private string monsterId;

    [SerializeField, Tooltip("몬스터 표시 이름")]
    private string displayName;

    [SerializeField, Tooltip("몬스터 타입")]
    private MonsterType monsterType;

    [Header("비주얼")]

    [SerializeField, Tooltip("몬스터 프리팹")]
    private GameObject monsterPrefab;

    [SerializeField, Tooltip("몬스터 초상화 또는 아이콘")]
    private Sprite portrait;

    [Header("기본 스탯")]

    [SerializeField, Tooltip("몬스터 최대 체력")]
    private float maxHp = 100f;

    [SerializeField, Tooltip("몬스터 이동 속도")]
    private float moveSpeed = 2f;

    [SerializeField, Tooltip("몬스터 공격력")]
    private float attackDamage = 10f;

    [SerializeField, Tooltip("플레이어 감지 범위")]
    private float detectRange = 5f;

    [SerializeField, Tooltip("플레이어 공격 범위")]
    private float attackRange = 1.5f;

    [SerializeField, Tooltip("공격 쿨타임")]
    private float attackCooldown = 1.5f;

    [Header("보상")]

    [SerializeField, Tooltip("처치 시 경험치 보상")]
    private int expReward = 10;

    [SerializeField, Tooltip("처치 시 골드 보상")]
    private int goldReward = 5;

    // 몬스터 고유 ID 반환
    public string MonsterId => monsterId;

    // 몬스터 표시 이름 반환
    public string DisplayName => displayName;

    // 몬스터 타입 반환
    public MonsterType MonsterType => monsterType;

    // 몬스터 프리팹 반환
    public GameObject MonsterPrefab => monsterPrefab;

    // 몬스터 아이콘 반환
    public Sprite Portrait => portrait;

    // 몬스터 최대 체력 반환
    public float MaxHp => maxHp;

    // 몬스터 이동 속도 반환
    public float MoveSpeed => moveSpeed;

    // 몬스터 공격력 반환
    public float AttackDamage => attackDamage;

    // 몬스터 감지 범위 반환
    public float DetectRange => detectRange;

    // 몬스터 공격 범위 반환
    public float AttackRange => attackRange;

    // 몬스터 공격 쿨타임 반환
    public float AttackCooldown => attackCooldown;

    // 경험치 보상 반환
    public int ExpReward => expReward;

    // 골드 보상 반환
    public int GoldReward => goldReward;
}