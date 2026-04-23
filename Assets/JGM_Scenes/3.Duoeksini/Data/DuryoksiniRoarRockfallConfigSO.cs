// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 두억시니 분노 포효 패턴의 정적 데이터만 보관한다.
// 괴성 이펙트와 돌 낙하 이펙트, 웨이브 수치만 여기서 관리한다.
// 실제 타이머와 실행 상태는 컨트롤러가 관리한다.

[CreateAssetMenu(
    menuName = "혼령검/Boss/Duryoksini/분노 포효 낙석 설정",
    fileName = "DuryoksiniRoarRockfallConfigSO")]
public sealed class DuryoksiniRoarRockfallConfigSO : ScriptableObject
{
    [Header("사용 거리")]

    [SerializeField, Tooltip("분노 포효를 사용할 최소 거리이다.")]
    private float minUseDistance = 3f;

    [SerializeField, Tooltip("분노 포효를 사용할 최대 거리이다.")]
    private float maxUseDistance = 12f;

    [SerializeField, Tooltip("분노 포효 재사용 대기시간이다.")]
    private float cooldown = 8f;


    [Header("포효 진행")]

    [SerializeField, Tooltip("포효 시작 후 낙석 웨이브를 시작하기 전까지의 시간이다.")]
    private float roarDuration = 1.2f;

    [SerializeField, Tooltip("포효 후 첫 웨이브가 시작되기 전 추가 지연 시간이다.")]
    private float firstWaveDelay = 0.2f;

    [SerializeField, Tooltip("모든 웨이브 종료 후 후딜 시간이다.")]
    private float recoverDuration = 1f;


    [Header("낙석 웨이브")]

    [SerializeField, Tooltip("총 웨이브 횟수이다.")]
    private int waveCount = 2;

    [SerializeField, Tooltip("한 웨이브에서 떨어질 돌 개수이다.")]
    private int rocksPerWave = 4;

    [SerializeField, Tooltip("웨이브 사이 간격이다.")]
    private float waveInterval = 0.55f;

    [SerializeField, Tooltip("돌 이펙트를 생성한 뒤 실제 피해를 적용하기까지의 지연 시간이다.")]
    private float rockHitDelay = 0.6f;


    [Header("낙석 위치")]

    [SerializeField, Tooltip("보스 중심 기준 낙석이 떨어질 최대 반경이다.")]
    private float spawnRadius = 6f;

    [SerializeField, Tooltip("플레이어 현재 위치 주변을 우선 노릴 돌 개수이다.")]
    private int playerFocusedRockCount = 2;

    [SerializeField, Tooltip("플레이어 주변 낙석이 퍼질 랜덤 반경이다.")]
    private float playerFocusRandomRadius = 1.5f;

    [SerializeField, Tooltip("낙석 위치를 너무 좁게 겹치지 않게 하기 위한 최소 간격이다.")]
    private float minSpacing = 1.25f;


    [Header("피해 판정")]

    [SerializeField, Tooltip("돌 1개의 피해량이다.")]
    private int damage = 20;

    [SerializeField, Tooltip("돌이 떨어질 때의 피해 반경이다.")]
    private float hitRadius = 1.2f;

    [SerializeField, Tooltip("낙석이 타격할 대상 레이어이다.")]
    private LayerMask targetLayerMask;


    [Header("괴성 이펙트")]

    [SerializeField, Tooltip("두억시니 몸에 붙여 생성할 괴성 시작 이펙트 프리팹이다.")]
    private GameObject roarEffectPrefab;

    [SerializeField, Tooltip("괴성 이펙트의 로컬 위치 오프셋이다.")]
    private Vector3 roarEffectOffset = Vector3.zero;

    [SerializeField, Tooltip("괴성 이펙트의 로컬 스케일이다.")]
    private Vector3 roarEffectLocalScale = Vector3.one;

    [SerializeField, Tooltip("괴성 이펙트 자동 삭제 시간이다.")]
    private float roarEffectLifetime = 2f;


    [Header("돌 낙하 이펙트")]

    [SerializeField, Tooltip("전장에 생성할 돌 낙하 이펙트 프리팹이다.")]
    private GameObject rockfallEffectPrefab;

    [SerializeField, Tooltip("돌 낙하 이펙트의 위치 오프셋이다.")]
    private Vector3 rockfallEffectOffset = Vector3.zero;

    [SerializeField, Tooltip("돌 낙하 이펙트의 로컬 스케일이다.")]
    private Vector3 rockfallEffectLocalScale = Vector3.one;

    [SerializeField, Tooltip("돌 낙하 이펙트 자동 삭제 시간이다.")]
    private float rockfallEffectLifetime = 2.5f;


    public float MinUseDistance => minUseDistance;
    public float MaxUseDistance => maxUseDistance;
    public float Cooldown => cooldown;

    public float RoarDuration => roarDuration;
    public float FirstWaveDelay => firstWaveDelay;
    public float RecoverDuration => recoverDuration;

    public int WaveCount => waveCount;
    public int RocksPerWave => rocksPerWave;
    public float WaveInterval => waveInterval;
    public float RockHitDelay => rockHitDelay;

    public float SpawnRadius => spawnRadius;
    public int PlayerFocusedRockCount => playerFocusedRockCount;
    public float PlayerFocusRandomRadius => playerFocusRandomRadius;
    public float MinSpacing => minSpacing;

    public int Damage => damage;
    public float HitRadius => hitRadius;
    public LayerMask TargetLayerMask => targetLayerMask;

    public GameObject RoarEffectPrefab => roarEffectPrefab;
    public Vector3 RoarEffectOffset => roarEffectOffset;
    public Vector3 RoarEffectLocalScale => roarEffectLocalScale;
    public float RoarEffectLifetime => roarEffectLifetime;

    public GameObject RockfallEffectPrefab => rockfallEffectPrefab;
    public Vector3 RockfallEffectOffset => rockfallEffectOffset;
    public Vector3 RockfallEffectLocalScale => rockfallEffectLocalScale;
    public float RockfallEffectLifetime => rockfallEffectLifetime;
}