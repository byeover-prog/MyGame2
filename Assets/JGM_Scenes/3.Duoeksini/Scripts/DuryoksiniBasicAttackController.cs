// UTF-8
using System.Collections.Generic;
using UnityEngine;

// 구현 원리 요약:
// 두억시니 기본 공격 1회를 담당한다.
// 전투 컨트롤러는 시작 시도만 하고,
// 실제 공격 가능 여부, 공격 진행, 타격, 종료는 이 실행기가 책임진다.
// 피해는 BossHitResolver로 통일하고, 넉백은 DuryoksiniChargeKnockbackHandler로 통일한다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class DuryoksiniBasicAttackController : MonoBehaviour
{
    [Header("기본 공격 데이터")]

    [Tooltip("두억시니 기본 공격 설정 SO")]
    [SerializeField] private DuryoksiniBasicAttackConfigSO basicAttackConfig;

    [Header("공통 참조")]

    [Tooltip("보스 공용 타겟 제공 컴포넌트")]
    [SerializeField] private BossTargetProvider targetProvider;

    [Tooltip("보스 공용 추적 이동 컴포넌트")]
    [SerializeField] private BossChaseMovementController chaseMovementController;

    [Tooltip("두억시니 넉백 처리기")]
    [SerializeField] private DuryoksiniChargeKnockbackHandler knockbackHandler;

    [Tooltip("두억시니 Rigidbody2D")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("두억시니 Animator")]
    [SerializeField] private Animator animator;

    [Header("공격 위치 참조")]

    [Tooltip("근접 공격 판정 기준 위치\n오른쪽 기준 오프셋을 잡는 용도로 사용한다.")]
    [SerializeField] private Transform attackPoint;

    [Header("애니메이터 설정")]

    [Tooltip("기본 공격 시작에 사용할 Animator Trigger 이름")]
    [SerializeField] private string basicAttackTriggerName = "BasicAttack";

    [Header("상태")]

    [Tooltip("디버그 로그 출력 여부")]
    [SerializeField] private bool debugLog = false;


    private readonly HashSet<Transform> hitTargetsThisAttack = new HashSet<Transform>();

    private float cooldownTimer = 0f;
    private float attackHitFallbackTimer = 0f;
    private float attackFinishFallbackTimer = 0f;

    private bool isAttackRunning = false;
    private bool hasAppliedHitThisAttack = false;


    private void Reset()
    {
        targetProvider = GetComponent<BossTargetProvider>();
        chaseMovementController = GetComponent<BossChaseMovementController>();
        knockbackHandler = GetComponent<DuryoksiniChargeKnockbackHandler>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        CacheLocalReferences();
    }

    private void OnEnable()
    {
        ResetAttackState(false);
        cooldownTimer = 0f;
        SetChaseEnabled(true);
    }

    private void OnDisable()
    {
        ForceStopAttack();
    }

    private void Update()
    {
        if (basicAttackConfig == null)
        {
            return;
        }

        UpdateCooldown();

        if (!isAttackRunning)
        {
            return;
        }

        UpdateAttackRuntime();
    }

    private void CacheLocalReferences()
    {
        if (targetProvider == null)
        {
            targetProvider = GetComponent<BossTargetProvider>();
        }

        if (chaseMovementController == null)
        {
            chaseMovementController = GetComponent<BossChaseMovementController>();
        }

        if (knockbackHandler == null)
        {
            knockbackHandler = GetComponent<DuryoksiniChargeKnockbackHandler>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void SetExternalConfig(DuryoksiniBasicAttackConfigSO externalConfig)
    {
        if (externalConfig == null)
        {
            return;
        }

        basicAttackConfig = externalConfig;
    }

    public bool CanStartAttack()
    {
        if (basicAttackConfig == null)
        {
            return false;
        }

        if (isAttackRunning)
        {
            return false;
        }

        if (!IsCooldownReady())
        {
            return false;
        }

        return true;
    }

    public bool CanStartAttackByDistance(float distanceToTarget)
    {
        if (!CanStartAttack())
        {
            return false;
        }

        if (distanceToTarget > basicAttackConfig.AttackDistance)
        {
            return false;
        }

        return true;
    }

    public bool TryStartAttack(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        if (!CanStartAttackByDistance(distanceToTarget))
        {
            return false;
        }

        StartAttackInternal();
        return true;
    }

    public bool TryStartAttack()
    {
        if (!CanStartAttack())
        {
            return false;
        }

        StartAttackInternal();
        return true;
    }

    public bool IsRunningAttack()
    {
        return isAttackRunning;
    }

    public bool IsCooldownReady()
    {
        return cooldownTimer <= 0f;
    }

    public void ForceStopAttack()
    {
        ResetAttackState(true);
        StopMovement();
        SetChaseEnabled(true);
    }

    public Vector2 GetAttackPointPosition()
    {
        Vector2 baseOffset = GetBaseAttackOffset();

        if (!IsTargetOnRightSide())
        {
            baseOffset.x *= -1f;
        }

        return (Vector2)transform.position + baseOffset;
    }

    // 구현 원리 요약:
    // 애니메이션 실제 타격 프레임에서 호출한다.
    public void ExecuteAttackHitEvent()
    {
        if (!isAttackRunning)
        {
            return;
        }

        if (hasAppliedHitThisAttack)
        {
            return;
        }

        hasAppliedHitThisAttack = true;
        ExecuteAttackHit();
    }

    // 구현 원리 요약:
    // 애니메이션 종료 프레임에서 호출한다.
    public void FinishAttackAnimationEvent()
    {
        if (!isAttackRunning)
        {
            return;
        }

        FinishAttack();
    }

    private void UpdateCooldown()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    private void UpdateAttackRuntime()
    {
        attackHitFallbackTimer -= Time.deltaTime;
        attackFinishFallbackTimer -= Time.deltaTime;

        TryExecuteFallbackHit();
        TryExecuteFallbackFinish();
    }

    private void TryExecuteFallbackHit()
    {
        if (hasAppliedHitThisAttack)
        {
            return;
        }

        if (attackHitFallbackTimer > 0f)
        {
            return;
        }

        hasAppliedHitThisAttack = true;
        ExecuteAttackHit();

        if (debugLog)
        {
            Debug.LogWarning("[DuryoksiniBasicAttackController] 타격 이벤트가 없어 fallback 타격을 적용했습니다.", this);
        }
    }

    private void TryExecuteFallbackFinish()
    {
        if (attackFinishFallbackTimer > 0f)
        {
            return;
        }

        FinishAttack();

        if (debugLog)
        {
            Debug.LogWarning("[DuryoksiniBasicAttackController] 종료 이벤트가 없어 fallback 종료를 적용했습니다.", this);
        }
    }

    private void StartAttackInternal()
    {
        isAttackRunning = true;
        hasAppliedHitThisAttack = false;
        hitTargetsThisAttack.Clear();

        InitializeFallbackTimers();

        StopMovement();
        SetChaseEnabled(false);
        PlayAttackAnimation();

        if (debugLog)
        {
            Debug.Log("[DuryoksiniBasicAttackController] 기본 공격 시작", this);
        }
    }

    private void InitializeFallbackTimers()
    {
        float fallbackTime = Mathf.Max(0.05f, basicAttackConfig.AttackFinishFallbackTime);

        attackHitFallbackTimer = fallbackTime;
        attackFinishFallbackTimer = fallbackTime;
    }

    private void PlayAttackAnimation()
    {
        if (animator == null || string.IsNullOrWhiteSpace(basicAttackTriggerName))
        {
            return;
        }

        animator.ResetTrigger(basicAttackTriggerName);
        animator.SetTrigger(basicAttackTriggerName);
    }

    private void ExecuteAttackHit()
    {
        Vector2 hitCenter = GetAttackPointPosition();

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            hitCenter,
            basicAttackConfig.HitRadius,
            basicAttackConfig.TargetLayerMask);

        for (int i = 0; i < hits.Length; i++)
        {
            TryApplyHitToTarget(hits[i]);
        }
    }

    private void TryApplyHitToTarget(Collider2D targetCollider)
    {
        if (targetCollider == null)
        {
            return;
        }

        Transform targetRoot = BossHitResolver.GetDamageRoot(targetCollider);
        if (targetRoot == null)
        {
            return;
        }

        if (hitTargetsThisAttack.Contains(targetRoot))
        {
            return;
        }

        bool hasAppliedDamage = BossHitResolver.TryApplyDamage(
            targetCollider,
            basicAttackConfig.Damage,
            debugLog,
            this);

        if (hasAppliedDamage)
        {
            ApplyKnockback(targetCollider);
        }

        hitTargetsThisAttack.Add(targetRoot);

        if (debugLog)
        {
            Debug.Log($"[DuryoksiniBasicAttackController] 기본 공격 타격 성공: {targetCollider.name}", this);
        }
    }

    private void FinishAttack()
    {
        ResetAttackState(true);
        StopMovement();
        SetChaseEnabled(true);

        if (debugLog)
        {
            Debug.Log("[DuryoksiniBasicAttackController] 기본 공격 종료", this);
        }
    }

    private void ResetAttackState(bool applyCooldown)
    {
        isAttackRunning = false;
        hasAppliedHitThisAttack = false;
        attackHitFallbackTimer = 0f;
        attackFinishFallbackTimer = 0f;
        hitTargetsThisAttack.Clear();

        if (applyCooldown)
        {
            cooldownTimer = basicAttackConfig != null ? basicAttackConfig.Cooldown : 0f;
        }
    }

    private void ApplyKnockback(Collider2D targetCollider)
    {
        if (targetCollider == null || basicAttackConfig == null)
        {
            return;
        }

        if (knockbackHandler == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("[DuryoksiniBasicAttackController] DuryoksiniChargeKnockbackHandler가 연결되지 않았습니다.", this);
            }

            return;
        }

        knockbackHandler.ApplyBasicAttackKnockback(
            targetCollider,
            transform.position,
            basicAttackConfig.KnockbackDistance,
            basicAttackConfig.KnockbackDuration,
            basicAttackConfig.KnockbackUpBias);
    }

    private void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void SetChaseEnabled(bool value)
    {
        if (chaseMovementController != null)
        {
            chaseMovementController.SetCanChase(value);
        }
    }

    private Transform GetCurrentTarget()
    {
        if (targetProvider != null && targetProvider.HasTarget())
        {
            return targetProvider.GetTarget();
        }

        return null;
    }

    private bool IsTargetOnRightSide()
    {
        Transform target = GetCurrentTarget();
        if (target == null)
        {
            return true;
        }

        return target.position.x >= transform.position.x;
    }

    private Vector2 GetBaseAttackOffset()
    {
        if (attackPoint == null)
        {
            return Vector2.zero;
        }

        return attackPoint.localPosition;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (basicAttackConfig == null || !basicAttackConfig.DrawGizmos)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetAttackPointPosition(), basicAttackConfig.HitRadius);
    }
#endif
}