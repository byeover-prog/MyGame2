// UTF-8
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class GrimReapermove : MonoBehaviour
{
    /* ============================================================
     * 저승사자 보스 - 플레이어 추적 (공격 중 멈춤 기능 포함)
     * ============================================================ */

    [Header("===== 추적 대상 =====")]
    [Tooltip("비워두면 'Player' 태그로 자동 탐색합니다.")]
    [SerializeField] private Transform playerTarget;

    [Header("===== 이동 속도 =====")]
    [Tooltip("저승사자의 추적 이동 속도입니다.")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("===== 스프라이트 설정 =====")]
    [Tooltip("저승사자 SpriteRenderer입니다. 비워두면 자식에서 자동 탐색합니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("좌우 방향에 따라 스프라이트를 뒤집을지 여부입니다.")]
    [SerializeField] private bool useFlip = true;

    [Header("===== 공격 상태 =====")]
    [Tooltip("체크되면 공격 중으로 판단하여 이동을 멈춥니다.")]
    [SerializeField] private bool isAttacking = false;

    [Header("===== 디버그 =====")]
    [Tooltip("체크하면 화면에 디버그 정보를 표시합니다.")]
    [SerializeField] private bool showDebug = true;

    // ===== 캐싱 =====
    private Rigidbody2D rb;
    private Collider2D col;

    // ===== 디버그용 변수 =====
    private Vector2 debugDirection;
    private Vector2 debugDesiredVelocity;
    private Vector2 debugActualVelocity;
    private bool debugIsTrigger;
    private float debugGravityScale;

    private void Awake()
    {
        // 한글 주석: 리지드바디와 콜라이더를 미리 저장
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // 한글 주석: 스프라이트 렌더러가 비어 있으면 자식에서 자동 탐색
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // 한글 주석: 현재 세팅 로그 출력
        Debug.Log("[저승사자 보스] === 컴포넌트 진단 ===");
        Debug.Log($"[저승사자 보스] Rigidbody2D bodyType: {rb.bodyType}");
        Debug.Log($"[저승사자 보스] Rigidbody2D gravityScale: {rb.gravityScale}");
        Debug.Log($"[저승사자 보스] Rigidbody2D freezeRotation: {rb.freezeRotation}");

        if (col != null)
        {
            Debug.Log($"[저승사자 보스] Collider2D 타입: {col.GetType().Name}");
            Debug.Log($"[저승사자 보스] Collider2D isTrigger: {col.isTrigger}");
        }
        else
        {
            Debug.LogWarning("[저승사자 보스] Collider2D가 없습니다!");
        }

        // 한글 주석: 디버그용 현재 값 저장
        debugGravityScale = rb.gravityScale;
        debugIsTrigger = col != null && col.isTrigger;
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerTarget = player.transform;
                Debug.Log($"[저승사자 보스] 플레이어 자동 탐색 성공: {player.name}");

                // 한글 주석: 플레이어 쪽 세팅도 같이 출력
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                Collider2D playerCol = player.GetComponent<Collider2D>();

                if (playerRb != null)
                    Debug.Log($"[저승사자 보스] 플레이어 Rigidbody2D bodyType: {playerRb.bodyType}");

                if (playerCol != null)
                    Debug.Log($"[저승사자 보스] 플레이어 Collider isTrigger: {playerCol.isTrigger}");
            }
            else
            {
                Debug.LogError("[저승사자 보스] 'Player' 태그 오브젝트를 찾지 못했습니다!");
            }
        }
    }

    private void FixedUpdate()
    {
        // 한글 주석: 공격 중이면 이동을 멈춤
        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;

            debugDirection = Vector2.zero;
            debugDesiredVelocity = Vector2.zero;
            debugActualVelocity = rb.linearVelocity;
            return;
        }

        if (playerTarget == null)
        {
            rb.linearVelocity = Vector2.zero;

            debugDirection = Vector2.zero;
            debugDesiredVelocity = Vector2.zero;
            debugActualVelocity = rb.linearVelocity;
            return;
        }

        Vector2 direction = (Vector2)(playerTarget.position - transform.position);

        // 한글 주석: 1초마다 위치 확인 로그 출력
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[저승사자 보스] 보스 pos: {transform.position} | " +
                      $"플레이어 pos: {playerTarget.position} | " +
                      $"거리: {direction.magnitude:F2} | " +
                      $"공격중: {isAttacking}");
        }

        if (direction.sqrMagnitude > 0.001f)
            direction.Normalize();

        Vector2 desiredVelocity = direction * moveSpeed;
        rb.linearVelocity = desiredVelocity;

        // 한글 주석: 스프라이트 방향 갱신
        UpdateSprite(direction);

        // 한글 주석: 디버그 값 저장
        debugDirection = direction;
        debugDesiredVelocity = desiredVelocity;
        debugActualVelocity = rb.linearVelocity;
        debugGravityScale = rb.gravityScale;
        debugIsTrigger = col != null && col.isTrigger;
    }

    private void UpdateSprite(Vector2 direction)
    {
        // 한글 주석: 반전 사용 안 하거나 렌더러가 없으면 종료
        if (!useFlip || spriteRenderer == null)
            return;

        // 한글 주석: 좌우 이동이 있을 때만 반전
        if (Mathf.Abs(direction.x) > 0.01f)
            spriteRenderer.flipX = direction.x < 0f;
    }

    public void SetAttacking(bool value)
    {
        // 한글 주석: 외부 공격 스크립트에서 공격 상태를 켜고 끄는 함수
        isAttacking = value;

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public bool GetAttacking()
    {
        // 한글 주석: 현재 공격 상태 반환
        return isAttacking;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.LogWarning($"[저승사자 보스] 물리 충돌 발생! 대상: {collision.gameObject.name} " +
                         $"(레이어: {LayerMask.LayerToName(collision.gameObject.layer)}) " +
                         $"→ 플레이어와 물리 충돌 중이면 Collider / isTrigger 설정을 확인하세요.");
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Debug.LogWarning($"[저승사자 보스] 물리 충돌 지속 중! 대상: {collision.gameObject.name} " +
                         $"→ 반발력 때문에 velocity가 밀릴 수 있습니다.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[저승사자 보스] 트리거 진입: {other.gameObject.name} " +
                  $"(레이어: {LayerMask.LayerToName(other.gameObject.layer)})");
    }

    private void OnGUI()
    {
        if (!showDebug)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };

        style.normal.textColor = Color.yellow;

        float y = 10f;
        float lineHeight = 20f;

        GUI.Label(new Rect(10, y, 650, lineHeight), "[저승사자 보스 디버그]", style);
        y += lineHeight;

        GUI.Label(new Rect(10, y, 650, lineHeight),
            $"방향: ({debugDirection.x:F2}, {debugDirection.y:F2})", style);
        y += lineHeight;

        GUI.Label(new Rect(10, y, 650, lineHeight),
            $"원하는 속도: ({debugDesiredVelocity.x:F2}, {debugDesiredVelocity.y:F2})", style);
        y += lineHeight;

        GUI.Label(new Rect(10, y, 650, lineHeight),
            $"실제 속도:   ({debugActualVelocity.x:F2}, {debugActualVelocity.y:F2})", style);
        y += lineHeight;

        GUI.Label(new Rect(10, y, 650, lineHeight),
            $"공격중 여부: {isAttacking}", style);
        y += lineHeight;

        Vector2 diff = debugActualVelocity - debugDesiredVelocity;
        if (diff.sqrMagnitude > 0.5f)
        {
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(10, y, 650, lineHeight),
                $"속도 차이 감지! diff: ({diff.x:F2}, {diff.y:F2}) → 외부 힘 작용 중!", style);
            y += lineHeight;
        }

        style.normal.textColor = Color.cyan;
        GUI.Label(new Rect(10, y, 650, lineHeight),
            $"isTrigger: {debugIsTrigger} | gravityScale: {debugGravityScale}", style);
    }
}
