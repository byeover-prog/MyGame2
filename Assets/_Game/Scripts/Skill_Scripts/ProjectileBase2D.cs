using UnityEngine;

/// <summary>
/// 공통 투사체 기반 클래스
/// - 수명 관리, 충돌 시 데미지 전달, 관통 처리
/// - 이동 방식(직선/호밍)은 파생 클래스에서 구현
/// </summary>
public abstract class ProjectileBase2D : MonoBehaviour, IProjectile2D
{
    [Header("공통(투사체)")]
    [SerializeField] protected Rigidbody2D rb;

    [Header("공통(수명)")]
    [SerializeField] protected float lifetime = 2.5f;

    [Header("공통(관통)")]
    [SerializeField, Tooltip("0이면 첫 타격에 사라짐, 1이면 1회 관통 후 사라짐")]
    protected int pierceCount = 0;

    protected int _damage;
    protected int _pierceLeft;
    protected LayerMask _targetMask;

    private float _dieAt;

    protected virtual void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 dir, int damage, LayerMask targetMask)
    {
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        _damage = Mathf.Max(0, damage);
        _pierceLeft = Mathf.Max(0, pierceCount);
        _targetMask = targetMask;

        _dieAt = Time.time + Mathf.Max(0.05f, lifetime);

        OnLaunch(dir.normalized);
    }

    protected abstract void OnLaunch(Vector2 dir);

    protected virtual void FixedUpdate()
    {
        if (Time.time >= _dieAt)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        // 레이어 마스크로 1차 필터(성능+확장)
        if (((1 << other.gameObject.layer) & _targetMask) == 0)
            return;

        // 데미지 받을 수 있는 대상만 처리
        if (!other.TryGetComponent<IDamageable2D>(out var dmg))
            return;

        if (dmg.IsDead) return;

        dmg.TakeDamage(_damage);

        if (_pierceLeft <= 0)
        {
            Destroy(gameObject);
            return;
        }

        _pierceLeft -= 1;
    }
}
