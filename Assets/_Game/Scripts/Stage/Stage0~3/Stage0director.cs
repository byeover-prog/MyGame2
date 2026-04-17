using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Stage0Director : MonoBehaviour
{
    public static Stage0Director Instance { get; private set; }

    // ─── 인스펙터 ───────────────────────────────────

    [Header("=== 엘리트 스폰 ===")]
    [Tooltip("엘리트 멧돼지 프리팹")]
    [SerializeField] GameObject elitePrefab;

    [Tooltip("엘리트 스폰 시간 (초)")]
    [SerializeField] float eliteSpawnSec = 120f;

    [Tooltip("엘리트 스폰 위치 오프셋 (플레이어 기준)")]
    [SerializeField] Vector2 eliteSpawnOffset = new Vector2(6f, 0f);

    [Header("=== 러시 스폰 ===")]
    [Tooltip("러시 적 프리팹 (약한 요괴)")]
    [SerializeField] GameObject rushEnemyPrefab;

    [Tooltip("러시 적 수")]
    [SerializeField] int rushCount = 40;

    [Tooltip("러시 스폰 최소 반경")]
    [SerializeField] float rushRadiusMin = 8f;

    [Tooltip("러시 스폰 최대 반경")]
    [SerializeField] float rushRadiusMax = 14f;

    [Tooltip("러시 스폰 지속 시간 (초)")]
    [SerializeField] float rushSpawnDuration = 2f;

    [Header("=== 궁극기 팝업 ===")]
    [Tooltip("궁극기 팝업 UI 컴포넌트")]
    [SerializeField] UltimatePopup2D ultimatePopup;

    [Tooltip("러시 후 팝업 표시까지 대기 시간 (초)")]
    [SerializeField] float popupDelay = 1f;

    [Header("=== 이벤트 ===")]
    [Tooltip("퀘스트 활성화 시 호출 (퀘스트 작업자가 구독)")]
    public UnityEvent OnQuestActivated;

    [Tooltip("클리어 시 호출")]
    public UnityEvent OnStageCleared;

    // ─── 내부 상태 ──────────────────────────────────

    Transform _playerTransform;
    bool _eliteSpawned;
    bool _questCleared;
    bool _rushStarted;
    bool _cleared;
    float _timer;

    readonly List<GameObject> _rushEnemies = new(48);

    // ─── 유니티 ─────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // 플레이어 탐색
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _playerTransform = playerObj.transform;

        // 궁극기 발동 이벤트 구독
        RunSignals.UltimateUsed += OnUltimateUsed;
    }

    void OnDestroy()
    {
        RunSignals.UltimateUsed -= OnUltimateUsed;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (_cleared) return;

        _timer += Time.deltaTime;

        // 2:00 → 엘리트 스폰 + 퀘스트 활성화
        if (!_eliteSpawned && _timer >= eliteSpawnSec)
            SpawnElite();
    }

    // ─── 외부 호출 API ──────────────────────────────

    /// <summary>
    /// 퀘스트 작업자가 퀘스트 완료 시 호출.
    /// </summary>
    public void NotifyQuestClear()
    {
        if (_questCleared || _rushStarted) return;
        _questCleared = true;

        GameLogger.Log("[Stage0] 퀘스트 클리어 → 러시 트리거");
        StartCoroutine(RushRoutine());
    }

    // ─── 내부 로직 ──────────────────────────────────

    void SpawnElite()
    {
        _eliteSpawned = true;

        if (elitePrefab == null)
        {
            GameLogger.LogWarning("[Stage0] 엘리트 프리팹 미연결", this);
        }
        else
        {
            Vector3 spawnPos = _playerTransform != null
                ? _playerTransform.position + (Vector3)eliteSpawnOffset
                : Vector3.zero;

            Instantiate(elitePrefab, spawnPos, Quaternion.identity);
            GameLogger.Log("[Stage0] 엘리트 멧돼지 스폰");
        }

        // 퀘스트 활성화 신호
        OnQuestActivated?.Invoke();
        GameLogger.Log("[Stage0] 퀘스트 활성화");
    }

    IEnumerator RushRoutine()
    {
        _rushStarted = true;

        // 러시 적 스폰
        if (rushEnemyPrefab != null)
        {
            float interval = rushSpawnDuration / rushCount;
            for (int i = 0; i < rushCount; i++)
            {
                Vector3 pos = GetRushSpawnPosition();
                var go = Instantiate(rushEnemyPrefab, pos, Quaternion.identity);
                _rushEnemies.Add(go);
                yield return new WaitForSeconds(interval);
            }
        }
        else
        {
            GameLogger.LogWarning("[Stage0] 러시 프리팹 미연결", this);
            yield return new WaitForSeconds(rushSpawnDuration);
        }

        // 팝업 표시 대기
        yield return new WaitForSeconds(popupDelay);

        // 궁극기 팝업 표시
        if (ultimatePopup != null)
            ultimatePopup.Show();
        else
            GameLogger.LogWarning("[Stage0] ultimatePopup 미연결", this);
    }

    void OnUltimateUsed()
    {
        if (!_rushStarted || _cleared) return;

        GameLogger.Log("[Stage0] 궁극기 발동 → 전체 적 소멸 + 클리어");
        StartCoroutine(ClearRoutine());
    }

    IEnumerator ClearRoutine()
    {
        _cleared = true;

        // 팝업 닫기
        if (ultimatePopup != null)
            ultimatePopup.Hide();

        // 러시 적 전체 소멸 (풀링 반환 또는 비활성)
        foreach (var go in _rushEnemies)
        {
            if (go == null) continue;
            // EnemyHealth를 통해 즉사 처리
            var health = go.GetComponent<EnemyHealth2D>();
            if (health != null)
                health.KillImmediate();
            else
                go.SetActive(false);
        }
        _rushEnemies.Clear();

        // 씬에 남아있는 모든 적도 정리
        KillAllEnemies();

        // 클리어 대기 (연출 여유)
        yield return new WaitForSeconds(1.5f);

        OnStageCleared?.Invoke();
        GameLogger.Log("[Stage0] 스테이지 클리어");
    }

    void KillAllEnemies()
    {
        var enemies = FindObjectsByType<EnemyHealth2D>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e == null || e.IsDead) continue;
            e.KillImmediate();
        }
    }

    Vector3 GetRushSpawnPosition()
    {
        Vector3 center = _playerTransform != null ? _playerTransform.position : Vector3.zero;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(rushRadiusMin, rushRadiusMax);
        return center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
    }
}