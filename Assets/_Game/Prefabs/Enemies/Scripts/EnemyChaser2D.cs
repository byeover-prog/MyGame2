using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyChaser2D : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float move_speed = 3.0f;

    [Header("타겟(비워두면 자동 탐색)")]
    [SerializeField] private Transform target;

    [Header("물리")]
    [SerializeField] private Rigidbody2D rb;

    [Header("탐색 태그")]
    [SerializeField] private string player_tag = "Player";

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("[EnemyChaser2D] Rigidbody2D가 없습니다. Enemy 프리팹에 Rigidbody2D를 추가하세요.");
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        //  핵심: 타겟이 없으면 계속 재탐색
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(player_tag);
            if (player != null) target = player.transform;

            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = (target.position - transform.position);
        if (dir.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        dir.Normalize();
        rb.linearVelocity = dir * move_speed;
    }
}
