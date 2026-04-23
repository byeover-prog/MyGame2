using UnityEngine;

// 두억시니 특수 패턴들이 공통으로 사용하는 상태 처리 베이스다.
// 쿨다운, 추적 on/off, 이동 정지, 공통 참조, 강제 정지 복구를 여기서 처리한다.
// 실제 패턴 차이점은 각 컨트롤러가 가진다.

public abstract class DuryoksiniPatternControllerBase : MonoBehaviour
{
    [Header("공통 참조")]

    [Tooltip("보스 공용 타겟 제공 컴포넌트")]
    [SerializeField] protected BossTargetProvider targetProvider;

    [Tooltip("보스 공용 추적 이동 컴포넌트")]
    [SerializeField] protected BossChaseMovementController chaseMovementController;

    [Tooltip("두억시니 Rigidbody2D")]
    [SerializeField] protected Rigidbody2D rb;

    [Header("공통 상태")]

    [Tooltip("디버그 로그 출력 여부")]
    [SerializeField] protected bool debugLog = false;


    protected float cooldownTimer = 0f;

    protected virtual void Reset()
    {
        targetProvider = GetComponent<BossTargetProvider>();
        chaseMovementController = GetComponent<BossChaseMovementController>();
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Awake()
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
    }

    protected virtual void OnEnable()
    {
    }

    protected virtual void OnDisable()
    {
        RestoreDefaultMovementState();
    }

    protected void InitializePatternBase(float startCooldown, bool stopVelocityOnEnable)
    {
        cooldownTimer = Mathf.Max(0f, startCooldown);

        if (stopVelocityOnEnable)
        {
            StopMovement();
        }

        SetChaseEnabled(true);
    }

    protected void UpdatePatternCooldown()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    protected bool CanStartPatternByDistanceCommon(bool isIdleState, float distanceToTarget, float minDistance, float maxDistance)
    {
        if (!isIdleState)
        {
            return false;
        }

        if (!IsPatternCooldownReady())
        {
            return false;
        }

        if (distanceToTarget < minDistance)
        {
            return false;
        }

        if (distanceToTarget > maxDistance)
        {
            return false;
        }

        return true;
    }

    protected void BeginPatternCommon(bool keepChasingDuringPattern, bool stopVelocityOnStart)
    {
        if (stopVelocityOnStart)
        {
            StopMovement();
        }

        SetChaseEnabled(keepChasingDuringPattern);
    }

    protected void EnterIdleCommon(float nextCooldown, bool stopVelocityOnIdle)
    {
        cooldownTimer = Mathf.Max(0f, nextCooldown);

        if (stopVelocityOnIdle)
        {
            StopMovement();
        }

        SetChaseEnabled(true);
    }

    protected void ForceStopCommon(float nextCooldown, bool stopVelocityOnStop)
    {
        cooldownTimer = Mathf.Max(0f, nextCooldown);

        if (stopVelocityOnStop)
        {
            StopMovement();
        }

        SetChaseEnabled(true);
    }

    protected void RestoreDefaultMovementState()
    {
        StopMovement();
        SetChaseEnabled(true);
    }

    protected bool IsPatternCooldownReady()
    {
        return cooldownTimer <= 0f;
    }

    protected void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    protected void SetChaseEnabled(bool value)
    {
        if (chaseMovementController != null)
        {
            chaseMovementController.SetCanChase(value);
        }
    }

    protected Transform GetCurrentTarget()
    {
        if (targetProvider != null && targetProvider.HasTarget())
        {
            return targetProvider.GetTarget();
        }

        return null;
    }

    protected bool IsTargetOnRightSide()
    {
        Transform target = GetCurrentTarget();
        if (target == null)
        {
            return true;
        }

        return target.position.x >= transform.position.x;
    }

    protected Transform GetDamageRoot(Collider2D targetCollider)
    {
        if (targetCollider == null)
        {
            return null;
        }

        return BossHitResolver.GetDamageRoot(targetCollider);
    }

    protected void LogPatternState(string message)
    {
        if (!debugLog)
        {
            return;
        }

        Debug.Log($"[{GetType().Name}] {message}", this);
    }
}