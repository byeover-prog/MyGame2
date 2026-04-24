// UTF-8
using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class GrimReaperBasicAttack : MonoBehaviour
{
    [Header("===== 기본 참조 =====")]
    [Tooltip("플레이어 Transform\n비워두면 Player 태그 자동 탐색")]
    [SerializeField] private Transform playerTarget;

    [Tooltip("저승사자 Animator")]
    [SerializeField] private Animator animator;

    [Tooltip("좌우 반전용 SpriteRenderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;


    [Header("===== 공격 설정 =====")]
    [Tooltip("공격 가능 거리")]
    [SerializeField] private float attackRange = 3f;

    [Tooltip("플레이어에게 들어갈 데미지")]
    [SerializeField] private int attackDamage = 15;

    [Tooltip("한 번 공격 후 다음 공격까지 대기 시간")]
    [SerializeField] private float attackInterval = 0.2f;

    [Tooltip("공격 시작 후 실제 데미지가 들어가기까지의 시간")]
    [SerializeField] private float damageDelay = 0.33f;

    [Tooltip("공격 애니메이션 전체 재생 시간")]
    [SerializeField] private float attackDuration = 0.6f;

    [Tooltip("공격 중에도 플레이어 방향을 계속 보정할지 여부")]
    [SerializeField] private bool updateFlipWhileAttacking = true;

    [Tooltip("공격 중 플레이어가 범위를 벗어나도 이번 공격 데미지를 유지할지 여부")]
    [SerializeField] private bool lockHitByAttackStart = true;

    [Tooltip("콘솔에 공격 로그를 출력할지 여부")]
    [SerializeField] private bool showDebugLog = true;


    [Header("===== 현재 상태 (디버그용) =====")]
    [Tooltip("현재 공격 중인지 여부")]
    [SerializeField] private bool isAttacking;

    [Tooltip("현재 플레이어와의 거리")]
    [SerializeField] private float currentDistance;

    [Tooltip("현재 공격 1사이클에서 데미지를 이미 적용했는지 여부")]
    [SerializeField] private bool hasAppliedDamageThisCycle;


    // 이번 공격 시작 시점에 범위 안이었는지 잠금
    private bool wasPlayerInRangeAtAttackStart;

    private Coroutine attackCoroutine;


    private void Start()
    {
        FindPlayerIfNeeded();
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            FindPlayerIfNeeded();
            return;
        }

        currentDistance = Vector2.Distance(transform.position, playerTarget.position);

        if (updateFlipWhileAttacking || !isAttacking)
        {
            UpdateSpriteFlip();
        }

        if (isAttacking)
            return;

        if (currentDistance <= attackRange)
        {
            if (attackCoroutine == null)
            {
                attackCoroutine = StartCoroutine(AttackRoutine());
            }
        }
    }

    private void FindPlayerIfNeeded()
    {
        if (playerTarget != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
    }

    private void UpdateSpriteFlip()
    {
        if (playerTarget == null || spriteRenderer == null)
            return;

        float diffX = playerTarget.position.x - transform.position.x;

        if (Mathf.Abs(diffX) < 0.01f)
            return;

        spriteRenderer.flipX = diffX < 0f;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        hasAppliedDamageThisCycle = false;

        wasPlayerInRangeAtAttackStart = false;

        if (playerTarget != null)
        {
            float startDistance = Vector2.Distance(transform.position, playerTarget.position);
            wasPlayerInRangeAtAttackStart = startDistance <= attackRange;
        }

        if (showDebugLog)
        {
            Debug.Log($"[저승사자] 공격 시작 | 시작거리: {currentDistance:F2} | 시작범위내: {wasPlayerInRangeAtAttackStart}");
        }

        if (animator != null)
        {
            animator.ResetTrigger("attack");
            animator.SetTrigger("attack");
        }

        if (damageDelay > 0f)
            yield return new WaitForSeconds(damageDelay);

        TryApplyDamageOnce();

        float remainTime = attackDuration - damageDelay;
        if (remainTime > 0f)
            yield return new WaitForSeconds(remainTime);

        if (attackInterval > 0f)
            yield return new WaitForSeconds(attackInterval);

        if (showDebugLog)
        {
            Debug.Log($"[저승사자] 공격 종료 | 데미지적용여부: {hasAppliedDamageThisCycle}");
        }

        hasAppliedDamageThisCycle = false;
        isAttacking = false;
        attackCoroutine = null;
    }

    private void TryApplyDamageOnce()
    {
        if (hasAppliedDamageThisCycle)
            return;

        if (playerTarget == null)
        {
            if (showDebugLog)
            {
                Debug.Log("[저승사자] 데미지 실패 | playerTarget 없음");
            }
            return;
        }

        bool canHit = false;

        if (lockHitByAttackStart)
        {
            canHit = wasPlayerInRangeAtAttackStart;
        }
        else
        {
            float distance = Vector2.Distance(transform.position, playerTarget.position);
            canHit = distance <= attackRange;
        }

        if (!canHit)
        {
            if (showDebugLog)
            {
                float distance = Vector2.Distance(transform.position, playerTarget.position);
                Debug.Log($"[저승사자] 데미지 실패 | 현재거리: {distance:F2} | 공격범위: {attackRange}");
            }
            return;
        }

        PlayerHealth hp = playerTarget.GetComponent<PlayerHealth>();
        if (hp == null)
        {
            if (showDebugLog)
            {
                Debug.Log("[저승사자] 데미지 실패 | PlayerHealth 없음");
            }
            return;
        }

        hp.TakeDamage(attackDamage);
        hasAppliedDamageThisCycle = true;

        if (showDebugLog)
        {
            Debug.Log($"[저승사자] 데미지 적용 성공 | 데미지: {attackDamage}");
        }
    }

    public void SetPlayerTarget(Transform target)
    {
        playerTarget = target;
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }

    private void OnDisable()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        isAttacking = false;
        hasAppliedDamageThisCycle = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
