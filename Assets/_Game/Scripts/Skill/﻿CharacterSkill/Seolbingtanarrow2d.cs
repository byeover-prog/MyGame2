using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 설빙탄 화살 투사체.
/// 3단계 라이프사이클:
///   1. Flight: 직선 비행 (적 충돌까지)
///   2. Attached: 적 위치 추적 (attachDelay 동안)
///   3. Explode: AOE 데미지 + 빙결 적용 후 풀로 반환
///
/// 패턴: ExplodingArrowProjectile2D와 거의 동일.
/// 차이점: 빙결 효과 부여, EnemyRegistry2D로 폭발 대상 검색.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class SeolbingtanArrow2D : PooledObject2D
{
    private enum Phase
    {
        Flight,
        Attached,
        Done
    }

    [Header("물리")]
    [Tooltip("이동용 Rigidbody2D입니다. 자동 할당.")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("히트 콜라이더입니다. 자동 할당.")]
    [SerializeField] private Collider2D hitCollider;

    [Header("폭발 VFX (선택)")]
    [Tooltip("폭발 시 스폰할 VFX 프리팹입니다. 비워두면 VFX 없음.")]
    [SerializeField] private GameObject explosionVfxPrefab;

    [Tooltip("폭발 VFX 자동 정리 시간(초).")]
    [SerializeField] private float explosionVfxLifetime = 1.0f;

    // ── 런타임 상태 ──
    private DamageElement2D _element;
    private int _damage;
    private Vector2 _direction;
    private float _speed;
    private float _maxFlightTime;
    private float _attachDelay;
    private float _explosionRadius;
    private float _frostDuration;
    private float _frostSlowMultiplier;

    private Phase _phase;
    private float _phaseTimer;
    private Transform _attachedTarget;
    private IDamageable2D _attachedDamageable;

    private readonly List<EnemyRegistryMember2D> _explosionTargets = new List<EnemyRegistryMember2D>(32);

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        hitCollider = GetComponent<Collider2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        if (hitCollider != null) hitCollider.isTrigger = true;
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (hitCollider == null) hitCollider = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        if (hitCollider != null) hitCollider.isTrigger = true;
    }

    public void Initialize(
        int damage,
        Vector2 direction,
        float speed,
        float maxFlightTime,
        float attachDelay,
        float explosionRadius,
        DamageElement2D element,
        float frostDuration,
        float frostSlowMultiplier)
    {
        _damage = Mathf.Max(1, damage);
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _speed = Mathf.Max(0.1f, speed);
        _maxFlightTime = Mathf.Max(0.1f, maxFlightTime);
        _attachDelay = Mathf.Max(0.05f, attachDelay);
        _explosionRadius = Mathf.Max(0.1f, explosionRadius);
        _element = element;
        _frostDuration = Mathf.Max(0f, frostDuration);
        _frostSlowMultiplier = frostSlowMultiplier;

        _phase = Phase.Flight;
        _phaseTimer = 0f;
        _attachedTarget = null;
        _attachedDamageable = null;

        if (hitCollider != null) hitCollider.enabled = true;
        if (rb != null) rb.linearVelocity = _direction * _speed;
    }

    private void OnEnable()
    {
        _phase = Phase.Flight;
        _phaseTimer = 0f;
        _attachedTarget = null;
        _attachedDamageable = null;

        // 풀에서 꺼낼 때 부모 해제
        transform.SetParent(null, true);
    }

    private void OnDisable()
    {
        _phase = Phase.Done;
        _attachedTarget = null;
        _attachedDamageable = null;
        transform.SetParent(null, false);
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

    private void FixedUpdate()
    {
        if (_phase != Phase.Flight) return;
        if (rb != null) rb.linearVelocity = _direction * _speed;
    }

    private void UpdateFlight()
    {
        if (_phaseTimer >= _maxFlightTime)
        {
            // 적 못 맞히고 시간 만료 → 그냥 반환
            _phase = Phase.Done;
        }
    }

    private void UpdateAttached()
    {
        // 적이 죽거나 사라지면 즉시 폭발
        if (_attachedTarget == null || !_attachedTarget.gameObject.activeInHierarchy)
        {
            Explode();
            return;
        }

        if (_attachedDamageable != null && _attachedDamageable.IsDead)
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

        // 적 위치 추적 (SetParent 안 함, 위치만 따라감)
        transform.position = _attachedTarget.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_phase != Phase.Flight) return;
        if (other == null) return;

        // 적 레이어 확인은 IDamageable2D 존재로 대체 (베이스 패턴 따름)
        IDamageable2D damageable = other.GetComponentInParent<IDamageable2D>();
        if (damageable == null) return;
        if (damageable.IsDead) return;

        AttachTo(other, damageable);
    }

    private void AttachTo(Collider2D enemyCol, IDamageable2D damageable)
    {
        _phase = Phase.Attached;
        _phaseTimer = 0f;

        _attachedTarget = enemyCol.transform.root;
        _attachedDamageable = damageable;

        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (hitCollider != null) hitCollider.enabled = false;

        // 초기 위치 동기화
        transform.position = _attachedTarget.position;
    }

    private void Explode()
    {
        if (_phase == Phase.Done) return;

        Vector3 explodePos = transform.position;
        transform.SetParent(null, true);
        transform.position = explodePos;

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
        // EnemyRegistry2D O(N)
        _explosionTargets.Clear();
        float sqrR = _explosionRadius * _explosionRadius;
        IReadOnlyList<EnemyRegistryMember2D> members = EnemyRegistry2D.Members;

        for (int i = 0; i < members.Count; i++)
        {
            EnemyRegistryMember2D enemy = members[i];
            if (enemy == null || !enemy.IsValidTarget) continue;
            if ((enemy.Position - (Vector2)center).sqrMagnitude > sqrR) continue;
            _explosionTargets.Add(enemy);
        }

        if (_explosionTargets.Count == 0) return;

        StatusEffectInfo frostInfo = StatusEffectInfo.Frost(_frostDuration, _frostSlowMultiplier);

        for (int i = 0; i < _explosionTargets.Count; i++)
        {
            EnemyRegistryMember2D enemy = _explosionTargets[i];
            if (enemy == null || !enemy.IsValidTarget) continue;

            // 데미지
            DamageUtil2D.TryApplyDamage(enemy.gameObject, _damage, _element);

            // 빙결 부여
            if (_frostDuration > 0f)
            {
                IStatusReceiver[] receivers = enemy.GetComponentsInChildren<IStatusReceiver>(true);
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

        _explosionTargets.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _explosionRadius > 0f ? _explosionRadius : 1.5f);

        if (_phase == Phase.Flight)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position,
                transform.position + (Vector3)(_direction * 1.5f));
        }
    }
#endif
}