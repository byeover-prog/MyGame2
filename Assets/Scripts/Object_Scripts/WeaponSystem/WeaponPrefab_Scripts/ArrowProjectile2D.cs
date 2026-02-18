using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArrowProjectile2D : PooledObject2D
{
    [SerializeField] private Rigidbody2D rb;

    private LayerMask enemyMask;
    private int damage;
    private float speed;
    private float life;
    private float age;
    private Vector2 dir;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, Vector2 direction)
    {
        enemyMask = mask;
        damage = dmg;
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.1f, lifeSeconds);
        age = 0f;

        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        if (rb != null)
            rb.linearVelocity = dir * speed;
    }

    private void FixedUpdate()
    {
        age += Time.fixedDeltaTime;
        if (age >= life)
        {
            ReturnToPool();
            return;
        }

        if (rb != null)
            rb.linearVelocity = dir * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        DamageUtil2D.TryApplyDamage(other, damage);
        ReturnToPool();
    }
}