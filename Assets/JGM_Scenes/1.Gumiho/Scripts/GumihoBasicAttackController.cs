// UTF-8
using System.Collections.Generic;
using UnityEngine;

// 구현 원리 요약:
// 구미호 기본 공격 전체 흐름을 담당한다.
// 공격 가능 거리와 쿨타임을 확인하고 Animator Trigger로 공격 모션을 시작한다.
// 실제 화염구 발사는 애니메이션 이벤트 시점에 맞춰 실행한다.
// 플레이어 탐색은 직접 하지 않고 BossTargetProvider에서 받아온다.
// FirePoint는 씬에 배치한 오른손 기준 위치를 저장하고,
// 보스가 좌우 반전되면 FirePoint.localPosition 자체를 반전해서 실제 손 위치에 맞춘다.
// 여우구슬 패턴이 강화 상태를 주면 기본 공격은 그 수치로 다중 발사한다.

[DisallowMultipleComponent]
public sealed class GumihoBasicAttackController : MonoBehaviour
{
    [Header("패턴 카탈로그")]

    [Tooltip("구미호 패턴 카탈로그 SO입니다.")]
    [SerializeField] private GumihoPatternCatalogSO patternCatalog;


    [Header("참조")]

    [Tooltip("구미호 Animator입니다.")]
    [SerializeField] private Animator animator;

    [Tooltip("보스 공용 타겟 제공 컴포넌트입니다.")]
    [SerializeField] private BossTargetProvider targetProvider;

    [Tooltip("구미호 방향 반전에 사용되는 스프라이트 렌더러입니다.")]
    [SerializeField] private SpriteRenderer bossSpriteRenderer;

    [Tooltip("씬에서 직접 배치한 발사 위치 Transform입니다.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("화염구 풀 루트입니다. 비어 있으면 자동 생성합니다.")]
    [SerializeField] private Transform projectilePoolRoot;

    [Tooltip("기본 공격 강화 상태를 제공하는 런타임 컴포넌트입니다.")]
    [SerializeField] private GumihoAttackEnhancementRuntime attackEnhancementRuntime;


    [Header("FirePoint 좌우 반전")]

    [Tooltip("보스가 좌우 반전될 때 FirePoint 자체를 좌우 반전할지 여부입니다.")]
    [SerializeField] private bool useMirroredFirePoint = true;


    [Header("다중 발사 보정")]

    [Tooltip("여러 발 발사 시 각 투사체 시작 위치를 방향 앞으로 조금 밀어주는 거리입니다.")]
    [Min(0f)]
    [SerializeField] private float multiShotForwardOffset = 0.08f;


    [Header("디버그")]

    [Tooltip("디버그 로그를 출력할지 여부입니다.")]
    [SerializeField] private bool debugLog = false;


    private readonly Queue<GumihoFireballProjectile2D> pooledProjectiles = new Queue<GumihoFireballProjectile2D>(16);

    private GumihoBasicAttackConfigSO config;

    private float nextAttackTime;
    private bool isAttackCycleActive;
    private bool hasEnteredAttackState;
    private bool hasFiredThisCycle;

    private Vector3 rightFirePointLocalPosition;


    private void Reset()
    {
        animator = GetComponent<Animator>();
        targetProvider = GetComponent<BossTargetProvider>();
        bossSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        attackEnhancementRuntime = GetComponent<GumihoAttackEnhancementRuntime>();
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (targetProvider == null)
        {
            targetProvider = GetComponent<BossTargetProvider>();
        }

        if (bossSpriteRenderer == null)
        {
            bossSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (attackEnhancementRuntime == null)
        {
            attackEnhancementRuntime = GetComponent<GumihoAttackEnhancementRuntime>();
        }

        if (patternCatalog != null)
        {
            config = patternCatalog.BasicAttackConfig;
        }

        CacheRightFirePointLocalPosition();
        EnsureProjectilePoolRoot();
        WarmPool();
    }

    private void OnEnable()
    {
        if (patternCatalog != null)
        {
            config = patternCatalog.BasicAttackConfig;
        }

        nextAttackTime = Time.time;
        isAttackCycleActive = false;
        hasEnteredAttackState = false;
        hasFiredThisCycle = false;

        CacheRightFirePointLocalPosition();
        ApplyFirePointMirror();
    }

    private void LateUpdate()
    {
        ApplyFirePointMirror();
    }

    private void Update()
    {
        if (config == null)
        {
            return;
        }

        UpdateAttackCycleState();

        Transform target = GetCurrentTarget();
        if (target == null)
        {
            return;
        }

        if (isAttackCycleActive)
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > config.AttackRange)
        {
            return;
        }

        StartAttackCycle();
    }

    private Transform GetCurrentTarget()
    {
        if (targetProvider == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("[GumihoBasicAttackController] BossTargetProvider가 연결되지 않았습니다.", this);
            }

            return null;
        }

        if (!targetProvider.HasTarget())
        {
            return null;
        }

        return targetProvider.GetTarget();
    }

    private void CacheRightFirePointLocalPosition()
    {
        if (firePoint == null)
        {
            return;
        }

        rightFirePointLocalPosition = firePoint.localPosition;
    }

    private void ApplyFirePointMirror()
    {
        if (!useMirroredFirePoint)
        {
            return;
        }

        if (firePoint == null)
        {
            return;
        }

        if (bossSpriteRenderer == null)
        {
            return;
        }

        Vector3 nextLocalPosition = rightFirePointLocalPosition;

        if (bossSpriteRenderer.flipX)
        {
            nextLocalPosition.x *= -1f;
        }

        firePoint.localPosition = nextLocalPosition;
    }

    private void UpdateAttackCycleState()
    {
        if (!isAttackCycleActive)
        {
            return;
        }

        if (animator == null)
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName(config.AttackStateName))
        {
            hasEnteredAttackState = true;
            return;
        }

        if (hasEnteredAttackState)
        {
            isAttackCycleActive = false;
            hasEnteredAttackState = false;
            hasFiredThisCycle = false;
        }
    }

    private void StartAttackCycle()
    {
        if (animator == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("[GumihoBasicAttackController] Animator가 연결되지 않았습니다.", this);
            }

            return;
        }

        isAttackCycleActive = true;
        hasEnteredAttackState = false;
        hasFiredThisCycle = false;
        nextAttackTime = Time.time + config.AttackCooldown;

        animator.ResetTrigger(config.AttackTriggerName);
        animator.SetTrigger(config.AttackTriggerName);

        if (debugLog)
        {
            Debug.Log("[GumihoBasicAttackController] 기본 공격 시작", this);
        }
    }

    public void OnBasicAttackFireEvent()
    {
        if (config == null)
        {
            return;
        }

        if (hasFiredThisCycle)
        {
            return;
        }

        Transform target = GetCurrentTarget();
        if (target == null)
        {
            return;
        }

        ApplyFirePointMirror();

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Vector2 baseDirection = ((Vector2)target.position - (Vector2)spawnPosition).normalized;

        if (baseDirection.sqrMagnitude <= 0.0001f)
        {
            baseDirection = Vector2.right;
        }

        int projectileCount = GetCurrentProjectileCount();
        float spreadAngle = GetCurrentSpreadAngle();
        int projectileDamage = GetCurrentProjectileDamage();

        FireProjectiles(spawnPosition, baseDirection, projectileCount, spreadAngle, projectileDamage);

        hasFiredThisCycle = true;

        if (debugLog)
        {
            Debug.Log(
                $"[GumihoBasicAttackController] 화염구 발사 | count={projectileCount} damage={projectileDamage} spread={spreadAngle}",
                this);
        }
    }

    private int GetCurrentProjectileCount()
    {
        if (attackEnhancementRuntime == null)
        {
            return 1;
        }

        return Mathf.Max(1, attackEnhancementRuntime.CurrentProjectileCount);
    }

    private float GetCurrentSpreadAngle()
    {
        if (attackEnhancementRuntime == null)
        {
            return 0f;
        }

        return Mathf.Max(0f, attackEnhancementRuntime.CurrentSpreadAngle);
    }

    private int GetCurrentProjectileDamage()
    {
        float damageMultiplier = 1f;

        if (attackEnhancementRuntime != null)
        {
            damageMultiplier = Mathf.Max(0.1f, attackEnhancementRuntime.CurrentDamageMultiplier);
        }

        return Mathf.Max(1, Mathf.RoundToInt(config.ProjectileDamage * damageMultiplier));
    }

    private void FireProjectiles(
        Vector3 spawnPosition,
        Vector2 baseDirection,
        int projectileCount,
        float spreadAngle,
        int projectileDamage)
    {
        projectileCount = Mathf.Max(1, projectileCount);

        if (projectileCount == 1)
        {
            FireSingleProjectile(spawnPosition, baseDirection, projectileDamage);
            return;
        }

        float centerIndex = (projectileCount - 1) * 0.5f;

        for (int i = 0; i < projectileCount; i++)
        {
            float angleOffset = (i - centerIndex) * spreadAngle;
            Vector2 shotDirection = RotateVector(baseDirection, angleOffset);
            Vector3 shotSpawnPosition = spawnPosition + (Vector3)(shotDirection * multiShotForwardOffset);

            FireSingleProjectile(shotSpawnPosition, shotDirection, projectileDamage);
        }
    }

    private void FireSingleProjectile(Vector3 spawnPosition, Vector2 direction, int projectileDamage)
    {
        GumihoFireballProjectile2D projectile = GetProjectile();
        projectile.Launch(
            spawnPosition,
            direction,
            config.ProjectileSpeed,
            config.ProjectileLifetime,
            projectileDamage,
            config.TargetLayerMask,
            transform);
    }

    private Vector2 RotateVector(Vector2 direction, float angle)
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        Vector2 rotated = rotation * direction;
        return rotated.normalized;
    }

    private void EnsureProjectilePoolRoot()
    {
        if (projectilePoolRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("[GumihoFireballPool]");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        projectilePoolRoot = root.transform;
    }

    private void WarmPool()
    {
        if (config == null)
        {
            return;
        }

        if (config.FireballPrefab == null)
        {
            return;
        }

        int needCount = Mathf.Max(1, config.PrewarmCount);

        if (attackEnhancementRuntime != null)
        {
            needCount = Mathf.Max(needCount, config.PrewarmCount * attackEnhancementRuntime.CurrentProjectileCount);
        }

        for (int i = pooledProjectiles.Count; i < needCount; i++)
        {
            GumihoFireballProjectile2D projectile = CreateProjectile();
            ReturnProjectile(projectile);
        }
    }

    private GumihoFireballProjectile2D CreateProjectile()
    {
        GumihoFireballProjectile2D projectile = Instantiate(config.FireballPrefab, projectilePoolRoot);
        projectile.gameObject.SetActive(false);
        projectile.BindReturn(ReturnProjectile);
        return projectile;
    }

    private GumihoFireballProjectile2D GetProjectile()
    {
        if (pooledProjectiles.Count == 0)
        {
            GumihoFireballProjectile2D created = CreateProjectile();
            pooledProjectiles.Enqueue(created);
        }

        GumihoFireballProjectile2D projectile = pooledProjectiles.Dequeue();
        projectile.transform.SetParent(null);
        projectile.gameObject.SetActive(true);
        return projectile;
    }

    private void ReturnProjectile(GumihoFireballProjectile2D projectile)
    {
        if (projectile == null)
        {
            return;
        }

        projectile.transform.SetParent(projectilePoolRoot);

        if (!pooledProjectiles.Contains(projectile))
        {
            pooledProjectiles.Enqueue(projectile);
        }
    }
}