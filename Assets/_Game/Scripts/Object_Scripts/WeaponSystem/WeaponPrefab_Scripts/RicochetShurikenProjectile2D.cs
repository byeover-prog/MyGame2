using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RicochetShurikenProjectile2D : PooledObject2D
{
    [SerializeField] private Rigidbody2D rb;

    private LayerMask enemyMask;
    private int damage;
    private float speed;
    private float life;
    private float age;

    private int remainingBounces;
    private EnemyRegistryMember2D target;

    private readonly HashSet<int> hitSet = new HashSet<int>(64);

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, int bounces, EnemyRegistryMember2D startTarget)
    {
        enemyMask = mask;
        damage = dmg;
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.1f, lifeSeconds);
        age = 0f;

        remainingBounces = Mathf.Max(0, bounces);
        target = startTarget;

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

        if (target != null && !target.IsValidTarget)
            target = null;

        if (target == null)
        {
            // 타겟이 없으면 직진 유지 대신 종료(화면 난사 방지)
            ReturnToPool();
            return;
        }

        Vector2 desired = (target.Position - (Vector2)transform.position);
        if (desired.sqrMagnitude < 0.0001f)
            return;

        Vector2 dir = desired.normalized;
        if (rb != null)
            rb.linearVelocity = dir * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        int id = DamageUtil2D.GetRootId(other);
        if (hitSet.Contains(id)) return;

        hitSet.Add(id);
        DamageUtil2D.TryApplyDamage(other, damage);

        if (remainingBounces > 0)
        {
            remainingBounces--;

            // 다음 타겟: 현재 위치 기준 가장 가까운 적(이미 맞은 적 제외)
            if (EnemyRegistry2D.TryGetNearestExcluding(transform.position, hitSet, out var next))
            {
                target = next;
                return;
            }
        }

        ReturnToPool();
    }
}
