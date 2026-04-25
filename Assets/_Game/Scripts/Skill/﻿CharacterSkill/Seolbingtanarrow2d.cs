using UnityEngine;

/// <summary>
/// 설빙탄 화살 투사체.
/// 3단계 라이프사이클:
///   1. Flight: 직선 비행 (적 충돌까지)
///   2. Attached: 적의 자식이 되어 따라다님 (attachDelay 동안)
///   3. Explode: AOE 데미지 + 빙결 적용 후 풀로 반환
///
/// 비행 단계에서 maxFlightTime 만료 시 폭발 없이 반환.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class SeolbingtanArrow2D : PooledObject2D
{
    private enum Phase
    {
        Flight,
        Attached,
        Done
    }

    [Header("폭발 VFX")]
    [Tooltip("폭발 시 스폰할 VFX 프리팹입니다. 비워두면 VFX 없음.")]
    [SerializeField] private GameObject explosionVfxPrefab;

    [Tooltip("폭발 VFX 자동 정리 시간(초).")]
    [SerializeField] private float explosionVfxLifetime = 1.0f;

    // ── 런타임 상태 ──
    private int _damage;
    private Vector2 _direction;
    private float _speed;
    private float _maxFlightTime;
    private float _attachDelay;
    private float _explosionRadius;
    private LayerMask _enemyMask;
    private float _frostDuration;
    private float _frostSlowMultiplier;

    private Phase _phase;
    private float _phaseTimer;
    private Transform _attachedTarget;

    private static readonly Collider2D[] s_explosionBuffer = new Collider2D[32];

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void Initialize(
        int damage,
        Vector2 direction,
        float speed,
        float maxFlightTime,
        float attachDelay,
        float explosionRadius,
        LayerMask enemyMask,
        float frostDuration,
        float frostSlowMultiplier)
    {
        _damage = damage;
        _direction = direction.normalized;
        _speed = speed;
        _maxFlightTime = maxFlightTime;
        _attachDelay = attachDelay;
        _explosionRadius = explosionRadius;
        _enemyMask = enemyMask;
        _frostDuration = frostDuration;
        _frostSlowMultiplier = frostSlowMultiplier;

        _phase = Phase.Flight;
        _phaseTimer = 0f;
        _attachedTarget = null;
    }

    private void OnEnable()
    {
        _phase = Phase.Flight;
        _phaseTimer = 0f;
        _attachedTarget = null;

        // 풀에서 꺼낼 때 부모 해제 (이전 사용에서 적의 자식으로 붙은 채로 남았을 수도)
        transform.SetParent(null, true);
    }

    private void OnDisable()
    {
        // 풀로 돌아갈 때 부모 해제
        transform.SetParent(null, false);
        _attachedTarget = null;
    }

    private void Update()
    {
        _phaseTimer += Time.deltaTime;

        switch (_phase)
        {
            case Phase.Flight:
                UpdateFlight();
                break;

            case Phase.Attached:
                UpdateAttached();
                break;

            case Phase.Done:
                ReturnToPool();
                break;
        }
    }

    // ── Flight: 직선 비행 ──
    private void UpdateFlight()
    {
        if (_phaseTimer >= _maxFlightTime)
        {
            // 적 못 맞히고 시간 만료 → 그냥 반환 (폭발 X)
            _phase = Phase.Done;
            return;
        }

        Vector3 move = (Vector3)(_direction * _speed * Time.deltaTime);
        transform.position += move;
    }

    // ── Attached: 적 따라다님 ──
    private void UpdateAttached()
    {
        // 적이 죽거나 사라지면 즉시 폭발
        if (_attachedTarget == null || !_attachedTarget.gameObject.activeInHierarchy)
        {
            Explode();
            return;
        }

        // 적이 살아있는지 체크
        var health = _attachedTarget.GetComponentInParent<EnemyHealth2D>();
        if (health != null && health.IsDead)
        {
            Explode();
            return;
        }

        // attachDelay 만료 시 폭발
        if (_phaseTimer >= _attachDelay)
        {
            Explode();
            return;
        }

        // 위치 동기화 (Transform 부모-자식 관계라 자동이지만, 안전을 위해)
        transform.position = _attachedTarget.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_phase != Phase.Flight) return;
        if (other == null) return;

        // 적 레이어 체크
        if ((_enemyMask.value & (1 << other.gameObject.layer)) == 0) return;

        // 죽은 적 필터링
        var health = other.GetComponentInParent<EnemyHealth2D>();
        if (health != null && health.IsDead) return;

        AttachToEnemy(other);
    }

    private void AttachToEnemy(Collider2D enemyCol)
    {
        // 적의 루트 Transform 찾기
        Transform target = enemyCol.transform;
        var rootHealth = enemyCol.GetComponentInParent<EnemyHealth2D>();
        if (rootHealth != null) target = rootHealth.transform;

        _attachedTarget = target;

        // 자식으로 부착 (적이 움직이면 같이 따라감)
        transform.SetParent(target, true);
        transform.localPosition = Vector3.zero;

        _phase = Phase.Attached;
        _phaseTimer = 0f;
    }

    private void Explode()
    {
        if (_phase == Phase.Done) return;

        // 부모 해제 (안전)
        Vector3 explodePos = transform.position;
        transform.SetParent(null, true);
        transform.position = explodePos;

        // AOE 데미지 적용
        ApplyExplosionDamage(explodePos);

        // 폭발 VFX 스폰
        if (explosionVfxPrefab != null)
        {
            var vfx = Instantiate(explosionVfxPrefab, explodePos, Quaternion.identity);
            Destroy(vfx, explosionVfxLifetime);
        }

        _phase = Phase.Done;
    }

    private void ApplyExplosionDamage(Vector3 center)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(_enemyMask);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        int hitCount = Physics2D.OverlapCircle(
            (Vector2)center, _explosionRadius, filter, s_explosionBuffer);

        if (hitCount == 0) return;

        StatusEffectInfo frostInfo = StatusEffectInfo.Frost(_frostDuration, _frostSlowMultiplier);
        var hitIds = new System.Collections.Generic.HashSet<int>();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = s_explosionBuffer[i];
            if (col == null) continue;

            // 죽은 적 필터링
            var health = col.GetComponentInParent<EnemyHealth2D>();
            if (health != null && health.IsDead) continue;

            // 중복 히트 방지
            int rootId = DamageUtil2D.GetRootId(col);
            if (!hitIds.Add(rootId)) continue;

            // 데미지
            DamageUtil2D.TryApplyDamage(col, _damage, DamageElement2D.Ice);

            // 빙결 부여
            IStatusReceiver[] receivers = col.GetComponentsInChildren<IStatusReceiver>(true);
            if (receivers != null)
            {
                for (int j = 0; j < receivers.Length; j++)
                {
                    if (receivers[j] != null)
                        receivers[j].TryApplyStatus(frostInfo);
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 폭발 반경 (파랑)
        Gizmos.color = new Color(0.4f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _explosionRadius > 0f ? _explosionRadius : 1.5f);

        // 비행 방향 (흰색)
        if (_phase == Phase.Flight)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position,
                transform.position + (Vector3)(_direction * 1.5f));
        }
    }
#endif
}