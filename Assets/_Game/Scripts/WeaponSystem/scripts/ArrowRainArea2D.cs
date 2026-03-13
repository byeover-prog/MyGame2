// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화살비 장판: 지정 위치에서 일정 시간 동안 범위 내 적에게 틱 데미지.
///
/// ■ 연출
///   - effectVfx(ParticleSystem)를 자식으로 두고, 장판 활성화 시 Play / 비활성화 시 Stop.
///   - eff_weapon_rainarrow 프리팹을 통째로 자식에 넣으면 됨.
///
/// ■ 데미지
///   - CircleCollider2D(Trigger) 기반 OnTriggerEnter/Exit로 적 추적.
///   - damageTickInterval 간격으로 범위 내 적에게 damagePerTick 피해.
///   - Setup()으로 런타임에 모든 파라미터 덮어쓰기 가능.
/// </summary>
[DisallowMultipleComponent]
public sealed class ArrowRainArea2D : MonoBehaviour
{
    [Header("장판(범위)")]
    [Tooltip("원형 장판 반지름(월드 단위). CircleCollider2D 반지름과 자동 동기화됩니다.")]
    [Min(0.1f)]
    [SerializeField] private float radius = 2.0f;

    [Tooltip("장판 지속 시간(초). 0 이하면 계속 유지됩니다.")]
    [Min(0f)]
    [SerializeField] private float durationSeconds = 3.0f;

    [Tooltip("장판 링(테두리) 스프라이트를 표시할 SpriteRenderer. 비우면 자동으로 찾습니다.")]
    [SerializeField] private SpriteRenderer areaSpriteRenderer;

    [Range(0f, 1f)]
    [Tooltip("장판 링(테두리) 알파 값. 도트 감성 유지용(과한 글로우/블러 금지).")]
    [SerializeField] private float areaAlpha = 0.55f;

    [Header("이펙트(파티클)")]
    [Tooltip("화살비 파티클 이펙트. eff_weapon_rainarrow를 자식으로 넣고 여기에 루트 ParticleSystem을 연결하세요.\n비워두면 이펙트 없이 데미지만 동작합니다.")]
    [SerializeField] private ParticleSystem effectVfx;

    [Tooltip("이펙트 크기를 장판 반경에 맞춰 자동 스케일할지 여부")]
    [SerializeField] private bool scaleVfxToRadius = true;

    [Tooltip("스케일 기준 반경(이 값일 때 scale=1). effectVfx의 원래 크기에 맞춰 설정.")]
    [Min(0.1f)]
    [SerializeField] private float vfxBaseRadius = 2.0f;

    [Header("데미지(틱 판정)")]
    [Tooltip("원 안에 있는 적에게 피해를 주는 틱 간격(초). 0.2~0.5 권장.")]
    [Min(0.05f)]
    [SerializeField] private float damageTickInterval = 0.25f;

    [Tooltip("틱마다 적용될 피해량(정수).")]
    [Min(0)]
    [SerializeField] private int damagePerTick = 2;

    [Tooltip("적 레이어만 체크하세요. (원 안의 콜라이더를 이 레이어로 필터링)")]
    [SerializeField] private LayerMask enemyLayerMask;

    // ── 내부 상태 ──
    private CircleCollider2D circleTrigger;
    private readonly HashSet<Collider2D> enemiesInside = new HashSet<Collider2D>(64);
    private readonly List<Collider2D> tempSnapshot = new List<Collider2D>(64);

    private float tickTimer;
    private float aliveTimer;

    // ════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════

    private void Awake()
    {
        circleTrigger = GetComponent<CircleCollider2D>();
        if (circleTrigger == null)
        {
            Debug.LogError("[ArrowRainArea2D] CircleCollider2D가 필요합니다.", this);
            enabled = false;
            return;
        }

        if (!circleTrigger.isTrigger)
            circleTrigger.isTrigger = true;

        if (areaSpriteRenderer == null)
            areaSpriteRenderer = GetComponent<SpriteRenderer>();

        // effectVfx 자동 탐색: 인스펙터에 안 넣었으면 자식에서 찾기
        if (effectVfx == null)
            effectVfx = GetComponentInChildren<ParticleSystem>(true);

        ApplyRadiusToCollider();
        ApplyAreaAlpha();
    }

    // ════════════════════════════════════════════
    //  외부 API
    // ════════════════════════════════════════════

    /// <summary>
    /// 런타임에서 장판 파라미터를 덮어쓴다.
    /// (CommonSkill 레벨 파라미터 적용용)
    /// </summary>
    public void Setup(float newRadius, float newDurationSeconds, float newDamageTickInterval, int newDamagePerTick, LayerMask newEnemyMask)
    {
        radius = Mathf.Max(0.1f, newRadius);
        durationSeconds = Mathf.Max(0f, newDurationSeconds);
        damageTickInterval = Mathf.Max(0.05f, newDamageTickInterval);
        damagePerTick = Mathf.Max(0, newDamagePerTick);
        enemyLayerMask = newEnemyMask;

        tickTimer = 0f;
        aliveTimer = 0f;
        enemiesInside.Clear();

        ApplyRadiusToCollider();
        ApplyAreaAlpha();
        ApplyVfxScale();
    }

    // ════════════════════════════════════════════
    //  생명주기
    // ════════════════════════════════════════════

    private void OnEnable()
    {
        tickTimer = 0f;
        aliveTimer = 0f;

        ApplyRadiusToCollider();
        ApplyAreaAlpha();
        ApplyVfxScale();

        // ★ 파티클 재생
        PlayVfx();
    }

    private void OnDisable()
    {
        enemiesInside.Clear();

        // ★ 파티클 정지
        StopVfx();
    }

    private void Update()
    {
        // ── 지속시간 체크 ──
        if (durationSeconds > 0f)
        {
            aliveTimer += Time.deltaTime;
            if (aliveTimer >= durationSeconds)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        // ── 틱 데미지 ──
        tickTimer += Time.deltaTime;
        if (tickTimer >= damageTickInterval)
        {
            tickTimer -= damageTickInterval;
            ApplyTickDamage();
        }
    }

    // ════════════════════════════════════════════
    //  데미지
    // ════════════════════════════════════════════

    private void ApplyTickDamage()
    {
        if (damagePerTick <= 0) return;
        if (enemiesInside.Count == 0) return;

        // ★ HashSet을 직접 순회하지 않고 스냅샷 리스트로 복사 후 순회
        tempSnapshot.Clear();
        tempSnapshot.AddRange(enemiesInside);

        for (int i = 0; i < tempSnapshot.Count; i++)
        {
            Collider2D col = tempSnapshot[i];

            if (col == null)
            {
                enemiesInside.Remove(col);
                continue;
            }

            if (((1 << col.gameObject.layer) & enemyLayerMask.value) == 0)
                continue;

            // IDamageable2D가 콜라이더 자체에 없으면 부모에서도 탐색
            IDamageable2D dmg = col.GetComponent<IDamageable2D>();
            if (dmg == null)
                dmg = col.GetComponentInParent<IDamageable2D>();

            if (dmg != null)
                dmg.TakeDamage(damagePerTick);
        }
    }

    // ════════════════════════════════════════════
    //  트리거 (적 진입/이탈 추적)
    // ════════════════════════════════════════════

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayerMask.value) == 0)
            return;

        enemiesInside.Add(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        enemiesInside.Remove(other);
    }

    // ════════════════════════════════════════════
    //  하위 호환 (ArrowRainFallingArrow가 호출함 — 삭제 전까지 유지)
    // ════════════════════════════════════════════

    /// <summary>
    /// 구 낙하 화살(ArrowRainFallingArrow)이 수명 종료 시 호출하는 메서드.
    /// 파티클 방식으로 전환했으므로 더 이상 사용하지 않지만,
    /// ArrowRainFallingArrow.cs가 프로젝트에 남아있으면 컴파일 에러 방지용으로 유지.
    /// ArrowRainFallingArrow.cs 삭제 후 이 메서드도 삭제해도 됨.
    /// </summary>
    internal void NotifyArrowFinished(ArrowRainFallingArrow arrow)
    {
        if (arrow != null)
            arrow.gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════
    //  VFX (파티클)
    // ════════════════════════════════════════════

    private void PlayVfx()
    {
        if (effectVfx == null) return;

        // 이미 재생 중이면 처음부터 다시
        effectVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effectVfx.Play(true);
    }

    private void StopVfx()
    {
        if (effectVfx == null) return;
        effectVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    /// <summary>
    /// 장판 반경에 맞춰 이펙트 스케일을 조정한다.
    /// vfxBaseRadius가 2.0이고 현재 radius가 3.0이면 scale = 1.5
    /// </summary>
    private void ApplyVfxScale()
    {
        if (effectVfx == null) return;
        if (!scaleVfxToRadius) return;

        float scale = Mathf.Max(0.1f, radius / Mathf.Max(0.1f, vfxBaseRadius));
        effectVfx.transform.localScale = new Vector3(scale, scale, scale);
    }

    // ════════════════════════════════════════════
    //  콜라이더/스프라이트
    // ════════════════════════════════════════════

    private void ApplyRadiusToCollider()
    {
        if (circleTrigger != null)
            circleTrigger.radius = radius;
    }

    private void ApplyAreaAlpha()
    {
        if (areaSpriteRenderer == null) return;
        Color c = areaSpriteRenderer.color;
        c.a = areaAlpha;
        areaSpriteRenderer.color = c;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (radius < 0.1f) radius = 0.1f;
        if (damageTickInterval < 0.05f) damageTickInterval = 0.05f;

        if (circleTrigger == null) circleTrigger = GetComponent<CircleCollider2D>();
        if (circleTrigger != null) circleTrigger.isTrigger = true;

        ApplyRadiusToCollider();
        ApplyAreaAlpha();
    }
#endif
}