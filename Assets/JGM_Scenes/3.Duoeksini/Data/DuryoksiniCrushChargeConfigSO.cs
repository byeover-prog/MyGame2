// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 두억시니 파쇄 돌진 패턴의 정적 데이터만 관리한다.
// 거리 조건, 준비 시간, 돌진 속도, 충돌 판정 크기, 피해량, 넉백 값,
// 돌진 시작 연출 프리팹과 연출 크기까지 여기서 조절한다.

[CreateAssetMenu(
    menuName = "혼령검/Boss/Duryoksini/파쇄 돌진 설정",
    fileName = "DuryoksiniCrushChargeConfigSO")]
public sealed class DuryoksiniCrushChargeConfigSO : BossPatternConfigSO
{
    [Header("발동 거리")]

    [Tooltip("이 거리보다 가까우면 돌진을 시작하지 않습니다.")]
    [Min(0f)]
    [SerializeField] private float minAttackDistance = 2f;

    [Tooltip("이 거리보다 멀면 돌진을 시작하지 않습니다.")]
    [Min(0f)]
    [SerializeField] private float maxAttackDistance = 8f;


    [Header("준비 동작")]

    [Tooltip("돌진 전에 힘을 모으는 준비 시간입니다.")]
    [Min(0.01f)]
    [SerializeField] private float prepareDuration = 0.5f;


    [Header("돌진 이동")]

    [Tooltip("돌진 속도입니다.")]
    [Min(0.1f)]
    [SerializeField] private float chargeSpeed = 12f;

    [Tooltip("최대 돌진 거리입니다.")]
    [Min(0.1f)]
    [SerializeField] private float maxChargeDistance = 5f;

    [Tooltip("최대 돌진 시간입니다.")]
    [Min(0.05f)]
    [SerializeField] private float maxChargeDuration = 0.7f;

    [Tooltip("돌진 종료 후 다시 움직이기 전 쉬는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float recoverDuration = 0.4f;


    [Header("충돌 판정")]

    [Tooltip("돌진 충돌 체크 반경입니다.")]
    [Min(0.05f)]
    [SerializeField] private float hitRadius = 0.8f;

    [Tooltip("피격 대상 판정에 사용할 레이어입니다.")]
    [SerializeField] private LayerMask targetLayerMask;

    [Tooltip("돌진 피해량입니다.")]
    [Min(1)]
    [SerializeField] private int damage = 20;


    [Header("넉백 값")]

    [Tooltip("플레이어를 밀어내는 넉백 거리입니다.")]
    [Min(0.1f)]
    [SerializeField] private float knockbackDistance = 1.6f;

    [Tooltip("플레이어를 밀어내는 넉백 시간입니다.")]
    [Min(0.01f)]
    [SerializeField] private float knockbackDuration = 0.12f;

    [Tooltip("넉백 시 위로 약간 뜨게 만드는 비율입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float knockbackUpBias = 0.2f;


    [Header("돌진 시작 연출")]

    [Tooltip("두억시니가 돌진 시작할 때 1회 생성할 연출 프리팹입니다.")]
    [SerializeField] private GameObject chargeStartEffectPrefab;

    [Tooltip("돌진 시작 연출을 VFXRoot 기준 어디에 띄울지 위치 오프셋입니다.")]
    [SerializeField] private Vector3 chargeStartEffectOffset = Vector3.zero;

    [Tooltip("돌진 시작 연출의 로컬 스케일입니다.\n보스 크기에 맞게 여기서 직접 줄입니다.")]
    [SerializeField] private Vector3 chargeStartEffectLocalScale = Vector3.one;

    [Tooltip("돌진 시작 연출을 몇 초 뒤 자동 삭제할지 시간입니다.")]
    [Min(0.1f)]
    [SerializeField] private float chargeStartEffectLifetime = 2f;


    [Header("디버그")]

    [Tooltip("기즈모로 충돌 반경을 표시할지 여부입니다.")]
    [SerializeField] private bool drawGizmos = true;


    public float MinAttackDistance => minAttackDistance;
    public float MaxAttackDistance => maxAttackDistance;

    public float PrepareDuration => prepareDuration;

    public float ChargeSpeed => chargeSpeed;
    public float MaxChargeDistance => maxChargeDistance;
    public float MaxChargeDuration => maxChargeDuration;
    public float RecoverDuration => recoverDuration;

    public float HitRadius => hitRadius;
    public LayerMask TargetLayerMask => targetLayerMask;
    public int Damage => damage;

    public float KnockbackDistance => knockbackDistance;
    public float KnockbackDuration => knockbackDuration;
    public float KnockbackUpBias => knockbackUpBias;

    public GameObject ChargeStartEffectPrefab => chargeStartEffectPrefab;
    public Vector3 ChargeStartEffectOffset => chargeStartEffectOffset;
    public Vector3 ChargeStartEffectLocalScale => chargeStartEffectLocalScale;
    public float ChargeStartEffectLifetime => chargeStartEffectLifetime;

    public bool DrawGizmos => drawGizmos;
}