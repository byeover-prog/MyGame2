// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 요호의 여우구슬 패턴에서 사용하는 정적 데이터만 관리한다.
// 여우구슬 본체, 폭발 이펙트, 폭발 전 경고 원 이미지,
// 패턴 시작 연출 프리팹을 각각 따로 관리한다.

[CreateAssetMenu(
    menuName = "혼령검/Boss/Gumiho/여우구슬 패턴 설정",
    fileName = "GumihoYohoFoxBeadPatternConfigSO")]
public sealed class GumihoYohoFoxBeadPatternConfigSO : BossPatternConfigSO
{
    [Header("패턴 시작 연출")]

    [Tooltip("여우구슬 패턴이 시작될 때 1회 재생할 시작 연출 프리팹입니다.")]
    [SerializeField] private GameObject patternStartEffectPrefab;

    [Tooltip("패턴 시작 연출 프리팹의 스케일입니다.")]
    [SerializeField] private Vector3 patternStartEffectScale = Vector3.one;

    [Tooltip("패턴 시작 연출을 보스 기준 어디에 띄울지 위치 오프셋입니다.")]
    [SerializeField] private Vector3 patternStartEffectOffset = Vector3.zero;

    [Tooltip("패턴 시작 연출 프리팹을 몇 초 뒤 자동 삭제할지입니다.")]
    [Min(0.1f)]
    [SerializeField] private float patternStartEffectLifetime = 2f;


    [Header("여우구슬 프리팹")]

    [Tooltip("요호의 여우구슬 패턴에서 사용할 여우구슬 본체 프리팹입니다.")]
    [SerializeField] private GumihoYohoFoxBeadObject foxBeadPrefab;

    [Tooltip("미리 생성해둘 여우구슬 풀 개수입니다.")]
    [Min(1)]
    [SerializeField] private int prewarmCount = 1;

    [Tooltip("여우구슬 본체 스케일입니다.")]
    [SerializeField] private Vector3 foxBeadScale = Vector3.one;


    [Header("폭발 이펙트 프리팹")]

    [Tooltip("여우구슬 폭발 시 생성할 이펙트 프리팹입니다.")]
    [SerializeField] private GameObject explosionEffectPrefab;

    [Tooltip("폭발 이펙트의 스케일입니다.")]
    [SerializeField] private Vector3 explosionEffectScale = Vector3.one;

    [Tooltip("폭발 이펙트 프리팹을 몇 초 뒤 자동 삭제할지입니다.")]
    [Min(0.1f)]
    [SerializeField] private float explosionEffectLifetime = 2f;


    [Header("폭발 경고 이미지")]

    [Tooltip("폭발 직전 바닥에 띄울 경고 스프라이트입니다.")]
    [SerializeField] private Sprite explosionWarningSprite;

    [Tooltip("경고 스프라이트 색상입니다.")]
    [SerializeField] private Color explosionWarningColor = Color.white;

    [Tooltip("경고 스프라이트 정렬 순서입니다.")]
    [SerializeField] private int explosionWarningSortingOrder = 5;

    [Tooltip("체크하면 폭발 반경에 맞춰 경고 원 크기를 자동으로 맞춥니다.")]
    [SerializeField] private bool autoFitWarningToExplosionRadius = true;

    [Tooltip("자동 크기 맞춤에 추가로 곱할 배율입니다.")]
    [Min(0.1f)]
    [SerializeField] private float warningScaleMultiplier = 1f;

    [Tooltip("자동 크기 맞춤을 끄면 이 값을 경고 원 스케일로 직접 사용합니다.")]
    [SerializeField] private Vector3 manualWarningScale = Vector3.one;


    [Header("생성 설정")]

    [Tooltip("여우구슬이 살아 있는 전체 유지 시간입니다.")]
    [Min(0.1f)]
    [SerializeField] private float beadLifetime = 8f;

    [Tooltip("여우구슬 시작 위치의 로컬 오프셋입니다.")]
    [SerializeField] private Vector2 spawnLocalOffset = new Vector2(0f, 0.8f);

    [Tooltip("시작 위치에 추가로 흩어지는 연출 반경입니다.")]
    [Min(0f)]
    [SerializeField] private float spawnJitterRadius = 0.1f;


    [Header("추적 설정")]

    [Tooltip("여우구슬 추적 이동 속도입니다.")]
    [Min(0.1f)]
    [SerializeField] private float followSpeed = 4.5f;

    [Tooltip("플레이어와 너무 가까우면 겹치지 않도록 유지할 최소 거리입니다.")]
    [Min(0f)]
    [SerializeField] private float minDistanceToTarget = 1f;


    [Header("폭발 설정")]

    [Tooltip("몇 초마다 폭발 패턴을 실행할지입니다.")]
    [Min(0.05f)]
    [SerializeField] private float explosionInterval = 1.1f;

    [Tooltip("체크하면 폭발 이펙트 프리팹의 실제 보이는 크기를 기준으로 폭발 반경을 계산합니다.")]
    [SerializeField] private bool useExplosionEffectVisualSize = true;

    [Tooltip("이펙트 크기로 계산된 반경에 곱할 배율입니다.")]
    [Min(0.1f)]
    [SerializeField] private float explosionRadiusMultiplier = 1f;

    [Tooltip("자동 계산을 끄면 이 반경 값을 직접 사용합니다.")]
    [Min(0.05f)]
    [SerializeField] private float manualExplosionRadius = 1.2f;

    [Tooltip("폭발 피해량입니다.")]
    [Min(1)]
    [SerializeField] private int explosionDamage = 12;

    [Tooltip("폭발 판정에 사용할 레이어입니다.")]
    [SerializeField] private LayerMask targetLayerMask;

    [Tooltip("폭발 직전 잠깐 멈추는 예고 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float preExplosionDelay = 0.18f;

    [Tooltip("폭발 직후 잠깐 멈춘 뒤 다시 플레이어를 추적하는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float postExplosionPause = 0.2f;


    [Header("기본 공격 강화 설정")]

    [Tooltip("여우구슬 활성 중 기본 화염구 발사 개수입니다.")]
    [Min(1)]
    [SerializeField] private int enhancedProjectileCount = 3;

    [Tooltip("여우구슬 활성 중 기본 화염구 데미지 배율입니다.")]
    [Min(0.1f)]
    [SerializeField] private float enhancedDamageMultiplier = 1.5f;

    [Tooltip("여우구슬 활성 중 기본 화염구 다중 발사 각도 간격입니다.")]
    [Min(0f)]
    [SerializeField] private float enhancedSpreadAngle = 8f;


    [Header("패턴 진행 보조")]

    [Tooltip("여우구슬 활성 중 기본 공격도 계속 함께 사용할지 여부입니다.")]
    [SerializeField] private bool allowBasicAttackWhileBeadActive = true;


    public GameObject PatternStartEffectPrefab => patternStartEffectPrefab;
    public Vector3 PatternStartEffectScale => patternStartEffectScale;
    public Vector3 PatternStartEffectOffset => patternStartEffectOffset;
    public float PatternStartEffectLifetime => patternStartEffectLifetime;

    public GumihoYohoFoxBeadObject FoxBeadPrefab => foxBeadPrefab;
    public int PrewarmCount => prewarmCount;
    public Vector3 FoxBeadScale => foxBeadScale;

    public GameObject ExplosionEffectPrefab => explosionEffectPrefab;
    public Vector3 ExplosionEffectScale => explosionEffectScale;
    public float ExplosionEffectLifetime => explosionEffectLifetime;

    public Sprite ExplosionWarningSprite => explosionWarningSprite;
    public Color ExplosionWarningColor => explosionWarningColor;
    public int ExplosionWarningSortingOrder => explosionWarningSortingOrder;
    public bool AutoFitWarningToExplosionRadius => autoFitWarningToExplosionRadius;
    public float WarningScaleMultiplier => warningScaleMultiplier;
    public Vector3 ManualWarningScale => manualWarningScale;

    public float BeadLifetime => beadLifetime;
    public Vector2 SpawnLocalOffset => spawnLocalOffset;
    public float SpawnJitterRadius => spawnJitterRadius;

    public float FollowSpeed => followSpeed;
    public float MinDistanceToTarget => minDistanceToTarget;

    public float ExplosionInterval => explosionInterval;
    public bool UseExplosionEffectVisualSize => useExplosionEffectVisualSize;
    public float ExplosionRadiusMultiplier => explosionRadiusMultiplier;
    public float ManualExplosionRadius => manualExplosionRadius;
    public int ExplosionDamage => explosionDamage;
    public LayerMask TargetLayerMask => targetLayerMask;
    public float PreExplosionDelay => preExplosionDelay;
    public float PostExplosionPause => postExplosionPause;

    public int EnhancedProjectileCount => enhancedProjectileCount;
    public float EnhancedDamageMultiplier => enhancedDamageMultiplier;
    public float EnhancedSpreadAngle => enhancedSpreadAngle;

    public bool AllowBasicAttackWhileBeadActive => allowBasicAttackWhileBeadActive;
}