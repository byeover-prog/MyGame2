// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 구미호 요화 패턴의 수치만 관리한다.
// 여우불 개수, 생성 간격, 공전 설정, 발사 설정을 모두 여기서 조절한다.

[CreateAssetMenu(
    menuName = "혼령검/Boss/Gumiho/요화 패턴 설정",
    fileName = "GumihoYoHwaPatternConfigSO")]
public sealed class GumihoYoHwaPatternConfigSO : BossPatternConfigSO
{
    [Header("여우불 프리팹")]

    [Tooltip("요화 패턴에서 사용할 여우불 프리팹입니다.")]
    [SerializeField] private GumihoFoxFireOrb foxFirePrefab;

    [Tooltip("미리 생성해둘 여우불 풀 개수입니다.")]
    [Min(1)]
    [SerializeField] private int prewarmCount = 8;


    [Header("생성 설정")]

    [Tooltip("최대 여우불 개수입니다.")]
    [Min(1)]
    [SerializeField] private int foxFireCount = 8;

    [Tooltip("여우불이 하나씩 생성되는 간격입니다.")]
    [Min(0.01f)]
    [SerializeField] private float spawnInterval = 0.18f;

    [Tooltip("생성될 때 보스 중심에서 약간 퍼져 나오는 연출 반경입니다.")]
    [Min(0f)]
    [SerializeField] private float spawnJitterRadius = 0.15f;

    [Tooltip("생성될 때 스케일이 커지는 시간입니다.")]
    [Min(0.01f)]
    [SerializeField] private float spawnScaleDuration = 0.12f;


    [Header("공전 설정")]

    [Tooltip("공전 중심의 로컬 오프셋입니다.\n보스 몸 중앙보다 조금 위로 주는 것이 자연스럽습니다.")]
    [SerializeField] private Vector2 orbitAnchorLocalOffset = new Vector2(0f, 0.6f);

    [Tooltip("여우불 공전 반지름입니다.")]
    [Min(0.1f)]
    [SerializeField] private float orbitRadius = 1.4f;

    [Tooltip("공전 속도(도/초)입니다.")]
    [Min(0f)]
    [SerializeField] private float orbitAngularSpeed = 140f;

    [Tooltip("여우불이 공전 슬롯 위치를 따라가는 부드러움 시간입니다.")]
    [Min(0.01f)]
    [SerializeField] private float orbitSmoothTime = 0.08f;

    [Tooltip("공전 중 위아래로 살짝 흔들리는 세기입니다.")]
    [Min(0f)]
    [SerializeField] private float orbitBobAmplitude = 0.06f;

    [Tooltip("공전 중 위아래 흔들림 속도입니다.")]
    [Min(0f)]
    [SerializeField] private float orbitBobFrequency = 2.8f;


    [Header("발사 설정")]

    [Tooltip("최대 개수에 도달한 뒤 발사되기 전 잠깐 모으는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float launchDelay = 0.35f;

    [Tooltip("발사 속도입니다.")]
    [Min(0.1f)]
    [SerializeField] private float launchSpeed = 7.5f;

    [Tooltip("발사 후 자동 회수까지 유지 시간입니다.")]
    [Min(0.1f)]
    [SerializeField] private float launchLifetime = 3f;

    [Tooltip("플레이어에게 주는 피해량입니다.")]
    [Min(1)]
    [SerializeField] private int damage = 8;

    [Tooltip("충돌 대상으로 사용할 레이어입니다.\n보통 Player 레이어를 넣습니다.")]
    [SerializeField] private LayerMask targetLayerMask;


    public GumihoFoxFireOrb FoxFirePrefab => foxFirePrefab;
    public int PrewarmCount => prewarmCount;
    public int FoxFireCount => foxFireCount;
    public float SpawnInterval => spawnInterval;
    public float SpawnJitterRadius => spawnJitterRadius;
    public float SpawnScaleDuration => spawnScaleDuration;
    public Vector2 OrbitAnchorLocalOffset => orbitAnchorLocalOffset;
    public float OrbitRadius => orbitRadius;
    public float OrbitAngularSpeed => orbitAngularSpeed;
    public float OrbitSmoothTime => orbitSmoothTime;
    public float OrbitBobAmplitude => orbitBobAmplitude;
    public float OrbitBobFrequency => orbitBobFrequency;
    public float LaunchDelay => launchDelay;
    public float LaunchSpeed => launchSpeed;
    public float LaunchLifetime => launchLifetime;
    public int Damage => damage;
    public LayerMask TargetLayerMask => targetLayerMask;
}