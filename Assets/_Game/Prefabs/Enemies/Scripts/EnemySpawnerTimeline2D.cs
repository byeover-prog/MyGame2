// UTF-8
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - EnemyRootSO(Id→프리팹/기본스탯)을 딕셔너리로 인덱싱(O(1))한다.
// - EnemySpawnTimelineSO의 "현재 스테이지"를 경과시간으로 결정한다.
// - 스테이지의 Options에서 가중치 랜덤으로 EnemyId를 뽑고, Root에서 실제 프리팹/스탯을 찾아 스폰한다.
[DisallowMultipleComponent]
public sealed class EnemySpawnerTimeline2D : MonoBehaviour
{
    [Header("데이터(필수)")]
    [SerializeField] private EnemyRootSO enemyRoot;
    [SerializeField] private EnemySpawnTimelineSO timeline;

    [Header("스폰 주기")]
    [Min(0.05f)]
    [SerializeField] private float spawnInterval = 1.0f;

    [Header("스폰 위치(간단 버전)")]
    [Tooltip("비워두면 이 오브젝트 위치에서 스폰")]
    [SerializeField] private Transform spawnOrigin;

    [Tooltip("원점 기준 랜덤 반경(유닛)")]
    [Min(0f)]
    [SerializeField] private float spawnRadius = 6f;

    [Header("플레이어 타겟(선택)")]
    [Tooltip("EnemyMotor2D가 있다면 SetTarget으로 넘겨줍니다.")]
    [SerializeField] private Transform player;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private readonly Dictionary<string, EnemyRootSO.EnemyEntry> _byId = new Dictionary<string, EnemyRootSO.EnemyEntry>(64);
    private float _elapsed;
    private float _nextSpawn;
    private int _stageIndex;

    private void Awake()
    {
        if (spawnOrigin == null) spawnOrigin = transform;
        BuildIndex();
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
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.Id)) continue;
            if (e.Prefab == null) continue;

            // 중복 ID면 마지막 것으로 덮어씀(디버그에 유리)
            _byId[e.Id.Trim()] = e;
        }
    }

    private void AdvanceStageIfNeeded()
    {
        // 현재 스테이지가 duration을 넘기면 다음으로 진행
        while (_stageIndex < timeline.Stages.Count)
        {
            var stage = timeline.Stages[_stageIndex];
            float dur = (stage != null) ? stage.Duration : 0f;

            // Duration==0이면 즉시 다음 스테이지로 넘어가게 허용(“정확한 순서” 연출용)
            if (dur <= 0f)
            {
                _stageIndex++;
                continue;
            }

            // 스테이지 시작 시각을 저장하지 않고도 처리하려면: 누적 경과시간 방식으로 계산해야 함.
            // 여기서는 간단하게: 스테이지 진행에 따라 elapsed를 깎는 방식으로 운영한다.
            // (스테이지 넘어갈 때 elapsed -= dur)
            if (_elapsed >= dur)
            {
                _elapsed -= dur;
                _stageIndex++;
                continue;
            }

            break;
        }

        // 끝까지 갔으면 마지막 스테이지 유지
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
        {
            if (debugLog)
                Debug.LogWarning($"[EnemySpawnerTimeline2D] EnemyRoot에 Id='{enemyId}'가 없습니다(또는 Prefab이 비었습니다).", this);
            return;
        }

        Vector2 pos = (Vector2)spawnOrigin.position + Random.insideUnitCircle * spawnRadius;

        GameObject go = Instantiate(entry.Prefab, pos, Quaternion.identity);

        // 스탯 주입(있으면 적용)
        var init = go.GetComponent<IEnemyInit2D>();
        if (init != null)
            init.ApplyBaseStats(entry.BaseHP, entry.BaseMoveSpeed, entry.BaseContactDamage);

        // 타겟 주입(있으면 적용)
        var motor = go.GetComponent<EnemyMotor2D>();
        if (motor != null && player != null)
            motor.SetTarget(player);

        if (debugLog)
            Debug.Log($"[EnemySpawnerTimeline2D] Spawn stage='{stage.Name}' id='{entry.Id}'", this);
    }

    private static string PickByWeight(List<EnemySpawnTimelineSO.SpawnOption> options)
    {
        float total = 0f;
        for (int i = 0; i < options.Count; i++)
        {
            var o = options[i];
            if (o == null) continue;
            if (string.IsNullOrWhiteSpace(o.EnemyId)) continue;
            if (o.Weight <= 0f) continue;
            total += o.Weight;
        }

        if (total <= 0f) return null;

        float r = Random.value * total;
        float acc = 0f;

        for (int i = 0; i < options.Count; i++)
        {
            var o = options[i];
            if (o == null) continue;
            if (string.IsNullOrWhiteSpace(o.EnemyId)) continue;
            if (o.Weight <= 0f) continue;

            acc += o.Weight;
            if (r <= acc)
                return o.EnemyId.Trim();
        }

        // 오차 안전망
        for (int i = options.Count - 1; i >= 0; i--)
        {
            var o = options[i];
            if (o != null && !string.IsNullOrWhiteSpace(o.EnemyId) && o.Weight > 0f)
                return o.EnemyId.Trim();
        }

        return null;
    }
}