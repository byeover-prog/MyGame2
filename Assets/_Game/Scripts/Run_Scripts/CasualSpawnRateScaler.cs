using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CasualSpawnRateScaler : MonoBehaviour
{
    [Header("대상 스포너")]
    [Tooltip("비워두면 씬에서 1회 자동 탐색합니다.")]
    [SerializeField] private EnemySpawner2D spawner;

    [Header("갱신 주기(초)")]
    [Tooltip("배율 계산/주입을 몇 초마다 할지 (권장: 0.1 ~ 0.5)")]
    [SerializeField] private float refresh_interval = 0.2f;

    [Header("디버그")]
    [Tooltip("콘솔 로그가 필요할 때만 켜세요.")]
    [SerializeField] private bool verbose_log = false;

    private float stage_start_time;
    private bool stage_started;
    private Coroutine refresh_co;

    private void Awake()
    {
        // 값 방어
        refresh_interval = Mathf.Max(0.05f, refresh_interval);

        // 인스펙터 연결이 없을 때만 1회 탐색
        if (spawner == null)
            spawner = Object.FindFirstObjectByType<EnemySpawner2D>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        // 이벤트 연결
        RunSignals.StageStarted += OnStageStarted;
        RunSignals.RunConfigChanged += ApplyNow;
        RunSignals.PlayerDead += OnPlayerDead;

        // BootEntry 없이도 시작하도록 백업
        if (!stage_started)
            OnStageStarted();

        // 즉시 1회 적용 후 루프 시작
        ApplyNow();
        refresh_co = StartCoroutine(Co_RefreshLoop());
    }

    private void OnDisable()
    {
        RunSignals.StageStarted -= OnStageStarted;
        RunSignals.RunConfigChanged -= ApplyNow;
        RunSignals.PlayerDead -= OnPlayerDead;

        if (refresh_co != null)
        {
            StopCoroutine(refresh_co);
            refresh_co = null;
        }
    }

    private void OnStageStarted()
    {
        stage_start_time = Time.time;
        stage_started = true;

        if (verbose_log)
            Debug.Log("[CasualSpawnRateScaler] StageStarted");
    }

    private void OnPlayerDead()
    {
        // 플레이어가 죽으면 배율을 최소로 내려 사실상 정지
        if (spawner != null)
            spawner.SetSpawnRateMultiplier(0.01f);
    }

    private IEnumerator Co_RefreshLoop()
    {
        var wait = new WaitForSeconds(refresh_interval);

        while (true)
        {
            ApplyNow();
            yield return wait;
        }
    }

    private void ApplyNow()
    {
        if (spawner == null) return;

        RunConfigSO cfg = RunConfigHolder.Current;

        // 런 설정이 없으면 기본 배율
        if (cfg == null)
        {
            spawner.SetSpawnRateMultiplier(1f);
            return;
        }

        // 캐주얼 모드 + 커브가 있을 때만 적용
        bool is_casual = (cfg.ModeId == "casual");
        CasualSpawnCurveConfigSO curve = cfg.CasualSpawnCurve;

        if (!is_casual || curve == null)
        {
            spawner.SetSpawnRateMultiplier(1f);
            return;
        }

        // 스테이지 시작 후 경과 시간 기반
        float elapsed = stage_started ? (Time.time - stage_start_time) : 0f;
        float mul = curve.EvaluateMultiplier(elapsed);

        // 스포너에 배율만 주입(스포너는 고정본 유지)
        spawner.SetSpawnRateMultiplier(mul);

        if (verbose_log)
            Debug.Log($"[CasualSpawnRateScaler] elapsed={elapsed:0.0}s mul={mul:0.###}");
    }
}
