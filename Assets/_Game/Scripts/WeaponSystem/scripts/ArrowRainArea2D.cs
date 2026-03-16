// UTF-8
// [구현 원리 요약]
// - 틱마다 OverlapCircleAll을 만들지 않고 재사용 버퍼로 범위 적을 처리한다.
// - 장판형 스킬의 GC를 줄여 다수 적 상황 프레임 저하를 완화한다.
using UnityEngine;

/// <summary>
/// 화살비 장판 영역.
/// 지정된 반경 안의 적에게 틱 간격으로 지속 피해를 준다.
/// </summary>
[DisallowMultipleComponent]
public sealed class ArrowRainArea2D : MonoBehaviour
{
    [Header("장판 범위")]
    [Tooltip("장판 반경")]
    [Min(0.1f)]
    [SerializeField] private float radius = 2.0f;

    [Tooltip("장판 지속시간 (초)")]
    [Min(0f)]
    [SerializeField] private float durationSeconds = 3.0f;

    [Tooltip("장판 스프라이트 렌더러 (자동 탐색)")]
    [SerializeField] private SpriteRenderer areaSpriteRenderer;

    [Tooltip("장판 투명도")]
    [Range(0f, 1f)]
    [SerializeField] private float areaAlpha = 0.55f;

    [Header("이펙트")]
    [Tooltip("파티클 VFX (자동 탐색)")]
    [SerializeField] private ParticleSystem effectVfx;

    [Tooltip("VFX를 장판 반경에 맞게 스케일링할지 여부")]
    [SerializeField] private bool scaleVfxToRadius = true;

    [Tooltip("VFX 기본 반경 (스케일 기준값)")]
    [Min(0.1f)]
    [SerializeField] private float vfxBaseRadius = 2.0f;

    [Header("피해량 (틱)")]
    [Tooltip("틱 간격 (초)")]
    [Min(0.05f)]
    [SerializeField] private float damageTickInterval = 0.3f;

    [Tooltip("틱당 피해량")]
    [Min(0)]
    [SerializeField] private int damagePerTick = 5;

    [Tooltip("적 레이어 마스크")]
    [SerializeField] private LayerMask enemyLayerMask;

    [Header("성능")]
    [Tooltip("OverlapCircle 버퍼 크기 (적 동시 피격 최대 수)")]
    [SerializeField, Min(8)] private int hitBufferSize = 64;

    [Header("디버그")]
    [Tooltip("틱 데미지 로그 출력")]
    [SerializeField] private bool debugLog = false;

    private CircleCollider2D circleTrigger;
    private float tickTimer;
    private float aliveTimer;
    private bool _setupDone;

    private Collider2D[] _hitBuffer;
    private ContactFilter2D _enemyFilter;
    private bool _filterReady;
    private bool _overflowWarned;

    private void Awake()
    {
        circleTrigger = GetComponent<CircleCollider2D>();
        if (circleTrigger != null) circleTrigger.isTrigger = true;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        if (areaSpriteRenderer == null)
            areaSpriteRenderer = GetComponent<SpriteRenderer>();
        if (effectVfx == null)
            effectVfx = GetComponentInChildren<ParticleSystem>(true);

        _hitBuffer = new Collider2D[Mathf.Max(8, hitBufferSize)];
    }

    /// <summary>장판 설정 (Weapon에서 호출)</summary>
    public void Setup(float newRadius, float newDurationSeconds, float newDamageTickInterval, int newDamagePerTick, LayerMask newEnemyMask)
    {
        radius = Mathf.Max(0.5f, newRadius);
        durationSeconds = Mathf.Max(0.25f, newDurationSeconds);
        damageTickInterval = Mathf.Max(0.05f, newDamageTickInterval);
        damagePerTick = Mathf.Max(1, newDamagePerTick);
        enemyLayerMask = newEnemyMask;

        tickTimer = 0f;
        aliveTimer = 0f;
        _setupDone = true;
        _filterReady = false;
        _overflowWarned = false;

        ApplyVisuals();

        if (debugLog)
            Debug.Log($"[ArrowRainArea2D] Setup: 반경={radius:F1}, 피해량={damagePerTick}, 틱={damageTickInterval:F2}초, 지속={durationSeconds:F1}초", this);
    }

    private void OnEnable()
    {
        tickTimer = 0f;
        aliveTimer = 0f;

        ApplyVisuals();

        if (effectVfx != null)
        {
            effectVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effectVfx.Play(true);
        }

        if (_setupDone && damagePerTick > 0)
            DoTickDamage();
    }

    private void OnDisable()
    {
        if (effectVfx != null)
            effectVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void Update()
    {
        if (!_setupDone) return;

        if (durationSeconds > 0f)
        {
            aliveTimer += Time.deltaTime;
            if (aliveTimer >= durationSeconds)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        tickTimer += Time.deltaTime;
        if (tickTimer >= damageTickInterval)
        {
            tickTimer -= damageTickInterval;
            DoTickDamage();
        }
    }

    private void EnsureFilter()
    {
        if (_filterReady) return;

        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(enemyLayerMask);
        _enemyFilter.useTriggers = true;
        _filterReady = true;
    }

    private void DoTickDamage()
    {
        if (damagePerTick <= 0) return;

        EnsureFilter();

        Vector2 center = transform.position;
        int hitCount = Physics2D.OverlapCircle(center, radius, _enemyFilter, _hitBuffer);
        if (hitCount <= 0) return;

        if (!_overflowWarned && hitCount >= _hitBuffer.Length)
        {
            _overflowWarned = true;
            Debug.LogWarning($"[ArrowRainArea2D] hitBuffer가 가득 찼습니다. hitBufferSize를 늘리세요. size={_hitBuffer.Length}", this);
        }

        int damaged = 0;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = _hitBuffer[i];
            if (col == null || !col.gameObject.activeInHierarchy) continue;

            bool applied = DamageUtil2D.TryApplyDamage(col, damagePerTick);
            if (applied) damaged++;
        }

        if (debugLog && damaged > 0)
            Debug.Log($"[ArrowRainArea2D] 적중: {damagePerTick} x {damaged}명", this);
    }

    private void ApplyVisuals()
    {
        if (circleTrigger != null)
            circleTrigger.radius = radius;

        if (areaSpriteRenderer != null && areaSpriteRenderer.sprite != null)
        {
            float spriteSize = areaSpriteRenderer.sprite.bounds.size.x;
            if (spriteSize > 0.001f)
            {
                float s = (radius * 2f) / spriteSize;
                areaSpriteRenderer.transform.localScale = new Vector3(s, s, 1f);
            }
        }

        if (areaSpriteRenderer != null)
        {
            Color c = areaSpriteRenderer.color;
            c.a = areaAlpha;
            areaSpriteRenderer.color = c;
        }

        if (effectVfx != null && scaleVfxToRadius)
        {
            float s = Mathf.Max(0.1f, radius / Mathf.Max(0.1f, vfxBaseRadius));
            effectVfx.transform.localScale = new Vector3(s, s, s);
        }
    }

    /// <summary>개별 화살 낙하 완료 통지</summary>
    internal void NotifyArrowFinished(ArrowRainFallingArrow arrow)
    {
        if (arrow != null) arrow.gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
