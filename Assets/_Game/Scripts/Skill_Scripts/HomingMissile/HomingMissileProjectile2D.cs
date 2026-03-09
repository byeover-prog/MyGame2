// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 붙어서 때리기(락온): 락온 대상만 타격한다.
// - 수명은 사실상 무한처럼 써도 되지만, 안전장치로 "타겟을 못 찾는 시간"이 길면 자동 종료한다.
// - 속도/턴스피드를 올리면 붙는 체감이 확 좋아진다.

[DisallowMultipleComponent]
public sealed class HomingMissileProjectile2D : PooledObject2D
{
    [Header("필수")]
    [Tooltip("투사체 Rigidbody2D(없으면 Transform 이동)")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("투사체 Collider2D(Trigger 권장)")]
    [SerializeField] private Collider2D col;

    [Header("붙어서 때리기")]
    [Tooltip("같은 적을 다시 때리는 간격(초)")]
    [SerializeField, Min(0.05f)] private float rehitIntervalSeconds = 0.20f;

    [Header("수명 대체 안전장치")]
    [Tooltip("타겟을 못 찾는 상태가 이 시간(초) 이상 지속되면 자동 종료(풀 반납)\n수명을 없애는 대신 이걸로 안전하게 끝낸다.")]
    [SerializeField, Min(0.2f)] private float noTargetKillSeconds = 1.5f;

    [Header("디버그(권장 OFF)")]
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

    private readonly Collider2D[] _acquireHits = new Collider2D[48];

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (col == null) col = GetComponent<Collider2D>();
    }

    public void Init(
        LayerMask enemyMask,
        float seekRadius,
        int damage,
        float speed,
        float turnSpeedDeg,
        int chainCount,
        float lifeSeconds,     // 호출부 호환용(실제론 거의 무시)
        Vector2 startDir,
        Transform startTarget
    )
    {
        _enemyMask = enemyMask;
        _seekRadius = Mathf.Max(0.1f, seekRadius);
        _damage = Mathf.Max(1, damage);
        _speed = Mathf.Max(0.1f, speed);
        _turnSpeedDeg = Mathf.Max(0f, turnSpeedDeg);

        // 총 타격 수 = 1 + 추가타격
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
            {
                _target = null;
            }
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

        // 타겟이 없으면 시간 누적 -> 일정 시간 지나면 종료(수명 대체)
        if (IsTargetInvalid(_target))
        {
            _noTargetTimer += Time.fixedDeltaTime;

            if (_noTargetTimer >= noTargetKillSeconds)
            {
                ReturnToPool();
                return;
            }

            // 타겟을 계속 찾아본다
            AcquireAndLockNewTarget();
            if (_target == null)
                return; // 다음 프레임에 또 찾기
        }
        else
        {
            _noTargetTimer = 0f;
        }

        // 추적
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

        // 락온 미설정이면 첫 충돌 대상으로 락온
        if (_lockedRootId == 0)
        {
            _lockedRootId = otherRootId;
            _target = GetTransformFromCollider(other);
        }

        // 락온 대상이 아니면 무시
        if (otherRootId != _lockedRootId)
            return;

        float now = Time.time;
        if (now < _nextHitTime)
            return;

        _nextHitTime = now + rehitIntervalSeconds;

        DamageUtil2D.TryApplyDamage(other, _damage);
        _remainingHits--;

        if (debugLog)
            Debug.Log($"[HomingMissileProjectile2D] HIT root={_lockedRootId} remain={_remainingHits}");

        if (_remainingHits <= 0)
            ReturnToPool();
    }

    private void AcquireAndLockNewTarget()
    {
        Transform t = AcquireNextTargetByPhysics();
        _target = t;
        _lockedRootId = 0;
        _nextHitTime = 0f;

        if (_target != null)
        {
            _lockedRootId = GetRootIdFromTransform(_target);
            if (_lockedRootId == 0)
                _target = null; // 충돌로 락온 유도
        }
    }

    private Transform AcquireNextTargetByPhysics()
    {
        // 여기서는 단순/확실하게 Physics로만 잡는다.
        // (Registry 쪽 시그니처 차이/누락으로 인한 불확실성 제거)
        Vector2 from = transform.position;

        int count = Physics2D.OverlapCircleNonAlloc(from, _seekRadius, _acquireHits, _enemyMask);
        for (int i = count; i < _acquireHits.Length; i++) _acquireHits[i] = null;
        if (count <= 0) return null;

        float best = float.PositiveInfinity;
        Transform bestT = null;

        for (int i = 0; i < count; i++)
        {
            var c = _acquireHits[i];
            if (c == null) continue;

            Transform tt = (c.attachedRigidbody != null) ? c.attachedRigidbody.transform : c.transform;
            float d = ((Vector2)tt.position - from).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = tt;
            }
        }

        return bestT;
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
        return false;
    }

    private void SetRotationFromDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude <= 0.0001f) return;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }
}