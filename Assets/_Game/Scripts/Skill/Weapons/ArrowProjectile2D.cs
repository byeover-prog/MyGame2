using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArrowProjectile2D : PooledObject2D
{
    [Header("참조")]
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
        if (selfCollider == null) selfCollider = GetComponent<Collider2D>();
    }

    private void OnDisable()
    {
        ClearOwnerIgnore();
        age = 0f;
    }

    public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, Vector2 direction, Transform owner)
    {
        enemyMask = mask;
        damage = dmg;
        speed = Mathf.Max(0.1f, spd);
        life = Mathf.Max(0.1f, lifeSeconds);
        age = 0f;

        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        // 진행 방향으로 회전
        if (dir.sqrMagnitude > 0.0001f)
            transform.right = dir;

        ClearOwnerIgnore();
        IgnoreOwnerCollision(owner);
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

        transform.position += (Vector3)(dir * speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        DamageUtil2D.TryApplyDamage(other, damage);
        ReturnToPool();
    }
}