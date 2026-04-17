using UnityEngine;

/// <summary>
/// 원거리 몬스터 행동의 공통 베이스입니다.
///
/// 이 클래스의 역할:
/// - 플레이어 타겟 탐색
/// - 거리 판정
/// - Idle / Chase / Retreat / AttackHold / Charging 상태 전환
/// - 차징 타이머 관리
/// - 기본 다발 발사 방향 계산 유틸
/// - 차징 프리뷰 생성 / 제거 / 조준 갱신 유틸
/// - 투사체 단발 / 다발 발사 유틸
///
/// 설계 원칙:
/// - SO는 설계값을 들고 있습니다.
/// - MonsterRuntimeApplier2D는 SO 값을 이 컴포넌트에 주입합니다.
/// - 이 베이스는 "원거리 몬스터라면 공통으로 가지는 흐름"만 담당합니다.
/// - 개별 몬스터만의 유니크한 발사 규칙은 하위 클래스가 오버라이드합니다.
/// </summary>
[DisallowMultipleComponent]
public abstract class EnemyRangedBehaviorBase2D : MonoBehaviour
{
    protected enum RangedState
    {
        Idle,
        Chase,
        Retreat,
        AttackHold,
        Charging
    }

    [Header("1. 대상 연결")]
    [SerializeField, Tooltip("현재 타겟 플레이어 Transform입니다.\n"
                             + "비워두면 playerTag 기준으로 자동 탐색합니다.")]
    protected Transform target;

    [SerializeField, Tooltip("이동에 사용할 Rigidbody2D입니다.\n"
                             + "보통 같은 루트 오브젝트의 Rigidbody2D를 연결합니다.")]
    protected Rigidbody2D rb;

    [SerializeField, Tooltip("발사 위치입니다.\n"
                             + "보통 몬스터 입 앞이나 손 앞의 ProjectileSpawnPoint를 연결합니다.\n"
                             + "비워두면 자기 위치를 사용합니다.")]
    protected Transform projectileSpawnPoint;

    [Header("2. 자동 탐색")]
    [SerializeField, Tooltip("target이 비어 있을 때 자동으로 찾을 플레이어 태그입니다.\n"
                             + "보통 Player를 사용합니다.")]
    protected string playerTag = "Player";

    [SerializeField, Min(0.1f), Tooltip("플레이어 참조를 잃었을 때 다시 찾는 간격입니다.\n"
                                        + "단위는 초입니다.")]
    protected float targetRefindInterval = 0.5f;

    [Header("3. 이동 / 공격 기준")]
    [SerializeField, Min(0f), Tooltip("이동 속도입니다.\n"
                                      + "detectRange 안에서 추적하거나\n"
                                      + "retreatRange 안에서 후퇴할 때 사용합니다.\n"
                                      + "단위는 world unit / second 기준입니다.")]
    protected float moveSpeed = 2f;

    [SerializeField, Min(0f), Tooltip("플레이어를 인식하고 전투 상태로 들어가는 거리입니다.\n"
                                      + "이 거리 밖이면 추적도 공격도 하지 않고 정지합니다.\n"
                                      + "단위는 world unit입니다.")]
    protected float detectRange = 8f;

    [SerializeField, Min(0f), Tooltip("공격을 시작할 수 있는 거리입니다.\n"
                                      + "이 거리 안에서는 멈추고 공격을 시도합니다.\n"
                                      + "단위는 world unit입니다.")]
    protected float attackRange = 5f;

    [SerializeField, Min(0f), Tooltip("플레이어가 너무 가까울 때 후퇴를 시작하는 거리입니다.\n"
                                      + "0이면 후퇴를 사용하지 않습니다.\n"
                                      + "단위는 world unit입니다.")]
    protected float retreatRange = 2f;

    [SerializeField, Min(0.01f), Tooltip("공격 주기입니다.\n"
                                         + "실제 발사 완료 후 다시 공격 가능해질 때까지의 대기 시간입니다.\n"
                                         + "단위는 초입니다.")]
    protected float attackCooldown = 2f;

    [SerializeField, Min(0f), Tooltip("차징 시간입니다.\n"
                                      + "0이면 즉시 발사처럼 동작합니다.\n"
                                      + "단위는 초입니다.")]
    protected float chargeDuration = 1f;

    [SerializeField, Min(0f), Tooltip("원거리 공격 기본 피해값입니다.\n"
                                      + "실제 발사체 피해나 특수 공격 피해의 기준값입니다.")]
    protected float attackDamage = 10f;

    [SerializeField, Min(1), Tooltip("한 번 공격할 때 발사할 투사체 개수입니다.\n"
                                     + "1이면 단발, 2 이상이면 다발 발사입니다.")]
    protected int projectileCount = 1;

    [SerializeField, Min(0f), Tooltip("다발 발사 시 퍼지는 전체 각도입니다.\n"
                                      + "단위는 degree입니다.\n"
                                      + "projectileCount가 1이면 0으로 두면 됩니다.")]
    protected float spreadAngle = 0f;

    [SerializeField, Min(0f), Tooltip("투사체 이동 속도입니다.\n"
                                      + "단위는 world unit / second 기준입니다.")]
    protected float projectileSpeed = 10f;

    [Header("4. 디버그")]
    [SerializeField, Tooltip("선택 상태에서 detectRange / attackRange / retreatRange를\n"
                             + "Scene 뷰에 표시할지 여부입니다.")]
    protected bool drawRangeGizmo = true;

    protected RangedState currentState = RangedState.Idle;
    protected float attackCooldownTimer;
    protected float chargeTimer;
    protected float targetRefindTimer;
    protected Vector2 currentAimDirection = Vector2.right;
    protected bool didWarnMissingPlayerTag;

    protected virtual void Reset()
    {
        rb = GetComponent<Rigidbody2D>();

        if (projectileSpawnPoint == null)
            projectileSpawnPoint = transform;
    }

    protected virtual void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (projectileSpawnPoint == null)
            projectileSpawnPoint = transform;
    }

    protected virtual void OnEnable()
    {
        currentState = RangedState.Idle;
        attackCooldownTimer = 0f;
        chargeTimer = 0f;
        targetRefindTimer = 0f;
        currentAimDirection = Vector2.right;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    protected virtual void OnDisable()
    {
        CancelChargeVisual();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// 런타임에 원거리형 기준 수치를 주입합니다.
    /// 거리 규칙이 어긋나지 않도록 일부 값을 보정합니다.
    /// </summary>
    public virtual void ConfigureRuntime(
        float runtimeMoveSpeed,
        float runtimeDetectRange,
        float runtimeAttackRange,
        float runtimeRetreatRange,
        float runtimeAttackCooldown,
        float runtimeChargeDuration,
        float runtimeProjectileSpeed,
        int runtimeProjectileCount,
        float runtimeSpreadAngle,
        float runtimeAttackDamage)
    {
        moveSpeed = Mathf.Max(0f, runtimeMoveSpeed);
        detectRange = Mathf.Max(0f, runtimeDetectRange);
        attackRange = Mathf.Max(0f, runtimeAttackRange);
        retreatRange = Mathf.Max(0f, runtimeRetreatRange);
        attackCooldown = Mathf.Max(0.01f, runtimeAttackCooldown);
        chargeDuration = Mathf.Max(0f, runtimeChargeDuration);
        projectileSpeed = Mathf.Max(0f, runtimeProjectileSpeed);
        projectileCount = Mathf.Max(1, runtimeProjectileCount);
        spreadAngle = Mathf.Max(0f, runtimeSpreadAngle);
        attackDamage = Mathf.Max(0f, runtimeAttackDamage);

        if (attackRange > detectRange)
            attackRange = detectRange;

        if (retreatRange > attackRange)
            retreatRange = attackRange;
    }

    /// <summary>
    /// 필요 시 외부에서 타겟을 강제로 지정할 수 있습니다.
    /// </summary>
    public virtual void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    protected virtual void Update()
    {
        bool hasTarget = TryResolveTarget();

        if (!hasTarget)
        {
            if (currentState == RangedState.Charging)
            {
                UpdateCharging();
                return;
            }

            currentState = RangedState.Idle;
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        if (currentState == RangedState.Charging)
        {
            UpdateCharging();
            return;
        }

        attackCooldownTimer -= Time.deltaTime;

        if (detectRange <= 0f || distanceToTarget > detectRange)
        {
            currentState = RangedState.Idle;
            return;
        }

        if (retreatRange > 0f && distanceToTarget <= retreatRange)
        {
            currentState = RangedState.Retreat;
            return;
        }

        if (distanceToTarget <= attackRange)
        {
            currentState = RangedState.AttackHold;

            if (attackCooldownTimer <= 0f)
                StartChargeOrFire();

            return;
        }

        currentState = RangedState.Chase;
    }

    protected virtual void FixedUpdate()
    {
        if (rb == null)
            return;

        switch (currentState)
        {
            case RangedState.Idle:
            case RangedState.AttackHold:
            case RangedState.Charging:
                rb.linearVelocity = Vector2.zero;
                break;

            case RangedState.Chase:
                MoveTowardTarget();
                break;

            case RangedState.Retreat:
                MoveAwayFromTarget();
                break;
        }
    }

    protected bool TryResolveTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy)
            return true;

        targetRefindTimer -= Time.deltaTime;
        if (targetRefindTimer > 0f)
            return false;

        targetRefindTimer = targetRefindInterval;

        if (string.IsNullOrEmpty(playerTag))
            return false;

        try
        {
            GameObject foundPlayer = GameObject.FindWithTag(playerTag);
            if (foundPlayer != null)
            {
                target = foundPlayer.transform;
                return true;
            }
        }
        catch (UnityException)
        {
            if (!didWarnMissingPlayerTag)
            {
                Debug.LogWarning(
                    $"[{GetType().Name}] Player 태그 '{playerTag}'를 찾지 못했습니다. Tag 설정을 확인하세요.",
                    this);
                didWarnMissingPlayerTag = true;
            }
        }

        return false;
    }

    protected void MoveTowardTarget()
    {
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = (Vector2)target.position - (Vector2)transform.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = direction.normalized * moveSpeed;
    }

    protected void MoveAwayFromTarget()
    {
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = (Vector2)transform.position - (Vector2)target.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = direction.normalized * moveSpeed;
    }

    protected Vector3 GetFireOriginPosition()
    {
        return projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position;
    }

    protected Vector2 GetDirectionToTargetFromFireOrigin()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector2 direction = (Vector2)target.position - (Vector2)GetFireOriginPosition();

            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;
        }

        if (currentAimDirection.sqrMagnitude > 0.0001f)
            return currentAimDirection.normalized;

        return Vector2.right;
    }

    protected void StartChargeOrFire()
    {
        if (!CanStartAttack())
            return;

        currentAimDirection = GetDirectionToTargetFromFireOrigin();

        if (chargeDuration <= 0f)
        {
            ExecuteAttack(currentAimDirection);
            attackCooldownTimer = attackCooldown;
            return;
        }

        currentState = RangedState.Charging;
        chargeTimer = chargeDuration;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        BeginChargeVisual();
    }

    protected void UpdateCharging()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        currentAimDirection = GetDirectionToTargetFromFireOrigin();
        UpdateChargeVisualAim(currentAimDirection);

        chargeTimer -= Time.deltaTime;

        if (chargeTimer > 0f)
            return;

        currentState = RangedState.AttackHold;

        Vector2 finalFireDirection = GetDirectionToTargetFromFireOrigin();
        currentAimDirection = finalFireDirection;

        ExecuteChargedAttack(finalFireDirection);
        attackCooldownTimer = attackCooldown;
    }

    protected void CreateChargePreview(
        ref EnemyProjectile2D previewProjectile,
        EnemyProjectile2D projectilePrefab)
    {
        DestroyChargePreview(ref previewProjectile);

        if (projectilePrefab == null)
            return;

        Vector3 spawnPosition = GetFireOriginPosition();
        previewProjectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        Transform previewAnchor = projectileSpawnPoint != null
            ? projectileSpawnPoint
            : transform;

        previewProjectile.PrepareForChargePreview(previewAnchor, currentAimDirection);
    }

    protected void UpdateChargePreviewAimDefault(
        EnemyProjectile2D previewProjectile,
        Vector2 aimDirection)
    {
        if (previewProjectile != null)
            previewProjectile.UpdateChargePreviewAim(aimDirection);
    }

    protected void DestroyChargePreview(ref EnemyProjectile2D previewProjectile)
    {
        if (previewProjectile == null)
            return;

        Destroy(previewProjectile.gameObject);
        previewProjectile = null;
    }

    protected void LaunchPreparedProjectileOrBurst(
        ref EnemyProjectile2D previewProjectile,
        EnemyProjectile2D projectilePrefab,
        Vector2 fireDirection)
    {
        if (projectileCount <= 1 && previewProjectile != null)
        {
            EnemyProjectile2D preparedProjectile = previewProjectile;
            previewProjectile = null;

            preparedProjectile.ConfigureRuntime(
                projectileSpeed,
                Mathf.Max(0, Mathf.RoundToInt(attackDamage)));

            preparedProjectile.Launch(fireDirection);
            return;
        }

        DestroyChargePreview(ref previewProjectile);
        FireProjectileBurst(projectilePrefab, fireDirection);
    }

    protected void FireProjectileBurst(
        EnemyProjectile2D projectilePrefab,
        Vector2 centerDirection)
    {
        if (projectilePrefab == null)
            return;

        int count = Mathf.Max(1, projectileCount);

        if (count == 1)
        {
            FireSingleProjectile(projectilePrefab, centerDirection);
            return;
        }

        float totalSpread = Mathf.Max(0f, spreadAngle);

        for (int i = 0; i < count; i++)
        {
            Vector2 shotDirection = GetSpreadDirection(centerDirection, i, count, totalSpread);
            FireSingleProjectile(projectilePrefab, shotDirection);
        }
    }

    protected void FireSingleProjectile(
        EnemyProjectile2D projectilePrefab,
        Vector2 fireDirection)
    {
        if (projectilePrefab == null)
            return;

        Vector3 spawnPosition = GetFireOriginPosition();
        EnemyProjectile2D spawnedProjectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        spawnedProjectile.ConfigureRuntime(
            projectileSpeed,
            Mathf.Max(0, Mathf.RoundToInt(attackDamage)));

        spawnedProjectile.Launch(fireDirection);
    }

    protected static Vector2 GetSpreadDirection(
        Vector2 centerDirection,
        int shotIndex,
        int shotCount,
        float totalSpreadAngle)
    {
        Vector2 normalizedCenter = centerDirection.sqrMagnitude > 0.0001f
            ? centerDirection.normalized
            : Vector2.right;

        if (shotCount <= 1 || totalSpreadAngle <= 0f)
            return normalizedCenter;

        float startAngle = -totalSpreadAngle * 0.5f;
        float stepAngle = totalSpreadAngle / (shotCount - 1);
        float currentAngle = startAngle + (stepAngle * shotIndex);

        return RotateVector(normalizedCenter, currentAngle);
    }

    protected static Vector2 RotateVector(Vector2 vector, float degree)
    {
        float radian = degree * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radian);
        float sin = Mathf.Sin(radian);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos);
    }

    protected virtual bool CanStartAttack()
    {
        return true;
    }

    /// <summary>
    /// 차징 시작 시 필요한 시각/준비 처리를 구현체가 처리합니다.
    /// </summary>
    protected virtual void BeginChargeVisual()
    {
    }

    /// <summary>
    /// 차징 중 현재 조준 방향이 바뀔 때 필요한 처리를 구현체가 처리합니다.
    /// </summary>
    protected virtual void UpdateChargeVisualAim(Vector2 aimDirection)
    {
    }

    /// <summary>
    /// 차징이 취소되거나 비활성화될 때 정리할 시각 요소를 구현체가 처리합니다.
    /// </summary>
    protected virtual void CancelChargeVisual()
    {
    }

    /// <summary>
    /// 차징 없는 즉시 발사 또는 기본 발사 로직을 구현체가 처리합니다.
    /// </summary>
    protected abstract void ExecuteAttack(Vector2 fireDirection);

    /// <summary>
    /// 차징이 끝난 뒤 실제 공격 실행 로직을 구현체가 처리합니다.
    /// 기본은 일반 공격 호출로 연결합니다.
    /// </summary>
    protected virtual void ExecuteChargedAttack(Vector2 fireDirection)
    {
        CancelChargeVisual();
        ExecuteAttack(fireDirection);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!drawRangeGizmo)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (retreatRange > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, retreatRange);
        }
    }
}