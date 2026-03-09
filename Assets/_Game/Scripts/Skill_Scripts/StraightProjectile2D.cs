using UnityEngine;

/// <summary>
/// 직선 투사체
/// - Launch 호출 시 지정 방향으로 속도를 부여해 직선 이동
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class StraightProjectile2D : ProjectileBase2D
{
    [Header("직선 이동")]
    [SerializeField, Tooltip("직선 이동 속도(0이면 Launch의 launchSpeed 사용)")]
    private float speed = 10f;

    [Header("컴포넌트")]
    [SerializeField, Tooltip("없으면 Awake에서 자동으로 가져옵니다")]
    private Rigidbody2D rb;

    private void Awake()
    {
        // 인스펙터 미할당/누락 방어
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public override void Launch(
        Vector2 direction,
        int newDamage,
        float launchSpeed,
        float lifeSeconds,
        LayerMask newEnemyMask,
        Transform newOwner)
    {
        // 1) 공통 발사 처리(데미지/수명/마스크/오너 등)
        base.Launch(direction, newDamage, launchSpeed, lifeSeconds, newEnemyMask, newOwner);

        // 2) 직선 투사체 전용: 인스펙터 speed 우선(0이면 launchSpeed)
        float s = (speed > 0f) ? speed : launchSpeed;

        // direction이 0이면 속도 주지 않음(방어)
        if (rb != null && direction.sqrMagnitude > 0.0001f)
        {
            // Unity 2D: linearVelocity 사용
            rb.linearVelocity = direction.normalized * Mathf.Max(0f, s);
        }

        // 3) 진행 방향으로 회전(스프라이트 오른쪽이 진행 방향 기준)
        if (direction.sqrMagnitude > 0.0001f)
            transform.right = direction.normalized;
    }
}