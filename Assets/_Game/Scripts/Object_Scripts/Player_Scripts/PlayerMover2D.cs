using UnityEngine;

public sealed class PlayerMover2D : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 4.0f;

    [Header("대시")]
    [Tooltip("스페이스바로 대시합니다.")]
    [SerializeField] private KeyCode dashKey = KeyCode.Space;

    [Tooltip("대시 거리(대략적으로 이동할 거리)")]
    [SerializeField] private float dashDistance = 2.5f;

    [Tooltip("대시 시간(짧을수록 '툭' 느낌)")]
    [SerializeField] private float dashDuration = 0.12f;

    [Tooltip("대시 쿨타임")]
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

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal"); // A/D
        float y = Input.GetAxisRaw("Vertical");   // W/S

        Vector2 input = new Vector2(x, y);
        if (input.sqrMagnitude > 1f) input.Normalize();
        MoveInput = input;

        // 바라보는 방향 갱신(입력이 있을 때만)
        if (MoveInput.sqrMagnitude > 0.0001f)
            FacingDir = MoveInput;

        if (Input.GetKeyDown(dashKey))
            TryDash();
    }

    private void FixedUpdate()
    {
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
