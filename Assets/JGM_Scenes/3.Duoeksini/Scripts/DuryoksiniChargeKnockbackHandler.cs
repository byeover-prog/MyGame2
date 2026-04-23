// UTF-8
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 구현 원리 요약:
// 두억시니가 플레이어를 밀어낼 때 사용하는 공통 넉백 처리기다.
// 플레이어 쪽 코드는 수정하지 않고, 두억시니 쪽에서만 넉백 시작/종료를 관리한다.
// 벽 충돌, 맵 경계, 좌우 보정은 유지하되 넉백 중 제어 비활성화/복구 흐름을 더 안전하게 정리한다.

[DisallowMultipleComponent]
public sealed class DuryoksiniChargeKnockbackHandler : MonoBehaviour
{
    [Header("기본 공격 넉백")]

    [Tooltip("기본 공격 넉백 기본 거리")]
    [Min(0.1f)]
    [SerializeField] private float defaultBasicAttackDistance = 1.2f;

    [Tooltip("기본 공격 넉백 기본 시간")]
    [Min(0.01f)]
    [SerializeField] private float defaultBasicAttackDuration = 0.10f;

    [Tooltip("기본 공격 넉백 시 위로 살짝 뜨는 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultBasicAttackUpBias = 0.12f;


    [Header("돌진 넉백")]

    [Tooltip("돌진 넉백 기본 거리")]
    [Min(0.1f)]
    [SerializeField] private float defaultChargeDistance = 1.6f;

    [Tooltip("돌진 넉백 기본 시간")]
    [Min(0.01f)]
    [SerializeField] private float defaultChargeDuration = 0.12f;

    [Tooltip("돌진 넉백 시 위로 살짝 뜨는 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultChargeUpBias = 0.2f;


    [Header("내려찍기 넉백")]

    [Tooltip("내려찍기 넉백 기본 거리")]
    [Min(0.1f)]
    [SerializeField] private float defaultGroundSmashDistance = 1.4f;

    [Tooltip("내려찍기 넉백 기본 시간")]
    [Min(0.01f)]
    [SerializeField] private float defaultGroundSmashDuration = 0.12f;

    [Tooltip("내려찍기 넉백 시 위로 살짝 뜨는 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultGroundSmashUpBias = 0.18f;


    [Header("플레이어 제어 잠금")]

    [Tooltip("넉백 중 PlayerMover2D 계열 스크립트를 잠시 꺼둘지 여부")]
    [SerializeField] private bool disablePlayerMoverDuringKnockback = true;

    [Tooltip("플레이어 이동 스크립트 이름 후보 목록")]
    [SerializeField] private string[] playerMoverTypeNames = { "PlayerMover2D" };


    [Header("장애물 보정")]

    [Tooltip("넉백 중 막혀야 하는 장애물 레이어")]
    [SerializeField] private LayerMask obstacleLayerMask;

    [Tooltip("장애물 바로 앞에 멈출 여유 거리")]
    [Min(0f)]
    [SerializeField] private float obstacleSkin = 0.05f;

    [Tooltip("정면이 막히면 좌우 보정 방향을 검사할지 여부")]
    [SerializeField] private bool useSideBounceWhenBlocked = true;

    [Tooltip("좌우 보정 강도")]
    [Range(0f, 1f)]
    [SerializeField] private float sideBounceWeight = 0.85f;


    [Header("맵 경계 보정")]

    [Tooltip("맵 경계 Collider2D\n비어 있으면 MapBounds2D를 자동 탐색한다.")]
    [SerializeField] private Collider2D mapBoundsCollider;

    [Tooltip("맵 경계 안쪽 여유 거리")]
    [Min(0f)]
    [SerializeField] private float mapBoundaryMargin = 0.5f;

    [Tooltip("넉백 위치를 맵 경계 안으로 제한할지 여부")]
    [SerializeField] private bool clampToMapBounds = true;


    [Header("디버그")]

    [Tooltip("디버그 로그 출력 여부")]
    [SerializeField] private bool debugLog = false;


    private Coroutine knockbackRoutine;
    private Rigidbody2D currentTargetRigidbody;
    private readonly List<MonoBehaviour> disabledBehaviours = new List<MonoBehaviour>();


    private void Awake()
    {
        TryFindMapBounds();
    }

    private void OnDisable()
    {
        StopCurrentKnockbackAndRestore();
    }

    public void ApplyBasicAttackKnockback(Collider2D targetCollider, Vector2 attackOrigin, float distance, float duration, float upBias)
    {
        float finalDistance = distance > 0f ? distance : defaultBasicAttackDistance;
        float finalDuration = duration > 0f ? duration : defaultBasicAttackDuration;
        float finalUpBias = upBias > 0f ? upBias : defaultBasicAttackUpBias;

        ApplyPatternKnockback(targetCollider, attackOrigin, finalDistance, finalDuration, finalUpBias, "기본 공격");
    }

    public void ApplyCrushChargeKnockback(Collider2D targetCollider, Vector2 attackOrigin, float distance, float duration, float upBias)
    {
        float finalDistance = distance > 0f ? distance : defaultChargeDistance;
        float finalDuration = duration > 0f ? duration : defaultChargeDuration;
        float finalUpBias = upBias > 0f ? upBias : defaultChargeUpBias;

        ApplyPatternKnockback(targetCollider, attackOrigin, finalDistance, finalDuration, finalUpBias, "파쇄 돌진");
    }

    public void ApplyGroundSmashKnockback(Collider2D targetCollider, Vector2 attackOrigin, float distance, float duration, float upBias)
    {
        float finalDistance = distance > 0f ? distance : defaultGroundSmashDistance;
        float finalDuration = duration > 0f ? duration : defaultGroundSmashDuration;
        float finalUpBias = upBias > 0f ? upBias : defaultGroundSmashUpBias;

        ApplyPatternKnockback(targetCollider, attackOrigin, finalDistance, finalDuration, finalUpBias, "내려찍기");
    }

    private void ApplyPatternKnockback(
        Collider2D targetCollider,
        Vector2 attackOrigin,
        float distance,
        float duration,
        float upBias,
        string patternName)
    {
        if (targetCollider == null)
        {
            return;
        }

        Rigidbody2D targetRb = GetTargetRigidbody(targetCollider);
        if (targetRb == null)
        {
            return;
        }

        Vector2 attackDirection = ((Vector2)targetCollider.bounds.center - attackOrigin).normalized;

        if (attackDirection.sqrMagnitude <= 0.0001f)
        {
            float xDirection = targetCollider.transform.position.x >= transform.position.x ? 1f : -1f;
            attackDirection = new Vector2(xDirection, 0f);
        }

        Vector2 finalDirection = (attackDirection + Vector2.up * upBias).normalized;

        StopCurrentKnockbackAndRestore();

        currentTargetRigidbody = targetRb;
        knockbackRoutine = StartCoroutine(ApplyKnockbackRoutine(
            targetRb,
            targetCollider,
            finalDirection,
            distance,
            duration));

        if (debugLog)
        {
            Debug.Log($"[DuryoksiniChargeKnockbackHandler] {patternName} 넉백 시작 | target={targetCollider.name}", this);
        }
    }

    private IEnumerator ApplyKnockbackRoutine(
        Rigidbody2D targetRb,
        Collider2D targetCollider,
        Vector2 direction,
        float distance,
        float duration)
    {
        if (targetRb == null || targetCollider == null)
        {
            yield break;
        }

        CacheAndDisablePlayerMovers(targetRb);

        targetRb.linearVelocity = Vector2.zero;
        yield return new WaitForFixedUpdate();

        Vector2 startPos = targetRb.position;
        Vector2 targetPos = GetSmartEndPosition(startPos, targetCollider, direction, distance);
        targetPos = ClampPositionToMapBounds(targetPos);

        float elapsed = 0f;
        Vector2 currentPos = startPos;

        while (elapsed < duration)
        {
            yield return new WaitForFixedUpdate();

            if (targetRb == null || targetCollider == null)
            {
                break;
            }

            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            Vector2 rawNextPos = Vector2.Lerp(startPos, targetPos, eased);
            rawNextPos = ClampPositionToMapBounds(rawNextPos);

            Vector2 moveDelta = rawNextPos - currentPos;
            float moveDistance = moveDelta.magnitude;

            Vector2 safeNextPos = currentPos;

            if (moveDistance > 0.0001f)
            {
                float allowedDistance = GetAllowedMoveDistance(targetCollider, moveDelta.normalized, moveDistance);
                safeNextPos = currentPos + moveDelta.normalized * allowedDistance;
            }

            safeNextPos = ClampPositionToMapBounds(safeNextPos);

            targetRb.linearVelocity = Vector2.zero;
            targetRb.MovePosition(safeNextPos);

            currentPos = safeNextPos;
        }

        if (targetRb != null)
        {
            targetRb.linearVelocity = Vector2.zero;
            targetRb.MovePosition(ClampPositionToMapBounds(targetRb.position));
        }

        RestoreDisabledBehaviours();

        knockbackRoutine = null;
        currentTargetRigidbody = null;

        if (debugLog)
        {
            Debug.Log("[DuryoksiniChargeKnockbackHandler] 넉백 종료", this);
        }
    }

    private Rigidbody2D GetTargetRigidbody(Collider2D targetCollider)
    {
        if (targetCollider == null)
        {
            return null;
        }

        if (targetCollider.attachedRigidbody != null)
        {
            return targetCollider.attachedRigidbody;
        }

        return targetCollider.GetComponentInParent<Rigidbody2D>();
    }

    private void CacheAndDisablePlayerMovers(Rigidbody2D targetRb)
    {
        disabledBehaviours.Clear();

        if (!disablePlayerMoverDuringKnockback || targetRb == null)
        {
            return;
        }

        MonoBehaviour[] behaviours = targetRb.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || !behaviour.enabled)
            {
                continue;
            }

            if (IsPlayerMoverBehaviour(behaviour))
            {
                behaviour.enabled = false;
                disabledBehaviours.Add(behaviour);

                if (debugLog)
                {
                    Debug.Log($"[DuryoksiniChargeKnockbackHandler] 플레이어 제어 비활성화 | {behaviour.GetType().Name}", this);
                }
            }
        }
    }

    private bool IsPlayerMoverBehaviour(MonoBehaviour behaviour)
    {
        if (behaviour == null)
        {
            return false;
        }

        string typeName = behaviour.GetType().Name;

        if (playerMoverTypeNames == null || playerMoverTypeNames.Length == 0)
        {
            return typeName == "PlayerMover2D";
        }

        for (int i = 0; i < playerMoverTypeNames.Length; i++)
        {
            string candidate = playerMoverTypeNames[i];
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (typeName == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private void RestoreDisabledBehaviours()
    {
        for (int i = 0; i < disabledBehaviours.Count; i++)
        {
            MonoBehaviour behaviour = disabledBehaviours[i];
            if (behaviour == null)
            {
                continue;
            }

            behaviour.enabled = true;

            if (debugLog)
            {
                Debug.Log($"[DuryoksiniChargeKnockbackHandler] 플레이어 제어 복구 | {behaviour.GetType().Name}", this);
            }
        }

        disabledBehaviours.Clear();
    }

    private void StopCurrentKnockbackAndRestore()
    {
        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            knockbackRoutine = null;
        }

        if (currentTargetRigidbody != null)
        {
            currentTargetRigidbody.linearVelocity = Vector2.zero;
            currentTargetRigidbody.position = ClampPositionToMapBounds(currentTargetRigidbody.position);
            currentTargetRigidbody = null;
        }

        RestoreDisabledBehaviours();
    }

    private Vector2 GetSmartEndPosition(Vector2 startPos, Collider2D targetCollider, Vector2 direction, float distance)
    {
        direction = direction.normalized;

        float mainAllowed = GetAllowedMoveDistance(targetCollider, direction, distance);
        Vector2 bestDirection = direction;
        float bestDistance = mainAllowed;

        if (useSideBounceWhenBlocked && mainAllowed < distance - 0.01f)
        {
            Vector2 leftDir = Vector2.Perpendicular(direction).normalized;
            Vector2 rightDir = -leftDir;

            Vector2 leftMixDir = (direction * (1f - sideBounceWeight) + leftDir * sideBounceWeight).normalized;
            Vector2 rightMixDir = (direction * (1f - sideBounceWeight) + rightDir * sideBounceWeight).normalized;

            float leftAllowed = GetAllowedMoveDistance(targetCollider, leftMixDir, distance);
            float rightAllowed = GetAllowedMoveDistance(targetCollider, rightMixDir, distance);

            if (leftAllowed > bestDistance)
            {
                bestDistance = leftAllowed;
                bestDirection = leftMixDir;
            }

            if (rightAllowed > bestDistance)
            {
                bestDistance = rightAllowed;
                bestDirection = rightMixDir;
            }
        }

        Vector2 result = startPos + bestDirection * bestDistance;
        return ClampPositionToMapBounds(result);
    }

    private float GetAllowedMoveDistance(Collider2D targetCollider, Vector2 moveDirection, float moveDistance)
    {
        if (targetCollider == null || moveDistance <= 0f)
        {
            return 0f;
        }

        RaycastHit2D[] hits = new RaycastHit2D[8];
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = obstacleLayerMask;
        filter.useTriggers = false;

        int hitCount = targetCollider.Cast(moveDirection.normalized, filter, hits, moveDistance);

        if (hitCount <= 0)
        {
            return moveDistance;
        }

        float nearest = moveDistance;

        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].collider == null)
            {
                continue;
            }

            if (hits[i].distance < nearest)
            {
                nearest = hits[i].distance;
            }
        }

        return Mathf.Max(0f, nearest - obstacleSkin);
    }

    private Vector2 ClampPositionToMapBounds(Vector2 worldPos)
    {
        if (!clampToMapBounds || mapBoundsCollider == null)
        {
            return worldPos;
        }

        Bounds bounds = mapBoundsCollider.bounds;

        float minX = bounds.min.x + mapBoundaryMargin;
        float maxX = bounds.max.x - mapBoundaryMargin;
        float minY = bounds.min.y + mapBoundaryMargin;
        float maxY = bounds.max.y - mapBoundaryMargin;

        worldPos.x = Mathf.Clamp(worldPos.x, minX, maxX);
        worldPos.y = Mathf.Clamp(worldPos.y, minY, maxY);

        return worldPos;
    }

    private void TryFindMapBounds()
    {
        if (mapBoundsCollider != null)
        {
            return;
        }

        GameObject found = GameObject.Find("MapBounds2D");
        if (found != null)
        {
            mapBoundsCollider = found.GetComponent<Collider2D>();
        }
    }
}