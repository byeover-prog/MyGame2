using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class KumihoBossChase2D : MonoBehaviour
{
    /* ============================================================
     *  구미호 보스 – 플레이어 추적 (디버그 로그 포함)
     * ============================================================ */

    [Header("===== 추적 대상 =====")]
    [Tooltip("비워두면 'Player' 태그로 자동 탐색")]
    [SerializeField] private Transform playerTarget;

    [Header("===== 이동 속도 =====")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("===== 스프라이트 설정 =====")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool useFlip = true;

    [Header("===== 디버그 =====")]
    [Tooltip("true면 화면에 디버그 정보 표시")]
    [SerializeField] private bool showDebug = true;

    // ── 캐싱 ──
    private Rigidbody2D rb;
    private Collider2D col;

    // ── 디버그용 변수 ──
    private Vector2 debugDirection;
    private Vector2 debugDesiredVelocity;
    private Vector2 debugActualVelocity;
    private bool debugIsTrigger;
    private float debugGravityScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // ── 현재 세팅 로그 출력 ──
        Debug.Log($"[구미호 보스] === 컴포넌트 진단 ===");
        Debug.Log($"[구미호 보스] Rigidbody2D bodyType: {rb.bodyType}");
        Debug.Log($"[구미호 보스] Rigidbody2D gravityScale: {rb.gravityScale}");
        Debug.Log($"[구미호 보스] Rigidbody2D freezeRotation: {rb.freezeRotation}");

        if (col != null)
        {
            Debug.Log($"[구미호 보스] Collider2D 타입: {col.GetType().Name}");
            Debug.Log($"[구미호 보스] Collider2D isTrigger: {col.isTrigger}");
        }
        else
        {
            Debug.LogWarning($"[구미호 보스] Collider2D가 없습니다!");
        }
    }

    void Start()
    {
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
                Debug.Log($"[구미호 보스] 플레이어 자동 탐색 성공: {player.name}");

                // 플레이어 쪽 세팅도 출력
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                Collider2D playerCol = player.GetComponent<Collider2D>();
                if (playerRb != null)
                    Debug.Log($"[구미호 보스] 플레이어 Rigidbody2D bodyType: {playerRb.bodyType}");
                if (playerCol != null)
                    Debug.Log($"[구미호 보스] 플레이어 Collider isTrigger: {playerCol.isTrigger}");
            }
            else
            {
                Debug.LogError($"[구미호 보스] 'Player' 태그 오브젝트를 찾지 못했습니다!");
            }
        }
    }

    void FixedUpdate()
    {
        if (playerTarget == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = (Vector2)(playerTarget.position - transform.position);

        // ── 위치 확인 로그 (1초마다 한 번만 출력) ──
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[구미호 보스] 보스 pos: {transform.position} | " +
                      $"플레이어 pos: {playerTarget.position} | " +
                      $"거리: {direction.magnitude:F2}");
        }

        if (direction.sqrMagnitude > 0.001f)
            direction.Normalize();

        rb.linearVelocity = direction * moveSpeed;
        UpdateSprite(direction);
    }

    void UpdateSprite(Vector2 direction)
    {
        if (!useFlip || spriteRenderer == null) return;
        if (Mathf.Abs(direction.x) > 0.01f)
            spriteRenderer.flipX = direction.x < 0;
    }

    // ================================================================
    //  충돌 이벤트 감지 – 어떤 오브젝트와 부딪히는지 로그
    // ================================================================
    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.LogWarning($"[구미호 보스] ⚠ 물리 충돌 발생! 대상: {collision.gameObject.name} " +
                         $"(레이어: {LayerMask.LayerToName(collision.gameObject.layer)}) " +
                         $"→ 이게 플레이어면 isTrigger 설정이 안 되어 있는 것!");
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        Debug.LogWarning($"[구미호 보스] ⚠ 물리 충돌 지속 중! 대상: {collision.gameObject.name} " +
                         $"→ 반발력이 velocity를 덮어쓰고 있을 수 있음");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[구미호 보스] ✅ 트리거 진입: {other.gameObject.name} " +
                  $"(레이어: {LayerMask.LayerToName(other.gameObject.layer)})");
    }

    // ================================================================
    //  화면 디버그 표시 (Game 뷰 좌측 상단)
    // ================================================================
    void OnGUI()
    {
        if (!showDebug) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.yellow;

        float y = 10f;
        float lineHeight = 20f;

        GUI.Label(new Rect(10, y, 600, lineHeight), $"[보스 디버그]", style);
        y += lineHeight;
        GUI.Label(new Rect(10, y, 600, lineHeight),
            $"방향: ({debugDirection.x:F2}, {debugDirection.y:F2})", style);
        y += lineHeight;
        GUI.Label(new Rect(10, y, 600, lineHeight),
            $"원하는 속도: ({debugDesiredVelocity.x:F2}, {debugDesiredVelocity.y:F2})", style);
        y += lineHeight;
        GUI.Label(new Rect(10, y, 600, lineHeight),
            $"실제 속도:   ({debugActualVelocity.x:F2}, {debugActualVelocity.y:F2})", style);
        y += lineHeight;

        // 원하는 속도와 실제 속도가 다르면 빨간색으로 경고
        Vector2 diff = debugActualVelocity - debugDesiredVelocity;
        if (diff.sqrMagnitude > 0.5f)
        {
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(10, y, 600, lineHeight),
                $"⚠ 속도 차이 감지! diff: ({diff.x:F2}, {diff.y:F2}) → 외부 힘 작용 중!", style);
            y += lineHeight;
        }

        style.normal.textColor = Color.cyan;
        GUI.Label(new Rect(10, y, 600, lineHeight),
            $"isTrigger: {debugIsTrigger} | gravityScale: {debugGravityScale}", style);
    }
}