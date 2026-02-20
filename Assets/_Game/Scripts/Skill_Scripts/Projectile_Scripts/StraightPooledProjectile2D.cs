using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StraightPooledProjectile2D : PooledObject2D
{
    [Header("참조")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D selfCollider;

    [Header("충돌 마스크(Init/Launch에서 덮어씀)")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private LayerMask obstacleMask;

    [Header("기본값(호환 Launch용)")]
    [SerializeField] private float defaultSpeed = 10f;
    [SerializeField] private float defaultLifeSeconds = 2f;

    private int _damage;
    private float _speed;
    private float _lifeSeconds;
    private float _age;
    private Vector2 _dir;

    private GameObject _originPrefab;
    private Collider2D[] _ignoredOwnerColliders;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (selfCollider == null) selfCollider = GetComponent<Collider2D>();
        ConfigureRigidbodyForVelocityMove();
    }

    private void OnDisable()
    {
        ClearOwnerIgnore();

        _age = 0f;
        _damage = 0;
        _speed = 0f;
        _lifeSeconds = 0f;
        _dir = Vector2.right;
        _originPrefab = null;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
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

    // --------------------------
    // 필수 초기화 API(권장)
    // --------------------------
    public void Init(
        LayerMask enemyLayerMask,
        LayerMask obstacleLayerMask,
        int damage,
        float projectileSpeed,
        float lifeSeconds,
        Vector2 direction,
        Transform owner
    )
    {
        enemyMask = enemyLayerMask;
        obstacleMask = obstacleLayerMask;

        _damage = Mathf.Max(0, damage);
        _speed = Mathf.Max(0.1f, projectileSpeed);
        _lifeSeconds = Mathf.Max(0.05f, lifeSeconds);
        _age = 0f;

        _dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        ConfigureRigidbodyForVelocityMove();

        ClearOwnerIgnore();
        IgnoreOwnerCollision(owner);

        if (rb != null)
            rb.linearVelocity = _dir * _speed;
    }

    public void Init(
        LayerMask enemyLayerMask,
        int damage,
        float projectileSpeed,
        float lifeSeconds,
        Vector2 direction,
        Transform owner
    )
    {
        Init(enemyLayerMask, 0, damage, projectileSpeed, lifeSeconds, direction, owner);
    }

    // --------------------------
    // 호환 API(기존 코드 유지용)
    // --------------------------
    public void SetOriginPrefab(GameObject originPrefab)
    {
        _originPrefab = originPrefab;
    }

    public GameObject GetOriginPrefab()
    {
        return _originPrefab;
    }

    // PlayerWeaponSystem2D 호환: Launch(Vector2, int, LayerMask)
    public void Launch(Vector2 dir, int damage, LayerMask enemyLayerMask)
    {
        Init(enemyLayerMask, 0, damage, defaultSpeed, defaultLifeSeconds, dir, owner: null);
    }

    public void Launch(LayerMask enemyLayerMask, int damage, float projectileSpeed, float lifeSeconds, Vector2 direction, Transform owner)
    {
        Init(enemyLayerMask, damage, projectileSpeed, lifeSeconds, direction, owner);
    }

    public void Launch(LayerMask enemyLayerMask, LayerMask obstacleLayerMask, int damage, float projectileSpeed, float lifeSeconds, Vector2 direction, Transform owner)
    {
        Init(enemyLayerMask, obstacleLayerMask, damage, projectileSpeed, lifeSeconds, direction, owner);
    }

    public void Launch(Vector2 origin, LayerMask enemyLayerMask, int damage, float projectileSpeed, float lifeSeconds, Vector2 direction, Transform owner)
    {
        transform.position = origin;
        Init(enemyLayerMask, damage, projectileSpeed, lifeSeconds, direction, owner);
    }

    public void Launch(Vector2 origin, Quaternion rotation, LayerMask enemyLayerMask, int damage, float projectileSpeed, float lifeSeconds, Vector2 direction, Transform owner)
    {
        transform.SetPositionAndRotation(origin, rotation);
        Init(enemyLayerMask, damage, projectileSpeed, lifeSeconds, direction, owner);
    }

    // --------------------------
    // 런타임 동작
    // --------------------------
    private void FixedUpdate()
    {
        _age += Time.fixedDeltaTime;
        if (_age >= _lifeSeconds)
        {
            ReturnToPool();
            return;
        }

        if (rb != null)
            rb.linearVelocity = _dir * _speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        int otherLayer = other.gameObject.layer;

        if (obstacleMask.value != 0 && DamageUtil2D.IsInLayerMask(otherLayer, obstacleMask))
        {
            ReturnToPool();
            return;
        }

        if (!DamageUtil2D.IsInLayerMask(otherLayer, enemyMask))
            return;

        DamageUtil2D.TryApplyDamage(other, _damage);
        ReturnToPool();
    }

    // --------------------------
    // 오너 충돌 무시(플레이어 몸에 박힘 방지)
    // --------------------------
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
}
