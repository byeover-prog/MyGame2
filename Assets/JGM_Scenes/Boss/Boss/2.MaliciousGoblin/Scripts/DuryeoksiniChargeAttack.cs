using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class DuryeoksiniChargeAttack : MonoBehaviour
{
    /* =========================================================
     * 두억시니 돌진 공격
     * - 플레이어가 일정 거리 이상 멀어지면 돌진
     * - 추적 스크립트와 충돌하지 않도록 이동 제어
     * - 돌진 중 보스 앞쪽 범위 판정으로 플레이어 히트 체크
     * - 플레이어가 Kinematic이어도 코드로 직접 넉백 이동
     * ========================================================= */

    [Header("===== 기본 참조 =====")]
    [Tooltip("플레이어 Transform\n비워두면 Player 태그 자동 탐색")]
    [SerializeField] private Transform player;

    [Tooltip("보스 Rigidbody2D\n비워두면 같은 오브젝트에서 자동 탐색")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("보스 추적 스크립트\n비워두면 같은 오브젝트에서 자동 탐색")]
    [SerializeField] private DuryeoksiniBossChase2D chaseController;


    [Header("===== 돌진 조건 =====")]
    [Tooltip("플레이어가 이 거리 이상 멀어지면 돌진 시작")]
    [SerializeField] private float chargeDistance = 6f;

    [Tooltip("돌진 쿨타임")]
    [SerializeField] private float chargeCooldown = 4f;


    [Header("===== 돌진 설정 =====")]
    [Tooltip("돌진 속도")]
    [SerializeField] private float chargeSpeed = 14f;

    [Tooltip("돌진 지속 시간")]
    [SerializeField] private float chargeTime = 1.5f;

    [Tooltip("돌진 시작 전 준비 시간\n0이면 바로 돌진")]
    [SerializeField] private float chargePrepareTime = 0.15f;


    [Header("===== 공격 설정 =====")]
    [Tooltip("돌진 공격 데미지")]
    [SerializeField] private int damage = 20;

    [Tooltip("한 번의 돌진에서 플레이어에게 한 번만 데미지를 줄지 여부")]
    [SerializeField] private bool hitOnlyOncePerCharge = true;


    [Header("===== 앞쪽 범위 판정 =====")]
    [Tooltip("두억시니 앞쪽 범위 판정 반경")]
    [SerializeField] private float hitRadius = 1.2f;

    [Tooltip("두억시니 중심에서 앞쪽으로 판정 시작 위치를 얼마나 띄울지")]
    [SerializeField] private float hitForwardOffset = 1.1f;

    [Tooltip("플레이어 레이어 마스크")]
    [SerializeField] private LayerMask playerLayerMask;


    [Header("===== Kinematic 넉백 설정 =====")]
    [Tooltip("플레이어 넉백 거리")]
    [SerializeField] private float knockbackDistance = 2.2f;

    [Tooltip("플레이어 넉백 시간")]
    [SerializeField] private float knockbackDuration = 0.18f;

    [Tooltip("넉백 시 위쪽으로 약간 띄우는 비율")]
    [SerializeField] private float knockbackUpBias = 0.35f;

    [Tooltip("넉백 동안 플레이어 이동 스크립트를 잠시 비활성화")]
    [SerializeField] private bool disablePlayerMoverDuringKnockback = true;


    [Header("===== 넉백 장애물 보정 =====")]
    [Tooltip("넉백 중 벽에 막히도록 캐스트 이동 사용")]
    [SerializeField] private bool useSafeCastMove = true;

    [Tooltip("벽/장애물 레이어")]
    [SerializeField] private LayerMask obstacleLayerMask;


    [Header("===== 디버그 설정 =====")]
    [Tooltip("체크하면 돌진 상태 로그를 출력합니다.")]
    [SerializeField] private bool showDebug = true;

    [Tooltip("체크하면 앞쪽 판정 Gizmos를 표시합니다.")]
    [SerializeField] private bool showGizmos = true;


    // ===== 내부 상태 =====
    private bool isPreparingCharge;
    private bool isCharging;
    private bool hasHitPlayerThisCharge;

    private float prepareTimer;
    private float chargeTimer;
    private float cooldownTimer;

    private Vector2 chargeDir;


    private void Awake()
    {
        // 한글 주석: 필요한 컴포넌트 자동 캐싱
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (chaseController == null)
            chaseController = GetComponent<DuryeoksiniBossChase2D>();

        // 한글 주석: 빠른 이동 안정성 보강
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
        }

        // 한글 주석: Player 레이어가 있으면 자동으로 플레이어 레이어 마스크 설정
        if (playerLayerMask.value == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
                playerLayerMask = 1 << playerLayer;
        }
    }

    private void Start()
    {
        TryFindPlayer();
    }

    private void Update()
    {
        // 한글 주석: 플레이어가 없으면 다시 탐색
        if (player == null)
        {
            TryFindPlayer();
            return;
        }

        // 한글 주석: 쿨타임 감소
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // 한글 주석: 돌진 준비 상태 처리
        if (isPreparingCharge)
        {
            prepareTimer -= Time.deltaTime;

            if (prepareTimer <= 0f)
                BeginCharge();

            return;
        }

        // 한글 주석: 돌진 중이면 시간 감소 + 앞쪽 히트 판정
        if (isCharging)
        {
            chargeTimer -= Time.deltaTime;

            CheckFrontHit();

            if (chargeTimer <= 0f)
                StopCharge();

            return;
        }

        // 한글 주석: 평상시 상태일 때 거리 검사 후 돌진 시작
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance >= chargeDistance && cooldownTimer <= 0f)
            StartPrepareCharge();
    }

    private void FixedUpdate()
    {
        // 한글 주석: 돌진 중일 때만 속도 적용
        if (isCharging)
            rb.linearVelocity = chargeDir * chargeSpeed;
    }

    private void TryFindPlayer()
    {
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;

            if (showDebug)
                Debug.Log($"[두억시니 돌진] 플레이어 자동 탐색 성공: {playerObject.name}");
        }
        else
        {
            if (showDebug)
                Debug.LogWarning("[두억시니 돌진] 'Player' 태그 오브젝트를 찾지 못했습니다.");
        }
    }

    private void StartPrepareCharge()
    {
        if (player == null)
            return;

        // 한글 주석: 돌진 시작 전에 방향 저장
        chargeDir = ((Vector2)(player.position - transform.position)).normalized;

        if (chargeDir.sqrMagnitude <= 0.0001f)
            chargeDir = Vector2.right;

        isPreparingCharge = true;
        isCharging = false;
        hasHitPlayerThisCharge = false;

        prepareTimer = chargePrepareTime;

        // 한글 주석: 준비 중에도 추적 이동은 멈춤
        if (chaseController != null)
            chaseController.IsCharging = true;

        if (chargePrepareTime <= 0f)
            BeginCharge();

        if (showDebug)
            Debug.Log("[두억시니 돌진] 돌진 준비 시작");
    }

    private void BeginCharge()
    {
        isPreparingCharge = false;
        isCharging = true;
        chargeTimer = chargeTime;

        if (player == null)
        {
            StopCharge();
            return;
        }

        // 한글 주석: 시작 시점 방향으로 돌진
        chargeDir = ((Vector2)(player.position - transform.position)).normalized;

        if (chargeDir.sqrMagnitude <= 0.0001f)
            chargeDir = Vector2.right;

        if (showDebug)
            Debug.Log("[두억시니 돌진] 돌진 시작");
    }

    private void StopCharge()
    {
        isPreparingCharge = false;
        isCharging = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (chaseController != null)
            chaseController.IsCharging = false;

        cooldownTimer = chargeCooldown;

        if (showDebug)
            Debug.Log("[두억시니 돌진] 돌진 종료");
    }

    private void CheckFrontHit()
    {
        // 한글 주석: 한 번만 맞게 할 경우 이미 히트했다면 검사 종료
        if (hitOnlyOncePerCharge && hasHitPlayerThisCharge)
            return;

        Vector2 hitCenter = GetFrontHitCenter();

        Collider2D target = Physics2D.OverlapCircle(hitCenter, hitRadius, playerLayerMask);

        if (target == null)
            return;

        // 한글 주석: 혹시 다른 오브젝트가 잡히는 상황을 방지하기 위해 태그 재확인
        if (!target.CompareTag("Player"))
            return;

        ApplyHitToPlayer(target);
    }

    private Vector2 GetFrontHitCenter()
    {
        Vector2 dir = chargeDir.sqrMagnitude > 0.0001f ? chargeDir.normalized : Vector2.right;
        return (Vector2)transform.position + dir * hitForwardOffset;
    }

    private void ApplyHitToPlayer(Collider2D target)
    {
        hasHitPlayerThisCharge = true;

        // 한글 주석: 플레이어 체력 처리
        PlayerHealth hp = target.GetComponent<PlayerHealth>();
        if (hp == null)
            hp = target.GetComponentInParent<PlayerHealth>();

        if (hp != null)
            hp.TakeDamage(damage);

        // 한글 주석: 넉백 방향 계산
        Vector2 dir = ((Vector2)(target.transform.position - transform.position)).normalized;

        if (dir.sqrMagnitude <= 0.0001f)
            dir = chargeDir.sqrMagnitude > 0.0001f ? chargeDir : Vector2.right;

        Vector2 finalDir = (dir + Vector2.up * knockbackUpBias).normalized;

        // 한글 주석: 플레이어 관련 컴포넌트 탐색
        PlayerMover2D mover = target.GetComponent<PlayerMover2D>();
        if (mover == null)
            mover = target.GetComponentInParent<PlayerMover2D>();

        Rigidbody2D playerRb = target.attachedRigidbody;
        if (playerRb == null)
            playerRb = target.GetComponent<Rigidbody2D>();

        Collider2D playerCol = target;
        if (playerCol == null)
            playerCol = target.GetComponent<Collider2D>();

        StartCoroutine(ApplyKinematicKnockback(target.transform, mover, playerRb, playerCol, finalDir));

        if (showDebug)
            Debug.Log("[두억시니 돌진] 플레이어 타격 성공");

        StopCharge();
    }

    private IEnumerator ApplyKinematicKnockback(
        Transform playerTransform,
        PlayerMover2D mover,
        Rigidbody2D playerRb,
        Collider2D playerCol,
        Vector2 knockDir)
    {
        if (playerTransform == null)
            yield break;

        // 한글 주석: 넉백 중 플레이어 입력 이동을 막음
        if (disablePlayerMoverDuringKnockback && mover != null)
            mover.enabled = false;

        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        Vector2 startPos = playerTransform.position;
        Vector2 desiredEndPos = startPos + knockDir * knockbackDistance;
        Vector2 finalEndPos = desiredEndPos;

        // 한글 주석: 장애물에 막히면 그 앞까지만 이동
        if (useSafeCastMove && playerCol != null)
        {
            Vector2 castDir = desiredEndPos - startPos;
            float castDistance = castDir.magnitude;

            if (castDistance > 0.0001f)
            {
                RaycastHit2D[] hits = new RaycastHit2D[8];
                ContactFilter2D filter = new ContactFilter2D();
                filter.useLayerMask = true;
                filter.layerMask = obstacleLayerMask;
                filter.useTriggers = false;

                int hitCount = playerCol.Cast(castDir.normalized, filter, hits, castDistance);

                if (hitCount > 0)
                {
                    float nearest = castDistance;

                    for (int i = 0; i < hitCount; i++)
                    {
                        if (hits[i].collider == null)
                            continue;

                        if (hits[i].distance < nearest)
                            nearest = hits[i].distance;
                    }

                    float safeDistance = Mathf.Max(0f, nearest - 0.05f);
                    finalEndPos = startPos + castDir.normalized * safeDistance;
                }
            }
        }

        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / knockbackDuration);

            // 한글 주석: 처음엔 빠르고 끝엔 완만해지는 보간
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Vector2 nextPos = Vector2.Lerp(startPos, finalEndPos, eased);

            if (playerRb != null)
                playerRb.MovePosition(nextPos);
            else
                playerTransform.position = nextPos;

            yield return null;
        }

        if (playerRb != null)
            playerRb.MovePosition(finalEndPos);
        else
            playerTransform.position = finalEndPos;

        // 한글 주석: 넉백 종료 후 플레이어 이동 다시 활성화
        if (disablePlayerMoverDuringKnockback && mover != null)
            mover.enabled = true;
    }

    private void OnDisable()
    {
        if (chaseController != null)
            chaseController.IsCharging = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        Gizmos.color = Color.red;

        Vector2 center;

        if (Application.isPlaying)
            center = GetFrontHitCenter();
        else
            center = (Vector2)transform.position + Vector2.right * hitForwardOffset;

        Gizmos.DrawWireSphere(center, hitRadius);
    }
}