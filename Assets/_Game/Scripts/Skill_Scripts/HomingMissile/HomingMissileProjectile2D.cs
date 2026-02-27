// UTF-8
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - 기본은 "한 발당 같은 적 1회만" (중복타격 방지)
// - 옵션으로 "같은 적 재타격(다단히트)" 지원:
//   - sameTargetRehitInterval > 0 이면, 같은 적도 일정 간격으로 다시 타격 가능
//   - maxHitsPerTarget로 같은 적 최대 타격 횟수 제한 가능
// - 같은 적 판정 키는 안정화(리짓바디 -> 루트 컴포넌트 -> fallback)
[DisallowMultipleComponent]
public sealed class HomingMissileProjectile2D : MonoBehaviour
{
    [Header("필수")]
    [Tooltip("투사체 리지드바디(없으면 자동 GetComponent)")]
    [SerializeField] private Rigidbody2D rb;

    [Header("같은 적 판정(옵션)")]
    [Tooltip("적 루트에 Rigidbody2D가 없을 때, 부모에서 이 컴포넌트를 찾아 같은 적 키로 사용합니다.\n예) EnemyRegistryMember2D")]
    [SerializeField] private string targetRootComponentTypeName = "EnemyRegistryMember2D";

    [Header("다단히트(옵션)")]
    [Tooltip("같은 적 재타격 간격(초)\n0이면 '같은 적 1회만'(기본)")]
    [Min(0f)]
    [SerializeField] private float sameTargetRehitInterval = 0f;

    [Tooltip("같은 적 최대 타격 횟수\n1이면 기본(중복타격 방지), 2 이상이면 다단히트 가능")]
    [Min(1)]
    [SerializeField] private int maxHitsPerTarget = 1;

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

    // 같은 적 판정 키 기반 히트 기록
    private readonly Dictionary<int, HitInfo> _hitInfo = new Dictionary<int, HitInfo>(64);

    private struct HitInfo
    {
        public int count;
        public float nextTime; // 재타격 가능 시간
    }

    private readonly Collider2D[] _acquireHits = new Collider2D[48];

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

        _hitInfo.Clear();

        if (rb != null)
        {
            rb.linearVelocity = _dir * _speed;
            rb.angularVelocity = 0f;
        }
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

        int key = GetTargetKey(other);
        if (key == 0) return;

        if (!CanHitNow(key))
            return;

        ApplyHit(key, other);

        // 체인(=다른 적 추가 타격) 처리
        if (_remainingChains > 0)
        {
            _remainingChains--;
            _target = null;
            TryAcquireTarget();
            if (_target != null) return;
        }

        // 체인이 없고, 다단히트 옵션도 없다면 여기서 종료
        // 다단히트가 켜져 있으면 "같은 적 재타격"을 위해 살아있어야 하므로,
        // destroy 조건을 다단히트 설정으로 나눈다.
        if (sameTargetRehitInterval <= 0f || maxHitsPerTarget <= 1)
        {
            Destroy(gameObject);
        }
        // 다단히트가 켜져 있으면, 다음 재타격은 Stay가 아니라 "다음에 다시 부딪힐 때" 발생한다.
        // (호밍이라 보통 계속 접촉/재진입이 생기므로 간단히 유지)
    }

    private bool CanHitNow(int key)
    {
        if (!_hitInfo.TryGetValue(key, out var info))
        {
            // 최초 히트는 무조건 허용
            return true;
        }

        // 횟수 제한
        if (info.count >= Mathf.Max(1, maxHitsPerTarget))
            return false;

        // 재타격이 꺼져 있으면 1회만
        if (sameTargetRehitInterval <= 0f)
            return false;

        // 시간 제한
        return Time.time >= info.nextTime;
    }

    private void ApplyHit(int key, Collider2D other)
    {
        if (!_hitInfo.TryGetValue(key, out var info))
            info = new HitInfo { count = 0, nextTime = 0f };

        info.count += 1;

        if (sameTargetRehitInterval > 0f)
            info.nextTime = Time.time + sameTargetRehitInterval;
        else
            info.nextTime = float.PositiveInfinity;

        _hitInfo[key] = info;

        DamageUtil2D.ApplyDamage(other, _damage);
    }

    private void TryAcquireTarget()
    {
        Vector2 origin = transform.position;

        int count = Physics2D.OverlapCircleNonAlloc(origin, _seekRadius, _acquireHits, _enemyMask);
        for (int i = count; i < _acquireHits.Length; i++) _acquireHits[i] = null;

        if (count <= 0) return;

        float best = float.MaxValue;
        Transform bestT = null;

        for (int i = 0; i < count; i++)
        {
            var c = _acquireHits[i];
            if (c == null) continue;

            int key = GetTargetKey(c);

            // "이미 다 맞춘 적"이면 타겟 후보에서 제외
            if (key != 0 && _hitInfo.TryGetValue(key, out var info))
            {
                if (info.count >= Mathf.Max(1, maxHitsPerTarget))
                    continue;

                // 재타격이 꺼져있고 1회 맞췄으면 제외
                if (sameTargetRehitInterval <= 0f && info.count >= 1)
                    continue;
            }

            Transform t = (c.attachedRigidbody != null) ? c.attachedRigidbody.transform : c.transform;
            float d = ((Vector2)t.position - origin).sqrMagnitude;

            if (d < best)
            {
                best = d;
                bestT = t;
            }
        }

        _target = bestT;
    }

    // 같은 적 판정 키
    private int GetTargetKey(Collider2D col)
    {
        if (col == null) return 0;

        if (col.attachedRigidbody != null)
            return col.attachedRigidbody.GetInstanceID();

        if (!string.IsNullOrEmpty(targetRootComponentTypeName))
        {
            var comps = col.GetComponentsInParent<Component>(true);
            for (int i = 0; i < comps.Length; i++)
            {
                var c = comps[i];
                if (c == null) continue;
                if (c.GetType().Name == targetRootComponentTypeName)
                    return c.GetInstanceID();
            }
        }

        return DamageUtil2D.GetRootInstanceId(col);
    }
}