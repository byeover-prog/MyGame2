using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PiercingBulletProjectile2D : PooledObject2D
{
    private LayerMask enemyMask;
    private int damage;
    private float speed;
    private float life;
    private float age;
    private Vector2 dir;

    private readonly HashSet<int> hitSet = new HashSet<int>(64);

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, Vector2 direction)
    {
        enemyMask = mask;
        damage = dmg;
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.1f, lifeSeconds);

        age = 0f;
        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        hitSet.Clear();
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

        int id = DamageUtil2D.GetRootId(other);
        if (hitSet.Contains(id)) return;
        hitSet.Add(id);

        DamageUtil2D.TryApplyDamage(other, damage);
    }
}