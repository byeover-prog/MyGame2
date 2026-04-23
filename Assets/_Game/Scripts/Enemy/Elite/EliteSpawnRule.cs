using UnityEngine;

public sealed class EliteSpawnRule : MonoBehaviour
{
    [Header("스폰 타이밍")]
    [Tooltip("첫 엘리트 스폰 시간(초)입니다.")]
    [SerializeField] private float firstSpawnTime = 60f;

    [Tooltip("이후 엘리트 스폰 간격(초)입니다.")]
    [SerializeField] private float spawnInterval = 45f;

    [Header("엘리트 프리팹")]
    [Tooltip("엘리트 몬스터 프리팹 목록입니다.")]
    [SerializeField] private GameObject[] elitePrefabs;

    [Tooltip("한 번에 스폰되는 엘리트 수입니다.")]
    [SerializeField] private int spawnCount = 1;

    [Header("스폰 위치")]
    [Tooltip("스폰 최소 거리 (플레이어 기준)입니다.")]
    [SerializeField] private float spawnRadiusMin = 8f;

    [Tooltip("스폰 최대 거리 (플레이어 기준)입니다.")]
    [SerializeField] private float spawnRadiusMax = 12f;

    [Header("참조")]
    [Tooltip("플레이어 Transform입니다. 비워두면 자동 탐색합니다.")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("엘리트 풀 사용 여부입니다. (미사용 시 Instantiate)")]
    [SerializeField] private bool usePooling = false;

    // ─── 런타임 ───
    private float _elapsed;
    private float _nextSpawnTime;
    private int _spawnedCount;

    void Start()
    {
        _nextSpawnTime = firstSpawnTime;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (elitePrefabs == null || elitePrefabs.Length == 0) return;
        if (playerTransform == null) return;

        _elapsed += Time.deltaTime;

        if (_elapsed >= _nextSpawnTime)
        {
            SpawnElites();
            _nextSpawnTime = _elapsed + spawnInterval;
        }
    }

    private void SpawnElites()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = elitePrefabs[Random.Range(0, elitePrefabs.Length)];
            if (prefab == null) continue;

            Vector2 spawnPos = GetRandomSpawnPosition();

            if (usePooling)
            {
                // TODO: Enemypool2d 통합 시 풀링 사용
                Instantiate(prefab, spawnPos, Quaternion.identity);
            }
            else
            {
                Instantiate(prefab, spawnPos, Quaternion.identity);
            }

            _spawnedCount++;
            GameLogger.Log($"[EliteSpawner] 엘리트 스폰: {prefab.name} (#{_spawnedCount})");
        }
    }

    private Vector2 GetRandomSpawnPosition()
    {
        Vector2 playerPos = playerTransform.position;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(spawnRadiusMin, spawnRadiusMax);
        return playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 center = playerTransform != null
            ? playerTransform.position
            : transform.position;

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
        Gizmos.DrawWireSphere(center, spawnRadiusMin);
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(center, spawnRadiusMax);
    }
#endif
}