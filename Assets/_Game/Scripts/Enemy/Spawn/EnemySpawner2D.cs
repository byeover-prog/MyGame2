using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 일반 몬스터 스포너입니다.
///
/// 현재 구조의 책임:
/// - 무엇을 스폰할지: MonsterSpawnTimelineSO
/// - 어떤 데이터를 쓸지: MonsterRootSO -> MonsterCatalogSO -> MonsterDefinitionSO
/// - 어디에 생성할지: 플레이어 기준 반경 / 스폰 영역
///
/// 스포너는 "생성"까지만 담당합니다.
/// 실제 체력 / 이동 / 공격 수치 적용은 MonsterRuntimeApplier2D가 담당합니다.
///
/// 하위 호환 주의:
/// - 기존 CasualSpawnRateScaler / TrialSpawnRateScaler / DebugRuntimeHUD가
///   EnemySpawner2D의 SpawnRateMultiplier, SetSpawnRateMultiplier를 사용하고 있으므로
///   이번 전환 단계에서는 그 API를 유지합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemySpawner2D : MonoBehaviour
{
    [Header("1. 플레이어 기준")]
    [SerializeField, Tooltip("스폰 거리 계산의 중심이 될 플레이어 Transform입니다.\n"
                             + "비워두면 playerTag로 자동 탐색합니다.\n"
                             + "자동 탐색이 실패하면 스폰도 진행되지 않습니다.")]
    private Transform player;

    [SerializeField, Tooltip("player가 비어 있을 때 자동으로 찾을 플레이어 태그입니다.\n"
                             + "보통 Player를 사용합니다.")]
    private string playerTag = "Player";

    [Header("2. 생성 부모")]
    [SerializeField, Tooltip("스폰된 몬스터를 정리해서 넣을 부모 Transform입니다.\n"
                             + "보통 씬의 MonsterRoot 오브젝트를 연결합니다.\n"
                             + "비워두면 autoFindMonsterRoot가 켜져 있을 때 자동 탐색합니다.")]
    private Transform monsterRoot;

    [SerializeField, Tooltip("monsterRoot가 비어 있을 때\n"
                             + "이름이 MonsterRoot인 오브젝트를 자동 탐색할지 여부입니다.")]
    private bool autoFindMonsterRoot = true;

    [Header("3. 스폰 반경")]
    [SerializeField, Min(0f), Tooltip("플레이어 기준 최소 스폰 반경입니다.\n"
                                      + "이 거리보다 가까운 위치에는 스폰하지 않습니다.\n"
                                      + "단위는 world unit입니다.")]
    private float minSpawnRadius = 8f;

    [SerializeField, Min(0f), Tooltip("플레이어 기준 최대 스폰 반경입니다.\n"
                                      + "이 거리보다 먼 위치에는 스폰하지 않습니다.\n"
                                      + "단위는 world unit입니다.")]
    private float maxSpawnRadius = 12f;

    [Header("4. 생존 수 제한")]
    [SerializeField, Min(1), Tooltip("동시에 살아있다고 간주할 일반 몬스터 최대 수입니다.\n"
                                     + "countsAsAliveEnemy가 true인 몬스터만\n"
                                     + "이 제한에 포함됩니다.")]
    private int maxAliveEnemies = 40;

    [Header("5. 몬스터 데이터 연결")]
    [SerializeField, Tooltip("몬스터 카탈로그 루트입니다.\n"
                             + "MonsterRootSO 안의 MonsterCatalogSO를 통해\n"
                             + "monsterId -> MonsterDefinitionSO를 조회합니다.")]
    private MonsterRootSO monsterDataRoot;

    [SerializeField, Tooltip("스폰 타임라인 데이터입니다.\n"
                             + "각 stage의 duration / spawnInterval / options를 기준으로\n"
                             + "언제 어떤 몬스터를 뽑을지 결정합니다.")]
    private MonsterSpawnTimelineSO timeline;

    [Header("6. 스폰 가능 영역")]
    [SerializeField, Tooltip("몬스터가 스폰될 수 있는 영역 Collider2D입니다.\n"
                             + "연결하면 이 영역 안에서만 스폰 위치를 찾습니다.\n"
                             + "비워두면 단순 반경 랜덤 위치를 사용합니다.")]
    private Collider2D spawnAreaCollider;

    [SerializeField, Min(0f), Tooltip("spawnAreaCollider 안쪽으로 한 번 더 밀어 넣을 여유 거리입니다.\n"
                                      + "영역 가장자리 겹침을 줄이는 용도입니다.\n"
                                      + "단위는 world unit입니다.")]
    private float innerMargin = 0.5f;

    [SerializeField, Min(10), Tooltip("스폰 위치를 찾을 때 최대 시도 횟수입니다.\n"
                                      + "값이 너무 낮으면 영역 안 위치를 못 찾고\n"
                                      + "플레이어 근처 기본값으로 떨어질 수 있습니다.")]
    private int spawnPosMaxTries = 80;

    [Header("7. 스폰 속도 배율")]
    [SerializeField, Tooltip("외부 스케일러가 스폰 속도를 조절할 때 쓰는 런타임 배율입니다.\n"
                             + "기본값은 1이며,\n"
                             + "MonsterSpawnTimelineSO의 spawnInterval에 곱셈이 아니라\n"
                             + "분모 배율로 적용됩니다.\n"
                             + "즉 값이 커질수록 더 자주 스폰됩니다.")]
    private float spawnRateMultiplier = 1f;

    [Header("8. 디버그")]
    [SerializeField, Tooltip("스폰 과정 로그를 출력할지 여부입니다.\n"
                             + "monsterId 누락, 프리팹 누락, 적용기 누락 확인용입니다.")]
    private bool verboseLog = false;

    /// <summary>현재 alive count에 포함된 몬스터 수</summary>
    public int AliveCount => aliveCount;

    /// <summary>alive count 최대값</summary>
    public int MaxAliveCount => maxAliveEnemies;

    /// <summary>다음 스폰까지 남은 시간</summary>
    public float TimeUntilNextSpawn => Mathf.Max(0f, timeUntilNextSpawn);

    /// <summary>현재 스폰 속도 배율</summary>
    public float SpawnRateMultiplier => spawnRateMultiplier;

    private int aliveCount;
    private float timeUntilNextSpawn;
    private float stageElapsed;
    private int stageIndex;

    private readonly Dictionary<string, MonsterDefinitionSO> monsterById
        = new Dictionary<string, MonsterDefinitionSO>(64);

    private EnemyPool2D enemyPool;

    private void Awake()
    {
        ResolvePlayer();
        ResolveMonsterRoot();
        BuildMonsterIndex();

        aliveCount = 0;
        timeUntilNextSpawn = 0f;
        stageElapsed = 0f;
        stageIndex = 0;
        spawnRateMultiplier = Mathf.Clamp(spawnRateMultiplier, 0.01f, 10f);

        enemyPool = new EnemyPool2D("[EnemyPool]");
    }

    private void Update()
    {
        ResolvePlayer();
        ResolveMonsterRoot();

        if (!CanRunSpawner())
            return;

        AdvanceStage(Time.deltaTime);
        TickSpawnTimer(Time.deltaTime);

        if (aliveCount >= maxAliveEnemies)
            return;

        if (timeUntilNextSpawn > 0f)
            return;

        timeUntilNextSpawn = GetCurrentStageSpawnInterval();
        SpawnFromCurrentStage();
    }

    /// <summary>
    /// 기존 런 모드 스케일러와의 하위 호환용 API입니다.
    /// 값이 클수록 실제 스폰 간격은 더 짧아집니다.
    /// </summary>
    public void SetSpawnRateMultiplier(float multiplier)
    {
        spawnRateMultiplier = Mathf.Clamp(multiplier, 0.01f, 10f);
    }

    /// <summary>
    /// 외부 리포터가 몬스터 활성화를 알려줄 때 호출합니다.
    /// </summary>
    public void NotifyEnemyBecameAlive(object enemy)
    {
        aliveCount = Mathf.Max(0, aliveCount + 1);
    }

    /// <summary>
    /// 외부 리포터가 몬스터 비활성화 / 사망을 알려줄 때 호출합니다.
    /// </summary>
    public void NotifyEnemyBecameDead(object enemy)
    {
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }

    private bool CanRunSpawner()
    {
        if (player == null)
            return false;

        if (monsterDataRoot == null)
        {
            if (verboseLog)
                Debug.LogWarning("[EnemySpawner2D] MonsterRootSO가 비어 있습니다.", this);

            return false;
        }

        if (monsterDataRoot.MonsterCatalog == null)
        {
            if (verboseLog)
            {
                Debug.LogWarning(
                    "[EnemySpawner2D] MonsterRootSO 안의 MonsterCatalogSO가 비어 있습니다.",
                    this);
            }

            return false;
        }

        if (timeline == null)
        {
            if (verboseLog)
                Debug.LogWarning("[EnemySpawner2D] MonsterSpawnTimelineSO가 비어 있습니다.", this);

            return false;
        }

        if (timeline.Stages == null || timeline.Stages.Count == 0)
        {
            if (verboseLog)
                Debug.LogWarning("[EnemySpawner2D] 타임라인 stage가 비어 있습니다.", this);

            return false;
        }

        return true;
    }

    private void TickSpawnTimer(float deltaTime)
    {
        timeUntilNextSpawn -= deltaTime;
    }

    private float GetCurrentStageSpawnInterval()
    {
        if (timeline == null || timeline.Stages == null || timeline.Stages.Count == 0)
            return 1f;

        int safeIndex = Mathf.Clamp(stageIndex, 0, timeline.Stages.Count - 1);
        MonsterSpawnTimelineSO.Stage stage = timeline.Stages[safeIndex];

        if (stage == null)
            return 1f;

        float baseInterval = Mathf.Max(0.01f, stage.SpawnInterval);
        float effectiveMultiplier = Mathf.Clamp(spawnRateMultiplier, 0.01f, 10f);

        return baseInterval / effectiveMultiplier;
    }

    private void SpawnFromCurrentStage()
    {
        if (monsterById.Count == 0)
            BuildMonsterIndex();

        if (timeline == null || timeline.Stages == null || timeline.Stages.Count == 0)
            return;

        int safeIndex = Mathf.Clamp(stageIndex, 0, timeline.Stages.Count - 1);
        MonsterSpawnTimelineSO.Stage stage = timeline.Stages[safeIndex];

        if (stage == null || stage.Options == null || stage.Options.Count == 0)
        {
            if (verboseLog)
                Debug.LogWarning("[EnemySpawner2D] 현재 stage의 스폰 후보가 비어 있습니다.", this);

            return;
        }

        string monsterId = PickByWeight(stage.Options);
        if (string.IsNullOrWhiteSpace(monsterId))
        {
            if (verboseLog)
                Debug.LogWarning("[EnemySpawner2D] 가중치 선택 결과 monsterId가 비어 있습니다.", this);

            return;
        }

        if (!monsterById.TryGetValue(monsterId, out MonsterDefinitionSO definition)
            || definition == null)
        {
            Debug.LogWarning(
                $"[EnemySpawner2D] monsterId '{monsterId}'에 해당하는 MonsterDefinitionSO를 찾지 못했습니다.",
                this);
            return;
        }

        if (definition.MonsterPrefab == null)
        {
            Debug.LogWarning(
                $"[EnemySpawner2D] monsterId '{monsterId}'의 MonsterPrefab이 비어 있습니다.",
                this);
            return;
        }

        Vector2 spawnPosition = PickSpawnPos();
        GameObject spawned = enemyPool.Get(
            definition.MonsterPrefab,
            spawnPosition,
            Quaternion.identity,
            monsterRoot);

        if (spawned == null)
        {
            Debug.LogWarning(
                $"[EnemySpawner2D] monsterId '{monsterId}' 스폰에 실패했습니다.",
                this);
            return;
        }

        ApplyRuntimeDefinition(spawned, definition);
        BindAliveReporter(spawned, definition);

        if (verboseLog)
        {
            Debug.Log(
                $"[EnemySpawner2D] 스폰 완료 | ID: {definition.MonsterId} | Behavior: {definition.BehaviorType}",
                this);
        }
    }

    private void ApplyRuntimeDefinition(GameObject spawned, MonsterDefinitionSO definition)
    {
        MonsterRuntimeApplier2D applier = spawned.GetComponent<MonsterRuntimeApplier2D>();
        if (applier == null)
        {
            Debug.LogWarning(
                $"[EnemySpawner2D] '{definition.MonsterId}' 프리팹에 MonsterRuntimeApplier2D가 없습니다.",
                spawned);
            return;
        }

        applier.ApplyDefinition(definition);
    }

    private void BindAliveReporter(GameObject spawned, MonsterDefinitionSO definition)
    {
        EnemyAliveReporter reporter = spawned.GetComponent<EnemyAliveReporter>();
        if (reporter == null)
        {
            if (definition.CountsAsAliveEnemy && verboseLog)
            {
                Debug.LogWarning(
                    $"[EnemySpawner2D] '{definition.MonsterId}'는 alive count 포함 대상인데 EnemyAliveReporter가 없습니다.",
                    spawned);
            }

            return;
        }

        reporter.Init(this, definition.CountsAsAliveEnemy);
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;

        if (string.IsNullOrEmpty(playerTag))
            return;

        GameObject foundPlayer = GameObject.FindGameObjectWithTag(playerTag);
        if (foundPlayer != null)
            player = foundPlayer.transform;
    }

    private void ResolveMonsterRoot()
    {
        if (monsterRoot != null)
            return;

        if (!autoFindMonsterRoot)
            return;

        GameObject foundRoot = GameObject.Find("MonsterRoot");
        if (foundRoot != null)
            monsterRoot = foundRoot.transform;
    }

    private void BuildMonsterIndex()
    {
        monsterById.Clear();

        if (monsterDataRoot == null)
            return;

        MonsterCatalogSO catalog = monsterDataRoot.MonsterCatalog;
        if (catalog == null || catalog.Monsters == null)
            return;

        List<MonsterDefinitionSO> monsters = catalog.Monsters;

        for (int i = 0; i < monsters.Count; i++)
        {
            MonsterDefinitionSO definition = monsters[i];

            if (definition == null)
                continue;

            if (string.IsNullOrWhiteSpace(definition.MonsterId))
                continue;

            monsterById[definition.MonsterId.Trim()] = definition;
        }
    }

    private void AdvanceStage(float deltaTime)
    {
        if (timeline == null || timeline.Stages == null || timeline.Stages.Count == 0)
            return;

        stageElapsed += deltaTime;

        while (stageIndex < timeline.Stages.Count)
        {
            MonsterSpawnTimelineSO.Stage currentStage = timeline.Stages[stageIndex];
            float duration = currentStage != null ? currentStage.Duration : 0f;

            if (duration <= 0f)
            {
                stageIndex++;
                continue;
            }

            if (stageElapsed >= duration)
            {
                stageElapsed -= duration;
                stageIndex++;
                continue;
            }

            break;
        }

        if (stageIndex >= timeline.Stages.Count)
            stageIndex = timeline.Stages.Count - 1;
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
                Vector2 direction = Random.insideUnitCircle;
                if (direction.sqrMagnitude < 0.0001f)
                    continue;

                direction.Normalize();

                Vector2 candidate = center + direction * Random.Range(min, max);

                if (!spawnAreaCollider.OverlapPoint(candidate))
                    continue;

                if (innerMargin > 0f)
                {
                    Vector2 toCenter = areaCenter - candidate;

                    if (toCenter.sqrMagnitude > 0.0001f)
                        candidate += toCenter.normalized * innerMargin;

                    if (!spawnAreaCollider.OverlapPoint(candidate))
                        continue;
                }

                return candidate;
            }

            for (int i = 0; i < spawnPosMaxTries; i++)
            {
                Bounds bounds = spawnAreaCollider.bounds;
                Vector2 candidate = new Vector2(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y));

                if (!spawnAreaCollider.OverlapPoint(candidate))
                    continue;

                if (innerMargin > 0f)
                {
                    Vector2 toCenter = areaCenter - candidate;

                    if (toCenter.sqrMagnitude > 0.0001f)
                        candidate += toCenter.normalized * innerMargin;

                    if (!spawnAreaCollider.OverlapPoint(candidate))
                        continue;
                }

                return candidate;
            }

            return center;
        }

        Vector2 fallbackDirection = Random.insideUnitCircle;
        if (fallbackDirection.sqrMagnitude < 0.0001f)
            fallbackDirection = Vector2.right;

        fallbackDirection.Normalize();
        return center + fallbackDirection * Random.Range(min, max);
    }

    private static string PickByWeight(List<MonsterSpawnTimelineSO.SpawnOption> options)
    {
        float totalWeight = 0f;

        for (int i = 0; i < options.Count; i++)
        {
            MonsterSpawnTimelineSO.SpawnOption option = options[i];

            if (option == null)
                continue;

            if (string.IsNullOrWhiteSpace(option.MonsterId))
                continue;

            if (option.Weight <= 0f)
                continue;

            totalWeight += option.Weight;
        }

        if (totalWeight <= 0f)
            return null;

        float randomValue = Random.value * totalWeight;
        float accumulated = 0f;

        for (int i = 0; i < options.Count; i++)
        {
            MonsterSpawnTimelineSO.SpawnOption option = options[i];

            if (option == null)
                continue;

            if (string.IsNullOrWhiteSpace(option.MonsterId))
                continue;

            if (option.Weight <= 0f)
                continue;

            accumulated += option.Weight;

            if (randomValue <= accumulated)
                return option.MonsterId.Trim();
        }

        for (int i = options.Count - 1; i >= 0; i--)
        {
            MonsterSpawnTimelineSO.SpawnOption option = options[i];

            if (option == null)
                continue;

            if (string.IsNullOrWhiteSpace(option.MonsterId))
                continue;

            if (option.Weight <= 0f)
                continue;

            return option.MonsterId.Trim();
        }

        return null;
    }
}