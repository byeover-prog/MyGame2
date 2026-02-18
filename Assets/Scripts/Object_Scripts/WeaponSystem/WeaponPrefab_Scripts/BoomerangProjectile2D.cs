using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BoomerangProjectile2D : PooledObject2D
{
    [SerializeField] private Rigidbody2D rb;

    private Transform owner;
    private LayerMask enemyMask;

    private int damage;
    private float outSpeed;
    private float returnSpeed;
    private float maxDistance;
    private float life;

    private Vector2 dir;
    private bool returning;
    private float age;
    private float traveled;

    private const float CatchRadius = 0.35f;

    private readonly HashSet<int> hitOut = new HashSet<int>(64);
    private readonly HashSet<int> hitBack = new HashSet<int>(64);

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Transform ownerTr, Vector2 direction, LayerMask mask, int dmg, float outSpd, float backSpd, float distance, float lifeSeconds)
    {
        owner = ownerTr;
        enemyMask = mask;

        damage = dmg;
        outSpeed = Mathf.Max(0.1f, outSpd);
        returnSpeed = Mathf.Max(0.1f, backSpd);
        maxDistance = Mathf.Max(0.1f, distance);
        life = Mathf.Max(0.1f, lifeSeconds);

        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        returning = false;
        age = 0f;
        traveled = 0f;

        hitOut.Clear();
        hitBack.Clear();

        if (rb != null)
            rb.linearVelocity = dir * outSpeed;
    }

    private void FixedUpdate()
    {
        age += Time.fixedDeltaTime;
        if (age >= life)
        {
            ReturnToPool();
            return;
        }

        if (rb == null)
            return;

        if (!returning)
        {
            traveled += outSpeed * Time.fixedDeltaTime;
            rb.linearVelocity = dir * outSpeed;

            if (traveled >= maxDistance)
                returning = true;
        }
        else
        {
            if (owner == null)
            {
                ReturnToPool();
                return;
            }

            Vector2 toOwner = (Vector2)owner.position - (Vector2)transform.position;
            float dist = toOwner.magnitude;

            if (dist <= CatchRadius)
            {
                ReturnToPool();
                return;
            }

            Vector2 d = toOwner / Mathf.Max(0.0001f, dist);
            rb.linearVelocity = d * returnSpeed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        int id = DamageUtil2D.GetRootId(other);

        if (!returning)
        {
            if (hitOut.Contains(id)) return;
            hitOut.Add(id);
            DamageUtil2D.TryApplyDamage(other, damage);
        }
        else
        {
            if (hitBack.Contains(id)) return;
            hitBack.Add(id);
            DamageUtil2D.TryApplyDamage(other, damage);
        }
    }
}
