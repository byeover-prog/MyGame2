// UTF-8
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

    private bool _initialized;
    private bool _loggedInitError;

    private readonly HashSet<int> hitSet = new HashSet<int>(64);

    [Header("연출")]
    [Tooltip("날아가는 동안 회전 속도(도/초). 0이면 회전 안 함.")]
    [SerializeField] private float rotateDegPerSec = 1080f;

    [Header("박힘 방지")]
    [Tooltip("적을 맞춘 직후 강제 전진 시간(초)")]
    [SerializeField] private float exitKickSeconds = 0.08f;

    [Tooltip("ExitKick 이동 속도 배율")]
    [SerializeField] private float exitKickSpeedMul = 1.5f;

    [Header("겹침 방지")]
    [Tooltip("적 타격 후 이 시간 동안 다른 적 충돌 무시")]
    [SerializeField] private float hitImmunitySeconds = 0.15f;

    [Header("소멸")]
    [Tooltip("마지막 타격 후 반대 방향으로 빠져나오는 시간(초)")]
    [SerializeField] private float despawnDelay = 0.10f;

    private float _exitKickLeft;
    private float _hitImmunityLeft;
    private Vector2 _lastDir = Vector2.right;
    private Vector2 _despawnDir = Vector2.right;

    private EnemyRegistryMember2D _pendingTarget;

    private bool _despawnScheduled;
    private float _despawnTimer;

    private Collider2D _collider;

    private void OnEnable()
    {
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

        _exitKickLeft = 0f;
        _hitImmunityLeft = 0f;
        _pendingTarget = null;
        _lastDir = Vector2.right;
        _despawnDir = Vector2.right;

        _despawnScheduled = false;
        _despawnTimer = 0f;

        if (_collider == null) _collider = GetComponent<Collider2D>();
        if (_collider != null) _collider.enabled = true;
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

        _exitKickLeft = 0f;
        _hitImmunityLeft = 0f;
        _pendingTarget = null;

        _despawnScheduled = false;
        _despawnTimer = 0f;

        if (_collider == null) _collider = GetComponent<Collider2D>();
        if (_collider != null) _collider.enabled = true;

        if (target != null)
        {
            Vector2 d = (target.Position - (Vector2)transform.position);
            if (d.sqrMagnitude > 0.0001f) _lastDir = d.normalized;
        }
    }

    private void FixedUpdate()
    {
        if (!_initialized)
        {
            if (!_loggedInitError)
            {
                _loggedInitError = true;
                Debug.LogError("[RicochetShurikenProjectile2D] Init() 미호출", this);
            }
            ReturnToPool();
            return;
        }

        float dt = Time.fixedDeltaTime;

        // ★ 소멸 중: 반대 방향으로 빠져나오면서 사라짐
        if (_despawnScheduled)
        {
            transform.position += (Vector3)(_despawnDir * speed * exitKickSpeedMul * dt);
            _despawnTimer -= dt;
            if (_despawnTimer <= 0f)
                ReturnToPool();
            return;
        }

        if (rotateDegPerSec != 0f)
            transform.Rotate(0f, 0f, rotateDegPerSec * dt);

        age += dt;
        if (age >= life)
        {
            ReturnToPool();
            return;
        }

        if (_hitImmunityLeft > 0f)
            _hitImmunityLeft -= dt;

        // ExitKick
        if (_exitKickLeft > 0f)
        {
            _exitKickLeft -= dt;
            transform.position += (Vector3)(_lastDir * (speed * exitKickSpeedMul) * dt);

            if (_exitKickLeft <= 0f && _pendingTarget != null)
            {
                target = _pendingTarget;
                _pendingTarget = null;
            }
            return;
        }

        // 타겟 유효성
        if (target != null && !target.IsValidTarget)
            target = null;

        if (target == null)
        {
            ScheduleDespawn(_lastDir);
            return;
        }

        // 타겟 추적
        Vector2 desired = (target.Position - (Vector2)transform.position);

        if (desired.sqrMagnitude < 0.0001f)
        {
            ScheduleDespawn(-_lastDir);
            return;
        }

        Vector2 dir = desired.normalized;
        _lastDir = dir;
        transform.position += (Vector3)(dir * speed * dt);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_initialized) return;
        if (_despawnScheduled) return;
        if (other == null) return;
        if (_hitImmunityLeft > 0f) return;

        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        int id = DamageUtil2D.GetRootId(other);
        if (hitSet.Contains(id)) return;

        hitSet.Add(id);
        DamageUtil2D.TryApplyDamage(other, damage);

        // 튕김
        if (remainingBounces > 0)
        {
            remainingBounces--;

            if (EnemyRegistry2D.TryGetNearestExcluding(transform.position, hitSet, out var next))
            {
                _pendingTarget = next;
                _exitKickLeft = Mathf.Max(0.01f, exitKickSeconds);
                _hitImmunityLeft = Mathf.Max(exitKickSeconds, hitImmunitySeconds);
                return;
            }
        }

        // ★ 마지막 타격: 반대 방향으로 빠져나감
        ScheduleDespawn(-_lastDir);
    }

    private void ScheduleDespawn(Vector2 dir)
    {
        if (_despawnScheduled) return;
        _despawnScheduled = true;

        _despawnDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : -_lastDir;

        if (_collider == null) _collider = GetComponent<Collider2D>();
        if (_collider != null) _collider.enabled = false;

        _despawnTimer = Mathf.Max(0.01f, despawnDelay);
    }
}