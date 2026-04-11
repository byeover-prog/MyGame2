using UnityEngine;

/// <summary>
/// 화살비 장판 — 진짜 최종판
/// 
///  핵심 수정: DamageUtil2D.TryApplyDamage() 사용
///   - 이전: target.TakeDamage() → HP만 깎이고 데미지 팝업 안 뜸
///   - 수정: DamageUtil2D.TryApplyDamage() → HP 감소 + 데미지 팝업 + 속성 처리
/// </summary>
[DisallowMultipleComponent]
public sealed class ArrowRainArea2D : MonoBehaviour
{
    [Header("장판(범위)")]
    [Min(0.1f)]
    [SerializeField] private float radius = 2.0f;

    [Min(0f)]
    [SerializeField] private float durationSeconds = 3.0f;

    [SerializeField] private SpriteRenderer areaSpriteRenderer;

    [Range(0f, 1f)]
    [SerializeField] private float areaAlpha = 0.55f;

    [Header("이펙트(파티클)")]
    [SerializeField] private ParticleSystem effectVfx;
    [SerializeField] private bool scaleVfxToRadius = true;
    [Min(0.1f)]
    [SerializeField] private float vfxBaseRadius = 2.0f;

    [Header("데미지(틱 판정)")]
    [Min(0.05f)]
    [SerializeField] private float damageTickInterval = 0.3f;

    [Min(0)]
    [SerializeField] private int damagePerTick = 5;

    [SerializeField] private LayerMask enemyLayerMask;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    // ── 내부 ──
    private CircleCollider2D circleTrigger;
    private float tickTimer;
    private float aliveTimer;
    private bool _setupDone;

    /// <summary>틱 데미지 판정용 사전 할당 버퍼 (GC 0)</summary>
    private readonly Collider2D[] _hitBuffer = new Collider2D[64];
    private ContactFilter2D _enemyFilter;

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
    }

    // ════════════════════════════════════════════
    //  Setup
    // ════════════════════════════════════════════

    public void Setup(float newRadius, float newDurationSeconds,
                      float newDamageTickInterval, int newDamagePerTick,
                      LayerMask newEnemyMask)
    {
        radius             = Mathf.Max(0.5f, newRadius);
        durationSeconds    = Mathf.Max(0.25f, newDurationSeconds);
        damageTickInterval = Mathf.Max(0.05f, newDamageTickInterval);
        damagePerTick      = Mathf.Max(1, newDamagePerTick);
        enemyLayerMask     = newEnemyMask;

        // ContactFilter2D 초기화 (NonAlloc 쿼리용)
        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(enemyLayerMask);
        _enemyFilter.useLayerMask = true;
        _enemyFilter.useTriggers = true;

        tickTimer  = 0f;
        aliveTimer = 0f;
        _setupDone = true;

        ApplyVisuals();

        if (debugLog)
            CombatLog.Log($"[ArrowRainArea2D] Setup: r={radius:F1}, dmg={damagePerTick}, " +
                      $"tick={damageTickInterval:F2}s, dur={durationSeconds:F1}s");
    }

    // ════════════════════════════════════════════
    //  생명주기
    // ════════════════════════════════════════════

    private void OnEnable()
    {
        tickTimer  = 0f;
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

    // ════════════════════════════════════════════
    //  ★★★ 핵심: DamageUtil2D 사용 ★★★
    // ════════════════════════════════════════════

    private void DoTickDamage()
    {
        if (damagePerTick <= 0) return;

        Vector2 center = (Vector2)transform.position;

        // ★ NonAlloc: 사전 할당 버퍼 사용 → GC 0
        int hitCount = Physics2D.OverlapCircle(center, radius, _enemyFilter, _hitBuffer);

        if (hitCount == 0) return;

        int damaged = 0;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = _hitBuffer[i];
            if (col == null || !col.gameObject.activeInHierarchy) continue;

            bool applied = DamageUtil2D.TryApplyDamage(col, damagePerTick);
            if (applied) damaged++;
        }

        if (debugLog && damaged > 0)
            CombatLog.Log($"[ArrowRainArea2D] 적중: {damagePerTick} x {damaged}명");
    }

    // ════════════════════════════════════════════
    //  비주얼
    // ════════════════════════════════════════════

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