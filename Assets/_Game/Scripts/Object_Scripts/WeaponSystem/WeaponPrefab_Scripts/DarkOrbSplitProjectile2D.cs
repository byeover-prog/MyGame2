using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DarkOrbSplitProjectile2D : PooledObject2D
{
    private LayerMask enemyMask;
    private int damage;
    private float speed;
    private float life;
    private float age;
    private Vector2 dir;

    private float explodeRadius;

    // Unity6 권장: ContactFilter + List 재사용(할당/GC 방지)
    private readonly List<Collider2D> _hitList = new List<Collider2D>(16);
    private ContactFilter2D _enemyFilter;

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, Vector2 direction, float explosionRadius)
    {
        enemyMask = mask;
        damage = dmg;
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.1f, lifeSeconds);
        age = 0f;

        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        explodeRadius = Mathf.Max(0f, explosionRadius);

        // 필터 준비(레이어 + 트리거 포함)
        _enemyFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = enemyMask,
            useTriggers = true
        };
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

        // 분열체도 "작은 폭발" 느낌
        if (explodeRadius > 0.05f)
        {
            _hitList.Clear();
            Physics2D.OverlapCircle(transform.position, explodeRadius, _enemyFilter, _hitList);

            for (int i = 0; i < _hitList.Count; i++)
            {
                var col = _hitList[i];
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