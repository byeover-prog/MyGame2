using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HomingMissileProjectile2D : PooledObject2D
{
    private LayerMask enemyMask;
    private int damage;
    private float speed;
    private float life;
    private float age;
    private float turnSpeedDeg;

    private int remainingChains;

    private Vector2 currentDir;
    private EnemyRegistryMember2D target;

    private readonly HashSet<int> hitSet = new HashSet<int>(64);

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, float turnDeg, int chainCount, Vector2 startDir, EnemyRegistryMember2D startTarget)
    {
        enemyMask = mask;
        damage = dmg;
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.1f, lifeSeconds);
        turnSpeedDeg = Mathf.Max(0f, turnDeg);

        remainingChains = Mathf.Max(0, chainCount);
        age = 0f;

        currentDir = startDir.sqrMagnitude > 0.0001f ? startDir.normalized : Vector2.right;
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

        if (target != null)
        {
            Vector2 desired = (target.Position - (Vector2)transform.position);
            if (desired.sqrMagnitude > 0.0001f)
            {
                desired.Normalize();
                float maxRad = turnSpeedDeg * Mathf.Deg2Rad * Time.fixedDeltaTime;
                Vector3 newDir3 = Vector3.RotateTowards(currentDir, desired, maxRad, 0f);
                currentDir = ((Vector2)newDir3).normalized;
            }
        }

        transform.position += (Vector3)(currentDir * speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        int id = DamageUtil2D.GetRootId(other);
        if (hitSet.Contains(id)) return;

        hitSet.Add(id);
        DamageUtil2D.TryApplyDamage(other, damage);

        if (remainingChains > 0)
        {
            remainingChains--;
            // 다음 타겟: 현재 위치 기준 가장 가까운 적(이미 맞은 적 제외)
            if (EnemyRegistry2D.TryGetNearestExcluding(transform.position, hitSet, out var next))
            {
                target = next;
                return;
            }
        }

        // 더 이상 갈 곳 없으면 종료
        ReturnToPool();
    }
}