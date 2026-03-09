using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerMover2D : MonoBehaviour
{
    [Header("입력(새 Input System)")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference dashAction;

    [Header("이동")]
    [SerializeField] private float moveSpeed = 4.0f;

    [Header("대시")]
    [SerializeField] private float dashDistance = 2.5f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 0.9f;

    [Header("맵 경계 제한")]
    [Tooltip("플레이어 이동을 제한할 Collider2D입니다. (BoxCollider2D/PolygonCollider2D)\n비우면 경계 제한 없음.")]
    [SerializeField] private Collider2D boundaryCollider;

    [Tooltip("경계 안쪽 여유 거리(월드 유닛). 캐릭터가 경계에 딱 붙지 않게 합니다.")]
    [Min(0f)]
    [SerializeField] private float boundaryMargin = 0.3f;

    [Header("참조")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    [Tooltip("플레이어 스프라이트(없으면 자식에서 자동 탐색). 좌/우 반전은 flipX로 처리합니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("애니 판정(떨림 방지)")]
    [Tooltip("이 속도 이상이면 걷기(true)로 전환")]
    [Min(0f)]
    [SerializeField] private float walkOnSpeed = 0.05f;

    [Tooltip("이 속도 이하이면 정지(false)로 전환")]
    [Min(0f)]
    [SerializeField] private float walkOffSpeed = 0.02f;

    public Vector2 MoveInput { get; private set; }
    public Vector2 FacingDir { get; private set; } = Vector2.right;

    private float _dashEndTime = -999f;
    private float _nextDashReadyTime = 0f;
    private Vector2 _dashVelocity;

    private bool _isWalkingCached;

    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (dashAction != null) dashAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (dashAction != null) dashAction.action.Disable();
    }

    private void Update()
    {
        // 입력
        Vector2 input = Vector2.zero;
        if (moveAction != null)
            input = moveAction.action.ReadValue<Vector2>();

        if (input.sqrMagnitude > 1f) input.Normalize();
        MoveInput = input;

        // 방향 규칙(요청사항 그대로)
        // - 왼쪽 입력(x<0)일 때만 왼쪽
        // - 위/아래만(x==0) 또는 오른쪽 포함(x>=0)은 오른쪽 유지
        if (MoveInput.x < -0.0001f)
            FacingDir = Vector2.left;
        else if (MoveInput.x > 0.0001f)
            FacingDir = Vector2.right;
        // 대시
        if (dashAction != null && dashAction.action.WasPressedThisFrame())
            TryDash();

        // isWalking 판정(실제 속도 기반 + 히스테리시스)
        if (animator != null && rb != null)
        {
            float speed = rb.linearVelocity.magnitude;

            bool next;
            if (_isWalkingCached)
                next = speed > walkOffSpeed;   // 걷는 중엔 더 낮아져야 꺼짐
            else
                next = speed >= walkOnSpeed;   // 서있을 땐 이 이상이면 켜짐

            if (next != _isWalkingCached)
            {
                _isWalkingCached = next;
                animator.SetBool(IsWalkingHash, _isWalkingCached);
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (Time.time < _dashEndTime)
        {
            rb.linearVelocity = _dashVelocity;
        }
        else
        {
            rb.linearVelocity = MoveInput * moveSpeed;
        }

        // 맵 경계 제한 (Kinematic+Trigger라 물리 충돌이 없으므로 수동 클램핑)
        ClampToBoundary();
    }

    /// <summary>
    /// boundaryCollider의 bounds 안으로 위치를 제한합니다.
    /// </summary>
    private void ClampToBoundary()
    {
        if (boundaryCollider == null) return;

        Vector3 pos = rb.position;
        Bounds b = boundaryCollider.bounds;

        float m = boundaryMargin;
        pos.x = Mathf.Clamp(pos.x, b.min.x + m, b.max.x - m);
        pos.y = Mathf.Clamp(pos.y, b.min.y + m, b.max.y - m);

        rb.position = pos;
    }

    private void TryDash()
    {
        if (Time.time < _nextDashReadyTime)
            return;

        Vector2 dir = MoveInput.sqrMagnitude > 0.0001f ? MoveInput : FacingDir;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        float speed = dashDistance / Mathf.Max(0.01f, dashDuration);
        _dashVelocity = dir.normalized * speed;

        _dashEndTime = Time.time + dashDuration;
        _nextDashReadyTime = Time.time + dashCooldown;
    }
}