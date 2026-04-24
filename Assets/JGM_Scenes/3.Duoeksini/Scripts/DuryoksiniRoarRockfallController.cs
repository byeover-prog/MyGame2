// UTF-8
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 구현 원리 요약:
// 두억시니 분노 포효 낙석 패턴 1회 실행만 담당한다.
// 공통 상태 처리인 쿨다운, 추적 on/off, 강제 정지는 베이스에서 관리한다.
// 이 파일은 포효 시작, 낙석 웨이브 생성, 지연 피해 처리, 회복 흐름만 책임진다.

[DisallowMultipleComponent]
public class DuryoksiniRoarRockfallController : DuryoksiniPatternControllerBase
{
    private enum PatternState
    {
        Idle,
        Roar,
        Recover
    }

    [Header("패턴 데이터")]
    [Tooltip("두억시니 패턴 카탈로그 SO")]
    [SerializeField] private DuryoksiniPatternCatalogSO patternCatalog;

    [Header("전용 참조")]
    [Tooltip("두억시니 Animator")]
    [SerializeField] private Animator animator;

    [Header("위치 참조")]
    [Tooltip("포효 이펙트를 생성할 기준 위치")]
    [SerializeField] private Transform vfxRoot;

    [Tooltip("낙석 중심점을 잡을 기준 위치\n비어 있으면 두억시니 본체 위치를 사용한다.")]
    [SerializeField] private Transform rockfallSpawnPoint;

    [Header("포효 방향 처리")]
    [Tooltip("플레이어 방향에 맞춰 포효 이펙트 X 위치를 좌우 반전할지 여부")]
    [SerializeField] private bool mirrorRoarEffectOffsetX = true;

    [Tooltip("플레이어 방향에 맞춰 포효 이펙트를 Y축 회전으로 뒤집을지 여부")]
    [SerializeField] private bool mirrorRoarEffectRotationY = false;

    [Header("이동 설정")]
    [Tooltip("포효 중에도 추적 이동을 유지할지 여부")]
    [SerializeField] private bool allowChaseWhileRoaring = true;

    [Tooltip("포효 시작 순간 속도를 0으로 만들지 여부")]
    [SerializeField] private bool stopVelocityOnPatternStart = false;

    [Header("애니메이터 설정")]
    [Tooltip("분노 포효 시작에 사용할 Animator Trigger 이름")]
    [SerializeField] private string roarTriggerName = "Roar";

    [Header("시작 사용 설정")]
    [Tooltip("패턴 시작 직후 분노 포효를 바로 사용할지 여부")]
    [SerializeField] private bool usePatternOnStart = false;

    [Header("디버그 표시")]
    [Tooltip("씬에서 낙석 기즈모를 표시할지 여부")]
    [SerializeField] private bool drawGizmos = true;

    [Tooltip("선택했을 때만 기즈모를 표시할지 여부")]
    [SerializeField] private bool drawOnlyWhenSelected = true;

    [Tooltip("최근 생성된 낙석 위치를 몇 개까지 기즈모로 유지할지 여부")]
    [SerializeField] private int gizmoPreviewMaxCount = 20;


    private readonly HashSet<Transform> damagedTargetsThisWave = new HashSet<Transform>();
    private readonly List<Vector2> gizmoPreviewPositions = new List<Vector2>();
    private readonly List<Vector2> cachedWaveSpawnPositions = new List<Vector2>();

    private DuryoksiniRoarRockfallConfigSO config;
    private PatternState currentState = PatternState.Idle;

    private Coroutine runningRoutine;
    private float recoverTimer = 0f;


    protected override void Reset()
    {
        base.Reset();
        animator = GetComponent<Animator>();
    }

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        RefreshConfig();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        RefreshConfig();
        ResetRuntimeState();

        float startCooldown = 0f;
        if (config != null)
        {
            startCooldown = usePatternOnStart ? 0f : config.Cooldown;
        }

        InitializePatternBase(startCooldown, true);
    }

    protected override void OnDisable()
    {
        StopRunningRoutine();
        ResetRuntimeState();

        base.OnDisable();
    }

    private void OnValidate()
    {
        if (gizmoPreviewMaxCount < 1)
        {
            gizmoPreviewMaxCount = 1;
        }

        RefreshConfig();
    }

    private void Update()
    {
        if (config == null)
        {
            return;
        }

        UpdatePatternCooldown();

        if (currentState != PatternState.Recover)
        {
            return;
        }

        UpdateRecover();
    }

    public void SetExternalPatternCatalog(DuryoksiniPatternCatalogSO externalCatalog)
    {
        if (externalCatalog == null)
        {
            return;
        }

        patternCatalog = externalCatalog;
        RefreshConfig();
    }

    public bool CanStartPatternByDistance(float distance)
    {
        if (config == null)
        {
            return false;
        }

        return CanStartPatternByDistanceCommon(
            currentState == PatternState.Idle,
            distance,
            config.MinUseDistance,
            config.MaxUseDistance);
    }

    public bool TryStartPattern(Transform target)
    {
        if (config == null || target == null)
        {
            return false;
        }

        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        if (!CanStartPatternByDistance(distanceToTarget))
        {
            return false;
        }

        BeginRoarPattern();
        return true;
    }

    public bool IsRunningPattern()
    {
        return currentState != PatternState.Idle;
    }

    public void ForceStopPattern()
    {
        StopRunningRoutine();
        ResetRuntimeState();
        ForceStopCommon(config != null ? config.Cooldown : 0f, true);
    }

    private void RefreshConfig()
    {
        config = patternCatalog != null ? patternCatalog.RoarRockfallConfig : null;
    }

    private void ResetRuntimeState()
    {
        currentState = PatternState.Idle;
        recoverTimer = 0f;
        damagedTargetsThisWave.Clear();
        cachedWaveSpawnPositions.Clear();
    }

    private void BeginRoarPattern()
    {
        currentState = PatternState.Roar;
        damagedTargetsThisWave.Clear();
        cachedWaveSpawnPositions.Clear();

        BeginPatternCommon(
            allowChaseWhileRoaring,
            stopVelocityOnPatternStart || !allowChaseWhileRoaring);

        PlayRoarAnimation();
        SpawnRoarEffect();
        StartPatternRoutine();

        LogPatternState("분노 포효 시작");
    }

    private void PlayRoarAnimation()
    {
        if (animator == null || string.IsNullOrWhiteSpace(roarTriggerName))
        {
            return;
        }

        animator.ResetTrigger(roarTriggerName);
        animator.SetTrigger(roarTriggerName);
    }

    private void StartPatternRoutine()
    {
        StopRunningRoutine();
        runningRoutine = StartCoroutine(RunPatternRoutine());
    }

    private void StopRunningRoutine()
    {
        if (runningRoutine == null)
        {
            return;
        }

        StopCoroutine(runningRoutine);
        runningRoutine = null;
    }

    private IEnumerator RunPatternRoutine()
    {
        yield return new WaitForSeconds(config.RoarDuration);

        if (config.FirstWaveDelay > 0f)
        {
            yield return new WaitForSeconds(config.FirstWaveDelay);
        }

        for (int waveIndex = 0; waveIndex < config.WaveCount; waveIndex++)
        {
            SpawnRockfallWave();

            if (waveIndex < config.WaveCount - 1 && config.WaveInterval > 0f)
            {
                yield return new WaitForSeconds(config.WaveInterval);
            }
        }

        EnterRecover();
        runningRoutine = null;
    }

    private void UpdateRecover()
    {
        recoverTimer -= Time.deltaTime;

        if (recoverTimer > 0f)
        {
            return;
        }

        EnterIdle();
    }

    private void EnterRecover()
    {
        currentState = PatternState.Recover;
        recoverTimer = config != null ? config.RecoverDuration : 0f;

        if (!allowChaseWhileRoaring)
        {
            StopMovement();
        }

        SetChaseEnabled(true);
        LogPatternState("분노 포효 회복 시작");
    }

    private void EnterIdle()
    {
        currentState = PatternState.Idle;
        recoverTimer = 0f;
        damagedTargetsThisWave.Clear();
        cachedWaveSpawnPositions.Clear();

        EnterIdleCommon(config != null ? config.Cooldown : 0f, !allowChaseWhileRoaring);
        LogPatternState("대기 상태 복귀");
    }

    private void SpawnRoarEffect()
    {
        if (config == null || config.RoarEffectPrefab == null)
        {
            return;
        }

        Transform spawnRoot = vfxRoot != null ? vfxRoot : transform;

        GameObject spawnedEffect = Instantiate(
            config.RoarEffectPrefab,
            spawnRoot.position,
            Quaternion.identity,
            spawnRoot);

        spawnedEffect.transform.localPosition = GetRoarEffectLocalOffset();
        spawnedEffect.transform.localRotation = GetRoarEffectLocalRotation();
        spawnedEffect.transform.localScale = config.RoarEffectLocalScale;

        if (config.RoarEffectLifetime > 0f)
        {
            Destroy(spawnedEffect, config.RoarEffectLifetime);
        }
    }

    private Vector3 GetRoarEffectLocalOffset()
    {
        if (config == null)
        {
            return Vector3.zero;
        }

        Vector3 localOffset = config.RoarEffectOffset;

        if (mirrorRoarEffectOffsetX && !IsTargetOnRightSide())
        {
            localOffset.x *= -1f;
        }

        return localOffset;
    }

    private Quaternion GetRoarEffectLocalRotation()
    {
        if (!mirrorRoarEffectRotationY)
        {
            return Quaternion.identity;
        }

        if (IsTargetOnRightSide())
        {
            return Quaternion.identity;
        }

        return Quaternion.Euler(0f, 180f, 0f);
    }

    private void SpawnRockfallWave()
    {
        if (config == null)
        {
            return;
        }

        damagedTargetsThisWave.Clear();
        cachedWaveSpawnPositions.Clear();

        int totalCount = Mathf.Max(1, config.RocksPerWave);
        for (int i = 0; i < totalCount; i++)
        {
            Vector2 spawnPosition = FindSpreadRockfallPosition();
            ProcessSingleRockfallSpawn(spawnPosition);
        }

        LogPatternState($"낙석 웨이브 생성: {totalCount}개");
    }

    private void ProcessSingleRockfallSpawn(Vector2 spawnPosition)
    {
        cachedWaveSpawnPositions.Add(spawnPosition);
        RegisterGizmoPreviewPosition(spawnPosition);

        SpawnRockfallEffect(spawnPosition);
        StartCoroutine(ApplyRockfallDamageDelayed(spawnPosition));
    }

    private Vector2 FindSpreadRockfallPosition()
    {
        Vector2 center = GetRockfallCenterPosition();
        Vector2 fallbackPosition = center;

        float radius = config != null ? Mathf.Max(0f, config.SpawnRadius) : 0f;
        float minSpacing = config != null ? Mathf.Max(0f, config.MinSpacing) : 0f;

        if (radius <= 0f)
        {
            return center;
        }

        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * radius;
            Vector2 candidate = center + randomOffset;

            if (HasEnoughSpacing(candidate, minSpacing))
            {
                return candidate;
            }

            fallbackPosition = candidate;
        }

        return fallbackPosition;
    }

    private bool HasEnoughSpacing(Vector2 candidate, float minSpacing)
    {
        if (minSpacing <= 0f)
        {
            return true;
        }

        for (int i = 0; i < cachedWaveSpawnPositions.Count; i++)
        {
            if (Vector2.Distance(candidate, cachedWaveSpawnPositions[i]) < minSpacing)
            {
                return false;
            }
        }

        return true;
    }

    private Vector2 GetRockfallCenterPosition()
    {
        Transform spawnRoot = rockfallSpawnPoint != null ? rockfallSpawnPoint : transform;
        return spawnRoot.position;
    }

    private void SpawnRockfallEffect(Vector2 spawnPosition)
    {
        if (config == null || config.RockfallEffectPrefab == null)
        {
            return;
        }

        GameObject spawnedEffect = Instantiate(
            config.RockfallEffectPrefab,
            (Vector3)spawnPosition + config.RockfallEffectOffset,
            Quaternion.identity);

        spawnedEffect.transform.localScale = config.RockfallEffectLocalScale;

        if (config.RockfallEffectLifetime > 0f)
        {
            Destroy(spawnedEffect, config.RockfallEffectLifetime);
        }
    }

    private IEnumerator ApplyRockfallDamageDelayed(Vector2 hitPosition)
    {
        if (config.RockHitDelay > 0f)
        {
            yield return new WaitForSeconds(config.RockHitDelay);
        }

        ApplyRockfallDamage(hitPosition);
    }

    private void ApplyRockfallDamage(Vector2 hitPosition)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            hitPosition,
            config.HitRadius,
            config.TargetLayerMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D targetCollider = hits[i];
            if (targetCollider == null)
            {
                continue;
            }

            Transform targetRoot = GetDamageRoot(targetCollider);
            if (targetRoot == null)
            {
                continue;
            }

            if (damagedTargetsThisWave.Contains(targetRoot))
            {
                continue;
            }

            BossHitResolver.TryApplyDamage(
                targetCollider,
                config.Damage,
                debugLog,
                this);

            damagedTargetsThisWave.Add(targetRoot);
            LogPatternState($"낙석 타격 성공: {targetCollider.name}");
        }
    }

    private void RegisterGizmoPreviewPosition(Vector2 position)
    {
        gizmoPreviewPositions.Add(position);

        while (gizmoPreviewPositions.Count > gizmoPreviewMaxCount)
        {
            gizmoPreviewPositions.RemoveAt(0);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || drawOnlyWhenSelected)
        {
            return;
        }

        DrawRockfallGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        DrawRockfallGizmos();
    }

    private void DrawRockfallGizmos()
    {
        RefreshConfig();

        if (config == null)
        {
            return;
        }

        Vector2 center = GetRockfallCenterPosition();

        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.95f);
        Gizmos.DrawSphere(center, 0.12f);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.85f);
        Gizmos.DrawWireSphere(center, config.SpawnRadius);

        Gizmos.color = new Color(1f, 1f, 0.15f, 0.85f);
        Gizmos.DrawWireSphere(center, config.HitRadius);

        Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.9f);
        for (int i = 0; i < gizmoPreviewPositions.Count; i++)
        {
            Vector2 spawnPosition = gizmoPreviewPositions[i];
            Gizmos.DrawSphere(spawnPosition, 0.1f);
            Gizmos.DrawWireSphere(spawnPosition, config.HitRadius);
        }
    }
}