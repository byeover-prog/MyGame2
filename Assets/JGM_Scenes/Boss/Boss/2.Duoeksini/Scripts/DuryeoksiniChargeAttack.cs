// UTF-8
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
     * - 벽 앞에서 맞았을 때 벽을 넘지 않고 옆으로 튕겨나오도록 보정
     * - PlayerMover2D를 수정하지 않아도 경계 밖으로 나갔다가 다시 돌아오는 현상 방지
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
    [SerializeField] private float knockbackDistance = 1.6f;

    [Tooltip("플레이어 넉백 시간")]
    [SerializeField] private float knockbackDuration = 0.12f;

    [Tooltip("넉백 시 위쪽으로 약간 띄우는 비율")]
    [SerializeField] private float knockbackUpBias = 0.2f;

    [Tooltip("넉백 동안 플레이어 이동 스크립트를 잠시 비활성화")]
    [SerializeField] private bool disablePlayerMoverDuringKnockback = true;

    [Header("===== 넉백 장애물 보정 =====")]
    [Tooltip("넉백 중 벽에 막히도록 캐스트 이동 사용")]
    [SerializeField] private bool useSafeCastMove = true;

    [Tooltip("벽/장애물 레이어")]
    [SerializeField] private LayerMask obstacleLayerMask;

    [Tooltip("장애물 바로 앞에 멈출 여유 거리")]
    [SerializeField] private float obstacleSkin = 0.05f;

    [Tooltip("뒤쪽이 막혔을 때 옆으로 튕겨나오는 보정 강도")]
    [Range(0f, 1f)]
    [SerializeField] private float sideBounceWeight = 0.85f;

    [Tooltip("정면이 막혔을 때 좌/우 보정 방향도 검사할지 여부")]
    [SerializeField] private bool useSideBounceWhenBlocked = true;

    [Header("===== 맵 경계 보정 =====")]
    [Tooltip("PlayerMover2D가 사용하는 맵 경계 Collider2D\n비워두면 MapBounds2D 자동 탐색")]
    [SerializeField] private Collider2D mapBoundsCollider;

    [Tooltip("맵 경계 안쪽 여유 거리\nPlayerMover2D의 boundaryMargin과 같게 맞추세요")]
    [Min(0f)]
    [SerializeField] private float mapBoundaryMargin = 0.5f;

    [Tooltip("넉백 최종 위치를 맵 경계 안으로 제한할지 여부")]
    [SerializeField] private bool clampKnockbackToMapBounds = true;

    [Header("===== 디버그 설정 =====")]
    [Tooltip("체크하면 돌진 상태 로그를 출력합니다.")]
    [SerializeField] private bool showDebug = true;

    [Tooltip("체크하면 앞쪽 판정 Gizmos를 표시합니다.")]
    [SerializeField] private bool showGizmos = true;

    private bool isPreparingCharge;
    private bool isCharging;
    private bool hasHitPlayerThisCharge;

    private float prepareTimer;
    private float chargeTimer;
    private float cooldownTimer;

    private Vector2 chargeDir;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (chaseController == null)
            chaseController = GetComponent<DuryeoksiniBossChase2D>();

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
        }

        if (playerLayerMask.value == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
                playerLayerMask = 1 << playerLayer;
        }

        if (mapBoundsCollider == null)
        {
            GameObject found = GameObject.Find("MapBounds2D");
            if (found != null)
                mapBoundsCollider = found.GetComponent<Collider2D>();
        }
    }

    private void Start()
    {
        TryFindPlayer();
    }

    private void Update()
    {
        if (player == null)
        {
            TryFindPlayer();
            return;
        }

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (isPreparingCharge)
        {
            prepareTimer -= Time.deltaTime;

            if (prepareTimer <= 0f)
                BeginCharge();

            return;
        }

        if (isCharging)
        {
            chargeTimer -= Time.deltaTime;
            CheckFrontHit();

            if (chargeTimer <= 0f)
                StopCharge();

            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance >= chargeDistance && cooldownTimer <= 0f)
            StartPrepareCharge();
    }

    private void FixedUpdate()
    {
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

        chargeDir = ((Vector2)(player.position - transform.position)).normalized;

        if (chargeDir.sqrMagnitude <= 0.0001f)
            chargeDir = Vector2.right;

        isPreparingCharge = true;
        isCharging = false;
        hasHitPlayerThisCharge = false;
        prepareTimer = chargePrepareTime;

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
        if (hitOnlyOncePerCharge && hasHitPlayerThisCharge)
            return;

        Vector2 hitCenter = GetFrontHitCenter();
        Collider2D target = Physics2D.OverlapCircle(hitCenter, hitRadius, playerLayerMask);

        if (target == null)
            return;

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

        PlayerHealth hp = target.GetComponent<PlayerHealth>();
        if (hp == null)
            hp = target.GetComponentInParent<PlayerHealth>();

        if (hp != null)
            hp.TakeDamage(damage);

        Vector2 dir = ((Vector2)(target.transform.position - transform.position)).normalized;

        if (dir.sqrMagnitude <= 0.0001f)
            dir = chargeDir.sqrMagnitude > 0.0001f ? chargeDir : Vector2.right;

        Vector2 finalDir = (dir + Vector2.up * knockbackUpBias).normalized;

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
        if (playerTransform == null || playerRb == null || playerCol == null)
            yield break;

        if (disablePlayerMoverDuringKnockback && mover != null)
            mover.enabled = false;

        playerRb.linearVelocity = Vector2.zero;

        yield return new WaitForFixedUpdate();

        Vector2 startPos = playerRb.position;
        Vector2 finalEndPos = startPos + knockDir.normalized * knockbackDistance;

        if (useSafeCastMove)
            finalEndPos = GetSmartKnockbackEndPosition(startPos, playerCol, knockDir, knockbackDistance);

        finalEndPos = ClampPositionToMapBounds(finalEndPos);

        float elapsed = 0f;
        Vector2 currentSafePos = startPos;

        while (elapsed < knockbackDuration)
        {
            yield return new WaitForFixedUpdate();

            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, knockbackDuration));
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            Vector2 rawNextPos = Vector2.Lerp(startPos, finalEndPos, eased);
            rawNextPos = ClampPositionToMapBounds(rawNextPos);

            Vector2 moveDelta = rawNextPos - currentSafePos;
            float moveDistance = moveDelta.magnitude;

            Vector2 safeNextPos = currentSafePos;

            if (moveDistance > 0.0001f)
            {
                float allowedDistance = moveDistance;

                if (useSafeCastMove)
                    allowedDistance = GetAllowedMoveDistance(playerCol, moveDelta.normalized, moveDistance);

                safeNextPos = currentSafePos + moveDelta.normalized * allowedDistance;
            }

            safeNextPos = ClampPositionToMapBounds(safeNextPos);

            playerRb.linearVelocity = Vector2.zero;
            playerRb.MovePosition(safeNextPos);

            currentSafePos = safeNextPos;
        }

        currentSafePos = ClampPositionToMapBounds(currentSafePos);

        playerRb.linearVelocity = Vector2.zero;
        playerRb.MovePosition(currentSafePos);

        yield return new WaitForFixedUpdate();

        playerRb.linearVelocity = Vector2.zero;
        playerRb.position = ClampPositionToMapBounds(playerRb.position);

        yield return new WaitForFixedUpdate();

        if (disablePlayerMoverDuringKnockback && mover != null)
            mover.enabled = true;
    }

    private Vector2 GetSmartKnockbackEndPosition(Vector2 startPos, Collider2D playerCol, Vector2 knockDir, float distance)
    {
        knockDir = knockDir.normalized;

        float mainAllowed = GetAllowedMoveDistance(playerCol, knockDir, distance);
        Vector2 bestDir = knockDir;
        float bestDistance = mainAllowed;

        if (useSideBounceWhenBlocked && mainAllowed < distance - 0.01f)
        {
            Vector2 leftDir = Vector2.Perpendicular(knockDir).normalized;
            Vector2 rightDir = -leftDir;

            Vector2 leftMixDir = (knockDir * (1f - sideBounceWeight) + leftDir * sideBounceWeight).normalized;
            Vector2 rightMixDir = (knockDir * (1f - sideBounceWeight) + rightDir * sideBounceWeight).normalized;

            float leftAllowed = GetAllowedMoveDistance(playerCol, leftMixDir, distance);
            float rightAllowed = GetAllowedMoveDistance(playerCol, rightMixDir, distance);

            if (leftAllowed > bestDistance)
            {
                bestDistance = leftAllowed;
                bestDir = leftMixDir;
            }

            if (rightAllowed > bestDistance)
            {
                bestDistance = rightAllowed;
                bestDir = rightMixDir;
            }

            if (bestDistance <= 0.01f)
            {
                float pureLeftAllowed = GetAllowedMoveDistance(playerCol, leftDir, distance * 0.8f);
                float pureRightAllowed = GetAllowedMoveDistance(playerCol, rightDir, distance * 0.8f);

                if (pureLeftAllowed > bestDistance)
                {
                    bestDistance = pureLeftAllowed;
                    bestDir = leftDir;
                }

                if (pureRightAllowed > bestDistance)
                {
                    bestDistance = pureRightAllowed;
                    bestDir = rightDir;
                }
            }
        }

        Vector2 result = startPos + bestDir * bestDistance;
        return ClampPositionToMapBounds(result);
    }

    private float GetAllowedMoveDistance(Collider2D playerCol, Vector2 moveDir, float moveDistance)
    {
        if (playerCol == null || moveDistance <= 0f)
            return 0f;

        RaycastHit2D[] hits = new RaycastHit2D[8];
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = obstacleLayerMask;
        filter.useTriggers = false;

        int hitCount = playerCol.Cast(moveDir.normalized, filter, hits, moveDistance);

        if (hitCount <= 0)
            return moveDistance;

        float nearest = moveDistance;

        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].collider == null)
                continue;

            if (hits[i].distance < nearest)
                nearest = hits[i].distance;
        }

        return Mathf.Max(0f, nearest - obstacleSkin);
    }

    private Vector2 ClampPositionToMapBounds(Vector2 worldPos)
    {
        if (!clampKnockbackToMapBounds || mapBoundsCollider == null)
            return worldPos;

        Bounds b = mapBoundsCollider.bounds;

        float minX = b.min.x + mapBoundaryMargin;
        float maxX = b.max.x - mapBoundaryMargin;
        float minY = b.min.y + mapBoundaryMargin;
        float maxY = b.max.y - mapBoundaryMargin;

        if (minX > maxX)
        {
            float midX = (minX + maxX) * 0.5f;
            minX = midX;
            maxX = midX;
        }

        if (minY > maxY)
        {
            float midY = (minY + maxY) * 0.5f;
            minY = midY;
            maxY = midY;
        }

        worldPos.x = Mathf.Clamp(worldPos.x, minX, maxX);
        worldPos.y = Mathf.Clamp(worldPos.y, minY, maxY);

        return worldPos;
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

        if (mapBoundsCollider != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Bounds b = mapBoundsCollider.bounds;

            float minX = b.min.x + mapBoundaryMargin;
            float maxX = b.max.x - mapBoundaryMargin;
            float minY = b.min.y + mapBoundaryMargin;
            float maxY = b.max.y - mapBoundaryMargin;

            Vector3 boxCenter = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
            Vector3 boxSize = new Vector3(Mathf.Max(0f, maxX - minX), Mathf.Max(0f, maxY - minY), 0f);
            Gizmos.DrawWireCube(boxCenter, boxSize);
        }
    }
}

