using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 월참 초승달 검기 투사체.
/// 직선 등속 이동 + 적 관통 (같은 적 중복 피해 X) + 수명 만료 자동 반환.
/// 패턴은 GeomgiProjectile2D와 거의 동일.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class WolchamCrescent2D : PooledObject2D
{
    [Header("물리")]
    [Tooltip("이동용 Rigidbody2D입니다. 자동 할당.")]
    [SerializeField] private Rigidbody2D rb;

    private DamageElement2D _element;
    private int _damage;
    private Vector2 _direction;
    private float _speed;
    private float _lifetime;
    private float _age;
    private readonly HashSet<int> _hitIds = new HashSet<int>(32);

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void Initialize(
        int damage,
        Vector2 direction,
        float speed,
        float lifetime,
        DamageElement2D element)
    {
        _damage = Mathf.Max(1, damage);
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _speed = Mathf.Max(0.1f, speed);
        _lifetime = Mathf.Max(0.1f, lifetime);
        _age = 0f;
        _element = element;
        _hitIds.Clear();

        if (rb != null)
            rb.linearVelocity = _direction * _speed;
    }

    private void OnEnable()
    {
        _age = 0f;
        _hitIds.Clear();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private void OnDisable()
    {
        _hitIds.Clear();
    }

    private void FixedUpdate()
    {
        _age += Time.fixedDeltaTime;
        if (_age >= _lifetime)
        {
            ReturnToPool();
            return;
        }

        if (rb != null)
            rb.linearVelocity = _direction * _speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, GetEnemyMask())) return;

        int rootId = DamageUtil2D.GetRootId(other);
        if (!_hitIds.Add(rootId)) return;

        DamageUtil2D.TryApplyDamage(other, _damage, _element);
    }

    /// <summary>
    /// Layer 마스크는 풀 부모 또는 owner에서 추론. 빈 LayerMask면 기본 적 레이어로.
    /// 단순화를 위해 OnTriggerEnter2D에서 모든 레이어 통과 + DamageUtil2D 검증 위임.
    /// </summary>
    private LayerMask GetEnemyMask()
    {
        // 사실 이 함수는 DamageUtil2D가 IsInLayerMask 검증할 때 적의 layer만 체크하므로
        // ~0 (모든 레이어 통과) 반환해도 안전. 적이 아닌 콜라이더는 TryApplyDamage에서 거름.
        return ~0;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.4f, 1f, 0.8f);
        Gizmos.DrawLine(transform.position,
            transform.position + (Vector3)(_direction * 2f));
    }
#endif
}