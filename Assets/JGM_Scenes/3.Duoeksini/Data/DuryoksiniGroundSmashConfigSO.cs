// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 두억시니 대지 분쇄 패턴의 정적 데이터를 관리한다.
// 발동 거리, 준비 시간, 타격 범위, 피해량,
// 준비 이펙트와 내려찍기 이펙트 정보를 여기서 설정한다.

[CreateAssetMenu(
    menuName = "혼령검/Boss/Duryoksini/대지 분쇄 설정",
    fileName = "DuryoksiniGroundSmashConfigSO")]
public sealed class DuryoksiniGroundSmashConfigSO : BossPatternConfigSO
{
    [Header("발동 거리")]

    [Tooltip("이 거리보다 가까우면 대지 분쇄를 시작하지 않는다.")]
    [Min(0f)]
    [SerializeField] private float minAttackDistance = 1.2f;

    [Tooltip("이 거리보다 멀면 대지 분쇄를 시작하지 않는다.")]
    [Min(0f)]
    [SerializeField] private float maxAttackDistance = 3.2f;


    [Header("시간 설정")]

    [Tooltip("준비 동작이 유지되는 시간이다.")]
    [Min(0.05f)]
    [SerializeField] private float prepareDuration = 0.7f;

    [Tooltip("실제 타격 후 후딜 시간이다.")]
    [Min(0.05f)]
    [SerializeField] private float recoverDuration = 0.5f;

    [Tooltip("타격 이벤트가 누락됐을 때 강제로 타격을 실행할 시간이다.")]
    [Min(0.05f)]
    [SerializeField] private float hitFallbackTime = 0.8f;

    [Tooltip("종료 이벤트가 누락됐을 때 강제로 종료할 시간이다.")]
    [Min(0.05f)]
    [SerializeField] private float finishFallbackTime = 1.2f;


    [Header("타격 판정")]

    [Tooltip("대지 분쇄 타격 반경이다.")]
    [Min(0.05f)]
    [SerializeField] private float hitRadius = 1.5f;

    [Tooltip("대지 분쇄 피해를 받을 대상 레이어이다.")]
    [SerializeField] private LayerMask targetLayerMask;

    [Tooltip("대지 분쇄 피해량이다.")]
    [Min(1)]
    [SerializeField] private int damage = 18;


    [Header("넉백 값")]

    [Tooltip("대지 분쇄 넉백 거리이다.")]
    [Min(0.1f)]
    [SerializeField] private float knockbackDistance = 1.2f;

    [Tooltip("대지 분쇄 넉백 시간이다.")]
    [Min(0.01f)]
    [SerializeField] private float knockbackDuration = 0.14f;

    [Tooltip("대지 분쇄 넉백 시 위로 약간 뜨는 비율이다.")]
    [Range(0f, 1f)]
    [SerializeField] private float knockbackUpBias = 0.15f;


    [Header("준비 연출")]

    [Tooltip("대지 분쇄 준비 동작 프리팹이다.")]
    [SerializeField] private GameObject prepareEffectPrefab;

    [Tooltip("준비 연출을 VFXRoot 기준 어디에 띄울지 위치 오프셋이다.")]
    [SerializeField] private Vector3 prepareEffectOffset = Vector3.zero;

    [Tooltip("준비 연출의 로컬 스케일이다.")]
    [SerializeField] private Vector3 prepareEffectLocalScale = Vector3.one;

    [Tooltip("준비 연출을 몇 초 뒤 자동 삭제할지 시간이다.")]
    [Min(0.1f)]
    [SerializeField] private float prepareEffectLifetime = 1.5f;


    [Header("내려찍기 연출")]

    [Tooltip("대지 분쇄 타격 순간 생성할 이펙트 프리팹이다.")]
    [SerializeField] private GameObject smashImpactEffectPrefab;

    [Tooltip("내려찍기 이펙트를 SmashPoint 기준 어디에 띄울지 위치 오프셋이다.")]
    [SerializeField] private Vector3 smashImpactEffectOffset = Vector3.zero;

    [Tooltip("내려찍기 이펙트의 로컬 스케일이다.")]
    [SerializeField] private Vector3 smashImpactEffectLocalScale = Vector3.one;

    [Tooltip("내려찍기 이펙트를 몇 초 뒤 자동 삭제할지 시간이다.")]
    [Min(0.1f)]
    [SerializeField] private float smashImpactEffectLifetime = 2f;


    [Header("디버그")]

    [Tooltip("기즈모로 타격 반경을 표시할지 여부이다.")]
    [SerializeField] private bool drawGizmos = true;


    public float MinAttackDistance => minAttackDistance;
    public float MaxAttackDistance => maxAttackDistance;

    public float PrepareDuration => prepareDuration;
    public float RecoverDuration => recoverDuration;
    public float HitFallbackTime => hitFallbackTime;
    public float FinishFallbackTime => finishFallbackTime;

    public float HitRadius => hitRadius;
    public LayerMask TargetLayerMask => targetLayerMask;
    public int Damage => damage;

    public float KnockbackDistance => knockbackDistance;
    public float KnockbackDuration => knockbackDuration;
    public float KnockbackUpBias => knockbackUpBias;

    public GameObject PrepareEffectPrefab => prepareEffectPrefab;
    public Vector3 PrepareEffectOffset => prepareEffectOffset;
    public Vector3 PrepareEffectLocalScale => prepareEffectLocalScale;
    public float PrepareEffectLifetime => prepareEffectLifetime;

    public GameObject SmashImpactEffectPrefab => smashImpactEffectPrefab;
    public Vector3 SmashImpactEffectOffset => smashImpactEffectOffset;
    public Vector3 SmashImpactEffectLocalScale => smashImpactEffectLocalScale;
    public float SmashImpactEffectLifetime => smashImpactEffectLifetime;

    public bool DrawGizmos => drawGizmos;
}