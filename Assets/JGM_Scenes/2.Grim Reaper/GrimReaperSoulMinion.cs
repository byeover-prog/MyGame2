// UTF-8
using System.Collections;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 망혼 부활로 소환된 망령의 동작입니다.
/// - 땅속에서 올라오는 짧은 등장 연출
/// - 플레이어 추적
/// - 근접 거리에서 반복 공격
/// - 일정 시간 후 자동 소멸
/// - 소환한 저승사자가 죽거나 비활성화되면 즉시 소멸
/// </summary>
[DisallowMultipleComponent]
public class GrimReaperSoulMinion : MonoBehaviour
{
    [Header("===== 기본 참조 =====")]
    [Tooltip("플레이어 Transform입니다.")]
    [SerializeField] private Transform playerTarget;

    [Tooltip("망령 Animator입니다.\n없으면 비워도 됩니다.")]
    [SerializeField] private Animator animator;

    [Tooltip("좌우 반전용 SpriteRenderer입니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("===== 망령 설정 =====")]
    [Tooltip("망령 이동 속도입니다.")]
    [SerializeField] private float moveSpeed = 2.4f;

    [Tooltip("망령 공격 거리입니다.")]
    [SerializeField] private float attackRange = 1.4f;

    [Tooltip("망령 공격 데미지입니다.")]
    [SerializeField] private int attackDamage = 8;

    [Tooltip("망령 공격 간격입니다.")]
    [SerializeField] private float attackInterval = 0.9f;

    [Tooltip("등장 후 행동 시작 전 대기 시간입니다.")]
    [SerializeField] private float riseDelay = 0.5f;

    [Tooltip("망령 유지 시간입니다.")]
    [SerializeField] private float lifeTime = 8f;

    [Tooltip("등장 연출 시 위로 올라올 높이값입니다.")]
    [SerializeField] private float spawnYOffset = 0.8f;

    [Header("===== 내부 상태 =====")]
    [Tooltip("현재 행동 가능 상태인지 표시합니다.")]
    [SerializeField] private bool isActive;

    [Tooltip("현재 공격 중인지 표시합니다.")]
    [SerializeField] private bool isAttacking;

    [Tooltip("소환한 저승사자입니다.")]
    [SerializeField] private Transform owner;

    private float attackTimer;
    private Vector3 spawnBasePosition;
    private Coroutine attackCoroutine;
    private bool isDestroying;

    public void Setup(
        Transform owner,
        Transform player,
        float moveSpeed,
        float attackRange,
        int attackDamage,
        float attackInterval,
        float riseDelay,
        float lifeTime,
        float spawnYOffset
    )
    {
        this.owner = owner;
        this.playerTarget = player;
        this.moveSpeed = moveSpeed;
        this.attackRange = attackRange;
        this.attackDamage = attackDamage;
        this.attackInterval = attackInterval;
        this.riseDelay = riseDelay;
        this.lifeTime = lifeTime;
        this.spawnYOffset = spawnYOffset;
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                playerTarget = playerObject.transform;
        }

        spawnBasePosition = transform.position;
        StartCoroutine(CoLifeRoutine());
    }

    private void Update()
    {
        // 한글 주석: 소환한 저승사자가 죽었거나 꺼졌으면 망령도 즉시 제거
        if (IsOwnerGone())
        {
            DestroySelf();
            return;
        }

        if (!isActive)
            return;

        if (playerTarget == null)
            return;

        attackTimer += Time.deltaTime;

        FacePlayer();

        float distance = Vector2.Distance(transform.position, playerTarget.position);

        if (!isAttacking && distance <= attackRange)
        {
            if (attackTimer >= attackInterval)
            {
                attackCoroutine = StartCoroutine(CoAttack());
            }

            return;
        }

        if (!isAttacking)
        {
            MoveToPlayer();
        }
    }

    private IEnumerator CoLifeRoutine()
    {
        Vector3 startPosition = spawnBasePosition;
        Vector3 endPosition = spawnBasePosition + Vector3.up * spawnYOffset;

        float riseTime = Mathf.Max(0.05f, riseDelay);
        float t = 0f;

        while (t < riseTime)
        {
            // 한글 주석: 등장 연출 중에도 보스가 죽었는지 계속 확인
            if (IsOwnerGone())
            {
                DestroySelf();
                yield break;
            }

            t += Time.deltaTime;
            float lerp = t / riseTime;
            transform.position = Vector3.Lerp(startPosition, endPosition, lerp);
            yield return null;
        }

        transform.position = endPosition;
        isActive = true;

        float lifeTimer = 0f;
        while (lifeTimer < lifeTime)
        {
            // 한글 주석: 수명 대기 중에도 보스가 죽었으면 즉시 제거
            if (IsOwnerGone())
            {
                DestroySelf();
                yield break;
            }

            lifeTimer += Time.deltaTime;
            yield return null;
        }

        DestroySelf();
    }

    private void MoveToPlayer()
    {
        if (playerTarget == null)
            return;

        Vector2 direction = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        Vector2 next = (Vector2)transform.position + direction * moveSpeed * Time.deltaTime;
        transform.position = next;

        if (animator != null)
        {
            animator.SetBool("IsMove", true);
        }
    }

    private IEnumerator CoAttack()
    {
        isAttacking = true;
        attackTimer = 0f;

        if (animator != null)
        {
            animator.SetTrigger("attack");
            animator.SetBool("IsMove", false);
        }

        yield return new WaitForSeconds(0.2f);

        // 한글 주석: 공격 직전 보스가 죽었으면 공격하지 않고 제거
        if (IsOwnerGone())
        {
            DestroySelf();
            yield break;
        }

        if (playerTarget != null)
        {
            float distance = Vector2.Distance(transform.position, playerTarget.position);
            if (distance <= attackRange + 0.2f)
            {
                PlayerHealth playerHealth = playerTarget.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                }
            }
        }

        yield return new WaitForSeconds(0.25f);

        isAttacking = false;
        attackCoroutine = null;
    }

    private void FacePlayer()
    {
        if (spriteRenderer == null || playerTarget == null)
            return;

        if (playerTarget.position.x < transform.position.x)
            spriteRenderer.flipX = true;
        else
            spriteRenderer.flipX = false;
    }

    private bool IsOwnerGone()
    {
        // 한글 주석: owner가 파괴되었거나 비활성화되었으면 true
        if (owner == null)
            return true;

        if (!owner.gameObject.activeInHierarchy)
            return true;

        return false;
    }

    private void DestroySelf()
    {
        if (isDestroying)
            return;

        isDestroying = true;
        isActive = false;
        isAttacking = false;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        Destroy(gameObject);
    }
}