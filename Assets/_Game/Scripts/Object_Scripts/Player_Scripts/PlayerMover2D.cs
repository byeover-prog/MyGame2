// UTF-8
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerMover2D : MonoBehaviour
{
    [Header("입력(새 Input System)")]
    [Tooltip("IA_Player/Player/Move 액션을 넣으세요.")]
    [SerializeField] private InputActionReference moveAction;

    [Tooltip("IA_Player/Player/Dash 액션을 넣으세요.")]
    [SerializeField] private InputActionReference dashAction;

    [Header("이동")]
    [SerializeField] private float moveSpeed = 4.0f;

    [Header("대시")]
    [SerializeField] private float dashDistance = 2.5f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 0.9f;

    [Header("참조")]
    [SerializeField] private Rigidbody2D rb;

    public Vector2 MoveInput { get; private set; }
    public Vector2 FacingDir { get; private set; } = Vector2.right;

    private float _dashEndTime = -999f;
    private float _nextDashReadyTime = 0f;
    private Vector2 _dashVelocity;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
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
        // 1) 이동 입력
        Vector2 input = Vector2.zero;
        if (moveAction != null)
            input = moveAction.action.ReadValue<Vector2>();

        if (input.sqrMagnitude > 1f) input.Normalize();
        MoveInput = input;

        if (MoveInput.sqrMagnitude > 0.0001f)
            FacingDir = MoveInput;

        // 2) 대시 입력
        if (dashAction != null && dashAction.action.WasPressedThisFrame())
            TryDash();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (Time.time < _dashEndTime)
        {
            rb.linearVelocity = _dashVelocity;
            return;
        }

        rb.linearVelocity = MoveInput * moveSpeed;
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