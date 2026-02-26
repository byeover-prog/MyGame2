// UTF-8
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - Rigidbody2D.linearVelocity로 이동하고, turnSpeedDeg만큼 회전하며 타겟을 추적한다.
// - hitSet(HashSet<int>)으로 같은 적 중복 타격을 막는다.
// - chainCount가 남아있으면 다음 타겟을 다시 탐색한다(간단 구현: OverlapCircleAll).

[DisallowMultipleComponent]
public sealed class HomingMissileProjectile2D : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;

    private LayerMask _enemyMask;
    private float _seekRadius;
    private int _damage;
    private float _speed;
    private float _turnSpeedDeg;
    private int _remainingChains;
    private float _life;
    private float _age;

    private Vector2 _dir;
    private Transform _target;

    private readonly HashSet<int> _hitSet = new HashSet<int>(64);

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public void Init(
        LayerMask enemyMask,
        float seekRadius,
        int damage,
        float speed,
        float turnSpeedDeg,
        int chainCount,
        float lifeSeconds,
        Vector2 startDir,
        Transform startTarget
    )
    {
        _enemyMask = enemyMask;
        _seekRadius = Mathf.Max(0.1f, seekRadius);
        _damage = Mathf.Max(1, damage);
        _speed = Mathf.Max(0.1f, speed);
        _turnSpeedDeg = Mathf.Max(0f, turnSpeedDeg);
        _remainingChains = Mathf.Max(0, chainCount);
        _life = Mathf.Max(0.1f, lifeSeconds);
        _age = 0f;

        _dir = startDir.sqrMagnitude > 0.0001f ? startDir.normalized : Vector2.right;
        _target = startTarget;

        _hitSet.Clear();

        if (rb != null)
            rb.linearVelocity = _dir * _speed;
    }

    private void FixedUpdate()
    {
        _age += Time.fixedDeltaTime;
        if (_age >= _life)
        {
            Destroy(gameObject);
            return;
        }

        if (_target == null)
            TryAcquireTarget();

        if (_target != null)
        {
            Vector2 desired = (Vector2)_target.position - (Vector2)transform.position;
            if (desired.sqrMagnitude > 0.0001f)
            {
                desired.Normalize();
                float maxRad = _turnSpeedDeg * Mathf.Deg2Rad * Time.fixedDeltaTime;
                Vector3 newDir3 = Vector3.RotateTowards(_dir, desired, maxRad, 0f);
                _dir = ((Vector2)newDir3).normalized;
            }
        }

        if (rb != null)
            rb.linearVelocity = _dir * _speed;
        else
            transform.position += (Vector3)(_dir * (_speed * Time.fixedDeltaTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, _enemyMask)) return;

        int id = DamageUtil2D.GetRootInstanceId(other);
        if (_hitSet.Contains(id)) return;

        _hitSet.Add(id);
        DamageUtil2D.ApplyDamage(other, _damage);

        if (_remainingChains > 0)
        {
            _remainingChains--;
            _target = null;
            TryAcquireTarget(); // 다음 타겟 시도
            if (_target != null) return;
        }

        Destroy(gameObject);
    }

    private void TryAcquireTarget()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, _seekRadius, _enemyMask);
        if (hits == null || hits.Length == 0) return;

        float best = float.MaxValue;
        Transform bestT = null;

        Vector2 o = transform.position;
        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i];
            if (c == null) continue;

            int id = DamageUtil2D.GetRootInstanceId(c);
            if (_hitSet.Contains(id)) continue;

            float d = ((Vector2)c.transform.position - o).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = c.transform;
            }
        }

        _target = bestT;
    }
}