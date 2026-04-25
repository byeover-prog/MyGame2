using UnityEngine;

/// <summary>
/// 뇌운 구름 본체.
///   - 가장 가까운 적을 따라다님 (followSpeed로 이동)
///   - 적이 죽거나 사라지면 새 타겟 재탐색
///   - boltInterval 마다 번개를 아래로 발사 (자식 풀 NoeunBolt2D)
///   - lifetime 만료 시 풀로 반환
///
/// 본체는 데미지 X. 번개만 데미지.
/// </summary>
[DisallowMultipleComponent]
public sealed class NoeunCloud2D : PooledObject2D
{
    [Header("VFX")]
    [Tooltip("구름 시각용 SpriteRenderer. 자동 탐색.")]
    [SerializeField] private SpriteRenderer cloudRenderer;

    [Tooltip("구름 부유 애니메이션 진폭(유닛). 0이면 정적.")]
    [SerializeField] private float floatAmplitude = 0.15f;

    [Tooltip("구름 부유 애니메이션 주기(초).")]
    [SerializeField] private float floatPeriod = 1.2f;

    [Header("타겟 재탐색")]
    [Tooltip("타겟 잃은 후 재탐색 간격(초).")]
    [SerializeField] private float retargetInterval = 0.5f;

    // ── 런타임 상태 ──
    private int _damage;
    private float _boltRadius;
    private float _lifetime;
    private float _boltInterval;
    private float _followSpeed;
    private float _seekRadius;
    private LayerMask _enemyMask;
    private ProjectilePool2D _boltPool;

    private float _age;
    private float _boltTimer;
    private float _retargetTimer;
    private Transform _currentTarget;
    private Vector3 _baseLocalPos;

    private void Awake()
    {
        if (cloudRenderer == null)
            cloudRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    public void Initialize(
        int damage,
        float boltRadius,
        float lifetime,
        float boltInterval,
        float followSpeed,
        float seekRadius,
        LayerMask enemyMask,
        ProjectilePool2D boltPool)
    {
        _damage = damage;
        _boltRadius = boltRadius;
        _lifetime = lifetime;
        _boltInterval = boltInterval;
        _followSpeed = followSpeed;
        _seekRadius = seekRadius;
        _enemyMask = enemyMask;
        _boltPool = boltPool;

        _age = 0f;
        _boltTimer = boltInterval; // 첫 번개는 interval 후 발사
        _retargetTimer = 0f;
        _currentTarget = FindNearestEnemy(transform.position);

        // 시각 초기화
        if (cloudRenderer != null)
        {
            Color c = cloudRenderer.color;
            c.a = 1f;
            cloudRenderer.color = c;
        }
    }

    private void OnEnable()
    {
        _age = 0f;
        _baseLocalPos = transform.localPosition;
    }

    private void OnDisable()
    {
        _currentTarget = null;
    }

    private void Update()
    {
        _age += Time.deltaTime;

        // 수명 만료
        if (_age >= _lifetime)
        {
            ReturnToPool();
            return;
        }

        // 페이드 아웃 (마지막 0.5초)
        if (cloudRenderer != null && _lifetime - _age < 0.5f)
        {
            float fadeT = Mathf.Clamp01((_lifetime - _age) / 0.5f);
            Color c = cloudRenderer.color;
            c.a = fadeT;
            cloudRenderer.color = c;
        }

        // 타겟 추적
        UpdateTargeting();
        UpdateMovement();

        // 부유 애니메이션 (시각만, 위치 판정엔 영향 없음 — Y 진동)
        if (cloudRenderer != null && floatAmplitude > 0.001f)
        {
            float bob = Mathf.Sin(_age / Mathf.Max(0.1f, floatPeriod) * Mathf.PI * 2f) * floatAmplitude;
            Vector3 bobPos = cloudRenderer.transform.localPosition;
            bobPos.y = bob;
            cloudRenderer.transform.localPosition = bobPos;
        }

        // 번개 발사
        _boltTimer -= Time.deltaTime;
        if (_boltTimer <= 0f)
        {
            FireBolt();
            _boltTimer = _boltInterval;
        }
    }

    private void UpdateTargeting()
    {
        // 현재 타겟이 죽었거나 사라졌으면 재탐색 타이머 시작
        bool needRetarget = false;
        if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy)
        {
            needRetarget = true;
        }
        else
        {
            var health = _currentTarget.GetComponentInParent<EnemyHealth2D>();
            if (health != null && health.IsDead)
                needRetarget = true;
        }

        if (needRetarget)
        {
            _retargetTimer -= Time.deltaTime;
            if (_retargetTimer <= 0f)
            {
                _currentTarget = FindNearestEnemy(transform.position);
                _retargetTimer = retargetInterval;
            }
        }
    }

    private void UpdateMovement()
    {
        if (_currentTarget == null) return;

        Vector3 toTarget = _currentTarget.position - transform.position;
        float dist = toTarget.magnitude;
        if (dist < 0.05f) return;

        Vector3 dir = toTarget / dist;
        float moveDist = _followSpeed * Time.deltaTime;
        if (moveDist > dist) moveDist = dist;

        transform.position += dir * moveDist;
    }

    private void FireBolt()
    {
        if (_boltPool == null) return;

        // 번개 시작 위치: 구름 위치
        Vector3 spawnPos = transform.position;

        var bolt = _boltPool.Get<NoeunBolt2D>(spawnPos, Quaternion.identity);
        if (bolt == null) return;

        // 번개는 즉시 폭발 (구름 위치에서 AOE)
        bolt.Initialize(
            damage: _damage,
            radius: _boltRadius,
            enemyMask: _enemyMask);
    }

    private Transform FindNearestEnemy(Vector3 origin)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, _seekRadius, _enemyMask);
        if (hits == null || hits.Length == 0) return null;

        Transform closest = null;
        float closestDistSqr = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i];
            if (col == null) continue;

            var health = col.GetComponentInParent<EnemyHealth2D>();
            if (health != null && health.IsDead) continue;

            float distSqr = ((Vector2)col.bounds.center - (Vector2)origin).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = col.transform;
            }
        }

        return closest;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 폭발 범위
        Gizmos.color = new Color(1f, 1f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _boltRadius > 0f ? _boltRadius : 1f);

        // 타겟 라인
        if (_currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _currentTarget.position);
        }
    }
#endif
}