using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class DuryeoksiniBossChase2D : MonoBehaviour
{
    /* ============================================================
     * 두억시니 보스 - 플레이어 추적 이동
     * - Rigidbody2D 기반 이동
     * - 플레이어 자동 탐색
     * - 일정 거리 안으로 들어오면 정지
     * - 좌우 스프라이트 반전 지원
     * - 돌진 공격 중일 때는 추적 이동 중지
     * - 디버그 로그 / 화면 표시 지원
     * ============================================================ */

    [Header("===== 추적 대상 설정 =====")]
    [Tooltip("추적할 플레이어 Transform\n비워두면 'Player' 태그로 자동 탐색합니다.")]
    [SerializeField] private Transform playerTarget;

    [Header("===== 이동 설정 =====")]
    [Tooltip("두억시니 기본 추적 이동 속도")]
    [SerializeField] private float moveSpeed = 3.5f;

    [Tooltip("이 거리보다 가까워지면 이동을 멈춥니다.")]
    [SerializeField] private float stopDistance = 1.2f;

    [Header("===== 스프라이트 방향 설정 =====")]
    [Tooltip("두억시니 스프라이트 Renderer\n비워두면 자식 오브젝트에서 자동 탐색합니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("좌우 방향에 따라 스프라이트를 뒤집을지 여부")]
    [SerializeField] private bool useFlip = true;

    [Header("===== 디버그 설정 =====")]
    [Tooltip("체크하면 콘솔 로그와 화면 디버그 정보를 표시합니다.")]
    [SerializeField] private bool showDebug = true;

    [Tooltip("체크하면 약 1초마다 위치 로그를 출력합니다.")]
    [SerializeField] private bool showPositionLog = true;

    // ===== 외부 제어용 =====
    [Tooltip("외부 스크립트에서 추적 이동을 막을 때 사용합니다.\n돌진 중에는 자동으로 체크됩니다.")]
    [SerializeField] private bool blockMovementExternally = false;

    // ===== 캐싱 =====
    private Rigidbody2D rb;
    private Collider2D col;

    // ===== 디버그용 =====
    private Vector2 debugDirection;
    private Vector2 debugDesiredVelocity;
    private Vector2 debugActualVelocity;
    private bool debugIsTrigger;
    private float debugGravityScale;
    private float debugDistanceToPlayer;

    /// <summary>
    /// 외부에서 현재 돌진 중인지 알려주기 위한 속성
    /// </summary>
    public bool IsCharging
    {
        get => blockMovementExternally;
        set => blockMovementExternally = value;
    }

    /// <summary>
    /// 현재 추적 대상 반환
    /// </summary>
    public Transform PlayerTarget => playerTarget;

    private void Awake()
    {
        // 한글 주석: 리지드바디와 콜라이더를 미리 캐싱
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // 한글 주석: 스프라이트 렌더러가 비어 있으면 자식에서 자동 탐색
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // 한글 주석: 디버그 표시용 현재 세팅 저장
        debugGravityScale = rb.gravityScale;
        debugIsTrigger = col != null && col.isTrigger;

        if (showDebug)
        {
            Debug.Log("[두억시니 보스] === 컴포넌트 진단 시작 ===");
            Debug.Log($"[두억시니 보스] Rigidbody2D bodyType: {rb.bodyType}");
            Debug.Log($"[두억시니 보스] Rigidbody2D gravityScale: {rb.gravityScale}");
            Debug.Log($"[두억시니 보스] Rigidbody2D freezeRotation: {rb.freezeRotation}");

            if (col != null)
            {
                Debug.Log($"[두억시니 보스] Collider2D 타입: {col.GetType().Name}");
                Debug.Log($"[두억시니 보스] Collider2D isTrigger: {col.isTrigger}");
            }
            else
            {
                Debug.LogWarning("[두억시니 보스] Collider2D가 없습니다.");
            }

            if (spriteRenderer == null)
            {
                Debug.LogWarning("[두억시니 보스] SpriteRenderer를 찾지 못했습니다.");
            }
        }
    }

    private void Start()
    {
        TryFindPlayer();
    }

    private void FixedUpdate()
    {
        // 한글 주석: 외부에서 이동 차단 상태(돌진 중 포함)면 추적 이동을 멈춤
        if (blockMovementExternally)
        {
            debugDirection = Vector2.zero;
            debugDesiredVelocity = Vector2.zero;
            debugActualVelocity = rb.linearVelocity;
            return;
        }

        // 한글 주석: 플레이어가 없으면 다시 찾고 정지
        if (playerTarget == null)
        {
            TryFindPlayer();
            rb.linearVelocity = Vector2.zero;

            debugDirection = Vector2.zero;
            debugDesiredVelocity = Vector2.zero;
            debugActualVelocity = rb.linearVelocity;
            return;
        }

        Vector2 toPlayer = (Vector2)(playerTarget.position - transform.position);
        debugDistanceToPlayer = toPlayer.magnitude;

        // 한글 주석: 가까워지면 정지
        if (debugDistanceToPlayer <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;

            debugDirection = Vector2.zero;
            debugDesiredVelocity = Vector2.zero;
            debugActualVelocity = rb.linearVelocity;
            return;
        }

        // 한글 주석: 플레이어 방향으로 이동
        Vector2 direction = toPlayer.normalized;
        Vector2 desiredVelocity = direction * moveSpeed;
        rb.linearVelocity = desiredVelocity;

        // 한글 주석: 스프라이트 반전 처리
        UpdateSprite(direction);

        // 한글 주석: 디버그 값 저장
        debugDirection = direction;
        debugDesiredVelocity = desiredVelocity;
        debugActualVelocity = rb.linearVelocity;
        debugGravityScale = rb.gravityScale;
        debugIsTrigger = col != null && col.isTrigger;

        // 한글 주석: 약 1초마다 로그 출력
        if (showDebug && showPositionLog && Time.frameCount % 60 == 0)
        {
            Debug.Log(
                $"[두억시니 보스] 보스 위치: {transform.position} | " +
                $"플레이어 위치: {playerTarget.position} | " +
                $"거리: {debugDistanceToPlayer:F2} | " +
                $"이동속도: ({rb.linearVelocity.x:F2}, {rb.linearVelocity.y:F2})");
        }
    }

    private void TryFindPlayer()
    {
        // 한글 주석: 이미 찾았으면 종료
        if (playerTarget != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTarget = player.transform;

            if (showDebug)
            {
                Debug.Log($"[두억시니 보스] 플레이어 자동 탐색 성공: {player.name}");

                if (player.TryGetComponent(out Rigidbody2D playerRb))
                    Debug.Log($"[두억시니 보스] 플레이어 Rigidbody2D bodyType: {playerRb.bodyType}");

                if (player.TryGetComponent(out Collider2D playerCol))
                    Debug.Log($"[두억시니 보스] 플레이어 Collider isTrigger: {playerCol.isTrigger}");
            }
        }
        else
        {
            if (showDebug)
                Debug.LogWarning("[두억시니 보스] 'Player' 태그 오브젝트를 찾지 못했습니다.");
        }
    }

    private void UpdateSprite(Vector2 direction)
    {
        // 한글 주석: 반전 사용 안 하거나 렌더러가 없으면 종료
        if (!useFlip || spriteRenderer == null)
            return;

        // 한글 주석: 좌우 이동이 있을 때만 반전
        if (Mathf.Abs(direction.x) > 0.01f)
        {
            spriteRenderer.flipX = direction.x < 0f;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!showDebug) return;

        Debug.LogWarning(
            $"[두억시니 보스] 물리 충돌 발생: {collision.gameObject.name} " +
            $"(레이어: {LayerMask.LayerToName(collision.gameObject.layer)})");
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!showDebug) return;

        Debug.LogWarning(
            $"[두억시니 보스] 물리 충돌 지속 중: {collision.gameObject.name} " +
            $"→ 밀림/반발력으로 이동이 꼬일 수 있습니다.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!showDebug) return;

        Debug.Log(
            $"[두억시니 보스] 트리거 진입: {other.gameObject.name} " +
            $"(레이어: {LayerMask.LayerToName(other.gameObject.layer)})");
    }

    private void OnGUI()
    {
        if (!showDebug) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.yellow;

        float y = 10f;
        float lineHeight = 22f;

        GUI.Label(new Rect(10, y, 700, lineHeight), "[두억시니 보스 디버그]", style);
        y += lineHeight;

        GUI.Label(new Rect(10, y, 700, lineHeight),
            $"플레이어 거리: {debugDistanceToPlayer:F2}", style);
        y += lineHeight;

        GUI.Label(new Rect(10, y, 700, lineHeight),
            $"이동 방향: ({debugDirection.x:F2}, {debugDirection.y:F2})", style);
        y += lineHeight;

        GUI.Label(new Rect(10, y, 700, lineHeight),
            $"목표 속도: ({debugDesiredVelocity.x:F2}, {debugDesiredVelocity.y:F2})", style);
        y += lineHeight;

        GUI.Label(new Rect(10, y, 700, lineHeight),
            $"실제 속도: ({debugActualVelocity.x:F2}, {debugActualVelocity.y:F2})", style);
        y += lineHeight;

        Vector2 diff = debugActualVelocity - debugDesiredVelocity;
        if (diff.sqrMagnitude > 0.5f)
        {
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(10, y, 700, lineHeight),
                $"속도 차이 감지: ({diff.x:F2}, {diff.y:F2}) → 외부 충돌/힘 확인 필요", style);
            y += lineHeight;
        }

        style.normal.textColor = Color.cyan;
        GUI.Label(new Rect(10, y, 700, lineHeight),
            $"isTrigger: {debugIsTrigger} | gravityScale: {debugGravityScale} | 돌진중: {blockMovementExternally}", style);
    }
}