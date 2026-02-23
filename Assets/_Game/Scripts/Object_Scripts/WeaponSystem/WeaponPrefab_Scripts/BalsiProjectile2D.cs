using UnityEngine;

[DisallowMultipleComponent]
public sealed class BalsiProjectile2D : PooledObject2D
{
    [Header("회전(스프라이트 기준 보정)")]
    [Tooltip("스프라이트가 기본으로 바라보는 각도 보정값(도). 기본 0=오른쪽(→)이 전방. 위(↑)가 전방이면 -90 또는 90으로 맞추세요.")]
    [SerializeField] private float spriteForwardAngleOffsetDeg = 0f;

    private LayerMask enemyMask;
    private int damage;
    private float speed;
    private float life;
    private float age;
    private Vector2 dir;

    private int pierceLeft;

    private void OnEnable()
    {
        age = 0f;
    }

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, Vector2 direction, int pierceCount)
    {
        enemyMask = mask;
        damage = Mathf.Max(1, dmg);
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.05f, lifeSeconds);

        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        pierceLeft = Mathf.Max(0, pierceCount);

        ApplyRotation(dir);
    }

    private void ApplyRotation(Vector2 d)
    {
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + spriteForwardAngleOffsetDeg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void FixedUpdate()
    {
        age += Time.fixedDeltaTime;
        if (age >= life)
        {
            ReturnToPool();
            return;
        }

        transform.position += (Vector3)(dir * speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        DamageUtil2D.TryApplyDamage(other, damage);

        if (pierceLeft <= 0)
        {
            ReturnToPool();
            return;
        }

        pierceLeft--;
    }
}