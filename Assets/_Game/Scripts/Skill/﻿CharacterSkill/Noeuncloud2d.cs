using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 뇌운 구름 본체.
///   - 가장 가까운 적을 따라다님 (followSpeed로 이동)
///   - 적이 죽거나 사라지면 새 타겟 재탐색
///   - boltInterval 마다 번개를 발사 (자식 풀 NoeunBolt2D)
///   - lifetime 만료 시 풀로 반환
///
/// 본체는 데미지 X. 번개만 데미지.
/// 적 검색은 EnemyRegistry2D 사용 (Physics 쿼리 X).
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
    private DamageElement2D _element;
    private int _damage;
    private float _boltRadius;
    private float _lifetime;
    private float _boltInterval;
    private float _followSpeed;
    private float _seekRadius;
    private ProjectilePool2D _boltPool;

    private float _age;
    private float _boltTimer;
    private float _retargetTimer;
    private EnemyRegistryMember2D _currentTarget;
    private Color _baseSpriteColor;
    private bool _hasBaseSpriteColor;

    private void Awake()
    {
        if (cloudRenderer == null)
            cloudRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (cloudRenderer != null)
        {
            _baseSpriteColor = cloudRenderer.color;
            _hasBaseSpriteColor = true;
        }
    }

    public void Initialize(
        int damage,
        float boltRadius,
        float lifetime,
        float boltInterval,
        float followSpeed,
        float seekRadius,
        DamageElement2D element,
        ProjectilePool2D boltPool)
    {
        _damage = Mathf.Max(1, damage);
        _boltRadius = Mathf.Max(0.1f, boltRadius);
        _lifetime = Mathf.Max(0.1f, lifetime);
        _boltInterval = Mathf.Max(0.05f, boltInterval);
        _followSpeed = Mathf.Max(0f, followSpeed);
        _seekRadius = Mathf.Max(1f, seekRadius);
        _element = element;
        _boltPool = boltPool;

        _age = 0f;
        _boltTimer = _boltInterval;
        _retargetTimer = 0f;
        _currentTarget = FindNearestEnemy(transform.position);

        if (_hasBaseSpriteColor && cloudRenderer != null)
        {
            cloudRenderer.color = _baseSpriteColor;
        }
    }

    private void OnEnable()
    {
        _age = 0f;
    }

    private void OnDisable()
    {
        _currentTarget = null;
        if (_hasBaseSpriteColor && cloudRenderer != null)
            cloudRenderer.color = _baseSpriteColor;
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
        if (_hasBaseSpriteColor && cloudRenderer != null && _lifetime - _age < 0.5f)
        {
            float fadeT = Mathf.Clamp01((_lifetime - _age) / 0.5f);
            Color c = _baseSpriteColor;
            c.a *= fadeT;
            cloudRenderer.color = c;
        }

        // 타겟 추적
        UpdateTargeting();
        UpdateMovement();

        // 부유 애니메이션 (시각만)
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
        // 현재 타겟이 죽었거나 사라졌으면 재탐색
        bool needRetarget = (_currentTarget == null || !_currentTarget.IsValidTarget);

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
        if (_currentTarget == null || !_currentTarget.IsValidTarget) return;

        Vector2 targetPos = _currentTarget.Position;
        Vector2 currentPos = transform.position;
        Vector2 toTarget = targetPos - currentPos;
        float dist = toTarget.magnitude;
        if (dist < 0.05f) return;

        Vector2 dir = toTarget / dist;
        float moveDist = _followSpeed * Time.deltaTime;
        if (moveDist > dist) moveDist = dist;

        transform.position = currentPos + dir * moveDist;
    }

    private void FireBolt()
    {
        if (_boltPool == null) return;

        Vector3 spawnPos = transform.position;

        var bolt = _boltPool.Get<NoeunBolt2D>(spawnPos, Quaternion.identity);
        if (bolt == null) return;

        bolt.Initialize(
            damage: _damage,
            radius: _boltRadius,
            element: _element);
    }

    private EnemyRegistryMember2D FindNearestEnemy(Vector3 origin)
    {
        if (EnemyRegistry2D.TryGetNearest(origin, _seekRadius, out EnemyRegistryMember2D enemy))
            return enemy;
        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _boltRadius > 0f ? _boltRadius : 1f);

        if (_currentTarget != null && _currentTarget.IsValidTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _currentTarget.Position);
        }
    }
#endif
}