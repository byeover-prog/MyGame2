// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타임라인 기반 적 스포너.
/// [최적화] Instantiate → EnemyPool2D 풀링
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemySpawnerTimeline2D : MonoBehaviour
{
    [Header("데이터(필수)")]
    [SerializeField] private EnemyRootSO enemyRoot;
    [SerializeField] private EnemySpawnTimelineSO timeline;

    [Header("스폰 주기")]
    [Min(0.05f)] [SerializeField] private float spawnInterval = 1.0f;

    [Header("스폰 위치(간단 버전)")]
    [SerializeField] private Transform spawnOrigin;
    [Min(0f)] [SerializeField] private float spawnRadius = 6f;

    [Header("플레이어 타겟(선택)")]
    [SerializeField] private Transform player;

    [Header("디버그")]
#pragma warning disable CS0414
    [SerializeField] private bool debugLog = false;
#pragma warning restore CS0414

    private readonly Dictionary<string, EnemyRootSO.EnemyEntry> _byId
        = new Dictionary<string, EnemyRootSO.EnemyEntry>(64);
    private float _elapsed;
    private float _nextSpawn;
    private int _stageIndex;

    // ★ 적 풀
    private EnemyPool2D _enemyPool;

    private void Awake()
    {
        if (spawnOrigin == null) spawnOrigin = transform;
        BuildIndex();
        _enemyPool = new EnemyPool2D("[EnemyPool_Timeline]");
    }

    private void OnEnable()
    {
        _elapsed = 0f;
        _nextSpawn = 0f;
        _stageIndex = 0;
    }

    private void Update()
    {
        if (enemyRoot == null || timeline == null) return;
        if (timeline.Stages == null || timeline.Stages.Count == 0) return;

        _elapsed += Time.deltaTime;
        AdvanceStageIfNeeded();

        _nextSpawn -= Time.deltaTime;
        if (_nextSpawn > 0f) return;
        _nextSpawn = spawnInterval;

        SpawnOne();
    }

    private void BuildIndex()
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

    private void AdvanceStageIfNeeded()
    {
        while (_stageIndex < timeline.Stages.Count)
        {
            var stage = timeline.Stages[_stageIndex];
            float dur = (stage != null) ? stage.Duration : 0f;
            if (dur <= 0f) { _stageIndex++; continue; }
            if (_elapsed >= dur) { _elapsed -= dur; _stageIndex++; continue; }
            break;
        }
        if (_stageIndex >= timeline.Stages.Count)
            _stageIndex = timeline.Stages.Count - 1;
    }

    private void SpawnOne()
    {
        var stage = timeline.Stages[_stageIndex];
        if (stage == null || stage.Options == null || stage.Options.Count == 0) return;

        string enemyId = PickByWeight(stage.Options);
        if (string.IsNullOrEmpty(enemyId)) return;

        if (!_byId.TryGetValue(enemyId, out var entry) || entry == null || entry.Prefab == null)
            return;

        Vector2 pos = (Vector2)spawnOrigin.position + Random.insideUnitCircle * spawnRadius;

        // Instantiate → 풀
        GameObject go = _enemyPool.Get(entry.Prefab, pos, Quaternion.identity, null);

        var init = go.GetComponent<IEnemyInit2D>();
        if (init != null)
            init.ApplyBaseStats(entry.BaseHP, entry.BaseMoveSpeed, entry.BaseContactDamage);

        var motor = go.GetComponent<EnemyMotor2D>();
        if (motor != null && player != null)
            motor.SetTarget(player);
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