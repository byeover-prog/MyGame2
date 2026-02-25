using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RicochetShurikenProjectile2D : PooledObject2D
{
    private LayerMask enemyMask;
    private int damage;
    private float speed;
    private float life;
    private float age;

    private int remainingBounces;
    private EnemyRegistryMember2D target;

    private bool _initialized; // Init 호출 보장 체크
    private bool _loggedInitError;

    private readonly HashSet<int> hitSet = new HashSet<int>(64);

    private void OnEnable()
    {
        // 풀 재사용 시 상태 오염 방지
        age = 0f;
        enemyMask = 0;
        damage = 0;
        speed = 0f;
        life = 0f;

        remainingBounces = 0;
        target = null;

        hitSet.Clear();

        _initialized = false;
        _loggedInitError = false;
    }

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, int bounces, EnemyRegistryMember2D startTarget)
    {
        enemyMask = mask;
        damage = Mathf.Max(1, dmg);
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.1f, lifeSeconds);
        age = 0f;

        remainingBounces = Mathf.Max(0, bounces);
        target = startTarget;

        hitSet.Clear();

        _initialized = true;
        _loggedInitError = false;
    }

    private void FixedUpdate()
    {
        if (!_initialized)
        {
            if (!_loggedInitError)
            {
                _loggedInitError = true;
                Debug.LogError("[RicochetShurikenProjectile2D] Init()이 호출되지 않았습니다. (무기 발사 로직에서 Init/startTarget 누락 가능)", this);
            }
            ReturnToPool();
            return;
        }

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
            // 정책: 타겟이 없으면 종료(난사 방지)
            ReturnToPool();
            return;
        }

        Vector2 desired = (target.Position - (Vector2)transform.position);
        if (desired.sqrMagnitude < 0.0001f)
            return;

        Vector2 dir = desired.normalized;
        transform.position += (Vector3)(dir * speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_initialized) return;
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