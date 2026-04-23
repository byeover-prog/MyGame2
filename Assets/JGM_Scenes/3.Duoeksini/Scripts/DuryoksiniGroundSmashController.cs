using System.Collections.Generic;
using UnityEngine;

// 두억시니 대지 분쇄 패턴 1회 실행만 담당한다.
// 공통 상태 처리인 쿨다운, 추적 on/off, 강제 정지는 베이스에서 관리한다.
// 실제 준비 연출, 타격 시점, 내려찍기 피해 처리만 이 파일에 남긴다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class DuryoksiniGroundSmashController : DuryoksiniPatternControllerBase
{
    private enum SmashState
    {
        Idle,
        Prepare,
        Recover
    }

    [Header("패턴 데이터")]
    [Tooltip("두억시니 패턴 카탈로그 SO")]
    [SerializeField] private DuryoksiniPatternCatalogSO patternCatalog;

    [Header("전용 참조")]
    [Tooltip("두억시니 Animator")]
    [SerializeField] private Animator animator;

    [Tooltip("두억시니 넉백 처리기")]
    [SerializeField] private DuryoksiniChargeKnockbackHandler knockbackHandler;

    [Header("위치 참조")]
    [Tooltip("대지 분쇄 타격 기준 위치\n오른쪽 기준 오프셋으로 사용한다.")]
    [SerializeField] private Transform smashPoint;

    [Tooltip("준비 동작 연출을 생성할 기준 위치이다.")]
    [SerializeField] private Transform vfxRoot;

    [Header("애니메이터 설정")]
    [Tooltip("대지 분쇄 시작에 사용할 Animator Trigger 이름")]
    [SerializeField] private string groundSmashTriggerName = "GroundSmash";


    private readonly HashSet<Transform> hitTargetsThisSmash = new HashSet<Transform>();

    private DuryoksiniGroundSmashConfigSO config;
    private SmashState currentState = SmashState.Idle;

    private float hitFallbackTimer = 0f;
    private float finishFallbackTimer = 0f;
    private float recoverTimer = 0f;

    private bool hasAppliedHitThisPattern = false;


    protected override void Reset()
    {
        base.Reset();
        animator = GetComponent<Animator>();
        knockbackHandler = GetComponent<DuryoksiniChargeKnockbackHandler>();
    }

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

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

        InitializePatternBase(0f, true);
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
            case SmashState.Prepare:
                UpdatePrepare();
                break;

            case SmashState.Recover:
                UpdateRecover();
                break;
        }
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
            currentState == SmashState.Idle,
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

        StartPrepare();
        return true;
    }

    public bool IsRunningPattern()
    {
        return currentState != SmashState.Idle;
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

    public void ExecuteGroundSmashHitEvent()
    {
        if (currentState != SmashState.Prepare)
        {
            return;
        }

        if (hasAppliedHitThisPattern)
        {
            return;
        }

        hasAppliedHitThisPattern = true;
        ExecuteGroundSmashHit();
    }

    public void FinishGroundSmashAnimationEvent()
    {
        if (currentState != SmashState.Prepare)
        {
            return;
        }

        EnterRecover();
    }

    public Vector2 GetSmashPointPosition()
    {
        Vector2 baseOffset = GetBaseSmashOffset();

        if (!IsTargetOnRightSide())
        {
            baseOffset.x *= -1f;
        }

        return (Vector2)transform.position + baseOffset;
    }

    private void RefreshConfig()
    {
        config = patternCatalog != null ? patternCatalog.GroundSmashConfig : null;
    }

    private void ResetRuntimeState()
    {
        currentState = SmashState.Idle;
        hitFallbackTimer = 0f;
        finishFallbackTimer = 0f;
        recoverTimer = 0f;
        hasAppliedHitThisPattern = false;
        hitTargetsThisSmash.Clear();
    }

    private void UpdatePrepare()
    {
        hitFallbackTimer -= Time.deltaTime;
        finishFallbackTimer -= Time.deltaTime;

        if (!hasAppliedHitThisPattern && hitFallbackTimer <= 0f)
        {
            hasAppliedHitThisPattern = true;
            ExecuteGroundSmashHit();
            LogPatternState("타격 이벤트가 없어 fallback 타격 적용");
        }

        if (finishFallbackTimer > 0f)
        {
            return;
        }

        EnterRecover();
        LogPatternState("종료 이벤트가 없어 fallback 종료 적용");
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

    private void StartPrepare()
    {
        currentState = SmashState.Prepare;
        hitFallbackTimer = Mathf.Max(0.05f, config.HitFallbackTime);
        finishFallbackTimer = Mathf.Max(hitFallbackTimer, config.FinishFallbackTime);
        hasAppliedHitThisPattern = false;
        hitTargetsThisSmash.Clear();

        BeginPatternCommon(false, true);
        SpawnPrepareEffect();

        if (animator != null && !string.IsNullOrWhiteSpace(groundSmashTriggerName))
        {
            animator.ResetTrigger(groundSmashTriggerName);
            animator.SetTrigger(groundSmashTriggerName);
        }

        LogPatternState("대지 분쇄 준비 시작");
    }

    private void EnterRecover()
    {
        currentState = SmashState.Recover;
        recoverTimer = config != null ? config.RecoverDuration : 0f;

        StopMovement();
        LogPatternState("대지 분쇄 회복 시작");
    }

    private void EnterIdle()
    {
        currentState = SmashState.Idle;
        hitFallbackTimer = 0f;
        finishFallbackTimer = 0f;
        recoverTimer = 0f;
        hasAppliedHitThisPattern = false;
        hitTargetsThisSmash.Clear();

        EnterIdleCommon(config != null ? config.Cooldown : 0f, true);
        LogPatternState("대기 상태 복귀");
    }

    private void ExecuteGroundSmashHit()
    {
        if (config == null)
        {
            return;
        }

        SpawnSmashImpactEffect();

        Vector2 hitCenter = GetSmashPointPosition();
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

            if (hitTargetsThisSmash.Contains(targetRoot))
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

            hitTargetsThisSmash.Add(targetRoot);
            LogPatternState($"대지 분쇄 타격 성공: {targetCollider.name}");
        }
    }

    private void ApplyKnockback(Collider2D targetCollider)
    {
        if (targetCollider == null || config == null || knockbackHandler == null)
        {
            return;
        }

        knockbackHandler.ApplyGroundSmashKnockback(
            targetCollider,
            GetSmashPointPosition(),
            config.KnockbackDistance,
            config.KnockbackDuration,
            config.KnockbackUpBias);
    }

    private void SpawnPrepareEffect()
    {
        if (config == null || config.PrepareEffectPrefab == null)
        {
            return;
        }

        Transform spawnRoot = vfxRoot != null ? vfxRoot : transform;
        Vector3 spawnPosition = spawnRoot.position + config.PrepareEffectOffset;

        GameObject spawnedEffect = Instantiate(
            config.PrepareEffectPrefab,
            spawnPosition,
            Quaternion.identity,
            spawnRoot);

        spawnedEffect.transform.localScale = config.PrepareEffectLocalScale;

        if (config.PrepareEffectLifetime > 0f)
        {
            Destroy(spawnedEffect, config.PrepareEffectLifetime);
        }
    }

    private void SpawnSmashImpactEffect()
    {
        if (config == null || config.SmashImpactEffectPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = (Vector3)GetSmashPointPosition() + config.SmashImpactEffectOffset;

        GameObject spawnedEffect = Instantiate(
            config.SmashImpactEffectPrefab,
            spawnPosition,
            Quaternion.identity);

        spawnedEffect.transform.localScale = config.SmashImpactEffectLocalScale;

        if (config.SmashImpactEffectLifetime > 0f)
        {
            Destroy(spawnedEffect, config.SmashImpactEffectLifetime);
        }
    }

    private Vector2 GetBaseSmashOffset()
    {
        if (smashPoint == null)
        {
            return Vector2.zero;
        }

        return smashPoint.localPosition;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        RefreshConfig();

        if (config == null || !config.DrawGizmos)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetSmashPointPosition(), config.HitRadius);
    }
#endif
}