using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StraightPooledProjectile2D : MonoBehaviour, IPooledProjectile2D, IPoolable
{
    [Header("풀 반납 키(런타임 주입)")]
    [Tooltip("풀에서 자동 주입되며, 없으면 반납 불가")]
    [SerializeField] private GameObject prefabKey;

    [Header("컴포넌트")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;
    [SerializeField] private TrailRenderer trail;

    [Header("발사 파라미터(프리팹 기본값)")]
    [Min(0f)][SerializeField] private float speed = 12f;
    [Min(0.01f)][SerializeField] private float lifetime = 2.5f;

    [Header("피격 파라미터(런타임 Launch로 덮어씀)")]
    [SerializeField] private int baseDamage = 30;
    [SerializeField] private LayerMask hitMask;

    [Header("회전(스프라이트 방향 보정)")]
    [Tooltip("스프라이트가 '오른쪽'을 바라보게 그려졌으면 0, '위'를 바라보게 그려졌으면 +90이 보통")]
    [SerializeField] private float rotationOffsetDeg = 0f;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private ProjectilePool _pool;
    private Coroutine _lifeRoutine;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        trail = GetComponent<TrailRenderer>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (col == null) col = GetComponent<Collider2D>();
        if (trail == null) trail = GetComponent<TrailRenderer>();
    }

    private void OnEnable()
    {
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (col != null) col.enabled = true;
        if (trail != null) trail.Clear();

        StopLifeRoutine();
    }

    private void OnDisable()
    {
        StopLifeRoutine();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }

    // IPooledProjectile2D
    public void SetPool(ProjectilePool pool) => _pool = pool;
    public void SetOriginPrefab(GameObject originPrefab) => prefabKey = originPrefab;

    // IPoolable (풀 설계 호환)
    public void BindPool(ProjectilePool pool, GameObject prefabKey)
    {
        _pool = pool;
        this.prefabKey = prefabKey;
    }

    public void OnPoolGet()
    {
        // 풀에서 꺼낼 때 상태 리셋
        if (trail != null) trail.Clear();
        StopLifeRoutine();
    }

    public void OnPoolRelease()
    {
        // 풀로 들어갈 때 정리
        StopLifeRoutine();
        if (trail != null) trail.Clear();
    }

    public void ReleaseToPool()
    {
        ReturnToPool();
    }

    public void Launch(Vector2 dir, int damage, LayerMask targetMask)
    {
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        baseDamage = Mathf.Max(1, damage);

        // targetMask가 0이면 기존 프리팹 기본값(hitMask) 유지
        if (targetMask.value != 0)
            hitMask = targetMask;

        if (trail != null) trail.Clear();

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = dir * speed;
        }

        ApplyRotation(dir);

        if (debugLog)
            Debug.Log($"[StraightPooledProjectile2D] Launch dir={dir} speed={speed} dmg={baseDamage} mask={hitMask.value}", this);

        StopLifeRoutine();
        _lifeRoutine = StartCoroutine(LifeTimer());
    }

    private void ApplyRotation(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffsetDeg);
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(lifetime);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (_pool != null && prefabKey != null)
        {
            _pool.Release(prefabKey, gameObject);
            return;
        }

        gameObject.SetActive(false);

        if (debugLog)
        {
            if (_pool == null) Debug.LogWarning("[StraightPooledProjectile2D] 풀 참조 없음", this);
            if (prefabKey == null) Debug.LogWarning("[StraightPooledProjectile2D] prefabKey 미지정", this);
        }
    }

    private void StopLifeRoutine()
    {
        if (_lifeRoutine != null)
        {
            StopCoroutine(_lifeRoutine);
            _lifeRoutine = null;
        }
    }

    public int Damage => baseDamage;
    public LayerMask HitMask => hitMask;
}
