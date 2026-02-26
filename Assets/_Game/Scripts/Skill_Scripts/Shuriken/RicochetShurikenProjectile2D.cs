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

    private float _exitKickLeft;
    private Vector2 _lastDir = Vector2.right;

    // 다음 타겟을 "예약"해두고, ExitKick 끝나면 전환
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
                Debug.LogError("[RicochetShurikenProjectile2D] Init()이 호출되지 않았습니다. (무기 발사 로직에서 Init/startTarget 누락 가능)", this);
            }
            ReturnToPool();
            return;
        }

        if (rotateDegPerSec != 0f)
            transform.Rotate(0f, 0f, rotateDegPerSec * Time.fixedDeltaTime);

        age += Time.fixedDeltaTime;
        if (age >= life)
        {
            ReturnToPool();
            return;
        }

        // 1) 맞춘 직후: 콜라이더 밖으로 빠져나오기(박힘 방지)
        if (_exitKickLeft > 0f)
        {
            _exitKickLeft -= Time.fixedDeltaTime;
            transform.position += (Vector3)(_lastDir * (speed * exitKickSpeedMul) * Time.fixedDeltaTime);

            // ExitKick 끝나면 예약된 타겟으로 전환
            if (_exitKickLeft <= 0f && _pendingTarget != null)
            {
                target = _pendingTarget;
                _pendingTarget = null;
            }
            return;
        }

        if (target != null && !target.IsValidTarget)
            target = null;

        if (target == null)
        {
            ReturnToPool();
            return;
        }

        Vector2 desired = (target.Position - (Vector2)transform.position);

        // target과 너무 겹치면(이미 들어가 있음) 그냥 종료(정화구는 박히면 안 됨)
        if (desired.sqrMagnitude < 0.0001f)
        {
            ReturnToPool();
            return;
        }

        Vector2 dir = desired.normalized;
        _lastDir = dir;
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
                // 즉시 방향 전환하지 말고, exitKick으로 일단 밖으로 뺀 뒤에 전환
                _pendingTarget = next;
                _exitKickLeft = Mathf.Max(0.01f, exitKickSeconds);
                return;
            }
        }

        ReturnToPool();
    }
}