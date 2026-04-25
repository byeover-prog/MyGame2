using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 월참 초승달 검기 투사체.
/// 직선 등속 이동 + 적 관통 (같은 적 중복 피해 X) + 수명 만료 자동 반환.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class WolchamCrescent2D : PooledObject2D
{
    [Header("물리")]
    [Tooltip("투사체 Rigidbody2D. 자동 할당.")]
    [SerializeField] private Rigidbody2D rb;

    // ── 런타임 상태 ──
    private int _damage;
    private Vector2 _direction;
    private float _speed;
    private float _lifetime;
    private float _age;
    private LayerMask _enemyMask;
    private readonly HashSet<int> _hitIds = new(16);

    private void Reset()
    {
        // 컴포넌트 자동 세팅 (에디터에서 추가 시)
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

        // 콜라이더는 Trigger여야 관통 가능
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void Initialize(
        int damage,
        Vector2 direction,
        float speed,
        float lifetime,
        LayerMask enemyMask)
    {
        _damage = damage;
        _direction = direction.normalized;
        _speed = speed;
        _lifetime = lifetime;
        _age = 0f;
        _enemyMask = enemyMask;
        _hitIds.Clear();
    }

    private void OnEnable()
    {
        _age = 0f;
        _hitIds.Clear();
    }

    private void Update()
    {
        _age += Time.deltaTime;
        if (_age >= _lifetime)
        {
            ReturnToPool();
            return;
        }

        // 직선 이동
        Vector3 move = (Vector3)(_direction * _speed * Time.deltaTime);
        transform.position += move;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        // 적 레이어 체크
        if ((_enemyMask.value & (1 << other.gameObject.layer)) == 0) return;

        // 죽은 적 필터링
        var health = other.GetComponentInParent<EnemyHealth2D>();
        if (health != null && health.IsDead) return;

        // 중복 히트 방지 (관통 시에도 같은 적엔 한 번만)
        int rootId = DamageUtil2D.GetRootId(other);
        if (!_hitIds.Add(rootId)) return;

        // 데미지
        DamageUtil2D.TryApplyDamage(other, _damage, DamageElement2D.Dark);
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