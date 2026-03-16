// UTF-8
// [구현 원리 요약]
// - 비행 중 재탐색도 EnemyRegistry2D를 우선 사용해서 물리 탐색 비용을 줄인다.
// - 목표가 끊기면 가장 가까운 적 기준으로 다시 잡아 추적을 유지한다.
// - rb.linearVelocity는 Unity 6 API이다. Unity 2022 이하에서는 rb.velocity로 변경할 것.
using UnityEngine;

/// <summary>
/// 호밍 미사일 투사체.
/// 타겟을 추적하며 수명 타격 횟수만큼 피해를 주고 소멸한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class HomingMissileProjectile2D : PooledObject2D
{
    [Header("필수 컴포넌트")]
    [Tooltip("Rigidbody2D (자동 탐색)")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("Collider2D (자동 탐색)")]
    [SerializeField] private Collider2D col;

    [Header("붙어서 때리기")]
    [Tooltip("동일 대상 재타격 간격 (초)")]
    [SerializeField, Min(0.05f)] private float rehitIntervalSeconds = 0.20f;

    [Header("수명 안전장치")]
    [Tooltip("타겟 없이 이 시간이 지나면 자동 소멸 (초)")]
    [SerializeField, Min(0.2f)] private float noTargetKillSeconds = 1.5f;

    [Header("디버그")]
    [Tooltip("타격 로그 출력")]
    [SerializeField] private bool debugLog = false;

    private LayerMask _enemyMask;
    private float _seekRadius;
    private int _damage;
    private float _speed;
    private float _turnSpeedDeg;
    private int _remainingHits;
    private Vector2 _dir;
    private Transform _target;
    private int _lockedRootId;
    private float _nextHitTime;
    private float _noTargetTimer;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (col == null) col = GetComponent<Collider2D>();
    }

    /// <summary>미사일 초기화 (발사 시 Weapon에서 호출)</summary>
    public void Init(LayerMask enemyMask, float seekRadius, int damage, float speed,
        float turnSpeedDeg, int chainCount, float lifeSeconds, Vector2 startDir, Transform startTarget)
    {
        _enemyMask = enemyMask;
        _seekRadius = Mathf.Max(0.1f, seekRadius);
        _damage = Mathf.Max(1, damage);
        _speed = Mathf.Max(0.1f, speed);
        _turnSpeedDeg = Mathf.Max(0f, turnSpeedDeg);
        _remainingHits = Mathf.Max(1, 1 + Mathf.Max(0, chainCount));
        _dir = startDir.sqrMagnitude > 0.0001f ? startDir.normalized : Vector2.right;
        _target = startTarget;
        _lockedRootId = 0;
        _nextHitTime = 0f;
        _noTargetTimer = 0f;

        if (col != null) col.enabled = true;

        if (rb != null)
        {
            rb.linearVelocity = _dir * _speed;
            rb.angularVelocity = 0f;
        }

        if (_target != null)
        {
            _lockedRootId = GetRootIdFromTransform(_target);
            if (_lockedRootId == 0)
                _target = null;
        }

        if (_target == null)
            AcquireAndLockNewTarget();
    }

    private void FixedUpdate()
    {
        if (_remainingHits <= 0)
        {
            ReturnToPool();
            return;
        }

        if (IsTargetInvalid(_target))
        {
            _noTargetTimer += Time.fixedDeltaTime;

            if (_noTargetTimer >= noTargetKillSeconds)
            {
                ReturnToPool();
                return;
            }

            AcquireAndLockNewTarget();
            if (_target == null)
                return;
        }
        else
        {
            _noTargetTimer = 0f;
        }

        Vector2 desired = (Vector2)_target.position - (Vector2)transform.position;
        if (desired.sqrMagnitude > 0.0001f)
        {
            desired.Normalize();
            float maxRad = _turnSpeedDeg * Mathf.Deg2Rad * Time.fixedDeltaTime;
            Vector3 newDir3 = Vector3.RotateTowards(_dir, desired, maxRad, 0f);
            _dir = ((Vector2)newDir3).normalized;
        }

        if (rb != null) rb.linearVelocity = _dir * _speed;
        else transform.position += (Vector3)(_dir * (_speed * Time.fixedDeltaTime));

        SetRotationFromDirection(_dir);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    private void OnTriggerStay2D(Collider2D other) => TryHit(other);

    private void TryHit(Collider2D other)
    {
        if (_remainingHits <= 0) return;
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, _enemyMask)) return;

        int otherRootId = GetRootIdFromCollider(other);
        if (otherRootId == 0) return;

        if (_lockedRootId == 0)
        {
            _lockedRootId = otherRootId;
            _target = GetTransformFromCollider(other);
        }

        if (otherRootId != _lockedRootId)
            return;

        float now = Time.time;
        if (now < _nextHitTime)
            return;

        _nextHitTime = now + rehitIntervalSeconds;

        DamageUtil2D.TryApplyDamage(other, _damage);
        _remainingHits--;

        if (debugLog)
            Debug.Log($"[HomingMissileProjectile2D] 적중 root={_lockedRootId} 남은횟수={_remainingHits}");

        if (_remainingHits <= 0)
            ReturnToPool();
    }

    private void AcquireAndLockNewTarget()
    {
        Transform t = AcquireNextTargetByRegistry();
        _target = t;
        _lockedRootId = 0;
        _nextHitTime = 0f;

        if (_target != null)
        {
            _lockedRootId = GetRootIdFromTransform(_target);
            if (_lockedRootId == 0)
                _target = null;
        }
    }

    private Transform AcquireNextTargetByRegistry()
    {
        if (EnemyRegistry2D.TryGetNearest(transform.position, _seekRadius, out var member) && member != null)
            return member.Transform;

        return null;
    }

    private static Transform GetTransformFromCollider(Collider2D col)
    {
        if (col == null) return null;
        if (col.attachedRigidbody != null) return col.attachedRigidbody.transform;
        return col.transform;
    }

    private static int GetRootIdFromCollider(Collider2D col)
    {
        if (col == null) return 0;

        var mem = col.GetComponentInParent<EnemyRegistryMember2D>();
        if (mem != null) return mem.RootInstanceId;

        return DamageUtil2D.GetRootInstanceId(col);
    }

    private static int GetRootIdFromTransform(Transform t)
    {
        if (t == null) return 0;
        var mem = t.GetComponentInParent<EnemyRegistryMember2D>();
        if (mem != null) return mem.RootInstanceId;
        return 0;
    }

    private static bool IsTargetInvalid(Transform t)
    {
        if (t == null) return true;
        if (!t.gameObject.activeInHierarchy) return true;

        var mem = t.GetComponentInParent<EnemyRegistryMember2D>();
        if (mem == null) return false;
        return !mem.IsValidTarget;
    }

    private void SetRotationFromDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude <= 0.0001f) return;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
