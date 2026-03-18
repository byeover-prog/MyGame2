using UnityEngine;

[DisallowMultipleComponent]
public class KumihoFireball : MonoBehaviour
{
    /* =========================================================
     * 구미호 보스 투사체 (여우불 / 에너지 구슬)
     * - 보스가 플레이어를 향해 발사하는 원거리 공격
     * - Trigger 충돌로 플레이어 데미지 처리
     * - 일정 시간이 지나면 자동 제거
     * ========================================================= */

    [Header("===== 이동 설정 =====")]

    [Tooltip("투사체 이동 속도")]
    [SerializeField] private float speed = 7f;

    [Tooltip("투사체 생존 시간 (초)")]
    [SerializeField] private float lifeTime = 5f;


    [Header("===== 공격 설정 =====")]

    [Tooltip("플레이어에게 줄 피해량")]
    [SerializeField] private int damage = 10;


    [Header("===== 디버그 =====")]

    [Tooltip("true면 콘솔에 공격 로그 출력")]
    [SerializeField] private bool showDebug = false;


    // 내부 이동 방향
    private Vector2 direction;


    /* =========================================================
     * 투사체 초기화
     * 보스가 발사할 때 호출
     * ========================================================= */
    public void Init(Vector2 dir)
    {
        direction = dir.normalized;

        // 이동 방향으로 스프라이트 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 일정 시간 후 자동 삭제
        Destroy(gameObject, lifeTime);

        if (showDebug)
            Debug.Log("[구미호 투사체] 발사 방향 : " + direction);
    }


    /* =========================================================
     * 투사체 이동
     * Rigidbody 없이 직접 이동
     * ========================================================= */
    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }


    /* =========================================================
     * 플레이어 충돌 판정
     * ========================================================= */
    void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        PlayerHealth hp = col.GetComponent<PlayerHealth>();

        if (hp != null)
        {
            hp.TakeDamage(damage);

            if (showDebug)
                Debug.Log("[구미호 투사체] 플레이어 피격 : -" + damage);
        }

        Destroy(gameObject);
    }
}