using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class EnemySpawner2D : MonoBehaviour
{
    [Header("프리팹")]
    [FormerlySerializedAs("enemy_prefab")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("스폰 기준(플레이어)")]
    [Tooltip("비워두면 자동 탐색(태그/컴포넌트)합니다.")]
    [FormerlySerializedAs("player")]
    [SerializeField] private Transform player;

    [Tooltip("플레이어 자동 탐색 태그")]
    [SerializeField] private string playerTag = "Player";

    [Header("스폰 부모(옵션)")]
    [Tooltip("지정하면 스폰된 적이 이 트랜스폼 하위로 들어갑니다. (Hierarchy 정리용)")]
    [SerializeField] private Transform monsterRoot;

    [Tooltip("monsterRoot가 비어있으면 이름으로 자동 탐색 (MonsterRoot)")]
    [SerializeField] private bool autoFindMonsterRootByName = true;

    [Header("스폰 반경")]
    [FormerlySerializedAs("min_spawn_radius")]
    [SerializeField, Min(0f)] private float minSpawnRadius = 8f;

    [FormerlySerializedAs("max_spawn_radius")]
    [SerializeField, Min(0f)] private float maxSpawnRadius = 12f;

    [Header("스폰 속도(기본)")]
    [Tooltip("초당 스폰 수(기본값). 외부 스크립트가 배율을 곱해서 조절합니다.")]
    [FormerlySerializedAs("spawn_rate_per_sec")]
    [SerializeField, Min(0f)] private float spawnRatePerSec = 1.0f;

    [Header("최대 생존 수")]
    [FormerlySerializedAs("max_alive_enemies")]
    [SerializeField, Min(0)] private int maxAliveEnemies = 40;

    [Header("안전장치")]
    [Tooltip("적 프리팹에 EnemyRegistryMember2D가 없으면 런타임에 자동 추가(스킬 타겟팅 실패 방지)")]
    [SerializeField] private bool ensureEnemyRegistryMember = true;

    [Header("디버그")]
    [FormerlySerializedAs("verbose_log")]
    [SerializeField] private bool verboseLog = false;

    private int _aliveCount;
    private float _nextSpawnTime;

    private bool _isStageStarted;
    private bool _stopSpawning;

    // 외부 주입 배율(스포너는 이 값만 믿고 동작)
    private float _spawnRateMultiplier = 1f;

    private void Awake()
    {
        if (autoFindMonsterRootByName && monsterRoot == null)
        {
            var mr = GameObject.Find("MonsterRoot");
            if (mr != null) monsterRoot = mr.transform;
        }

        TryResolvePlayer();

        _aliveCount = 0;
        _nextSpawnTime = Time.time;

        _isStageStarted = false;
        _stopSpawning = false;
    }

    private void OnEnable()
    {
        RunSignals.StageStarted += OnStageStarted;
        RunSignals.PlayerDead += OnPlayerDead;
    }

    private void OnDisable()
    {
        RunSignals.StageStarted -= OnStageStarted;
        RunSignals.PlayerDead -= OnPlayerDead;
    }

    private void Start()
    {
        // BootEntry 없이 Scene_Game을 직접 실행해도 돌아가게 백업
        if (!_isStageStarted)
            ForceStageStart();
    }

    /// <summary>
    /// 외부에서 스폰 속도 배율 주입 (1 = 기본)
    /// - 캐주얼/모드/밸런스 로직은 외부 스크립트가 담당
    /// </summary>
    public void SetSpawnRateMultiplier(float multiplier)
    {
        _spawnRateMultiplier = Mathf.Max(0.01f, multiplier);
    }

    private void OnStageStarted()
    {
        if (_isStageStarted) return;

        _isStageStarted = true;
        _stopSpawning = false;
        _nextSpawnTime = Time.time;

        if (verboseLog)
            Debug.Log("[EnemySpawner2D] StageStarted -> 스폰 시작", this);
    }

    private void ForceStageStart()
    {
        _isStageStarted = true;
        _stopSpawning = false;
        _nextSpawnTime = Time.time;

        if (verboseLog)
            Debug.Log("[EnemySpawner2D] ForceStageStart -> 스폰 시작", this);
    }

    private void OnPlayerDead()
    {
        _stopSpawning = true;

        if (verboseLog)
            Debug.Log("[EnemySpawner2D] PlayerDead -> 스폰 정지", this);
    }

    private void Update()
    {
        if (!_isStageStarted) return;
        if (_stopSpawning) return;
        if (enemyPrefab == null) return;

        if (player == null)
            TryResolvePlayer();

        if (player == null)
            return;

        if (_aliveCount >= maxAliveEnemies) return;
        if (Time.time < _nextSpawnTime) return;

        float currentRate = spawnRatePerSec * _spawnRateMultiplier;
        float interval = (currentRate <= 0f) ? 999f : (1f / currentRate);
        _nextSpawnTime = Time.time + interval;

        SpawnOne();

        if (verboseLog)
            Debug.Log($"[EnemySpawner2D] alive={_aliveCount}/{maxAliveEnemies}, rate={currentRate:0.###}/s", this);
    }

    private void TryResolvePlayer()
    {
        if (player != null) return;

        // 1) Tag 기반
        if (!string.IsNullOrWhiteSpace(playerTag))
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null)
            {
                player = go.transform;
                return;
            }
        }

        // 2) 컴포넌트 기반(태그 미설정 대응)
        var exp = FindFirstObjectByType<PlayerExp>();
        if (exp != null)
        {
            player = exp.transform;
            return;
        }

        var mover = FindFirstObjectByType<PlayerMover2D>();
        if (mover != null)
        {
            player = mover.transform;
            return;
        }
    }

    private void SpawnOne()
    {
        float minR = Mathf.Max(0f, minSpawnRadius);
        float maxR = Mathf.Max(minR, maxSpawnRadius);

        Vector2 dir = Random.insideUnitCircle;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        float dist = Random.Range(minR, maxR);
        Vector3 pos = player.position + new Vector3(dir.x, dir.y, 0f) * dist;

        GameObject go = Instantiate(enemyPrefab, pos, Quaternion.identity, monsterRoot);

        // 살아있는 적 수 추적(풀링/Destroy 모두 커버)
        EnemyAliveReporter reporter = go.GetComponent<EnemyAliveReporter>();
        if (reporter == null) reporter = go.AddComponent<EnemyAliveReporter>();
        reporter.Init(this);

        // 스킬 타겟팅(EnemyRegistry2D) 안전장치
        if (ensureEnemyRegistryMember && !go.TryGetComponent(out EnemyRegistryMember2D _))
            go.AddComponent<EnemyRegistryMember2D>();
    }

    // ===== EnemyAliveReporter 콜백 =====
    public void NotifyEnemyBecameAlive(EnemyAliveReporter enemy)
    {
        _aliveCount++;
    }

    public void NotifyEnemyBecameDead(EnemyAliveReporter enemy)
    {
        _aliveCount--;
        if (_aliveCount < 0) _aliveCount = 0;
    }
}
