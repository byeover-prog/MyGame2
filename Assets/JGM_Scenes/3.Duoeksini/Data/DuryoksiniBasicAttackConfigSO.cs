// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 두억시니 기본 공격의 정적 데이터를 관리한다.
// 공격 거리, 판정 반경, 피해량, 넉백 값,
// 그리고 애니메이션 이벤트가 누락됐을 때 강제 종료할 보정 시간도 여기서 관리한다.

[CreateAssetMenu(
    menuName = "혼령검/Boss/Duryoksini/기본 공격 설정",
    fileName = "DuryoksiniBasicAttackConfigSO")]
public sealed class DuryoksiniBasicAttackConfigSO : BossPatternConfigSO
{
    [Header("공격 거리")]

    [Tooltip("이 거리 안에 플레이어가 들어오면 기본 공격을 시도한다.")]
    [Min(0f)]
    [SerializeField] private float attackDistance = 2f;


    [Header("공격 판정")]

    [Tooltip("기본 공격 판정 반경이다.")]
    [Min(0.05f)]
    [SerializeField] private float hitRadius = 1.2f;

    [Tooltip("피해 판정을 받을 대상 레이어이다.")]
    [SerializeField] private LayerMask targetLayerMask;

    [Tooltip("기본 공격 피해량이다.")]
    [Min(1)]
    [SerializeField] private int damage = 10;


    [Header("넉백 값")]

    [Tooltip("기본 공격 넉백 거리이다.")]
    [Min(0.1f)]
    [SerializeField] private float knockbackDistance = 0.8f;

    [Tooltip("기본 공격 넉백 시간이다.")]
    [Min(0.01f)]
    [SerializeField] private float knockbackDuration = 0.1f;

    [Tooltip("기본 공격 넉백 시 위로 약간 뜨는 비율이다.")]
    [Range(0f, 1f)]
    [SerializeField] private float knockbackUpBias = 0.1f;


    [Header("공격 종료 보정")]

    [Tooltip("애니메이션 종료 이벤트가 누락됐을 때 이 시간 후 공격을 강제 종료한다.")]
    [Min(0.05f)]
    [SerializeField] private float attackFinishFallbackTime = 1.1f;


    [Header("기즈모")]

    [Tooltip("공격 판정 반경을 씬에서 표시할지 여부이다.")]
    [SerializeField] private bool drawGizmos = true;


    public float AttackDistance => attackDistance;
    public float HitRadius => hitRadius;
    public LayerMask TargetLayerMask => targetLayerMask;
    public int Damage => damage;

    public float KnockbackDistance => knockbackDistance;
    public float KnockbackDuration => knockbackDuration;
    public float KnockbackUpBias => knockbackUpBias;

    public float AttackFinishFallbackTime => attackFinishFallbackTime;

    public bool DrawGizmos => drawGizmos;
}