// UTF-8
using UnityEngine;

/// <summary>
/// 화살비 장판 — 최종 수정판
/// 
/// ★ Physics2D.OverlapCircleAll(center, radius, layerMask) 만 사용
///    ContactFilter2D, OnTriggerEnter2D 전부 제거
///    가장 단순하고 확실한 방식
/// </summary>
[DisallowMultipleComponent]
public sealed class ArrowRainArea2D : MonoBehaviour
{
    [Header("장판(범위)")]
    [Min(0.1f)]
    [SerializeField] private float radius = 4.0f;

    [Min(0f)]
    [SerializeField] private float durationSeconds = 3.0f;

    [SerializeField] private SpriteRenderer areaSpriteRenderer;

    [Range(0f, 1f)]
    [SerializeField] private float areaAlpha = 0.55f;

    [Header("이펙트(파티클)")]
    [SerializeField] private ParticleSystem effectVfx;
    [SerializeField] private bool scaleVfxToRadius = true;
    [Min(0.1f)]
    [SerializeField] private float vfxBaseRadius = 4.0f;

    [Header("데미지(틱 판정)")]
    [Min(0.05f)]
    [SerializeField] private float damageTickInterval = 0.25f;

    [Min(0)]
    [SerializeField] private int damagePerTick = 5;

    [SerializeField] private LayerMask enemyLayerMask;

    [Header("디버그 (문제 해결 후 끄세요)")]
    [SerializeField] private bool debugLog = true;

    // ── 내부 ──
    private CircleCollider2D circleTrigger;
    private float tickTimer;
    private float aliveTimer;

    // ════════════════════════════════════════════

    private void Awake()
    {
        circleTrigger = GetComponent<CircleCollider2D>();
        if (circleTrigger != null)
            circleTrigger.isTrigger = true;

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
    //  Setup (ArrowRainWeapon2D가 호출)
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

        tickTimer  = 0f;
        aliveTimer = 0f;

        ApplyVisuals();

        if (debugLog)
            Debug.Log($"[ArrowRainArea2D] Setup: radius={radius}, dmg={damagePerTick}, " +
                      $"tick={damageTickInterval}s, dur={durationSeconds}s, " +
                      $"mask={enemyLayerMask.value}, pos={transform.position}", this);
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

        // ★ 활성화 직후 즉시 첫 틱
        DoTickDamage();
    }

    private void OnDisable()
    {
        if (effectVfx != null)
            effectVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void Update()
    {
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
    //  ★★★ 핵심: 가장 단순한 데미지 로직 ★★★
    // ════════════════════════════════════════════

    private void DoTickDamage()
    {
        if (damagePerTick <= 0)
        {
            if (debugLog) Debug.LogWarning("[ArrowRainArea2D] damagePerTick이 0 이하!", this);
            return;
        }

        Vector2 center = (Vector2)transform.position;

        // ★★★ 가장 단순한 API — ContactFilter2D 없음 ★★★
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayerMask);

        if (debugLog && hits.Length == 0)
            Debug.Log($"[ArrowRainArea2D] 적 없음. pos={center}, radius={radius}, mask={enemyLayerMask.value}", this);

        int damaged = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i];
            if (col == null) continue;
            if (!col.gameObject.activeInHierarchy) continue;

            // IDamageable2D 찾기: 자기 자신 → 부모
            IDamageable2D target = col.GetComponent<IDamageable2D>();
            if (target == null)
                target = col.GetComponentInParent<IDamageable2D>();

            if (target == null)
            {
                if (debugLog) Debug.Log($"[ArrowRainArea2D] IDamageable2D 없음: {col.gameObject.name} (Layer={col.gameObject.layer})", this);
                continue;
            }

            if (target.IsDead) continue;

            target.TakeDamage(damagePerTick);
            damaged++;
        }

        if (debugLog && damaged > 0)
            Debug.Log($"[ArrowRainArea2D] ★ 데미지 적중: {damagePerTick} x {damaged}명", this);
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

    // ════════════════════════════════════════════
    //  하위 호환
    // ════════════════════════════════════════════

    internal void NotifyArrowFinished(ArrowRainFallingArrow arrow)
    {
        if (arrow != null)
            arrow.gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.1f);
        Gizmos.DrawSphere(transform.position, radius);
    }
#endif
}