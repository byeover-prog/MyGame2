using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [구현 원리 요약]
/// 새 Input System 기반 플레이어 이동 + 대시.
/// 최종 이동속도는 기본 이동속도 × PlayerCombatStats2D.MoveSpeedMul 로 계산합니다.
/// 맵 경계는 Bounds(AABB)를 직접 계산하거나, 인스펙터에서 수동 입력합니다.
/// 또한 모든 캐릭터가 공통으로 사용할 전투 연출 함수(착지, 궁극기, 퇴장, 페이드)를 함께 제공합니다.
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

    [Tooltip("이동속도 배율을 읽을 전투 스탯 컴포넌트입니다.")]
    [SerializeField] private PlayerCombatStats2D combatStats;

    [Header("애니 판정(떨림 방지)")]
    [Tooltip("이 속도 이상이면 걷기(true)로 전환")]
    [Min(0f)]
    [SerializeField] private float walkOnSpeed = 0.05f;

    [Tooltip("이 속도 이하이면 정지(false)로 전환")]
    [Min(0f)]
    [SerializeField] private float walkOffSpeed = 0.02f;

    [Header("전투 연출")]
    [Tooltip("체크 시 전투 연출 중 이동과 대시를 잠급니다.")]
    [SerializeField] private bool lockMoveDuringAction = true;

    [Tooltip("퇴장 시 알파를 서서히 줄이는 연출 시간입니다.")]
    [Min(0.01f)]
    [SerializeField] private float exitFadeDuration = 0.25f;

    [Tooltip("로그를 출력할지 여부입니다.")]
    [SerializeField] private bool debugLog;

    public Vector2 MoveInput { get; private set; }
    public Vector2 FacingDir { get; private set; } = Vector2.right;

    public bool IsActionLocked => _isActionLocked;
    public bool IsInDash => Time.time < _dashEndTime;

    private float _dashEndTime = -999f;
    private float _nextDashReadyTime = 0f;
    private Vector2 _dashVelocity;

    private bool _isWalkingCached;
    private bool _isActionLocked;
    private Coroutine _fadeRoutine;

    private float _boundsMinX, _boundsMaxX, _boundsMinY, _boundsMaxY;
    private bool _boundsReady;

    private Color _originalSpriteColor = Color.white;

    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
    private static readonly int TriggerUltHash = Animator.StringToHash("Trigger_Ult");
    private static readonly int TriggerLandHash = Animator.StringToHash("Trigger_Land");
    private static readonly int TriggerExitHash = Animator.StringToHash("Trigger_Exit");

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        combatStats = GetComponent<PlayerCombatStats2D>();
        if (combatStats == null)
            combatStats = GetComponentInParent<PlayerCombatStats2D>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (combatStats == null) combatStats = GetComponent<PlayerCombatStats2D>();
        if (combatStats == null) combatStats = GetComponentInParent<PlayerCombatStats2D>();

        if (spriteRenderer != null)
        {
            _originalSpriteColor = spriteRenderer.color;
        }

        InitBounds();
    }

    private void InitBounds()
    {
        if (!useBoundary) return;

        if (mapBoundsCollider == null)
        {
            GameObject found = GameObject.Find("MapBounds2D");
            if (found != null)
            {
                mapBoundsCollider = found.GetComponent<Collider2D>();
            }
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
            float m = boundaryMargin;
            _boundsMinX = manualBounds.xMin + m;
            _boundsMaxX = manualBounds.xMax - m;
            _boundsMinY = manualBounds.yMin + m;
            _boundsMaxY = manualBounds.yMax - m;
            _boundsReady = true;
        }

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
        ReadMoveInput();
        UpdateFacingDirection();
        HandleDashInput();
        UpdateWalkAnimationState();
        UpdateFlip();
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        if (_isActionLocked)
        {
            rb.linearVelocity = Vector2.zero;
            ClampToBoundary();
            return;
        }

        if (Time.time < _dashEndTime)
        {
            rb.linearVelocity = _dashVelocity;
        }
        else
        {
            float finalMoveSpeed = moveSpeed * (combatStats != null ? combatStats.MoveSpeedMul : 1f);
            rb.linearVelocity = MoveInput * finalMoveSpeed;
        }

        ClampToBoundary();
    }

    private void ReadMoveInput()
    {
        if (_isActionLocked)
        {
            MoveInput = Vector2.zero;
            return;
        }

        Vector2 input = Vector2.zero;
        if (moveAction != null)
        {
            input = moveAction.action.ReadValue<Vector2>();
        }

        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        MoveInput = input;
    }

    private void UpdateFacingDirection()
    {
        if (MoveInput.x < -0.0001f)
        {
            FacingDir = Vector2.left;
        }
        else if (MoveInput.x > 0.0001f)
        {
            FacingDir = Vector2.right;
        }
    }

    private void HandleDashInput()
    {
        if (_isActionLocked)
        {
            return;
        }

        if (dashAction != null && dashAction.action.WasPressedThisFrame())
        {
            TryDash();
        }
    }

    private void UpdateWalkAnimationState()
    {
        if (animator == null || rb == null)
        {
            return;
        }

        if (_isActionLocked)
        {
            SetWalkingAnimation(false);
            return;
        }

        float speed = rb.linearVelocity.magnitude;

        bool next;
        if (_isWalkingCached)
        {
            next = speed > walkOffSpeed;
        }
        else
        {
            next = speed >= walkOnSpeed;
        }

        SetWalkingAnimation(next);
    }

    private void SetWalkingAnimation(bool isWalking)
    {
        if (animator == null)
        {
            return;
        }

        if (isWalking == _isWalkingCached)
        {
            return;
        }

        _isWalkingCached = isWalking;
        animator.SetBool(IsWalkingHash, _isWalkingCached);
    }

    private void UpdateFlip()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (FacingDir.x < -0.0001f)
        {
            spriteRenderer.flipX = true;
        }
        else if (FacingDir.x > 0.0001f)
        {
            spriteRenderer.flipX = false;
        }
    }

    private void ClampToBoundary()
    {
        if (!useBoundary || !_boundsReady || rb == null)
        {
            return;
        }

        Vector2 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, _boundsMinX, _boundsMaxX);
        pos.y = Mathf.Clamp(pos.y, _boundsMinY, _boundsMaxY);
        rb.position = pos;
    }

    private void TryDash()
    {
        if (Time.time < _nextDashReadyTime)
        {
            return;
        }

        Vector2 dir = MoveInput.sqrMagnitude > 0.0001f ? MoveInput : FacingDir;
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = Vector2.right;
        }

        float speed = dashDistance / Mathf.Max(0.01f, dashDuration);
        _dashVelocity = dir.normalized * speed;

        _dashEndTime = Time.time + dashDuration;
        _nextDashReadyTime = Time.time + dashCooldown;
    }

    public void PlayUltAnimation()
    {
        if (debugLog)
        {
            Debug.Log($"[{name}] 궁극기 애니메이션 재생", this);
        }

        BeginActionLock();
        ResetActionTriggers();
        TriggerAnimation(TriggerUltHash);
    }

    public void PlayLandAnimation()
    {
        if (debugLog)
        {
            Debug.Log($"[{name}] 착지 애니메이션 재생", this);
        }

        BeginActionLock();
        ResetActionTriggers();
        TriggerAnimation(TriggerLandHash);
    }

    public void PlayExitAnimation(bool useFadeOut = true)
    {
        if (debugLog)
        {
            Debug.Log($"[{name}] 퇴장 애니메이션 재생", this);
        }

        BeginActionLock();
        ResetActionTriggers();
        TriggerAnimation(TriggerExitHash);

        if (useFadeOut)
        {
            StartExitFade();
        }
    }

    public void BeginActionLock()
    {
        if (!lockMoveDuringAction)
        {
            return;
        }

        _isActionLocked = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        SetWalkingAnimation(false);
    }

    public void EndActionLock()
    {
        _isActionLocked = false;
        SetWalkingAnimation(false);

        if (debugLog)
        {
            Debug.Log($"[{name}] 연출 잠금 해제", this);
        }
    }

    public void SetCharacterVisible(bool visible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }
    }

    public void ResetVisualAlpha()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color color = spriteRenderer.color;
        color.a = _originalSpriteColor.a;
        spriteRenderer.color = color;
    }

    public void StartExitFade()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        _fadeRoutine = StartCoroutine(Co_ExitFade());
    }

    private IEnumerator Co_ExitFade()
    {
        ResetVisualAlpha();

        float elapsed = 0f;
        float startAlpha = spriteRenderer.color.a;

        while (elapsed < exitFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / exitFadeDuration);

            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(startAlpha, 0f, t);
            spriteRenderer.color = color;

            yield return null;
        }

        Color endColor = spriteRenderer.color;
        endColor.a = 0f;
        spriteRenderer.color = endColor;

        _fadeRoutine = null;
    }

    private void ResetActionTriggers()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(TriggerUltHash);
        animator.ResetTrigger(TriggerLandHash);
        animator.ResetTrigger(TriggerExitHash);
    }

    private void TriggerAnimation(int triggerHash)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(triggerHash);
    }

    // 애니메이션 이벤트에서 호출
    public void AnimEvent_EndActionLock()
    {
        EndActionLock();
    }

    // 애니메이션 이벤트에서 호출
    public void AnimEvent_ResetAlpha()
    {
        ResetVisualAlpha();
    }

    // 애니메이션 이벤트에서 호출
    public void AnimEvent_DisableObject()
    {
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!useBoundary) return;

        float minX, maxX, minY, maxY;
        if (Application.isPlaying && _boundsReady)
        {
            minX = _boundsMinX;
            maxX = _boundsMaxX;
            minY = _boundsMinY;
            maxY = _boundsMaxY;
        }
        else if (mapBoundsCollider != null)
        {
            Bounds b = mapBoundsCollider.bounds;
            float m = boundaryMargin;
            minX = b.min.x + m;
            maxX = b.max.x - m;
            minY = b.min.y + m;
            maxY = b.max.y - m;
        }
        else
        {
            float m = boundaryMargin;
            minX = manualBounds.xMin + m;
            maxX = manualBounds.xMax - m;
            minY = manualBounds.yMin + m;
            maxY = manualBounds.yMax - m;
        }

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}