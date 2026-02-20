using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerMover2D : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 4.0f;

    [Header("대시")]
    [Tooltip("스페이스바로 대시합니다.")]
    [SerializeField] private KeyCode dashKey = KeyCode.Space; // (레거시 호환용) 인스펙터 노출 유지

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

    // 입력 시스템 값 캐시
    private Vector2 _moveInputCache;
    private bool _dashPressedThisFrame;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    /*
     * [Input System 연결 방식]
     * - PlayerInput(Invoke Unity Events) 사용 시:
     *   - Move(Action: Value/Vector2)  -> 이 스크립트의 OnMove에 연결
     *   - Dash(Action: Button)         -> 이 스크립트의 OnDash에 연결
     *
     * - 복잡도: O(1)
     * - 주의: Update에서 UnityEngine.Input(레거시) 호출 금지
     */

    // Move 액션(Unity Events)에서 호출
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInputCache = context.ReadValue<Vector2>();

        // 대각선 입력 정규화(길이 1 초과 방지)
        if (_moveInputCache.sqrMagnitude > 1f)
            _moveInputCache.Normalize();
    }

    // Dash 액션(Unity Events)에서 호출 (Press)
    public void OnDash(InputAction.CallbackContext context)
    {
        // Button이 눌린 “순간”만 트리거
        if (context.performed)
            _dashPressedThisFrame = true;
    }

    private void Update()
    {
        // 입력 적용
        MoveInput = _moveInputCache;

        // 바라보는 방향 갱신(입력이 있을 때만)
        if (MoveInput.sqrMagnitude > 0.0001f)
            FacingDir = MoveInput;

        // 대시 입력(이번 프레임에 눌렸으면)
        if (_dashPressedThisFrame)
        {
            _dashPressedThisFrame = false;
            TryDash();
        }

        // (레거시 호환) dashKey는 인스펙터 유지용으로 남겼지만,
        // Active Input Handling = New일 때는 아래 레거시 입력 호출을 절대 쓰면 안 됨.
        // if (Input.GetKeyDown(dashKey)) TryDash();  // <- 금지
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