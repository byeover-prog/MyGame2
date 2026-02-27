// UTF-8
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - Rigidbody2D.linearVelocity로 이동하고, turnSpeedDeg만큼 회전하며 타겟을 추적한다.
// - 적을 타격하면 remainingChains만큼 추가 타격(재추적)을 수행한다.
// - 최적화: EnemyRegistry2D(등록된 적 목록) 기반으로 타겟을 찾고, 없을 때만 Physics2D를 fallback 한다.
// - 안전장치: 근거리에서 Enter 누락 대비 OnTriggerStay에서도 동일 처리한다.
// - 보스 1마리 보정: 신규 타겟이 없으면 같은 적 재타격(옵션)

[DisallowMultipleComponent]
public sealed class HomingMissileProjectile2D : PooledObject2D
{
    [Header("필수")]
    [Tooltip("투사체 리지드바디(없으면 Transform 이동으로 처리)")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("투사체 콜라이더(Trigger 권장)")]
    [SerializeField] private Collider2D col;

    [Header("재타격/체인 옵션")]
    [Tooltip("같은 적에게 연속으로 맞는 것을 막는 간격(초)\n(TriggerStay로 프레임마다 맞는 것 방지)")]
    [SerializeField, Min(0.02f)] private float rehitIntervalSeconds = 0.10f;

    [Tooltip("체인 중 '새로운 적'을 못 찾으면, 이미 맞은 적도 다시 타격할지(보스 1마리 보정)")]
    [SerializeField] private bool allowRehitWhenNoOtherTarget = true;

    [Header("디버그")]
    [SerializeField] private bool debugLog;

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

    // 체인 시 "이미 맞은 적"을 우선 제외하기 위한 목록(EnemyRegistry 기준 RootInstanceId)
    private readonly HashSet<int> _hitRootIds = new HashSet<int>(64);

    // 같은 적 루트에 대해 TriggerStay로 반복 타격되는 것 방지
    private readonly Dictionary<int, float> _nextHitTimeByRoot = new Dictionary<int, float>(16);

    private bool _despawnScheduled;
    private float _despawnTimer;
    private Vector2 _despawnDir;
    private const float DESPAWN_DELAY = 0.06f;
    private const float EXIT_KICK_SPEED = 10f;

    // fallback(Physics2D)용 NonAlloc 버퍼
    private readonly Collider2D[] _acquireHits = new Collider2D[48];

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (col == null) col = GetComponent<Collider2D>();
    }

    // 무기/스킬 스크립트에서 호출되는 Init 시그니처(호출부 유지)
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

        _hitRootIds.Clear();
        _nextHitTimeByRoot.Clear();

        _despawnScheduled = false;
        _despawnTimer = 0f;

        if (col != null) col.enabled = true;

        if (rb != null)
        {
            rb.linearVelocity = _dir * _speed;
            rb.angularVelocity = 0f;
        }

        SetRotationFromDirection(_dir);

        if (_target == null)
            TryAcquireTarget(excludeHitTargets: false);
    }

    private void FixedUpdate()
    {
        if (_despawnScheduled)
        {
            _despawnTimer -= Time.fixedDeltaTime;
            transform.position += (Vector3)(_despawnDir * (EXIT_KICK_SPEED * Time.fixedDeltaTime));
            if (_despawnTimer <= 0f)
                ReturnToPool();
            return;
        }

        _age += Time.fixedDeltaTime;
        if (_age >= _life)
        {
            ReturnToPool();
            return;
        }

        if (_target == null)
            TryAcquireTarget(excludeHitTargets: false);

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

        SetRotationFromDirection(_dir);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);

    private void OnTriggerStay2D(Collider2D other)
    {
        // 근거리 리타겟/고속 이동 시 Enter 누락 대비
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (_despawnScheduled) return;
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, _enemyMask)) return;

        int rootKey = DamageUtil2D.GetRootInstanceId(other);
        float now = Time.time;

        if (_nextHitTimeByRoot.TryGetValue(rootKey, out float nextTime))
        {
            if (now < nextTime) return;
        }

        // 데미지
        DamageUtil2D.TryApplyDamage(other, _damage);
        _nextHitTimeByRoot[rootKey] = now + rehitIntervalSeconds;

        // 체인 제외용 RootId 기록
        var member = other.GetComponentInParent<EnemyRegistryMember2D>();
        if (member != null) _hitRootIds.Add(member.RootInstanceId);

        if (debugLog)
            Debug.Log($"[HomingMissileProjectile2D] Hit: {other.name}, remainChains={_remainingChains}");

        // 체인 소진
        if (_remainingChains <= 0)
        {
            ScheduleDespawn(_dir);
            return;
        }

        _remainingChains--;

        // 다음 타겟 획득(이미 맞은 적은 우선 제외)
        Transform next = AcquireNextTarget(excludeHitTargets: true);

        if (next == null && allowRehitWhenNoOtherTarget)
        {
            // 주변에 새 적이 없으면(보스 1마리) 동일 적 재타겟
            next = other.transform;
        }

        if (next == null)
        {
            ScheduleDespawn(_dir);
            return;
        }

        _target = next;

        // 박힘 방지용 미세 이동
        transform.position += (Vector3)(_dir * 0.05f);
    }

    private void TryAcquireTarget(bool excludeHitTargets) => _target = AcquireNextTarget(excludeHitTargets);

    private Transform AcquireNextTarget(bool excludeHitTargets)
    {
        Vector2 from = transform.position;

        // 1) EnemyRegistry 기반
        if (excludeHitTargets && _hitRootIds.Count > 0)
        {
            // EnemyRegistry2D.TryGetNearestExcluding(from, excludeIds, out result) : 매개변수 3개만 존재
            if (EnemyRegistry2D.TryGetNearestExcluding(from, _hitRootIds, out var m) && m != null)
            {
                // 거리 제한은 호출부에서 체크
                if (_seekRadius <= 0f) return m.Transform;
                if ((m.Position - from).sqrMagnitude <= _seekRadius * _seekRadius) return m.Transform;
            }
        }
        else
        {
            // EnemyRegistry2D.TryGetNearest(from, maxDistance, out result) : 존재
            if (EnemyRegistry2D.TryGetNearest(from, _seekRadius, out var m) && m != null)
                return m.Transform;
        }

        // 2) fallback: Physics2D
        int count = Physics2D.OverlapCircleNonAlloc(from, _seekRadius, _acquireHits, _enemyMask);
        for (int i = count; i < _acquireHits.Length; i++) _acquireHits[i] = null;
        if (count <= 0) return null;

        float best = float.MaxValue;
        Transform bestT = null;

        for (int i = 0; i < count; i++)
        {
            var c = _acquireHits[i];
            if (c == null) continue;

            if (excludeHitTargets)
            {
                var mem = c.GetComponentInParent<EnemyRegistryMember2D>();
                if (mem != null && _hitRootIds.Contains(mem.RootInstanceId))
                    continue;
            }

            Transform t = (c.attachedRigidbody != null) ? c.attachedRigidbody.transform : c.transform;
            float d = ((Vector2)t.position - from).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = t;
            }
        }

        return bestT;
    }

    private void ScheduleDespawn(Vector2 dir)
    {
        _despawnScheduled = true;
        _despawnTimer = DESPAWN_DELAY;
        _despawnDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        if (col != null) col.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private void SetRotationFromDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude <= 0.0001f) return;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }
}