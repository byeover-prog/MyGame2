using UnityEngine;

[DisallowMultipleComponent]
public sealed class HomingProjectile2D : MonoBehaviour, IPooledProjectile2D
{
    [Header("이동 파라미터")]
    [SerializeField] private float projectileSpeed = 8f;

    [Tooltip("초당 회전 속도(도)")]
    [SerializeField] private float turnSpeedDeg = 360f;

    [Header("수명(초)")]
    [SerializeField] private float lifeTime = 6f;

    private int _damage = 1;
    private LayerMask _enemyMask;

    private Vector2 _velocityDir = Vector2.right;
    private Transform _target;

    private float _dieAt;
    private bool _launched; // Launch 호출 여부

    private ProjectilePool _pool;
    private GameObject _originPrefab;

    public void SetOriginPrefab(GameObject prefab) => _originPrefab = prefab;
    public void SetPool(ProjectilePool pool) => _pool = pool;

    private void OnEnable()
    {
        // 풀에서 꺼낼 때 기본값 방어 (Launch 전 즉사 방지)
        _launched = false;
        _target = null;
        _dieAt = float.PositiveInfinity;
    }

    public void Launch(Vector2 dir, int damage, LayerMask enemyMask)
    {
        _launched = true;

        _velocityDir = (dir.sqrMagnitude < 0.0001f) ? Vector2.right : dir.normalized;
        _damage = Mathf.Max(1, damage);
        _enemyMask = enemyMask;

        _dieAt = Time.time + Mathf.Max(0.1f, lifeTime);

        _target = null;
        TryAcquireTarget();

        float angle = Mathf.Atan2(_velocityDir.y, _velocityDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void FixedUpdate()
    {
        if (!_launched) return; // Launch 전엔 아무것도 안 함

        if (Time.time >= _dieAt)
        {
            Release();
            return;
        }

        if (_target == null || !_target.gameObject.activeInHierarchy)
            TryAcquireTarget();

        if (_target != null)
        {
            Vector2 to = ((Vector2)_target.position - (Vector2)transform.position);
            if (to.sqrMagnitude > 0.0001f)
            {
                Vector2 desired = to.normalized;

                float maxRad = turnSpeedDeg * Mathf.Deg2Rad * Time.fixedDeltaTime;
                _velocityDir = RotateTowards(_velocityDir, desired, maxRad);

                float angle = Mathf.Atan2(_velocityDir.y, _velocityDir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        transform.position += (Vector3)(_velocityDir * projectileSpeed * Time.fixedDeltaTime);
    }

    private static Vector2 RotateTowards(Vector2 from, Vector2 to, float maxRadians)
    {
        from = from.sqrMagnitude < 0.0001f ? Vector2.right : from.normalized;
        to = to.sqrMagnitude < 0.0001f ? Vector2.right : to.normalized;

        float angle = Mathf.Atan2(from.x * to.y - from.y * to.x, Vector2.Dot(from, to));
        float clamped = Mathf.Clamp(angle, -maxRadians, maxRadians);

        float cos = Mathf.Cos(clamped);
        float sin = Mathf.Sin(clamped);

        return new Vector2(
            from.x * cos - from.y * sin,
            from.x * sin + from.y * cos
        ).normalized;
    }

    private void TryAcquireTarget()
    {
        if (_enemyMask.value == 0) return;

        const float range = 20f;
        var hits = Physics2D.OverlapCircleAll(transform.position, range, _enemyMask);
        if (hits == null || hits.Length == 0) return;

        float best = float.MaxValue;
        Transform bestT = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i];
            if (c == null) continue;

            float d = ((Vector2)c.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = c.transform;
            }
        }

        _target = bestT;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_launched) return;
        if (((1 << other.gameObject.layer) & _enemyMask.value) == 0) return;

        // 공통 데미지 파이프라인 우선
        if (!DamageUtil2D.TryApplyDamage(other, _damage))
        {
            // fallback
            if (other.TryGetComponent(out EnemyHealth2D hp))
                hp.TakeDamage(_damage);
        }

        Release();
    }

    private void Release()
    {
        if (_pool != null && _originPrefab != null)
        {
            _pool.Release(_originPrefab, gameObject);
            return;
        }

        gameObject.SetActive(false);
    }
}