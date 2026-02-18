using UnityEngine;

[DisallowMultipleComponent]
public sealed class DarkOrbSplitProjectile2D : PooledObject2D
{
    [SerializeField] private Rigidbody2D rb;

    private LayerMask enemyMask;
    private int damage;
    private float speed;
    private float life;
    private float age;
    private Vector2 dir;

    private float explodeRadius;
    private readonly Collider2D[] hitBuf = new Collider2D[16];

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, Vector2 direction, float explosionRadius)
    {
        enemyMask = mask;
        damage = dmg;
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.1f, lifeSeconds);
        age = 0f;

        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        explodeRadius = Mathf.Max(0f, explosionRadius);

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

        // 분열체도 "작은 폭발" 느낌
        if (explodeRadius > 0.05f)
        {
            int n = Physics2D.OverlapCircleNonAlloc(transform.position, explodeRadius, hitBuf, enemyMask);
            for (int i = 0; i < n; i++)
            {
                var col = hitBuf[i];
                if (col == null) continue;
                DamageUtil2D.TryApplyDamage(col, damage);
            }
        }
        else
        {
            DamageUtil2D.TryApplyDamage(other, damage);
        }

        ReturnToPool();
    }
}