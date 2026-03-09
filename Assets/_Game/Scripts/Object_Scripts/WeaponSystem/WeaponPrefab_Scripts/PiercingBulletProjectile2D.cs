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

    private bool _initialized; // Init 호출 보장 체크
    private bool _loggedInitError;

    private readonly HashSet<int> hitSet = new HashSet<int>(64);

    private void OnEnable()
    {
        // 풀에서 재사용될 때 상태 오염 방지
        age = 0f;
        dir = Vector2.right;
        enemyMask = 0;
        damage = 0;
        speed = 0f;
        life = 0f;

        hitSet.Clear();

        _initialized = false;
        _loggedInitError = false;
    }

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, Vector2 direction)
    {
        enemyMask = mask;
        damage = Mathf.Max(1, dmg);
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.1f, lifeSeconds);

        age = 0f;
        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

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
                Debug.LogError("[PiercingBulletProjectile2D] Init()이 호출되지 않았습니다. (무기 발사 로직에서 Init 누락 가능)", this);
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
    }
}