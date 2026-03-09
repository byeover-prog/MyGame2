// UTF-8
using UnityEngine;

/// <summary>
/// 낙뢰부 부적 투사체
/// - PooledObject2D 상속 (ProjectilePool2D에서 풀링)
/// - 직선 이동 → 적 히트 시 "적의 위치"로 thunderCallback 호출 → 풀 반납
/// </summary>
[DisallowMultipleComponent]
public sealed class ThunderTalismanProjectile2D : PooledObject2D
{
    [Header("회전(스프라이트 보정)")]
    [SerializeField] private float spriteAngleOffset = 0f;

    [Header("컴포넌트")]
    [SerializeField] private Rigidbody2D rb;

    private LayerMask _enemyMask;
    private int _damage;
    private float _speed;
    private float _life;
    private float _age;
    private Vector2 _dir;

    // ★ 콜백: Vector2 = 적의 위치, int = 데미지, float = 범위
    private System.Action<Vector2> _thunderCallback;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _age = 0f;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    public void Init(
        LayerMask mask,
        int dmg,
        float spd,
        float life,
        Vector2 dir,
        System.Action<Vector2> thunderCallback)
    {
        _enemyMask = mask;
        _damage = Mathf.Max(1, dmg);
        _speed = Mathf.Max(0.1f, spd);
        _life = Mathf.Max(0.1f, life);
        _dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        _thunderCallback = thunderCallback;

        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg + spriteAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (rb != null)
            rb.linearVelocity = _dir * _speed;
    }

    private void FixedUpdate()
    {
        _age += Time.fixedDeltaTime;

        if (_age >= _life)
        {
            ReturnToPool();
            return;
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

        // ★ 핵심 수정: "적의 위치"를 콜백에 전달 (부적 위치가 아님!)
        // 적의 Transform 중심 위치로 번개가 떨어져야 자연스러움
        Vector2 enemyPosition = other.transform.position;
        _thunderCallback?.Invoke(enemyPosition);

        ReturnToPool();
    }
}