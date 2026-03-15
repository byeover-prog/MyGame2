using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class DuryeoksiniBasicAttack : MonoBehaviour
{
    /* =========================================================
     * 두억시니 보스 기본 공격
     * - 플레이어 근접 시 공격
     * - 공격 애니메이션 실행
     * - 애니메이션 동안 여러 번 데미지 적용
     * ========================================================= */

    [Header("===== 기본 참조 =====")]

    [Tooltip("플레이어 Transform\n비워두면 Player 태그 자동 탐색")]
    [SerializeField] private Transform playerTarget;

    [Tooltip("두억시니 Animator")]
    [SerializeField] private Animator animator;


    [Header("===== 공격 설정 =====")]

    [Tooltip("공격 간격 (초)")]
    [SerializeField] private float attackInterval = 2f;

    [Tooltip("공격 가능 거리")]
    [SerializeField] private float attackRange = 2f;

    [Tooltip("플레이어에게 들어갈 데미지")]
    [SerializeField] private int attackDamage = 20;

    [Tooltip("한 번의 공격 애니메이션에서 타격 횟수")]
    [SerializeField] private int attackHitCount = 3;

    [Tooltip("타격 사이 시간 간격")]
    [SerializeField] private float hitInterval = 0.25f;


    [Header("===== 현재 상태 (디버그용) =====")]

    [Tooltip("현재 공격 중인지 여부")]
    [SerializeField] private bool isAttacking;

    private float attackTimer;


    void Start()
    {
        // 플레이어 자동 탐색
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                playerTarget = player.transform;
        }
    }


    void Update()
    {
        if (playerTarget == null)
            return;

        // 공격 중이면 다시 공격하지 않음
        if (isAttacking)
            return;

        float distance = Vector2.Distance(transform.position, playerTarget.position);

        // 공격 거리 밖이면 공격 안함
        if (distance > attackRange)
            return;

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            StartCoroutine(AttackRoutine());
        }
    }


    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // 공격 애니메이션 실행
        if (animator != null)
            animator.SetTrigger("attack");

        // 공격 애니메이션 동안 여러 번 타격
        for (int i = 0; i < attackHitCount; i++)
        {
            yield return new WaitForSeconds(hitInterval);
            ApplyDamage();
        }

        // 다음 공격 대기
        yield return new WaitForSeconds(attackInterval);

        isAttacking = false;
    }


    void ApplyDamage()
    {
        if (playerTarget == null)
            return;

        float distance = Vector2.Distance(transform.position, playerTarget.position);

        // 공격 범위 안에 있을 때만 데미지
        if (distance <= attackRange)
        {
            PlayerHealth hp = playerTarget.GetComponent<PlayerHealth>();

            if (hp != null)
            {
                hp.TakeDamage(attackDamage);
            }
        }
    }


    public void SetPlayerTarget(Transform target)
    {
        playerTarget = target;
    }
}