// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 저승사자 전투 전체 설정을 관리한다.
// 현재 단계에서는 기본 공격 설정과 전투 판단 주기만 가진다.
// 특수 패턴은 나중에 PatternCatalogSO로 분리해서 추가한다.

[CreateAssetMenu(
    menuName = "혼령검/Boss/GrimReaper/전투 설정",
    fileName = "GrimReaperCombatConfigSO")]
public sealed class GrimReaperCombatConfigSO : ScriptableObject
{
    [Header("기본 공격")]

    [Tooltip("저승사자 기본 공격 설정 SO\n기본 공격은 패턴이 아니므로 PatternCatalogSO에 넣지 않는다.")]
    [SerializeField] private GrimReaperBasicAttackConfigSO basicAttackConfig;


    [Header("전투 판단")]

    [Tooltip("전투 판단을 다시 시도하는 간격이다.\n값이 너무 낮으면 불필요하게 자주 검사하고, 너무 높으면 반응이 느려진다.")]
    [Min(0.02f)]
    [SerializeField] private float thinkInterval = 0.1f;

    [Tooltip("전투가 켜진 뒤 실제 공격 판단을 시작하기 전 대기 시간이다.")]
    [Min(0f)]
    [SerializeField] private float battleStartDelay = 0f;

    [Tooltip("전투 시작 대기 시간 중에도 플레이어를 추적할지 여부이다.")]
    [SerializeField] private bool chaseDuringStartDelay = true;


    public GrimReaperBasicAttackConfigSO BasicAttackConfig => basicAttackConfig;

    public float ThinkInterval => thinkInterval;
    public float BattleStartDelay => battleStartDelay;
    public bool ChaseDuringStartDelay => chaseDuringStartDelay;
}