// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 두억시니 전투 전체 설정 진입점 SO다.
// 기본 공격 설정, 패턴 카탈로그, 전투 판단 우선순위를 한 곳에서 관리한다.
// 런타임 상태는 두지 않고 정적 설정만 들고 있다.

[CreateAssetMenu(
    fileName = "DuryoksiniCombatConfigSO",
    menuName = "Tool/혼령검/Boss/Duryoksini/DuryoksiniCombatConfigSO")]
public class DuryoksiniCombatConfigSO : ScriptableObject
{
    public enum PatternType
    {
        RoarRockfall,
        CrushCharge,
        GroundSmash
    }

    [Header("전투 데이터 연결")]

    [Tooltip("두억시니 기본 공격 설정 SO")]
    [SerializeField] private DuryoksiniBasicAttackConfigSO basicAttackConfig;

    [Tooltip("두억시니 특수 패턴 카탈로그 SO")]
    [SerializeField] private DuryoksiniPatternCatalogSO patternCatalog;

    [Header("전투 판단 주기")]

    [Tooltip("전투 판단을 다시 수행하는 간격이다.")]
    [Min(0.02f)]
    [SerializeField] private float thinkInterval = 0.08f;

    [Header("기본 공격 정책")]

    [Tooltip("기본 공격 사거리 안이면 특수 패턴보다 기본 공격을 먼저 시도할지 여부")]
    [SerializeField] private bool preferBasicAttackInRange = true;

    [Header("특수 패턴 우선순위")]

    [Tooltip("특수 패턴 1순위")]
    [SerializeField] private PatternType firstPattern = PatternType.RoarRockfall;

    [Tooltip("특수 패턴 2순위")]
    [SerializeField] private PatternType secondPattern = PatternType.CrushCharge;

    [Tooltip("특수 패턴 3순위")]
    [SerializeField] private PatternType thirdPattern = PatternType.GroundSmash;

    public DuryoksiniBasicAttackConfigSO BasicAttackConfig => basicAttackConfig;
    public DuryoksiniPatternCatalogSO PatternCatalog => patternCatalog;
    public float ThinkInterval => thinkInterval;
    public bool PreferBasicAttackInRange => preferBasicAttackInRange;

    public PatternType FirstPattern => firstPattern;
    public PatternType SecondPattern => secondPattern;
    public PatternType ThirdPattern => thirdPattern;
}