using System.Collections.Generic;
using UnityEngine;

// 두억시니의 파쇄 돌진 패턴 1회 실행만 담당한다.
// 공통 상태 처리인 쿨다운, 추적 on/off, 강제 정지는 베이스에서 관리한다.
// 실제 돌진 준비, 돌진 이동, 충돌 판정만 이 파일에 남긴다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class DuryoksiniCrushChargeController : DuryoksiniPatternControllerBase
{
    private enum ChargeState
    {
        Idle,
        Prepare,
        Charge,
        Recover
    }

    [Header("패턴 데이터")]
    [Tooltip("두억시니 패턴 카탈로그 SO")]
    [SerializeField] private DuryoksiniPatternCatalogSO patternCatalog;

    [Header("패턴 위치 참조")]
    [Tooltip("돌진 충돌 판정 기준 위치\n비어 있으면 본체 위치를 사용한다")]
    [SerializeField] private Transform chargeHitPoint;

    [Tooltip("돌진 시작 연출을 생성할 기준 위치\n비어 있으면 현재 오브젝트 위치를 사용한다")]
    [SerializeField] private Transform vfxRoot;

    [Header("전용 참조")]
    [Tooltip("두억시니 돌진 넉백 처리기")]
    [SerializeField] private DuryoksiniChargeKnockbackHandler knockbackHandler;

    [Header("상태")]
    [Tooltip("전투 시작 시 첫 돌진을 바로 허용할지 여부")]
    [SerializeField] private bool usePatternOnStart = true;


    private readonly HashSet<Transform> damagedTargetsThisCharge = new HashSet<Transform>();

    private DuryoksiniCrushChargeConfigSO config;
    private ChargeState currentState = ChargeState.Idle;

    private float stateTimer = 0f;
    private Vector2 chargeDirection = Vector2.zero;
    private Vector2 chargeStartPosition = Vector2.zero;


    protected override void Reset()
    {
        base.Reset();
        knockbackHandler = GetComponent<DuryoksiniChargeKnockbackHandler>();
    }

    protected override void Awake()
    {
        base.Awake();

        if (knockbackHandler == null)
        {
            knockbackHandler = GetComponent<DuryoksiniChargeKnockbackHandler>();
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
            startCooldown = usePatternOnStart && config.PlayOnEnable ? 0f : config.Cooldown;
        }

        InitializePatternBase(startCooldown, true);
    }

    protected override void OnDisable()
    {
        ResetRuntimeState();
        base.OnDisable();
    }

    private void Update()
    {
        if (config == null)
        {
            return;
        }

        UpdatePatternCooldown();

        switch (currentState)
        {
            case ChargeState.Prepare:
                UpdatePrepare();
                break;

            case ChargeState.Recover:
                UpdateRecover();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (config == null)
        {
            return;
        }

        if (currentState != ChargeState.Charge)
        {
            return;
        }

        UpdateCharge();
        CheckChargeHit();
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

    public bool CanStartPatternByDistance(float distanceToTarget)
    {
        if (config == null)
        {
            return false;
        }

        return CanStartPatternByDistanceCommon(
            currentState == ChargeState.Idle,
            distanceToTarget,
            config.MinAttackDistance,
            config.MaxAttackDistance);
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

        StartPrepare(target);
        return true;
    }

    public bool IsRunningPattern()
    {
        return currentState != ChargeState.Idle;
    }

    public bool IsIdleState()
    {
        return currentState == ChargeState.Idle;
    }

    public bool IsCooldownReady()
    {
        return IsPatternCooldownReady();
    }

    public void ForceStopPattern()
    {
        ResetRuntimeState();
        ForceStopCommon(config != null ? config.Cooldown : 0f, true);
    }

    public Vector2 GetHitPointPosition()
    {
        if (chargeHitPoint != null)
        {
            return chargeHitPoint.position;
        }

        return transform.position;
    }

    private void RefreshConfig()
    {
        config = patternCatalog != null ? patternCatalog.CrushChargeConfig : null;
    }

    private void ResetRuntimeState()
    {
        currentState = ChargeState.Idle;
        stateTimer = 0f;
        chargeDirection = Vector2.zero;
        chargeStartPosition = rb != null ? rb.position : (Vector2)transform.position;
        damagedTargetsThisCharge.Clear();
    }

    private void UpdatePrepare()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
        {
            return;
        }

        BeginCharge();
    }

    private void UpdateRecover()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
        {
            return;
        }

        EnterIdle();
    }

    private void UpdateCharge()
    {
        if (rb == null)
        {
            EnterRecover();
            return;
        }

        rb.linearVelocity = chargeDirection * config.ChargeSpeed;
        stateTimer -= Time.fixedDeltaTime;

        float movedDistance = Vector2.Distance(chargeStartPosition, rb.position);

        if (movedDistance >= config.MaxChargeDistance)
        {
            EnterRecover();
            return;
        }

        if (stateTimer <= 0f)
        {
            EnterRecover();
        }
    }

    private void StartPrepare(Transform target)
    {
        Vector2 toTarget = target.position - transform.position;
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        currentState = ChargeState.Prepare;
        stateTimer = config.PrepareDuration;
        chargeDirection = toTarget.normalized;
        damagedTargetsThisCharge.Clear();

        BeginPatternCommon(false, true);
        LogPatternState("돌진 준비 시작");
    }

    private void BeginCharge()
    {
        currentState = ChargeState.Charge;
        stateTimer = config.MaxChargeDuration;
        chargeStartPosition = rb != null ? rb.position : (Vector2)transform.position;
        damagedTargetsThisCharge.Clear();

        SpawnChargeStartEffect();
        LogPatternState("돌진 시작");
    }

    private void EnterRecover()
    {
        currentState = ChargeState.Recover;
        stateTimer = config != null ? config.RecoverDuration : 0f;

        StopMovement();
        LogPatternState("돌진 종료");
    }

    private void EnterIdle()
    {
        currentState = ChargeState.Idle;
        stateTimer = 0f;
        chargeDirection = Vector2.zero;
        damagedTargetsThisCharge.Clear();

        EnterIdleCommon(config != null ? config.Cooldown : 0f, true);
        LogPatternState("대기 상태 복귀");
    }

    private void CheckChargeHit()
    {
        Vector2 hitCenter = GetHitPointPosition();
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, config.HitRadius, config.TargetLayerMask);

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

            if (damagedTargetsThisCharge.Contains(targetRoot))
            {
                continue;
            }

            bool hasAppliedDamage = BossHitResolver.TryApplyDamage(
                targetCollider,
                config.Damage,
                debugLog,
                this);

            if (hasAppliedDamage)
            {
                ApplyKnockback(targetCollider);
            }

            damagedTargetsThisCharge.Add(targetRoot);
            LogPatternState($"돌진 타격 성공: {targetCollider.name}");

            EnterRecover();
            return;
        }
    }

    private void ApplyKnockback(Collider2D targetCollider)
    {
        if (targetCollider == null || config == null || knockbackHandler == null)
        {
            return;
        }

        knockbackHandler.ApplyCrushChargeKnockback(
            targetCollider,
            transform.position,
            config.KnockbackDistance,
            config.KnockbackDuration,
            config.KnockbackUpBias);
    }

    private void SpawnChargeStartEffect()
    {
        if (config == null || config.ChargeStartEffectPrefab == null)
        {
            return;
        }

        Transform spawnRoot = vfxRoot != null ? vfxRoot : transform;
        Vector3 spawnPosition = spawnRoot.position + config.ChargeStartEffectOffset;
        Quaternion spawnRotation = GetChargeEffectRotation();

        GameObject spawnedEffect = Instantiate(
            config.ChargeStartEffectPrefab,
            spawnPosition,
            spawnRotation,
            spawnRoot);

        spawnedEffect.transform.localScale = config.ChargeStartEffectLocalScale;

        if (config.ChargeStartEffectLifetime > 0f)
        {
            Destroy(spawnedEffect, config.ChargeStartEffectLifetime);
        }
    }

    private Quaternion GetChargeEffectRotation()
    {
        Vector2 direction = chargeDirection;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, angle);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (patternCatalog == null)
        {
            return;
        }

        DuryoksiniCrushChargeConfigSO drawConfig = patternCatalog.CrushChargeConfig;
        if (drawConfig == null || !drawConfig.DrawGizmos)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetHitPointPosition(), drawConfig.HitRadius);

        Gizmos.color = Color.yellow;
        Vector3 start = Application.isPlaying ? (Vector3)chargeStartPosition : transform.position;
        Vector3 end = start + (Vector3)(chargeDirection.normalized * drawConfig.MaxChargeDistance);
        Gizmos.DrawLine(start, end);
    }
#endif
}