// UTF-8
using UnityEngine;

/// <summary>
/// 발시 투사체: 얼음 화살.
/// 적에게 닿으면 Ice 속성 데미지 1회 적용 후 풀 반납 (비관통).
///
/// [데미지 경로]
/// DamageUtil2D.TryApplyDamage(target, damage, DamageElement2D.Ice)
///   → 데미지 팝업 자동
///   → ElementVFXObserver2D가 빙결 이펙트 자동 부착
///
/// [이동]
/// Rigidbody2D.linearVelocity로 이동 (Transform 직접 이동 금지 — 트리거 누락 방지)
/// </summary>
[DisallowMultipleComponent]
public sealed class BalsiProjectile2D : PooledObject2D
{
    [Header("스프라이트 회전 보정")]
    [Tooltip("스프라이트 기본 전방 각도 보정(도). 0=오른쪽(→)이 전방.")]
    [SerializeField] private float spriteAngleOffset;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    [Header("컴포넌트")]
    [SerializeField] private Rigidbody2D rb;

    // ── 런타임 (Init으로 덮어씀) ──
    private LayerMask _enemyMask;
    private int _damage;
    private float _speed;
    private float _life;
    private float _age;
    private Vector2 _dir;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _age = 0f;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// 투사체 초기화. BalsiWeapon2D에서 호출.
    /// </summary>
    public void Init(LayerMask mask, int dmg, float spd, float life, Vector2 direction)
    {
        _enemyMask = mask;
        _damage = Mathf.Max(1, dmg);
        _speed = Mathf.Max(0.1f, spd);
        _life = Mathf.Max(0.05f, life);
        _dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        // 회전
        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg + spriteAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 물리 이동
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

        // 속도 유지
        if (rb != null)
            rb.linearVelocity = _dir * _speed;
        else
            transform.position += (Vector3)(_dir * (_speed * Time.fixedDeltaTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        // Enemy 레이어만 판정
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, _enemyMask))
            return;

        // ★ Ice 속성으로 데미지 적용 — 팝업 + 속성 VFX 자동
        bool applied = DamageUtil2D.TryApplyDamage(other, _damage, DamageElement2D.Ice);

        // 콜라이더 오브젝트에 없으면 부모(루트)에서 재시도
        if (!applied)
        {
            var rootCol = other.GetComponentInParent<Collider2D>();
            if (rootCol != null && rootCol != other)
                applied = DamageUtil2D.TryApplyDamage(rootCol, _damage, DamageElement2D.Ice);
        }

        if (debugLog && !applied)
            Debug.LogWarning($"[발시] 트리거 발생했으나 데미지 실패: {other.name}", this);

        // 비관통 — 즉시 풀 반납
        ReturnToPool();
    }
}