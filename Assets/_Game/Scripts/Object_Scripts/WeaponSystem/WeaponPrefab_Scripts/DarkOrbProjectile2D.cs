using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DarkOrbProjectile2D : PooledObject2D
{
    [SerializeField] private SpriteRenderer sr;

    private LayerMask enemyMask;
    private int damage;
    private float speed;
    private float life;
    private float age;
    private Vector2 dir;

    private float explosionRadius;

    private int splitCount;
    private float splitSpeed;
    private float splitLife;
    private int splitDamage;
    private ProjectilePool2D splitPool;

    // Unity6 권장: ContactFilter + List 재사용(할당/GC 방지)
    private readonly List<Collider2D> _hitList = new List<Collider2D>(32);
    private ContactFilter2D _enemyFilter;

    private void Awake()
    {
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    public void Init(
        LayerMask mask,
        int dmg,
        float spd,
        float lifeSeconds,
        Vector2 direction,
        float explodeRadius,
        int splitN,
        float splitSpd,
        float splitLifeSeconds,
        int splitDmg,
        ProjectilePool2D splitProjectilePool,
        float alpha)
    {
        enemyMask = mask;
        damage = dmg;
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.1f, lifeSeconds);
        age = 0f;

        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        explosionRadius = Mathf.Max(0.1f, explodeRadius);

        splitCount = Mathf.Max(0, splitN);
        splitSpeed = Mathf.Max(0.1f, splitSpd);
        splitLife = Mathf.Max(0.1f, splitLifeSeconds);
        splitDamage = Mathf.Max(0, splitDmg);
        splitPool = splitProjectilePool;

        // 필터 준비(레이어 + 트리거 포함)
        _enemyFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = enemyMask,
            useTriggers = true
        };

        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Clamp01(alpha);
            sr.color = c;
        }
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

        ExplodeAndSplit();
    }

    private void ExplodeAndSplit()
    {
        // 폭발 데미지(반경)
        _hitList.Clear();
        Physics2D.OverlapCircle(transform.position, explosionRadius, _enemyFilter, _hitList);

        for (int i = 0; i < _hitList.Count; i++)
        {
            var col = _hitList[i];
            if (col == null) continue;
            DamageUtil2D.TryApplyDamage(col, damage);
        }

        // 분열 발사
        if (splitPool != null && splitCount > 0 && splitDamage > 0)
        {
            float step = 360f / splitCount;
            for (int i = 0; i < splitCount; i++)
            {
                float ang = step * i * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

                var p = splitPool.Get<DarkOrbSplitProjectile2D>(transform.position, Quaternion.identity);
                p.Init(enemyMask, splitDamage, splitSpeed, splitLife, d, explosionRadius * 0.5f);
            }
        }

        ReturnToPool();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}