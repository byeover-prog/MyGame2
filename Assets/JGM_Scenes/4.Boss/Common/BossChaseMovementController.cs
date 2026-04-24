// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 모든 보스가 공통으로 사용할 수 있는 추적 이동 컨트롤러다.
// 플레이어 탐색은 직접 하지 않고 BossTargetProvider에서 받아온다.
// 이동만 담당하고, 공격 패턴과 분리되어 있어서 이동하면서 공격하는 구조에 맞다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class BossChaseMovementController : MonoBehaviour
{
    [Header("추적 설정")]

    [Tooltip("보스의 최대 이동 속도입니다.")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("보스가 목표 속도에 가까워질 때 사용하는 가속력입니다.")]
    [Min(0f)]
    [SerializeField] private float acceleration = 18f;

    [Tooltip("방향 전환이나 감속 시 사용하는 보정값입니다.")]
    [Min(0f)]
    [SerializeField] private float deceleration = 22f;


    [Header("참조")]

    [Tooltip("보스의 Rigidbody2D입니다. 비어 있으면 자동으로 가져옵니다.")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("보스의 스프라이트 렌더러입니다. 좌우 반전에 사용합니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("보스 공용 타겟 제공 컴포넌트입니다.")]
    [SerializeField] private BossTargetProvider targetProvider;


    [Header("방향 처리")]

    [Tooltip("이동 방향에 따라 스프라이트를 좌우 반전할지 여부입니다.")]
    [SerializeField] private bool useSpriteFlip = true;

    [Tooltip("오른쪽으로 이동할 때 flipX를 true로 사용할지 여부입니다.")]
    [SerializeField] private bool flipXWhenMoveRight = false;


    [Header("동작 옵션")]

    [Tooltip("이 값을 끄면 추적 이동을 멈춥니다.\n패턴 스크립트에서 필요할 때만 꺼서 사용할 수 있습니다.")]
    [SerializeField] private bool canChase = true;

    [Tooltip("타겟과 완전히 같은 위치가 되었을 때만 부드럽게 멈춥니다.")]
    [Min(0f)]
    [SerializeField] private float zeroDistanceEpsilon = 0.01f;


    [Header("디버그")]

    [Tooltip("디버그 로그를 출력할지 여부입니다.")]
    [SerializeField] private bool debugLog = false;


    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        targetProvider = GetComponent<BossTargetProvider>();
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (targetProvider == null)
        {
            targetProvider = GetComponent<BossTargetProvider>();
        }
    }

    private void OnEnable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        if (!canChase)
        {
            SmoothStop();
            return;
        }

        Transform target = GetCurrentTarget();
        if (target == null)
        {
            SmoothStop();
            return;
        }

        Vector2 currentPosition = rb.position;
        Vector2 targetPosition = target.position;
        Vector2 toTarget = targetPosition - currentPosition;

        // 거의 같은 위치일 때만 미세 떨림 방지용으로 정지
        if (toTarget.sqrMagnitude <= zeroDistanceEpsilon * zeroDistanceEpsilon)
        {
            SmoothStop();
            return;
        }

        Vector2 moveDirection = toTarget.normalized;
        Vector2 desiredVelocity = moveDirection * moveSpeed;

        ApplySmoothedVelocity(desiredVelocity);
        UpdateSpriteFlip(moveDirection);
    }

    private Transform GetCurrentTarget()
    {
        if (targetProvider == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("[BossChaseMovementController] BossTargetProvider가 연결되지 않았습니다.", this);
            }

            return null;
        }

        if (!targetProvider.HasTarget())
        {
            return null;
        }

        return targetProvider.GetTarget();
    }

    private void ApplySmoothedVelocity(Vector2 desiredVelocity)
    {
        Vector2 currentVelocity = rb.linearVelocity;

        float currentMagnitude = currentVelocity.sqrMagnitude;
        float desiredMagnitude = desiredVelocity.sqrMagnitude;

        float blendRate = acceleration;

        // 현재 이동 방향과 목표 이동 방향이 반대면 더 강하게 방향을 꺾는다
        if (currentMagnitude > 0.0001f && desiredMagnitude > 0.0001f)
        {
            float dot = Vector2.Dot(currentVelocity.normalized, desiredVelocity.normalized);

            if (dot < 0f)
            {
                blendRate = deceleration;
            }
        }

        Vector2 nextVelocity = Vector2.MoveTowards(
            currentVelocity,
            desiredVelocity,
            blendRate * Time.fixedDeltaTime);

        rb.linearVelocity = nextVelocity;
    }

    private void SmoothStop()
    {
        Vector2 nextVelocity = Vector2.MoveTowards(
            rb.linearVelocity,
            Vector2.zero,
            deceleration * Time.fixedDeltaTime);

        rb.linearVelocity = nextVelocity;
    }

    private void UpdateSpriteFlip(Vector2 moveDirection)
    {
        if (!useSpriteFlip)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (Mathf.Abs(moveDirection.x) < 0.01f)
        {
            return;
        }

        bool isMovingRight = moveDirection.x > 0f;

        if (isMovingRight)
        {
            spriteRenderer.flipX = flipXWhenMoveRight;
        }
        else
        {
            spriteRenderer.flipX = !flipXWhenMoveRight;
        }
    }

    public void SetCanChase(bool value)
    {
        canChase = value;
    }

    public void SetMoveSpeed(float value)
    {
        moveSpeed = Mathf.Max(0f, value);
    }

    public bool CanChase()
    {
        return canChase;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, zeroDistanceEpsilon);
    }
#endif
}