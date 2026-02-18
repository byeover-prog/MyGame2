using UnityEngine;

public sealed class HomingProjectile2D : MonoBehaviour
{
    [Header("투사체")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer sr;

    [Header("수명")]
    [SerializeField] private float lifetime = 3.5f;

    [Header("탐색")]
    [SerializeField] private float seekRadius = 6f;

    // GC 줄이기: 매 프레임 new 하지 않도록 캐시
    [SerializeField] private int overlapBufferSize = 32;
    private Collider2D[] _buffer;

    private PlayerSkillController _owner;
    private LayerMask _enemyMask;

    private float _speed;
    private float _turnSpeedDeg;
    private int _damage;
    private int _pierceLeft;

    private float _dieAt;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        _buffer = new Collider2D[Mathf.Max(4, overlapBufferSize)];
    }

    /// <summary>
    /// (구버전 API) PlayerSkillController / SkillShooter2D가 호출하는 초기화 함수
    /// </summary>
    public void Setup(
        PlayerSkillController owner,
        Vector2 initialDir,
        float speed,
        float turnSpeedDeg,
        int damage,
        int pierce,
        LayerMask enemyMask)
    {
        _owner = owner;
        _speed = Mathf.Max(0f, speed);
        _turnSpeedDeg = Mathf.Max(0f, turnSpeedDeg);
        _damage = Mathf.Max(0, damage);
        _pierceLeft = Mathf.Max(0, pierce);
        _enemyMask = enemyMask;

        _dieAt = Time.time + Mathf.Max(0.05f, lifetime);

        if (initialDir.sqrMagnitude < 0.0001f) initialDir = Vector2.right;

        rb.linearVelocity = initialDir.normalized * _speed;
        transform.right = initialDir.normalized;
    }

    /// <summary>
    /// (구버전 API) 이펙트 투명도 옵션 반영
    /// SkillVfxSettings가 프로젝트에 없으면 이 함수 자체를 지우거나,
    /// SkillVfxSettings 스크립트를 추가해야 함.
    /// </summary>
    public void ApplyVfxAlphaFromSettings()
    {
        if (sr == null) return;

        float alphaMul = SkillVfxSettings.Instance != null ? SkillVfxSettings.Instance.VfxAlpha : 1f;
        alphaMul = Mathf.Clamp01(alphaMul);

        Color c = sr.color;
        c.a *= alphaMul;
        sr.color = c;
    }

    private void FixedUpdate()
    {
        if (Time.time >= _dieAt)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 vel = rb.linearVelocity;
        if (vel.sqrMagnitude < 0.0001f)
            vel = Vector2.right;

        Vector2 currentDir = vel.normalized;
        Vector2 desiredDir = FindDesiredDir(currentDir);

        Vector2 newDir = RotateTowards(currentDir, desiredDir, _turnSpeedDeg * Time.fixedDeltaTime);
        rb.linearVelocity = newDir * _speed;

        transform.right = newDir;
    }

    private Vector2 FindDesiredDir(Vector2 fallbackDir)
    {
        if (_buffer == null || _buffer.Length == 0)
            return fallbackDir;

        Collider2D best = null;
        float bestSqr = float.MaxValue;
        Vector2 myPos = transform.position;

        int count = Physics2D.OverlapCircleNonAlloc(transform.position, seekRadius, _buffer, _enemyMask);

        for (int i = 0; i < count; i++)
        {
            Collider2D c = _buffer[i];
            _buffer[i] = null;

            if (c == null) continue;

            float sqr = ((Vector2)c.transform.position - myPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = c;
            }
        }

        if (best == null)
            return fallbackDir;

        Vector2 dir = (Vector2)best.transform.position - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.0001f) return fallbackDir;

        return dir.normalized;
    }

    private static Vector2 RotateTowards(Vector2 from, Vector2 to, float maxDeg)
    {
        float maxRad = maxDeg * Mathf.Deg2Rad;
        float angle = Mathf.Atan2(from.x * to.y - from.y * to.x, Vector2.Dot(from, to));
        float clamped = Mathf.Clamp(angle, -maxRad, maxRad);

        float sin = Mathf.Sin(clamped);
        float cos = Mathf.Cos(clamped);

        return new Vector2(
            from.x * cos - from.y * sin,
            from.x * sin + from.y * cos
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        if (!other.TryGetComponent(out EnemyHealth2D enemy))
            return;

        enemy.TakeDamage(_damage);

        if (_pierceLeft <= 0)
        {
            Destroy(gameObject);
            return;
        }

        _pierceLeft -= 1;
    }
}
