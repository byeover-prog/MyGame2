using UnityEngine;

/// <summary>
/// 기존 추적형 몬스터에 원거리 공격 행동, 차징 상태, 차징 미리보기 발사체를 추가하는 컴포넌트입니다.
///
/// 구현 원리:
/// 1. 플레이어가 공격 사거리 안으로 들어오면 기존 추적 스크립트를 끄고 원거리 행동으로 전환합니다.
/// 2. 공격 쿨타임이 끝나면 차징을 시작하고, 차징용 미리보기 투사체를 SpawnPoint에 붙여 보여줍니다.
/// 3. 차징 중에는 매 프레임 현재 플레이어 위치를 다시 기준으로 조준 방향을 갱신하여, 미리보기 발사체도 계속 플레이어를 따라보게 합니다.
/// 4. 차징이 끝나는 바로 그 순간 다시 한 번 플레이어 위치를 기준으로 최종 발사 방향을 계산한 뒤 발사합니다.
/// </summary>
[DisallowMultipleComponent]
public class EnemyRangedAttacker2D : MonoBehaviour
{
    public enum InRangeMoveMode
    {
        Stop,
        SlowAdvance
    }

    [Header("참조 설정")]
    [Tooltip("현재 타겟 플레이어의 Transform입니다. 비워두면 Player 태그로 자동 탐색합니다.")]
    [SerializeField] private Transform target;

    [Tooltip("기존 추적 이동 스크립트입니다. 현재 프로젝트의 EnemyChaser2D를 여기에 연결합니다.")]
    [SerializeField] private Behaviour chaseMovement;

    [Tooltip("원거리 몬스터 루트의 Rigidbody2D입니다. 비워두면 자동으로 찾아서 연결합니다.")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("투사체가 생성되고 차징 중 붙어 있을 위치입니다. 보통 몬스터 입 앞이나 손 앞의 ProjectileSpawnPoint를 연결합니다. 비워두면 자기 위치를 사용합니다.")]
    [SerializeField] private Transform projectileSpawnPoint;

    [Tooltip("발사할 투사체 프리팹입니다. EnemyProjectile2D가 붙어 있어야 합니다.")]
    [SerializeField] private EnemyProjectile2D projectilePrefab;

    [Header("타겟 탐색 설정")]
    [Tooltip("Target이 비어 있을 때 자동으로 찾을 플레이어 태그입니다. 보통 Player를 사용합니다.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("플레이어 참조를 잃었을 때 다시 찾는 간격입니다.")]
    [SerializeField][Min(0.1f)] private float targetRefindInterval = 0.5f;

    [Header("거리 유지 설정")]
    [Tooltip("플레이어가 이 거리 안으로 들어오면 기존 추적 스크립트를 끄고 원거리 행동으로 전환합니다.")]
    [SerializeField][Min(0.1f)] private float attackRange = 5f;

    [Tooltip("사거리 경계에서 추적 스크립트 On/Off가 떨리지 않도록 복귀 거리를 따로 둡니다. attackRange보다 같거나 크게 두세요.")]
    [SerializeField][Min(0.1f)] private float chaseResumeDistance = 6f;

    [Tooltip("사거리 안에서 완전히 멈출지, 아주 느리게 접근할지 선택합니다. 단, 차징 중에는 이 설정과 무관하게 반드시 멈춥니다.")]
    [SerializeField] private InRangeMoveMode inRangeMoveMode = InRangeMoveMode.Stop;

    [Tooltip("사거리 안에서도 아주 느리게 움직이고 싶을 때 사용하는 속도입니다. Stop 모드에서는 무시됩니다.")]
    [SerializeField][Min(0f)] private float inRangeMoveSpeed = 0.5f;

    [Header("공격 설정")]
    [Tooltip("투사체를 다시 발사하기까지의 대기 시간입니다. 차징이 끝나고 실제 발사한 뒤부터 다시 계산됩니다.")]
    [SerializeField][Min(0.01f)] private float attackCooldown = 2f;

    [Tooltip("공격 준비에 걸리는 차징 시간입니다. 0이면 즉시 발사처럼 동작하고, 1이면 1초 차징 후 발사합니다.")]
    [SerializeField][Min(0f)] private float chargeDuration = 1f;

    [Header("디버그")]
    [Tooltip("체크하면 선택된 상태에서 공격 사거리와 복귀 거리를 씬 뷰에 표시합니다.")]
    [SerializeField] private bool drawRangeGizmo = true;

    private bool _isHoldingRange;
    private bool _isCharging;
    private float _attackCooldownTimer;
    private float _chargeTimer;
    private float _targetRefindTimer;

    // 차징 중 계속 갱신되는 현재 조준 방향
    private Vector2 _currentAimDirection = Vector2.right;

    private EnemyProjectile2D _chargingPreviewProjectile;
    private bool _didWarnMissingPlayerTag;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();

        if (chaseMovement == null)
            chaseMovement = GetComponent("EnemyChaser2D") as Behaviour;

        if (projectileSpawnPoint == null)
            projectileSpawnPoint = transform;
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (chaseMovement == null)
            chaseMovement = GetComponent("EnemyChaser2D") as Behaviour;

        if (projectileSpawnPoint == null)
            projectileSpawnPoint = transform;

        if (projectilePrefab == null)
            Debug.LogWarning("[EnemyRangedAttacker2D] Projectile Prefab이 비어 있습니다. 발사할 수 없습니다.", this);
    }

    private void OnEnable()
    {
        _isHoldingRange = false;
        _isCharging = false;
        _attackCooldownTimer = 0f;
        _chargeTimer = 0f;
        _targetRefindTimer = 0f;
        _chargingPreviewProjectile = null;
        _currentAimDirection = Vector2.right;

        SetChaseMovementEnabled(true);

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDisable()
    {
        _isHoldingRange = false;
        _isCharging = false;

        DestroyChargingPreviewProjectile();
        SetChaseMovementEnabled(true);

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnValidate()
    {
        if (attackRange < 0.1f)
            attackRange = 0.1f;

        if (chaseResumeDistance < attackRange)
            chaseResumeDistance = attackRange;

        if (attackCooldown < 0.01f)
            attackCooldown = 0.01f;

        if (targetRefindInterval < 0.1f)
            targetRefindInterval = 0.1f;

        if (inRangeMoveSpeed < 0f)
            inRangeMoveSpeed = 0f;

        if (chargeDuration < 0f)
            chargeDuration = 0f;
    }

    private void Update()
    {
        if (!TryResolveTarget())
        {
            // 차징 중에는 타겟을 일시적으로 잃어도 마지막 조준 방향을 유지한 채 차징을 계속 진행합니다.
            if (_isCharging)
            {
                _isHoldingRange = true;
                SetChaseMovementEnabled(false);
                UpdateCharging();
                return;
            }

            _isHoldingRange = false;
            SetChaseMovementEnabled(true);
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        bool shouldHoldRange =
            _isCharging ||
            distanceToTarget <= attackRange ||
            (_isHoldingRange && distanceToTarget <= chaseResumeDistance);

        _isHoldingRange = shouldHoldRange;
        SetChaseMovementEnabled(!_isHoldingRange);

        if (!_isHoldingRange)
            return;

        if (_isCharging)
        {
            UpdateCharging();
            return;
        }

        _attackCooldownTimer -= Time.deltaTime;

        if (distanceToTarget <= attackRange && _attackCooldownTimer <= 0f)
            StartChargeOrFire();
    }

    private void FixedUpdate()
    {
        if (!_isHoldingRange || rb == null)
            return;

        if (_isCharging)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (target == null)
            return;

        Vector2 directionToTarget = GetDirectionToTargetFromFireOrigin();

        if (inRangeMoveMode == InRangeMoveMode.Stop)
        {
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.linearVelocity = directionToTarget * inRangeMoveSpeed;
        }
    }

    private bool TryResolveTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy)
            return true;

        _targetRefindTimer -= Time.deltaTime;
        if (_targetRefindTimer > 0f)
            return false;

        _targetRefindTimer = targetRefindInterval;

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
            if (!_didWarnMissingPlayerTag)
            {
                Debug.LogWarning($"[EnemyRangedAttacker2D] Player 태그 '{playerTag}'를 찾지 못했습니다. Tag 설정을 확인하세요.", this);
                _didWarnMissingPlayerTag = true;
            }
        }

        return false;
    }

    /// <summary>
    /// 실제 투사체가 출발할 위치를 반환합니다.
    /// 조준 계산도 이 위치를 기준으로 하는 것이 더 정확합니다.
    /// </summary>
    private Vector3 GetFireOriginPosition()
    {
        return projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position;
    }

    /// <summary>
    /// 현재 발사 위치 기준으로 타겟 방향을 계산합니다.
    /// 타겟이 잠시 없어졌을 때는 마지막으로 유효했던 조준 방향을 유지합니다.
    /// </summary>
    private Vector2 GetDirectionToTargetFromFireOrigin()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector2 direction = (Vector2)target.position - (Vector2)GetFireOriginPosition();

            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;
        }

        if (_currentAimDirection.sqrMagnitude > 0.0001f)
            return _currentAimDirection.normalized;

        return Vector2.right;
    }

    private void StartChargeOrFire()
    {
        if (projectilePrefab == null)
            return;

        _currentAimDirection = GetDirectionToTargetFromFireOrigin();

        if (chargeDuration <= 0f)
        {
            FireNewProjectile(_currentAimDirection);
            _attackCooldownTimer = attackCooldown;
            return;
        }

        _isCharging = true;
        _chargeTimer = chargeDuration;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        CreateChargingPreviewProjectile();
    }

    private void UpdateCharging()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // 차징 중에도 현재 플레이어 방향을 계속 추적해서 미리보기 조준을 갱신합니다.
        _currentAimDirection = GetDirectionToTargetFromFireOrigin();

        if (_chargingPreviewProjectile != null)
            _chargingPreviewProjectile.UpdateChargePreviewAim(_currentAimDirection);

        _chargeTimer -= Time.deltaTime;

        if (_chargeTimer > 0f)
            return;

        _isCharging = false;

        // 발사 직전의 최신 플레이어 위치를 다시 기준으로 최종 방향을 계산합니다.
        Vector2 finalFireDirection = GetDirectionToTargetFromFireOrigin();
        _currentAimDirection = finalFireDirection;

        LaunchPreparedProjectileOrFallback(finalFireDirection);
        _attackCooldownTimer = attackCooldown;
    }

    private void CreateChargingPreviewProjectile()
    {
        DestroyChargingPreviewProjectile();

        if (projectilePrefab == null)
            return;

        Vector3 spawnPosition = GetFireOriginPosition();
        _chargingPreviewProjectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        Transform previewAnchor = projectileSpawnPoint != null
            ? projectileSpawnPoint
            : transform;

        _chargingPreviewProjectile.PrepareForChargePreview(previewAnchor, _currentAimDirection);
    }

    private void LaunchPreparedProjectileOrFallback(Vector2 fireDirection)
    {
        if (_chargingPreviewProjectile != null)
        {
            EnemyProjectile2D preparedProjectile = _chargingPreviewProjectile;
            _chargingPreviewProjectile = null;
            preparedProjectile.Launch(fireDirection);
            return;
        }

        FireNewProjectile(fireDirection);
    }

    private void FireNewProjectile(Vector2 fireDirection)
    {
        if (projectilePrefab == null)
            return;

        Vector3 spawnPosition = GetFireOriginPosition();
        EnemyProjectile2D spawnedProjectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        spawnedProjectile.Launch(fireDirection);
    }

    private void DestroyChargingPreviewProjectile()
    {
        if (_chargingPreviewProjectile == null)
            return;

        Destroy(_chargingPreviewProjectile.gameObject);
        _chargingPreviewProjectile = null;
    }

    private void SetChaseMovementEnabled(bool shouldEnable)
    {
        if (chaseMovement != null && chaseMovement.enabled != shouldEnable)
            chaseMovement.enabled = shouldEnable;

        if (!shouldEnable && rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawRangeGizmo)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseResumeDistance);
    }
}