using System.Collections.Generic;
using UnityEngine;

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

    [Header("낙하 화살(연출 전용)")]
    [Tooltip("위에서 떨어지는 '연출용' 화살 프리팹(필수). 데미지 판정과 무관합니다.")]
    [SerializeField] private ArrowRainFallingArrow fallingArrowPrefab;

    [Tooltip("낙하 스폰 간격(초). 값이 작을수록 더 촘촘히 떨어집니다.")]
    [Min(0.01f)]
    [SerializeField] private float fallSpawnInterval = 0.06f;

    [Tooltip("한 번에 살아있을 수 있는 낙하 화살 최대 수(핵심: 풀/성능 보호막).")]
    [Min(1)]
    [SerializeField] private int maxSimultaneousFallingArrows = 35;

    [Tooltip("장판 중심 기준으로, 화살이 생성될 높이(Y 오프셋).")]
    [Min(0f)]
    [SerializeField] private float spawnHeight = 3.5f;

    [Tooltip("낙하 속도(월드 단위/초).")]
    [Min(0.1f)]
    [SerializeField] private float fallSpeed = 10f;

    [Tooltip("낙하 화살이 자동으로 풀로 돌아가는 시간(초). 짧게 유지하는 게 안전합니다.")]
    [Min(0.05f)]
    [SerializeField] private float fallingArrowLifetime = 0.6f;

    [Tooltip("낙하 화살에 랜덤 회전(도 단위)을 적용합니다. 0이면 회전 없음.")]
    [Range(0f, 360f)]
    [SerializeField] private float randomRotationDegrees = 20f;

    [Header("데미지(틱 판정)")]
    [Tooltip("원 안에 있는 적에게 피해를 주는 틱 간격(초). 0.2~0.5 권장.")]
    [Min(0.05f)]
    [SerializeField] private float damageTickInterval = 0.25f;

    [Tooltip("틱마다 적용될 피해량(정수).")]
    [Min(0)]
    [SerializeField] private int damagePerTick = 2;

    [Tooltip("적 레이어만 체크하세요. (원 안의 콜라이더를 이 레이어로 필터링)")]
    [SerializeField] private LayerMask enemyLayerMask;

    [Header("내장 풀(안정성)")]
    [Tooltip("초기 풀 생성 개수(프레임 스파이크 방지). maxSimultaneousFallingArrows 이하 권장.")]
    [Min(0)]
    [SerializeField] private int prewarmCount = 20;

    private CircleCollider2D circleTrigger;

    private readonly HashSet<Collider2D> enemiesInside = new HashSet<Collider2D>(64);

    // ──────────────────────────────────────────────────────────────────
    // 런타임 구성 API
    // ArrowRainWeapon2D(CommonSkillWeapon2D 래퍼)가 SkillEffectConfig 값을
    // 이 메서드로 주입합니다. Enable 전에 호출해야 합니다.
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// SkillEffectConfig 기반으로 장판 파라미터를 런타임에 주입합니다.
    /// ArrowRainWeapon2D가 Spawn 직후에 호출합니다.
    /// </summary>
    public void Configure(int tickDamage, float tickInterval, float areaRad, float duration, LayerMask mask)
    {
        damagePerTick       = Mathf.Max(0, tickDamage);
        damageTickInterval  = Mathf.Max(0.05f, tickInterval);
        radius              = Mathf.Max(0.1f, areaRad);
        durationSeconds     = Mathf.Max(0f, duration);
        enemyLayerMask      = mask;

        // 컬라이더가 이미 생성됐다면 즉시 반영
        if (circleTrigger != null)
            ApplyRadiusToCollider();
    }

    private float tickTimer;
    private float spawnTimer;
    private float aliveTimer;

    private readonly Queue<ArrowRainFallingArrow> pool = new Queue<ArrowRainFallingArrow>(128);
    private readonly List<ArrowRainFallingArrow> active = new List<ArrowRainFallingArrow>(128);

    private readonly List<Collider2D> tempToRemove = new List<Collider2D>(16);

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

        ApplyRadiusToCollider();
        ApplyAreaAlpha();

        if (fallingArrowPrefab == null)
        {
            Debug.LogError("[ArrowRainArea2D] 낙하 화살 프리팹이 비어있습니다.", this);
            enabled = false;
            return;
        }

        PrewarmPool();
    }

    private void OnEnable()
    {
        tickTimer = 0f;
        spawnTimer = 0f;
        aliveTimer = 0f;

        ApplyRadiusToCollider();
        ApplyAreaAlpha();
    }

    private void OnDisable()
    {
        ReturnAllActiveToPool();
        enemiesInside.Clear();
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

        spawnTimer += Time.deltaTime;
        while (spawnTimer >= fallSpawnInterval)
        {
            spawnTimer -= fallSpawnInterval;
            TrySpawnFallingArrow();
        }

        tickTimer += Time.deltaTime;
        if (tickTimer >= damageTickInterval)
        {
            tickTimer -= damageTickInterval;
            ApplyTickDamage();
        }
    }

    private void ApplyRadiusToCollider()
    {
        circleTrigger.radius = radius;
    }

    private void ApplyAreaAlpha()
    {
        if (areaSpriteRenderer == null) return;
        Color c = areaSpriteRenderer.color;
        c.a = areaAlpha;
        areaSpriteRenderer.color = c;
    }

    private void PrewarmPool()
    {
        int target = Mathf.Clamp(prewarmCount, 0, Mathf.Max(0, maxSimultaneousFallingArrows));
        for (int i = 0; i < target; i++)
        {
            ArrowRainFallingArrow a = CreateNewArrowInstance();
            ReturnToPool(a);
        }
    }

    private ArrowRainFallingArrow CreateNewArrowInstance()
    {
        ArrowRainFallingArrow a = Instantiate(fallingArrowPrefab, transform);
        a.gameObject.SetActive(false);
        a.BindOwner(this);
        return a;
    }

    private void TrySpawnFallingArrow()
    {
        if (active.Count >= maxSimultaneousFallingArrows)
            return;

        ArrowRainFallingArrow arrow = GetFromPool();
        Vector2 center = transform.position;

        float angle = Random.value * Mathf.PI * 2f;
        float dist = Mathf.Sqrt(Random.value) * radius;
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

        Vector2 spawnPos = center + offset + Vector2.up * spawnHeight;

        float rotZ = 0f;
        if (randomRotationDegrees > 0f)
            rotZ = Random.Range(-randomRotationDegrees, randomRotationDegrees);

        arrow.transform.SetPositionAndRotation(spawnPos, Quaternion.Euler(0f, 0f, rotZ));
        arrow.Launch(new Vector2(0f, -fallSpeed), fallingArrowLifetime);

        active.Add(arrow);
        arrow.gameObject.SetActive(true);
    }

    private ArrowRainFallingArrow GetFromPool()
    {
        if (pool.Count > 0)
            return pool.Dequeue();

        return CreateNewArrowInstance();
    }

    internal void NotifyArrowFinished(ArrowRainFallingArrow arrow)
    {
        int idx = active.IndexOf(arrow);
        if (idx >= 0) active.RemoveAt(idx);

        ReturnToPool(arrow);
    }

    private void ReturnToPool(ArrowRainFallingArrow arrow)
    {
        arrow.gameObject.SetActive(false);
        pool.Enqueue(arrow);
    }

    private void ReturnAllActiveToPool()
    {
        for (int i = 0; i < active.Count; i++)
        {
            ArrowRainFallingArrow a = active[i];
            if (a != null)
                ReturnToPool(a);
        }
        active.Clear();
    }

    private void ApplyTickDamage()
    {
        if (damagePerTick <= 0) return;
        if (enemiesInside.Count == 0) return;

        tempToRemove.Clear();

        foreach (Collider2D col in enemiesInside)
        {
            if (col == null)
            {
                tempToRemove.Add(col);
                continue;
            }

            if (((1 << col.gameObject.layer) & enemyLayerMask.value) == 0)
                continue;

            if (col.TryGetComponent<IDamageable2D>(out var dmg))
            {
                dmg.TakeDamage(damagePerTick); // ✅ 프로젝트 인터페이스 시그니처에 맞춤
            }
        }

        for (int i = 0; i < tempToRemove.Count; i++)
            enemiesInside.Remove(tempToRemove[i]);
    }

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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (radius < 0.1f) radius = 0.1f;
        if (fallSpawnInterval < 0.01f) fallSpawnInterval = 0.01f;
        if (damageTickInterval < 0.05f) damageTickInterval = 0.05f;

        if (circleTrigger == null) circleTrigger = GetComponent<CircleCollider2D>();
        if (circleTrigger != null) circleTrigger.isTrigger = true;

        ApplyRadiusToCollider();
        ApplyAreaAlpha();

        if (prewarmCount > maxSimultaneousFallingArrows)
            prewarmCount = maxSimultaneousFallingArrows;
    }
#endif
}