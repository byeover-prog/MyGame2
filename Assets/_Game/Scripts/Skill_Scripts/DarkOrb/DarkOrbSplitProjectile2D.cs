// UTF-8
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DarkOrbSplitProjectile2D : PooledObject2D
{
    public sealed class FragmentBudget
    {
        public int used;
        public int max;
    }

    private LayerMask _enemyMask;
    private int _explosionDamage;
    private float _speed;
    private float _life;
    private float _age;

    private Vector2 _dir;
    private float _explodeRadius;

    private float _splitDistance;
    private float _splitAngleDeg;

    private int _depth;
    private int _maxDepth;

    private FragmentBudget _budget;
    private ProjectilePool2D _splitPool;

    private Vector2 _startPos;

    private bool _initialized;
    private bool _loggedInitError;
    private bool _exploding;

    private readonly Collider2D[] _hits = new Collider2D[32];
    private ContactFilter2D _enemyFilter;

    private void OnEnable()
    {
        _age = 0f;

        _enemyMask = 0;
        _explosionDamage = 0;
        _speed = 0f;
        _life = 0f;

        _dir = Vector2.right;
        _explodeRadius = 0f;

        _splitDistance = 0f;
        _splitAngleDeg = 40f;

        _depth = 1;
        _maxDepth = 1;

        _budget = null;
        _splitPool = null;

        _startPos = Vector2.zero;

        _initialized = false;
        _loggedInitError = false;
        _exploding = false;
    }

    public void Init(
        LayerMask enemyMask,
        int explosionDmg,
        float speed,
        float lifeSeconds,
        Vector2 direction,
        float explosionRadius,
        float splitDistance,
        float splitAngleDeg,
        int depth,
        int maxDepth,
        object sharedBudget,
        int maxBudget,
        object sharedPool)
    {
        _enemyMask = enemyMask;
        _explosionDamage = Mathf.Max(1, explosionDmg);

        _speed = Mathf.Max(0.1f, speed);
        _life = Mathf.Max(0.05f, lifeSeconds);
        _age = 0f;

        _dir = (direction.sqrMagnitude > 0.0001f) ? direction.normalized : Vector2.right;
        _explodeRadius = Mathf.Max(0f, explosionRadius);

        _splitDistance = Mathf.Max(0f, splitDistance);
        _splitAngleDeg = splitAngleDeg;

        _depth = Mathf.Max(1, depth);
        _maxDepth = Mathf.Max(1, maxDepth);

        _startPos = transform.position;

        // ★ Unity 6: ContactFilter2D 사용 (NonAlloc deprecated 대체)
        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(_enemyMask);
        _enemyFilter.useTriggers = true;

        if (sharedBudget is FragmentBudget fb)
            _budget = fb;
        else
            _budget = (maxBudget > 0) ? new FragmentBudget { used = 0, max = maxBudget } : null;

        if (sharedPool is ProjectilePool2D p)
            _splitPool = p;
        else
            _splitPool = null;

        _initialized = true;
        _loggedInitError = false;
        _exploding = false;
    }

    private void Update()
    {
        if (!_initialized)
        {
            if (!_loggedInitError)
            {
                _loggedInitError = true;
                Debug.LogWarning($"[DarkOrbSplitProjectile2D] Init() 없이 활성화됨: {gameObject.name}", this);
            }

            ReturnToPool();
            return;
        }

        _age += Time.deltaTime;

        if (_age >= _life)
        {
            ReturnToPool();
            return;
        }

        Vector2 pos = transform.position;
        pos += _dir * (_speed * Time.deltaTime);
        transform.position = pos;

        if (_splitDistance > 0.0001f)
        {
            float dist = Vector2.Distance(_startPos, pos);
            if (dist >= _splitDistance)
                ExplodeAndSplit();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_initialized) return;
        if (((1 << other.gameObject.layer) & _enemyMask.value) == 0) return;
        ExplodeAndSplit();
    }

    private void ExplodeAndSplit()
    {
        if (_exploding) return;
        _exploding = true;

        ApplyExplosionDamage();

        if (_depth < _maxDepth)
        {
            if (TryConsumeBudget(2))
            {
                Vector2 pos = transform.position;
                const float spawnEps = 0.05f;

                Vector2 dirA = Rotate(_dir, +_splitAngleDeg).normalized;
                Vector2 dirB = Rotate(_dir, -_splitAngleDeg).normalized;

                SpawnChild(pos + dirA * spawnEps, dirA, _depth + 1, _maxDepth);
                SpawnChild(pos + dirB * spawnEps, dirB, _depth + 1, _maxDepth);
            }
        }

        ReturnToPool();
    }

    // ★ Unity 6: Physics2D.OverlapCircle(pos, radius, filter, results[]) — GC 0, 경고 0
    private void ApplyExplosionDamage()
    {
        if (_explodeRadius > 0.05f)
        {
            int count = Physics2D.OverlapCircle(
                transform.position, _explodeRadius, _enemyFilter, _hits);

            for (int i = 0; i < count; i++)
            {
                var c = _hits[i];
                if (c == null) continue;
                DamageUtil2D.TryApplyDamage(c, _explosionDamage);
            }
        }
        else
        {
            var one = Physics2D.OverlapCircle(transform.position, 0.1f, _enemyMask);
            if (one != null) DamageUtil2D.TryApplyDamage(one, _explosionDamage);
        }
    }

    private bool TryConsumeBudget(int amount)
    {
        if (_budget == null) return true;
        if (_budget.max <= 0) return false;
        if (_budget.used + amount > _budget.max) return false;
        _budget.used += amount;
        return true;
    }

    private void SpawnChild(Vector2 pos, Vector2 childDir, int childDepth, int maxDepth)
    {
        DarkOrbSplitProjectile2D child = null;

        if (_splitPool != null)
            child = _splitPool.Get<DarkOrbSplitProjectile2D>(pos, Quaternion.identity);

        if (child == null)
            child = Instantiate(this, pos, Quaternion.identity);

        child.Init(
            enemyMask: _enemyMask,
            explosionDmg: _explosionDamage,
            speed: _speed,
            lifeSeconds: _life,
            direction: childDir,
            explosionRadius: _explodeRadius,
            splitDistance: _splitDistance,
            splitAngleDeg: _splitAngleDeg,
            depth: childDepth,
            maxDepth: maxDepth,
            sharedBudget: _budget,
            maxBudget: (_budget != null) ? _budget.max : 0,
            sharedPool: _splitPool
        );
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad); float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}