using UnityEngine;

/// <summary>
/// 직선 투사체
/// - Launch 호출 시 지정 방향으로 속도를 부여해 직선 이동
/// </summary>
public sealed class StraightProjectile2D : ProjectileBase2D
{
    [Header("직선 이동")]
    [SerializeField] private float speed = 10f;

    public override void Launch(
        Vector2 direction,
        int newDamage,
        float launchSpeed,
        float lifeSeconds,
        LayerMask newEnemyMask,
        Transform newOwner)
    {
        // 1) 공통 발사 처리(데미지/수명/마스크/부모 분리 + 기본 velocity 설정)
        base.Launch(direction, newDamage, launchSpeed, lifeSeconds, newEnemyMask, newOwner);

        // 2) 직선 투사체 전용: 인스펙터 speed를 우선(0이면 launchSpeed 사용)
        float s = speed > 0f ? speed : launchSpeed;

        if (rb != null)
            rb.linearVelocity = direction.normalized * Mathf.Max(0f, s);

        // 3) 진행 방향으로 회전(스프라이트 오른쪽이 진행 방향 기준)
        if (direction.sqrMagnitude > 0.0001f)
            transform.right = direction.normalized;
    }
}