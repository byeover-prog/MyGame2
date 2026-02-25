// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 이동만 담당(SRP). 목표(플레이어) 방향으로 Rigidbody2D를 이동시킨다.
// - 이동속도는 외부(EnemyStatsApplier2D)에서 주입받는다.
[DisallowMultipleComponent]
public sealed class EnemyMotor2D : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("없으면 자동으로 GetComponent 합니다.")]
    [SerializeField] private Rigidbody2D rb;

    [Header("이동 설정(런타임 주입)")]
    [Tooltip("EnemyRootSO/BaseMoveSpeed가 여기로 들어옵니다.")]
    [SerializeField] private float moveSpeed = 2.5f;

    private Transform _target;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public void SetTarget(Transform target) => _target = target;

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }

    private void FixedUpdate()
    {
        if (rb == null) return;
        if (_target == null) return;

        Vector2 pos = rb.position;
        Vector2 dir = ((Vector2)_target.position - pos);
        if (dir.sqrMagnitude <= 0.0001f) return;

        dir.Normalize();
        rb.MovePosition(pos + dir * moveSpeed * Time.fixedDeltaTime);
    }
}