// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스포너. 레거시/타임라인 모드 모두 지원.
/// [최적화] Instantiate → EnemyPool2D 풀링
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemySpawner2D : MonoBehaviour
{
    [Header("프리팹(레거시 모드)")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("스폰 기준(플레이어)")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("스폰 부모(옵션)")]
    [SerializeField] private Transform monsterRoot;
    [SerializeField] private bool autoFindMonsterRoot = true;

    [Header("스폰 반경")]
    [Min(0f)] [SerializeField] private float minSpawnRadius = 8f;
    [Min(0f)] [SerializeField] private float maxSpawnRadius = 12f;

    [Header("스폰 속도(기본)")]
    [Min(0.01f)] [SerializeField] private float spawnRatePerSec = 3f;

    [Header("최대 생존 수")]
    [Min(1)] [SerializeField] private int maxAliveEnemies = 40;

    [Header("안전장치(레거시 호환)")]
#pragma warning disable CS0414
    [SerializeField] private bool ensureEnemyRegistry = true;
#pragma warning restore CS0414

    [Header("디버그")]
    [SerializeField] private bool verboseLog = false;

    [Header("타임라인 모드(추천)")]
    [SerializeField] private EnemyRootSO enemyRoot;
    [SerializeField] private EnemySpawnTimelineSO timeline;

    [Header("스폰 영역(필수 권장)")]
    [SerializeField] private Collider2D spawnAreaCollider;
    [Min(0f)] [SerializeField] private float innerMargin = 0.5f;
    [Min(10)] [SerializeField] private int spawnPosMaxTries = 80;

    // ===== 공개 API =====
    public int AliveCount => _aliveCount;
    public int MaxAliveCount => maxAliveEnemies;
    public float TimeUntilNextSpawn => Mathf.Max(0f, _timeUntilNextSpawn);
    public float SpawnRateMultiplier => _spawnRateMultiplier;

    // ===== 런타임 =====
    private int _aliveCount;
    private float _spawnRateMultiplier = 1f;
    private float _timeUntilNextSpawn;
    private float _stageElapsed;
    private int _stageIndex;

    private readonly Dictionary<string, EnemyRootSO.EnemyEntry> _byId
        = new Dictionary<string, EnemyRootSO.EnemyEntry>(64);

    // ★ 적 풀
    private EnemyPool2D _enemyPool;

    private void Awake()
    {
        ResolvePlayer();
        ResolveMonsterRoot();
        BuildEnemyIndex();

        _aliveCount = 0;
        _spawnRateMultiplier = 1f;
        _timeUntilNextSpawn = 0f;
        _stageElapsed = 0f;
        _stageIndex = 0;

        // ★ 풀 생성
        _enemyPool = new EnemyPool2D("[EnemyPool]");
    }

    private void Update()
    {
        ResolvePlayer();
        ResolveMonsterRoot();

        if (player == null) return;

        if (_aliveCount >= maxAliveEnemies)
        {
            TickSpawnTimer(Time.deltaTime);
            return;
        }

        TickSpawnTimer(Time.deltaTime);
        if (_timeUntilNextSpawn > 0f) return;

        _timeUntilNextSpawn = GetSpawnInterval();

        if (enemyPrefab != null)
        {
            SpawnLegacy(enemyPrefab);
            return;
        }

        if (enemyRoot == null || timeline == null || timeline.Stages == null || timeline.Stages.Count == 0)
            return;

        AdvanceStage(Time.deltaTime);
        SpawnFromTimeline();
    }

    private void TickSpawnTimer(float dt) => _timeUntilNextSpawn -= dt;

    private float GetSpawnInterval()
    {
        float rate = Mathf.Max(0.01f, spawnRatePerSec) * Mathf.Max(0.01f, _spawnRateMultiplier);
        return 1f / rate;
    }

    // ===== 레거시 API =====

    public void SetSpawnRateMultiplier(float multiplier)
        => _spawnRateMultiplier = Mathf.Clamp(multiplier, 0.1f, 10f);

    public void NotifyEnemyBecameAlive(object enemy)
    {
        _aliveCount = Mathf.Max(0, _aliveCount + 1);
    }

    public void NotifyEnemyBecameDead(object enemy)
    {
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
    }

    // ===== 스폰 =====

    private void SpawnLegacy(GameObject prefab)
    {
        Vector2 pos = PickSpawnPos();

        // ★ Instantiate → 풀
        var go = _enemyPool.Get(prefab, pos, Quaternion.identity, monsterRoot);

        var motor = go.GetComponent<EnemyMotor2D>();
        if (motor != null) motor.SetTarget(player);
    }

    private void SpawnFromTimeline()
    {
        if (_byId.Count == 0) BuildEnemyIndex();

        var stage = timeline.Stages[_stageIndex];
        if (stage == null || stage.Options == null || stage.Options.Count == 0) return;

        string id = PickByWeight(stage.Options);
        if (string.IsNullOrEmpty(id)) return;

        if (!_byId.TryGetValue(id, out var entry) || entry == null || entry.Prefab == null)
            return;

        Vector2 pos = PickSpawnPos();

        // ★ Instantiate → 풀
        var go = _enemyPool.Get(entry.Prefab, pos, Quaternion.identity, monsterRoot);

        var init = go.GetComponent<IEnemyInit2D>();
        if (init != null)
            init.ApplyBaseStats(entry.BaseHP, entry.BaseMoveSpeed, entry.BaseContactDamage);

        var motor = go.GetComponent<EnemyMotor2D>();
        if (motor != null) motor.SetTarget(player);
    }

    // ===== 유틸 (변경 없음) =====

    private void ResolvePlayer()
    {
        if (player != null) return;
        if (!string.IsNullOrEmpty(playerTag))
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) player = go.transform;
        }
    }

    private void ResolveMonsterRoot()
    {
        if (monsterRoot != null) return;
        if (!autoFindMonsterRoot) return;
        var t = GameObject.Find("MonsterRoot");
        if (t != null) monsterRoot = t.transform;
    }

    private void BuildEnemyIndex()
    {
        _byId.Clear();
        if (enemyRoot == null || enemyRoot.Enemies == null) return;
        for (int i = 0; i < enemyRoot.Enemies.Count; i++)
        {
            var e = enemyRoot.Enemies[i];
            if (e == null || string.IsNullOrWhiteSpace(e.Id) || e.Prefab == null) continue;
            _byId[e.Id.Trim()] = e;
        }
    }

    private void AdvanceStage(float dt)
    {
        _stageElapsed += dt;
        while (_stageIndex < timeline.Stages.Count)
        {
            var stage = timeline.Stages[_stageIndex];
            float dur = (stage != null) ? stage.Duration : 0f;
            if (dur <= 0f) { _stageIndex++; continue; }
            if (_stageElapsed >= dur) { _stageElapsed -= dur; _stageIndex++; continue; }
            break;
        }
        if (_stageIndex >= timeline.Stages.Count)
            _stageIndex = timeline.Stages.Count - 1;
    }

    private Vector2 PickSpawnPos()
    {
        float min = Mathf.Max(0f, minSpawnRadius);
        float max = Mathf.Max(min + 0.01f, maxSpawnRadius);
        Vector2 center = player.position;

        if (spawnAreaCollider != null)
        {
            Vector2 areaCenter = spawnAreaCollider.bounds.center;

            for (int i = 0; i < spawnPosMaxTries; i++)
            {
                Vector2 dir = Random.insideUnitCircle;
                if (dir.sqrMagnitude < 0.0001f) continue;
                dir.Normalize();
                Vector2 p = center + dir * Random.Range(min, max);

                if (!spawnAreaCollider.OverlapPoint(p)) continue;

                if (innerMargin > 0f)
                {
                    Vector2 toCenter = (areaCenter - p);
                    if (toCenter.sqrMagnitude > 0.0001f)
                        p += toCenter.normalized * innerMargin;
                    if (!spawnAreaCollider.OverlapPoint(p)) continue;
                }
                return p;
            }

            for (int i = 0; i < spawnPosMaxTries; i++)
            {
                var b = spawnAreaCollider.bounds;
                Vector2 p = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));
                if (!spawnAreaCollider.OverlapPoint(p)) continue;
                if (innerMargin > 0f)
                {
                    Vector2 toCenter = (areaCenter - p);
                    if (toCenter.sqrMagnitude > 0.0001f) p += toCenter.normalized * innerMargin;
                    if (!spawnAreaCollider.OverlapPoint(p)) continue;
                }
                return p;
            }
            return center;
        }

        {
            Vector2 dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            dir.Normalize();
            return center + dir * Random.Range(min, max);
        }
    }

    private static string PickByWeight(List<EnemySpawnTimelineSO.SpawnOption> options)
    {
        float total = 0f;
        for (int i = 0; i < options.Count; i++)
        {
            var o = options[i];
            if (o == null || string.IsNullOrWhiteSpace(o.EnemyId) || o.Weight <= 0f) continue;
            total += o.Weight;
        }
        if (total <= 0f) return null;

        float r = Random.value * total;
        float acc = 0f;
        for (int i = 0; i < options.Count; i++)
        {
            var o = options[i];
            if (o == null || string.IsNullOrWhiteSpace(o.EnemyId) || o.Weight <= 0f) continue;
            acc += o.Weight;
            if (r <= acc) return o.EnemyId.Trim();
        }
        for (int i = options.Count - 1; i >= 0; i--)
        {
            var o = options[i];
            if (o != null && !string.IsNullOrWhiteSpace(o.EnemyId) && o.Weight > 0f)
                return o.EnemyId.Trim();
        }
        return null;
    }
}