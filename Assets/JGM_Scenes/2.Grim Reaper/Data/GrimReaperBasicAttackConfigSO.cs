// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 저승사자 기본 낫 공격의 정적 데이터를 관리한다.
// 공격 거리, 판정 크기, 피해량, 쿨타임, 애니메이션 보정 시간을 여기서 조절한다.
// 현재 공격 중인지, 이미 타격했는지 같은 런타임 상태는 Controller에서만 관리한다.

[CreateAssetMenu(
    menuName = "혼령검/Boss/GrimReaper/기본 공격 설정",
    fileName = "GrimReaperBasicAttackConfigSO")]
public sealed class GrimReaperBasicAttackConfigSO : BossPatternConfigSO
{
    [Header("공격 거리")]

    [Tooltip("이 거리 안에 플레이어가 들어오면 저승사자가 기본 낫 공격을 시도한다.")]
    [Min(0f)]
    [SerializeField] private float attackDistance = 2f;


    [Header("공격 판정")]

    [Tooltip("기본 낫 공격의 가로 판정 크기이다.")]
    [Min(0.05f)]
    [SerializeField] private float hitBoxWidth = 1.6f;

    [Tooltip("기본 낫 공격의 세로 판정 크기이다.")]
    [Min(0.05f)]
    [SerializeField] private float hitBoxHeight = 1.1f;

    [Tooltip("피해 판정을 받을 대상 레이어이다. 보통 Player 레이어를 넣는다.")]
    [SerializeField] private LayerMask targetLayerMask;

    [Tooltip("기본 낫 공격 피해량이다.")]
    [Min(1)]
    [SerializeField] private int damage = 10;


    [Header("공격 타이밍")]

    [Tooltip("애니메이션 타격 이벤트가 누락됐을 때 이 시간 후 강제로 타격한다.")]
    [Min(0.01f)]
    [SerializeField] private float hitFallbackTime = 0.35f;

    [Tooltip("애니메이션 종료 이벤트가 누락됐을 때 이 시간 후 강제로 공격을 종료한다.")]
    [Min(0.05f)]
    [SerializeField] private float finishFallbackTime = 0.8f;


    [Header("기즈모")]

    [Tooltip("씬에서 기본 공격 판정 박스를 표시할지 여부이다.")]
    [SerializeField] private bool drawGizmos = true;


    public float AttackDistance => attackDistance;

    public float HitBoxWidth => hitBoxWidth;
    public float HitBoxHeight => hitBoxHeight;
    public Vector2 HitBoxSize => new Vector2(hitBoxWidth, hitBoxHeight);

    public LayerMask TargetLayerMask => targetLayerMask;
    public int Damage => damage;

    public float HitFallbackTime => hitFallbackTime;
    public float FinishFallbackTime => finishFallbackTime;

    public bool DrawGizmos => drawGizmos;
}