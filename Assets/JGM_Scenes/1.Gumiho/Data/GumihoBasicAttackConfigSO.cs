// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 구미호 기본 화염구 공격의 정적 데이터만 관리한다.
// 공격 조건, 애니메이터 파라미터 이름, 투사체 설정을 모두 여기서 조절한다.

[CreateAssetMenu(
    menuName = "혼령검/Boss/Gumiho/기본 공격 설정",
    fileName = "GumihoBasicAttackConfigSO")]
public sealed class GumihoBasicAttackConfigSO : ScriptableObject
{
    [Header("공격 조건")]

    [Tooltip("기본 공격 쿨타임입니다.")]
    [Min(0f)]
    [SerializeField] private float attackCooldown = 1.4f;

    [Tooltip("기본 공격 가능 거리입니다.")]
    [Min(0.1f)]
    [SerializeField] private float attackRange = 7f;


    [Header("애니메이터 설정")]

    [Tooltip("공격 시작에 사용할 Animator Trigger 이름입니다.")]
    [SerializeField] private string attackTriggerName = "Attack";

    [Tooltip("현재 공격 상태로 간주할 Animator State 이름입니다.")]
    [SerializeField] private string attackStateName = "attack";


    [Header("화염구 프리팹")]

    [Tooltip("구미호 기본 공격에 사용할 화염구 프리팹입니다.")]
    [SerializeField] private GumihoFireballProjectile2D fireballPrefab;

    [Tooltip("미리 생성해둘 화염구 개수입니다.")]
    [Min(1)]
    [SerializeField] private int prewarmCount = 8;


    [Header("화염구 수치")]

    [Tooltip("화염구 이동 속도입니다.")]
    [Min(0.1f)]
    [SerializeField] private float projectileSpeed = 8f;

    [Tooltip("화염구 생존 시간입니다.")]
    [Min(0.1f)]
    [SerializeField] private float projectileLifetime = 3f;

    [Tooltip("화염구 데미지입니다.")]
    [Min(1)]
    [SerializeField] private int projectileDamage = 10;

    [Tooltip("화염구가 피격할 대상 레이어입니다.\n보통 Player 레이어를 넣습니다.")]
    [SerializeField] private LayerMask targetLayerMask;


    public float AttackCooldown => attackCooldown;
    public float AttackRange => attackRange;
    public string AttackTriggerName => attackTriggerName;
    public string AttackStateName => attackStateName;
    public GumihoFireballProjectile2D FireballPrefab => fireballPrefab;
    public int PrewarmCount => prewarmCount;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileLifetime => projectileLifetime;
    public int ProjectileDamage => projectileDamage;
    public LayerMask TargetLayerMask => targetLayerMask;
}