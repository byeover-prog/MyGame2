using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [구현 원리 요약]
/// 새 Input System 기반 플레이어 이동 + 대시.
/// 맵 경계는 Bounds(AABB)를 직접 계산하거나, 인스펙터에서 수동 입력합니다.
/// Kinematic+Trigger 환경에서도 맵 밖으로 나가지 않습니다.
/// </summary>
public sealed class PlayerMover2D : MonoBehaviour
{
    [Header("입력(새 Input System)")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference dashAction;

    [Header("이동")]
    [Tooltip("플레이어 기본 이동 속도입니다.")]
    [SerializeField] private float moveSpeed = 4.0f;

    [Header("대시")]
    [Tooltip("대시 거리(월드 유닛)입니다.")]
    [SerializeField] private float dashDistance = 2.5f;
    [Tooltip("대시 지속 시간(초)입니다.")]
    [SerializeField] private float dashDuration = 0.12f;
    [Tooltip("대시 쿨다운(초)입니다.")]
    [SerializeField] private float dashCooldown = 0.9f;

    [Header("맵 경계 제한")]
    [Tooltip("맵 경계 영역 Collider2D를 넣으면 Bounds를 자동 계산합니다.\n" +
             "99_Map의 MapBounds2D처럼 맵 전체를 감싸는 콜라이더를 넣으세요.\n" +
             "비우면 아래 수동 범위(Manual Bounds)를 사용합니다.")]
    [SerializeField] private Collider2D mapBoundsCollider;

    [Tooltip("mapBoundsCollider가 없을 때 사용할 수동 경계입니다.\n" +
             "x=왼쪽끝, y=아래끝, width=가로길이, height=세로길이")]
    [SerializeField] private Rect manualBounds = new Rect(-20, -20, 40, 40);

    [Tooltip("경계 안쪽 여유 거리(월드 유닛)입니다.\n캐릭터가 경계에 딱 붙지 않게 합니다.")]
    [Min(0f)]
    [SerializeField] private float boundaryMargin = 0.5f;

    [Tooltip("경계 제한을 사용할지 여부입니다.")]
    [SerializeField] private bool useBoundary = true;

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

    // 런타임 경계 (Awake에서 1회 계산)
    private float _boundsMinX, _boundsMaxX, _boundsMinY, _boundsMaxY;
    private bool _boundsReady;

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

        InitBounds();
    }

    /// <summary>
    /// 맵 경계를 1회 계산합니다.
    /// Collider2D가 있으면 그 bounds를 사용하고, 없으면 manualBounds를 사용합니다.
    /// </summary>
    private void InitBounds()
    {
        if (!useBoundary) return;

        // 자동 탐색: mapBoundsCollider가 비어있으면 "MapBounds" 이름의 오브젝트를 찾아봄
        if (mapBoundsCollider == null)
        {
            var found = GameObject.Find("MapBounds2D");
            if (found != null) mapBoundsCollider = found.GetComponent<Collider2D>();
        }

        if (mapBoundsCollider != null)
        {
            Bounds b = mapBoundsCollider.bounds;
            float m = boundaryMargin;
            _boundsMinX = b.min.x + m;
            _boundsMaxX = b.max.x - m;
            _boundsMinY = b.min.y + m;
            _boundsMaxY = b.max.y - m;
            _boundsReady = true;
        }
        else
        {
            // 수동 범위 사용
            float m = boundaryMargin;
            _boundsMinX = manualBounds.xMin + m;
            _boundsMaxX = manualBounds.xMax - m;
            _boundsMinY = manualBounds.yMin + m;
            _boundsMaxY = manualBounds.yMax - m;
            _boundsReady = true;
        }

        // 안전장치: min이 max보다 크면 스왑
        if (_boundsMinX > _boundsMaxX) (_boundsMinX, _boundsMaxX) = (_boundsMaxX, _boundsMinX);
        if (_boundsMinY > _boundsMaxY) (_boundsMinY, _boundsMaxY) = (_boundsMaxY, _boundsMinY);
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

        // 방향 규칙
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
                next = speed > walkOffSpeed;
            else
                next = speed >= walkOnSpeed;

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

        // 맵 경계 클램핑 (Kinematic+Trigger라 물리 충돌이 없으므로 수동 처리)
        ClampToBoundary();
    }

    /// <summary>
    /// 미리 계산된 AABB 범위로 위치를 제한합니다.
    /// 매 FixedUpdate마다 Collider2D.bounds를 읽지 않아 성능에 유리합니다.
    /// </summary>
    private void ClampToBoundary()
    {
        if (!useBoundary || !_boundsReady) return;

        Vector2 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, _boundsMinX, _boundsMaxX);
        pos.y = Mathf.Clamp(pos.y, _boundsMinY, _boundsMaxY);
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 에디터에서 경계를 시각적으로 확인
        if (!useBoundary) return;

        float minX, maxX, minY, maxY;
        if (Application.isPlaying && _boundsReady)
        {
            minX = _boundsMinX; maxX = _boundsMaxX;
            minY = _boundsMinY; maxY = _boundsMaxY;
        }
        else if (mapBoundsCollider != null)
        {
            var b = mapBoundsCollider.bounds;
            float m = boundaryMargin;
            minX = b.min.x + m; maxX = b.max.x - m;
            minY = b.min.y + m; maxY = b.max.y - m;
        }
        else
        {
            float m = boundaryMargin;
            minX = manualBounds.xMin + m; maxX = manualBounds.xMax - m;
            minY = manualBounds.yMin + m; maxY = manualBounds.yMax - m;
        }

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
