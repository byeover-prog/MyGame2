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

    [Header("정화구 규칙: 박힘 방지")]
    [Tooltip("적을 맞춘 직후, 잠깐 강제 전진해서 콜라이더 밖으로 빠져나오는 시간(초)")]
    [SerializeField] private float exitKickSeconds = 0.06f;

    [Tooltip("ExitKick 동안의 이동 속도 배율(기본 속도 * 배율)")]
    [SerializeField] private float exitKickSpeedMul = 1.2f;

    [Header("겹침 방지")]
    [Tooltip("적을 맞춘 뒤, 이 시간 동안 다른 적과의 충돌을 무시(겹친 몬스터 관통용)")]
    [SerializeField] private float hitImmunitySeconds = 0.12f;

    private float _exitKickLeft;
    private float _hitImmunityLeft;      // ★ 히트 후 무적 타이머
    private Vector2 _lastDir = Vector2.right;

    private EnemyRegistryMember2D _pendingTarget;

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
                Debug.LogError("[RicochetShurikenProjectile2D] Init()이 호출되지 않았습니다.", this);
            }
            ReturnToPool();
            return;
        }

        float dt = Time.fixedDeltaTime;

        if (rotateDegPerSec != 0f)
            transform.Rotate(0f, 0f, rotateDegPerSec * dt);

        age += dt;
        if (age >= life)
        {
            ReturnToPool();
            return;
        }

        // ★ 히트 무적 타이머 감소
        if (_hitImmunityLeft > 0f)
            _hitImmunityLeft -= dt;

        // 1) ExitKick: 맞춘 직후 콜라이더 밖으로 빠져나오기
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

        // 2) 타겟 유효성 검사
        if (target != null && !target.IsValidTarget)
            target = null;

        if (target == null)
        {
            ReturnToPool();
            return;
        }

        // 3) 타겟을 향해 이동
        Vector2 desired = (target.Position - (Vector2)transform.position);

        if (desired.sqrMagnitude < 0.0001f)
        {
            ReturnToPool();
            return;
        }

        Vector2 dir = desired.normalized;
        _lastDir = dir;
        transform.position += (Vector3)(dir * speed * dt);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_initialized) return;
        if (other == null) return;

        // ★ 히트 무적 중이면 충돌 무시 (겹친 몬스터 즉사 방지)
        if (_hitImmunityLeft > 0f) return;

        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        int id = DamageUtil2D.GetRootId(other);
        if (hitSet.Contains(id)) return;

        hitSet.Add(id);
        DamageUtil2D.TryApplyDamage(other, damage);

        // 튕김 처리
        if (remainingBounces > 0)
        {
            remainingBounces--;

            if (EnemyRegistry2D.TryGetNearestExcluding(transform.position, hitSet, out var next))
            {
                _pendingTarget = next;
                _exitKickLeft = Mathf.Max(0.01f, exitKickSeconds);
                _hitImmunityLeft = Mathf.Max(exitKickSeconds, hitImmunitySeconds); // ★ 무적 시작
                return;
            }
        }

        ReturnToPool();
    }
}