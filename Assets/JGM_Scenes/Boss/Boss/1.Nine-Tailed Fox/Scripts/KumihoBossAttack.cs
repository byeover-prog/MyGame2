using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class KumihoBasicAttack : MonoBehaviour
{
    [Header("===== 기본 참조 =====")]

    [Tooltip("플레이어 Transform")]
    [SerializeField] private Transform playerTarget;

    [Tooltip("투사체 생성 위치")]
    [SerializeField] private Transform firePoint;

    [Tooltip("투사체 프리팹")]
    [SerializeField] private GameObject fireballPrefab;

    [Tooltip("보스 Animator")]
    [SerializeField] private Animator animator;

    [Header("===== 공격 설정 =====")]

    [Tooltip("공격 간격")]
    [SerializeField] private float attackInterval = 1.5f;

    [Tooltip("공격 가능 거리")]
    [SerializeField] private float attackRange = 12f;

    [Tooltip("3발 발사 시 퍼지는 각도")]
    [SerializeField] private float spreadAngle = 12f;

    [Header("===== 현재 상태 =====")]

    [Tooltip("현재 발사되는 투사체 수")]
    [SerializeField] private int currentProjectileCount = 1;

    private float attackTimer;

    public int CurrentProjectileCount => currentProjectileCount;

    void Reset()
    {
        attackInterval = 1.5f;
        attackRange = 12f;
        spreadAngle = 12f;
        currentProjectileCount = 1;
    }

    void Update()
    {
        if (playerTarget == null || firePoint == null || fireballPrefab == null)
            return;

        float distance = Vector2.Distance(transform.position, playerTarget.position);

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
        // 공격 애니메이션 실행
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // 애니메이션 타이밍 맞추기
        yield return new WaitForSeconds(0.3f);

        Fire();
    }

    public void SetProjectileCount(int count)
    {
        currentProjectileCount = Mathf.Max(1, count);
    }

    void Fire()
    {
        Vector2 baseDir = (playerTarget.position - firePoint.position).normalized;

        if (currentProjectileCount == 1)
        {
            SpawnProjectile(baseDir);
            return;
        }

        int half = currentProjectileCount / 2;

        for (int i = -half; i <= half; i++)
        {
            float angleOffset = i * spreadAngle;
            Vector2 shotDir = RotateVector(baseDir, angleOffset);
            SpawnProjectile(shotDir);
        }
    }

    void SpawnProjectile(Vector2 direction)
    {
        GameObject projectile = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);

        KumihoFireball fireball = projectile.GetComponent<KumihoFireball>();

        if (fireball != null)
        {
            fireball.Init(direction);
        }
    }

    Vector2 RotateVector(Vector2 dir, float angle)
    {
        float rad = angle * Mathf.Deg2Rad;

        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        float x = dir.x * cos - dir.y * sin;
        float y = dir.x * sin + dir.y * cos;

        return new Vector2(x, y).normalized;
    }

    public void SetPlayerTarget(Transform target)
    {
        playerTarget = target;
    }
}