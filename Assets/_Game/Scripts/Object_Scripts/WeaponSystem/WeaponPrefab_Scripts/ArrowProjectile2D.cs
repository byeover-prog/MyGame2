using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArrowProjectile2D : PooledObject2D
{
    [Header("참조")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D selfCollider;

    private LayerMask enemyMask;
    private int damage;
    private float speed;
    private float life;
    private float age;
    private Vector2 dir;

    private Collider2D[] _ignoredOwnerColliders;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (selfCollider == null) selfCollider = GetComponent<Collider2D>();

        ConfigureRigidbodyForVelocityMove();
    }

    private void OnDisable()
    {
        // 풀에 반납될 때 상태 초기화(풀링 필수)
        ClearOwnerIgnore();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        age = 0f;
    }

    private void ConfigureRigidbodyForVelocityMove()
    {
        if (rb == null) return;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.simulated = true;
    }

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, Vector2 direction, Transform owner)
    {
        enemyMask = mask;
        damage = dmg;
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.1f, lifeSeconds);
        age = 0f;

        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        ConfigureRigidbodyForVelocityMove();

        // 풀링 재사용 대비: 이전 Ignore 해제 후 새 owner Ignore 적용
        ClearOwnerIgnore();
        IgnoreOwnerCollision(owner);

        if (rb != null)
            rb.linearVelocity = dir * speed;
    }

    private void IgnoreOwnerCollision(Transform owner)
    {
        if (owner == null) return;
        if (selfCollider == null) return;

        _ignoredOwnerColliders = owner.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < _ignoredOwnerColliders.Length; i++)
        {
            var c = _ignoredOwnerColliders[i];
            if (c == null) continue;
            Physics2D.IgnoreCollision(selfCollider, c, true);
        }
    }

    private void ClearOwnerIgnore()
    {
        if (selfCollider == null) return;
        if (_ignoredOwnerColliders == null) return;

        for (int i = 0; i < _ignoredOwnerColliders.Length; i++)
        {
            var c = _ignoredOwnerColliders[i];
            if (c == null) continue;
            Physics2D.IgnoreCollision(selfCollider, c, false);
        }

        _ignoredOwnerColliders = null;
    }

    private void FixedUpdate()
    {
        age += Time.fixedDeltaTime;
        if (age >= life)
        {
            ReturnToPool();
            return;
        }

        if (rb != null)
            rb.linearVelocity = dir * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        DamageUtil2D.TryApplyDamage(other, damage);
        ReturnToPool();
    }
}
