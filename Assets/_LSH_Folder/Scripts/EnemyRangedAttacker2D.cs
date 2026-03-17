using UnityEngine;

/// <summary>
/// 기존 추적형 몬스터에 원거리 공격 행동을 추가하는 컴포넌트입니다.
/// 구현 원리:
/// 1. 플레이어가 공격 사거리 안으로 들어오면 기존 추적 스크립트를 끄고 이동 충돌을 막습니다.
/// 2. 사거리 안에서는 멈추거나 아주 느리게 움직이면서 쿨타임마다 투사체를 발사합니다.
/// 3. 플레이어가 다시 멀어지면 기존 추적 스크립트를 다시 켜서 원래 이동 로직으로 복귀합니다.
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

    [Tooltip("기존 추적 이동 스크립트입니다. 현재 프로젝트의 EnemyChaser2D를 여기에 드래그하세요.")]
    [SerializeField] private Behaviour chaseMovement;

    [Tooltip("원거리 몬스터 루트의 Rigidbody2D입니다. 비워두면 자동으로 찾아서 연결합니다.")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("투사체가 생성될 위치입니다. 보통 몬스터 입 앞이나 손 앞에 만든 ProjectileSpawnPoint를 연결합니다. 비워두면 자기 위치에서 발사합니다.")]
    [SerializeField] private Transform projectileSpawnPoint;

    [Tooltip("발사할 투사체 프리팹입니다. EnemyProjectile2D가 붙어 있어야 합니다.")]
    [SerializeField] private EnemyProjectile2D projectilePrefab;

    [Header("타겟 탐색 설정")]
    [Tooltip("Target이 비어 있을 때 자동으로 찾을 플레이어 태그입니다.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("플레이어 참조를 잃었을 때 다시 찾는 간격입니다.")]
    [SerializeField][Min(0.1f)] private float targetRefindInterval = 0.5f;

    [Header("거리 유지 설정")]
    [Tooltip("플레이어가 이 거리 안으로 들어오면 기존 추적 스크립트를 끄고 원거리 행동으로 전환합니다.")]
    [SerializeField][Min(0.1f)] private float attackRange = 15f;

    [Tooltip("사거리 경계에서 추적 스크립트 On/Off가 덜 떨리도록 복귀 거리를 따로 둡니다. attackRange보다 크게 두세요.")]
    [SerializeField][Min(0.1f)] private float chaseResumeDistance = 15f;

    [Tooltip("사거리 안에서 완전히 멈출지, 아주 느리게 접근할지 선택합니다.")]
    [SerializeField] private InRangeMoveMode inRangeMoveMode = InRangeMoveMode.Stop;

    [Tooltip("사거리 안에서도 아주 느리게 움직이고 싶을 때 사용하는 속도입니다. Stop 모드에서는 무시됩니다.")]
    [SerializeField][Min(0f)] private float inRangeMoveSpeed = 0.5f;

    [Header("공격 설정")]
    [Tooltip("투사체를 다시 발사하기까지의 대기 시간입니다.")]
    [SerializeField][Min(0.01f)] private float attackCooldown = 2f;

    [Header("디버그")]
    [Tooltip("체크하면 선택된 상태에서 공격 사거리와 복귀 거리를 씬 뷰에 표시합니다.")]
    [SerializeField] private bool drawRangeGizmo = true;

    private bool _isHoldingRange;
    private float _attackCooldownTimer;
    private float _targetRefindTimer;
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
        _attackCooldownTimer = 0f;
        _targetRefindTimer = 0f;
        SetChaseMovementEnabled(true);
    }

    private void OnDisable()
    {
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
    }

    private void Update()
    {
        if (!TryResolveTarget())
        {
            _isHoldingRange = false;
            SetChaseMovementEnabled(true);
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        bool shouldHoldRange =
            distanceToTarget <= attackRange ||
            (_isHoldingRange && distanceToTarget <= chaseResumeDistance);

        _isHoldingRange = shouldHoldRange;
        SetChaseMovementEnabled(!_isHoldingRange);

        if (!_isHoldingRange)
            return;

        _attackCooldownTimer -= Time.deltaTime;

        if (distanceToTarget <= attackRange && _attackCooldownTimer <= 0f)
        {
            FireProjectile();
            _attackCooldownTimer = attackCooldown;
        }
    }

    private void FixedUpdate()
    {
        if (!_isHoldingRange || rb == null || target == null)
            return;

        Vector2 directionToTarget = GetDirectionToTarget();

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

    private Vector2 GetDirectionToTarget()
    {
        if (target == null)
            return Vector2.right;

        Vector2 direction = (target.position - transform.position);

        if (direction.sqrMagnitude <= 0.0001f)
            return Vector2.right;

        return direction.normalized;
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null || target == null)
            return;

        Vector2 fireDirection = GetDirectionToTarget();
        Vector3 spawnPosition = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;

        EnemyProjectile2D spawnedProjectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        spawnedProjectile.Launch(fireDirection);
    }

    private void SetChaseMovementEnabled(bool shouldEnable)
    {
        if (chaseMovement != null && chaseMovement.enabled != shouldEnable)
            chaseMovement.enabled = shouldEnable;

        if (!shouldEnable && rb != null && inRangeMoveMode == InRangeMoveMode.Stop)
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