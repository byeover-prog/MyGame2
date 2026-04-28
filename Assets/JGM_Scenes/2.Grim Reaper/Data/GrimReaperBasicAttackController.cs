// UTF-8
using System.Collections.Generic;
using UnityEngine;

// 구현 원리 요약:
// 저승사자의 기본 낫 공격 실행기다.
// 이 공격은 특수 패턴이 아니며, 전투 컨트롤러가 사거리 안에서 호출할 일반 공격이다.
// 공격 중에는 이동을 멈추고, 타격 프레임에서 박스 판정으로 플레이어에게 피해를 준다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class GrimReaperBasicAttackController : MonoBehaviour
{
    [Header("기본 공격 데이터")]

    [Tooltip("저승사자 기본 공격 설정 SO")]
    [SerializeField] private GrimReaperBasicAttackConfigSO basicAttackConfig;


    [Header("공통 참조")]

    [Tooltip("보스 공용 타겟 제공 컴포넌트")]
    [SerializeField] private BossTargetProvider targetProvider;

    [Tooltip("보스 공용 추적 이동 컴포넌트")]
    [SerializeField] private BossChaseMovementController chaseMovementController;

    [Tooltip("저승사자 Rigidbody2D")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("저승사자 Animator")]
    [SerializeField] private Animator animator;


    [Header("공격 위치")]

    [Tooltip("기본 낫 공격 판정 중심 위치\n오른쪽을 바라보는 기준으로 낫 앞쪽에 배치한다.")]
    [SerializeField] private Transform attackPoint;


    [Header("애니메이터 설정")]

    [Tooltip("기본 공격 시작에 사용할 Animator Trigger 이름")]
    [SerializeField] private string basicAttackTriggerName = "BasicAttack";


    [Header("임시 테스트")]

    [Tooltip("전투 컨트롤러 없이 단독으로 기본 공격을 테스트할지 여부\n최종 구조에서는 false로 둔다.")]
    [SerializeField] private bool useSelfTestAttack = false;


    [Header("디버그")]

    [Tooltip("디버그 로그 출력 여부")]
    [SerializeField] private bool debugLog = false;


    private readonly HashSet<Transform> hitTargetsThisAttack = new HashSet<Transform>();

    private float cooldownTimer = 0f;
    private float hitFallbackTimer = 0f;
    private float finishFallbackTimer = 0f;

    private bool isAttackRunning = false;
    private bool hasAppliedHitThisAttack = false;


    private void Reset()
    {
        targetProvider = GetComponent<BossTargetProvider>();
        chaseMovementController = GetComponent<BossChaseMovementController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        CacheReferences();
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

        if (isAttackRunning)
        {
            UpdateAttackRuntime();
            return;
        }

        if (useSelfTestAttack)
        {
            TryStartAttack();
        }
    }

    private void CacheReferences()
    {
        if (targetProvider == null)
        {
            targetProvider = GetComponent<BossTargetProvider>();
        }

        if (chaseMovementController == null)
        {
            chaseMovementController = GetComponent<BossChaseMovementController>();
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

    public void SetExternalConfig(GrimReaperBasicAttackConfigSO externalConfig)
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

        return distanceToTarget <= basicAttackConfig.AttackDistance;
    }

    public bool TryStartAttack()
    {
        Transform target = GetCurrentTarget();
        if (target == null)
        {
            return false;
        }

        return TryStartAttack(target);
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
        ResetAttackState(false);
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

    // 애니메이션 실제 타격 프레임에서 호출한다.
    public void OnAnimationEvent_GrimReaperBasicAttackHit()
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

    // 기존 애니메이션 이벤트 이름을 잘못 넣었을 때도 동작하도록 남긴다.
    public void ExecuteAttackHitEvent()
    {
        OnAnimationEvent_GrimReaperBasicAttackHit();
    }

    // 애니메이션 종료 프레임에서 호출한다.
    public void OnAnimationEvent_GrimReaperBasicAttackEnd()
    {
        if (!isAttackRunning)
        {
            return;
        }

        FinishAttack();
    }

    // 기존 애니메이션 이벤트 이름을 잘못 넣었을 때도 동작하도록 남긴다.
    public void FinishAttackAnimationEvent()
    {
        OnAnimationEvent_GrimReaperBasicAttackEnd();
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
        hitFallbackTimer -= Time.deltaTime;
        finishFallbackTimer -= Time.deltaTime;

        TryExecuteFallbackHit();
        TryExecuteFallbackFinish();
    }

    private void TryExecuteFallbackHit()
    {
        if (hasAppliedHitThisAttack)
        {
            return;
        }

        if (hitFallbackTimer > 0f)
        {
            return;
        }

        hasAppliedHitThisAttack = true;
        ExecuteAttackHit();

        if (debugLog)
        {
            Debug.LogWarning("[GrimReaperBasicAttackController] 타격 이벤트가 없어 fallback 타격을 적용했습니다.", this);
        }
    }

    private void TryExecuteFallbackFinish()
    {
        if (finishFallbackTimer > 0f)
        {
            return;
        }

        FinishAttack();

        if (debugLog)
        {
            Debug.LogWarning("[GrimReaperBasicAttackController] 종료 이벤트가 없어 fallback 종료를 적용했습니다.", this);
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
            Debug.Log("[GrimReaperBasicAttackController] 기본 공격 시작", this);
        }
    }

    private void InitializeFallbackTimers()
    {
        float hitTime = Mathf.Max(0.01f, basicAttackConfig.HitFallbackTime);
        float finishTime = Mathf.Max(hitTime + 0.01f, basicAttackConfig.FinishFallbackTime);

        hitFallbackTimer = hitTime;
        finishFallbackTimer = finishTime;
    }

    private void PlayAttackAnimation()
    {
        if (animator == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(basicAttackTriggerName))
        {
            return;
        }

        animator.ResetTrigger(basicAttackTriggerName);
        animator.SetTrigger(basicAttackTriggerName);
    }

    private void ExecuteAttackHit()
    {
        if (basicAttackConfig == null)
        {
            return;
        }

        Vector2 hitCenter = GetAttackPointPosition();
        Vector2 hitSize = basicAttackConfig.HitBoxSize;

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            hitCenter,
            hitSize,
            0f,
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
            hitTargetsThisAttack.Add(targetRoot);
        }

        if (debugLog && hasAppliedDamage)
        {
            Debug.Log($"[GrimReaperBasicAttackController] 기본 낫 공격 타격 성공: {targetCollider.name}", this);
        }
    }

    private void FinishAttack()
    {
        ResetAttackState(true);
        StopMovement();
        SetChaseEnabled(true);

        if (debugLog)
        {
            Debug.Log("[GrimReaperBasicAttackController] 기본 공격 종료", this);
        }
    }

    private void ResetAttackState(bool applyCooldown)
    {
        isAttackRunning = false;
        hasAppliedHitThisAttack = false;
        hitFallbackTimer = 0f;
        finishFallbackTimer = 0f;
        hitTargetsThisAttack.Clear();

        if (applyCooldown)
        {
            cooldownTimer = basicAttackConfig != null ? basicAttackConfig.Cooldown : 0f;
        }
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
        Gizmos.DrawWireCube(GetAttackPointPosition(), basicAttackConfig.HitBoxSize);
    }
#endif
}