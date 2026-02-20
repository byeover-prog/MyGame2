using UnityEngine;

/// <summary>
/// ���� ����ü
/// - Launch ���� �������� ���� �ӵ��� ����
/// </summary>
public sealed class StraightProjectile2D : ProjectileBase2D
{
    [Header("���� �̵�")]
    [SerializeField] private float speed = 10f;

    protected override void OnLaunch(Vector2 dir)
    {
        float s = Mathf.Max(0f, speed);
        rb.linearVelocity = dir * s;

        // ���� ���� ȸ��(����)
        transform.right = dir;
    }
}
