// UTF-8
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - 레거시 모드: Enemy Prefab이 있으면 그 프리팹만 반복 스폰(기존 프로젝트 호환)
// - 타임라인 모드: enemyPrefab이 비어있으면 EnemyRootSO + EnemySpawnTimelineSO로 "순서/구간" 스폰
// - 기존 의존 코드 호환을 위해 Alive/SpawnRate 관련 API를 유지한다.
// - [추가] SpawnAreaCollider가 있으면, 스폰 좌표를 뽑을 때 콜라이더 내부인지 검사해서 "맵 밖 스폰"을 원천 차단한다.
[DisallowMultipleComponent]
public sealed class EnemySpawner2D : MonoBehaviour
{
    [Header("프리팹(레거시 모드)")]
    [Tooltip("값이 있으면 이 프리팹만 계속 스폰합니다.\n비워두면 EnemyRoot/Timeline 기반 스폰으로 동작합니다.")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("스폰 기준(플레이어)")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("스폰 부모(옵션)")]
    [SerializeField] private Transform monsterRoot;
    [SerializeField] private bool autoFindMonsterRoot = true;

    [Header("스폰 반경")]
    [Min(0f)]
    [SerializeField] private float minSpawnRadius = 8f;
    [Min(0f)]
    [SerializeField] private float maxSpawnRadius = 12f;

    [Header("스폰 속도(기본)")]
    [Min(0.01f)]
    [SerializeField] private float spawnRatePerSec = 3f;

    [Header("최대 생존 수")]
    [Min(1)]
    [SerializeField] private int maxAliveEnemies = 40;

    [Header("안전장치(레거시 호환)")]
    [SerializeField] private bool ensureEnemyRegistry = true;

    [Header("디버그")]
    [SerializeField] private bool verboseLog = false;

    [Header("타임라인 모드(추천)")]
    [Tooltip("스폰 가능한 몬스터 등록소(Id/프리팹/기본스탯)")]
    [SerializeField] private EnemyRootSO enemyRoot;

    [Tooltip("시간 흐름에 따른 스폰 순서/구간")]
    [SerializeField] private EnemySpawnTimelineSO timeline;

    [Header("스폰 영역(필수 권장)")]
    [Tooltip("스폰 가능한 '플레이 영역'을 정의하는 Collider2D 입니다.\n" +
             "예) 나무 라인 안쪽만 감싸는 PolygonCollider2D (IsTrigger 체크 권장)\n" +
             "이 값이 있으면, 스폰 좌표는 반드시 이 콜라이더 내부에서만 생성됩니다.")]
    [SerializeField] private Collider2D spawnAreaCollider;

    [Tooltip("영역 가장자리(나무/벽)에 너무 붙지 않게, 안쪽으로 조금 밀어주는 마진(월드 유닛).")]
    [Min(0f)]
    [SerializeField] private float innerMargin = 0.5f;

    [Tooltip("스폰 좌표 재시도 횟수. (플레이어가 가장자리에 붙어도 안쪽 점을 찾기 위해 필요)")]
    [Min(10)]
    [SerializeField] private int spawnPosMaxTries = 80;

    // ===== 기존 프로젝트가 요구하는 공개 API =====
    public int AliveCount => _aliveCount;
    public int MaxAliveCount => maxAliveEnemies;
    public float TimeUntilNextSpawn => Mathf.Max(0f, _timeUntilNextSpawn);
    public float SpawnRateMultiplier => _spawnRateMultiplier;

    // ===== 런타임 =====
    private int _aliveCount;
    private float _spawnRateMultiplier = 1f;

    private float _timeUntilNextSpawn;   // "다음 스폰까지 남은 시간" 카운트다운
    private float _stageElapsed;
    private int _stageIndex;

    private readonly Dictionary<string, EnemyRootSO.EnemyEntry> _byId = new Dictionary<string, EnemyRootSO.EnemyEntry>(64);

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
    }

    private void Update()
    {
        ResolvePlayer();
        ResolveMonsterRoot();

        if (player == null) return;

        // 최대 생존 제한
        if (_aliveCount >= maxAliveEnemies)
        {
            // 그래도 타이머는 계속 흐르게 두는 게 디버그/체감에 자연스러움
            TickSpawnTimer(Time.deltaTime);
            return;
        }

        TickSpawnTimer(Time.deltaTime);
        if (_timeUntilNextSpawn > 0f) return;

        // 스폰!
        _timeUntilNextSpawn = GetSpawnInterval();

        // 레거시: 프리팹 1개 반복
        if (enemyPrefab != null)
        {
            SpawnLegacy(enemyPrefab);
            return;
        }

        // 타임라인 모드
        if (enemyRoot == null || timeline == null || timeline.Stages == null || timeline.Stages.Count == 0)
        {
            if (verboseLog)
                Debug.LogWarning("[EnemySpawner2D] enemyPrefab도 없고 EnemyRoot/Timeline도 비어있어서 스폰 불가", this);
            return;
        }

        AdvanceStage(Time.deltaTime);
        SpawnFromTimeline();
    }

    private void TickSpawnTimer(float dt)
    {
        _timeUntilNextSpawn -= dt;
    }

    private float GetSpawnInterval()
    {
        float rate = Mathf.Max(0.01f, spawnRatePerSec) * Mathf.Max(0.01f, _spawnRateMultiplier);
        return 1f / rate;
    }

    // ===== 외부에서 호출되는 레거시 API (의존 코드 호환) =====

    public void SetSpawnRateMultiplier(float multiplier)
    {
        _spawnRateMultiplier = Mathf.Clamp(multiplier, 0.1f, 10f);
    }

    // EnemyAliveReporter가 어떤 타입을 넘기든 컴파일 되게 "object"로 받는다.
    public void NotifyEnemyBecameAlive(object enemy)
    {
        _aliveCount = Mathf.Max(0, _aliveCount + 1);
        if (verboseLog)
            Debug.Log($"[EnemySpawner2D] Alive +1 => {_aliveCount}", this);
    }

    public void NotifyEnemyBecameDead(object enemy)
    {
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
        if (verboseLog)
            Debug.Log($"[EnemySpawner2D] Alive -1 => {_aliveCount}", this);
    }

    // ===== 내부 유틸 =====

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
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.Id)) continue;
            if (e.Prefab == null) continue;

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

            if (dur <= 0f)
            {
                _stageIndex++;
                continue;
            }

            if (_stageElapsed >= dur)
            {
                _stageElapsed -= dur;
                _stageIndex++;
                continue;
            }

            break;
        }

        if (_stageIndex >= timeline.Stages.Count)
            _stageIndex = timeline.Stages.Count - 1;
    }

    private void SpawnLegacy(GameObject prefab)
    {
        Vector2 pos = PickSpawnPos();
        var go = Instantiate(prefab, pos, Quaternion.identity, monsterRoot);

        // 타겟 주입(있으면)
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
        {
            if (verboseLog)
                Debug.LogWarning($"[EnemySpawner2D] EnemyRoot에 Id='{id}'가 없습니다(또는 Prefab 비어있음).", this);
            return;
        }

        Vector2 pos = PickSpawnPos();
        var go = Instantiate(entry.Prefab, pos, Quaternion.identity, monsterRoot);

        // 스탯 주입(있으면)
        var init = go.GetComponent<IEnemyInit2D>();
        if (init != null)
            init.ApplyBaseStats(entry.BaseHP, entry.BaseMoveSpeed, entry.BaseContactDamage);

        // 타겟 주입(있으면)
        var motor = go.GetComponent<EnemyMotor2D>();
        if (motor != null) motor.SetTarget(player);

        if (verboseLog)
            Debug.Log($"[EnemySpawner2D] stage='{stage.Name}' spawn id='{entry.Id}'", this);
    }

    private Vector2 PickSpawnPos()
    {
        float min = Mathf.Max(0f, minSpawnRadius);
        float max = Mathf.Max(min + 0.01f, maxSpawnRadius);

        Vector2 center = player.position;

        // [핵심] SpawnAreaCollider가 있으면: "반경 링"에서 뽑되, 콜라이더 내부인 점만 채택
        if (spawnAreaCollider != null)
        {
            // 콜라이더 중심(마진을 안쪽으로 밀 때 사용)
            Vector2 areaCenter = spawnAreaCollider.bounds.center;

            for (int i = 0; i < spawnPosMaxTries; i++)
            {
                Vector2 dir = Random.insideUnitCircle;
                if (dir.sqrMagnitude < 0.0001f) continue;
                dir.Normalize();

                float r = Random.Range(min, max);
                Vector2 p = center + dir * r;

                // 영역 밖이면 재시도
                if (!spawnAreaCollider.OverlapPoint(p))
                    continue;

                // 가장자리 너무 붙는 것 방지: 영역 중심 방향으로 살짝 밀기
                if (innerMargin > 0f)
                {
                    Vector2 toCenter = (areaCenter - p);
                    if (toCenter.sqrMagnitude > 0.0001f)
                        p += toCenter.normalized * innerMargin;

                    // 마진 적용 후에도 내부인지 확인
                    if (!spawnAreaCollider.OverlapPoint(p))
                        continue;
                }

                return p;
            }

            // 여기까지 왔다 = 링 안에서 내부 점을 못 찾음(플레이어가 가장자리 바깥쪽에 너무 붙은 경우 등)
            // 최후 안전장치: 영역 내부에서 임의 점을 뽑는다(반경 조건은 포기하되 "밖 스폰"은 절대 안 함)
            for (int i = 0; i < spawnPosMaxTries; i++)
            {
                var b = spawnAreaCollider.bounds;
                float x = Random.Range(b.min.x, b.max.x);
                float y = Random.Range(b.min.y, b.max.y);
                Vector2 p = new Vector2(x, y);

                if (!spawnAreaCollider.OverlapPoint(p))
                    continue;

                if (innerMargin > 0f)
                {
                    Vector2 toCenter = (areaCenter - p);
                    if (toCenter.sqrMagnitude > 0.0001f)
                        p += toCenter.normalized * innerMargin;

                    if (!spawnAreaCollider.OverlapPoint(p))
                        continue;
                }

                if (verboseLog)
                    Debug.LogWarning("[EnemySpawner2D] 링 내부 점을 못 찾아서 '영역 내부 랜덤'으로 대체 스폰했습니다.", this);

                return p;
            }

            // 그래도 실패면(콜라이더가 너무 얇거나 잘못 세팅): 그냥 플레이어 위치 근처
            if (verboseLog)
                Debug.LogWarning("[EnemySpawner2D] SpawnAreaCollider 내부 점 추출 실패. SpawnArea 콜라이더 모양/IsTrigger/두께를 확인하세요.", this);

            return center;
        }

        // SpawnAreaCollider가 없으면: 기존 방식 그대로(플레이어 주변 링)
        {
            Vector2 dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            dir.Normalize();

            float r = Random.Range(min, max);
            return center + dir * r;
        }
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