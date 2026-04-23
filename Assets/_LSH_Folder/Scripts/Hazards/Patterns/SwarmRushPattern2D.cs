// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무리 돌진형 방해 패턴을 제어하는 패턴 루트 컴포넌트입니다.
///
/// 변경된 구현 흐름 요약:
/// 1. 패턴은 HazardEventManager2D가 계산한 맵 내부 안전 좌표에서 시작합니다.
/// 2. 시작 후 여러 멤버 몬스터를 진형으로 생성합니다.
/// 3. 1단계(Aiming)에서는 플레이어를 향해 조준하며 접근합니다.
/// 4. 더 이상 "원형 거리 warningTriggerRange"로 경고하지 않습니다.
/// 5. 대신 무리 중심을 Camera.WorldToViewportPoint()로 변환해서,
///    화면 바깥 warningViewportMargin 띠 영역에 들어왔을 때 경고 UI를 1회 표시합니다.
/// 6. 경고 UI를 표시한 뒤 Telegraphing 상태로 warningDuration만큼 대기합니다.
/// 7. 대기 종료 시 그 순간의 방향을 고정하고 Dashing 상태로 돌진합니다.
/// 8. 돌진 후 시간이 끝나거나 무리 전체가 화면 밖으로 나가면 조용히 정리됩니다.
///
/// 왜 뷰포트 기반이 필요한가?
/// - 화면은 직사각형인데, Vector2.Distance는 원형 기준입니다.
/// - 그래서 위/아래와 좌/우의 "보이기 직전" 타이밍이 어긋날 수 있습니다.
/// - 뷰포트 좌표는 화면 내부를 (0~1, 0~1) 직사각형으로 다루므로
///   어느 방향에서 오든 더 일관된 경고 시점을 만들 수 있습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SwarmRushPattern2D : MonoBehaviour
{
    public enum PatternPhase
    {
        Idle,
        Aiming,
        Telegraphing,
        Dashing,
        Finished
    }

    [Header("필수 참조")]
    [Tooltip("무리에 사용할 개별 멤버 몬스터 프리팹입니다. 예: Normal_Mouse.prefab, Normal_Wolf.prefab")]
    [SerializeField] private GameObject memberPrefab;

    [Tooltip("플레이어 Transform입니다. 비워두면 Player 태그로 자동 탐색합니다.")]
    [SerializeField] private Transform target;

    [Tooltip("플레이어를 자동 탐색할 때 사용할 태그입니다.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("뷰포트 판정과 화면 밖 정리에 사용할 카메라입니다. 비워두면 Camera.main을 사용합니다.")]
    [SerializeField] private Camera gameplayCamera;

    [Header("스폰 / 진형 설정")]
    [Tooltip("생성할 멤버 수입니다.")]
    [SerializeField][Min(1)] private int swarmSize = 12;

    [Tooltip("한 줄에 몇 마리씩 배치할지 설정합니다.")]
    [SerializeField][Min(1)] private int formationColumns = 4;

    [Tooltip("멤버들 간의 기본 간격입니다. X는 좌우 간격, Y는 상하 간격입니다.")]
    [SerializeField] private Vector2 formationSpacing = new Vector2(1.2f, 0.8f);

    [Tooltip("진형이 너무 기계적으로 보이지 않도록 약간의 랜덤 위치 오차를 줍니다.")]
    [SerializeField][Min(0f)] private float formationJitter = 0.15f;

    [Header("이동 단계 설정")]
    [Tooltip("1단계 조준 상태에서 플레이어를 향해 접근할 때의 속도입니다.")]
    [SerializeField][Min(0f)] private float aimMoveSpeed = 4.0f;

    [Tooltip("실제 돌진 직전 방향을 고정할 거리입니다.")]
    [SerializeField][Min(0.01f)] private float detectionRange = 4.5f;

    [Tooltip("돌진 단계 속도입니다.")]
    [SerializeField][Min(0.01f)] private float dashSpeed = 15f;

    [Tooltip("돌진 시작 후 이 시간이 지나면 남은 무리를 조용히 정리합니다.")]
    [SerializeField][Min(0.01f)] private float swarmLifeTime = 4f;

    [Header("텔레그래프(경고) 설정")]
    [Tooltip("화면 경계선 바깥 몇 퍼센트 지점에서 경고를 켤지 결정합니다. 0.1이면 화면 바깥 10% 띠에서 경고합니다.")]
    [SerializeField][Range(0.01f, 0.5f)] private float warningViewportMargin = 0.1f;

    [Tooltip("경고 UI를 표시한 뒤 실제 돌진 전까지 대기할 시간입니다.")]
    [SerializeField][Min(0.01f)] private float warningDuration = 0.6f;

    [Header("화면 밖 정리 설정")]
    [Tooltip("체크하면 무리 전체가 화면 밖으로 충분히 벗어나자마자 바로 정리합니다.")]
    [SerializeField] private bool despawnWhenAllMembersLeaveView = true;

    [Tooltip("화면 밖 이탈 판정에 사용할 뷰포트 패딩입니다. 0.15면 화면 경계보다 15% 더 나간 뒤 제거됩니다.")]
    [SerializeField][Min(0f)] private float despawnViewportPadding = 0.15f;

    [Header("멤버 방향 표시 설정")]
    [Tooltip("개별 멤버의 방향 표현 방식입니다. 일반 2D 스프라이트는 FlipX를 권장합니다.")]
    [SerializeField] private SwarmRushMember2D.OrientationMode memberOrientationMode =
        SwarmRushMember2D.OrientationMode.FlipX;

    [Tooltip("원본 멤버 스프라이트가 기본적으로 바라보는 방향입니다.")]
    [SerializeField] private SwarmRushMember2D.DefaultFacingDirection memberDefaultFacingDirection =
        SwarmRushMember2D.DefaultFacingDirection.Left;

    [Header("기존 몬스터 컴포넌트 제어")]
    [Tooltip("체크하면 개별 멤버의 EnemyChaser2D를 꺼서 기본 추적 AI와 충돌하지 않게 합니다.")]
    [SerializeField] private bool disableEnemyChaserOnMembers = true;

    [Tooltip("체크하면 개별 멤버의 EnemyAutoDespawn2D를 꺼서 패턴 이동 중 따로 사라지지 않게 합니다.")]
    [SerializeField] private bool disableEnemyAutoDespawnOnMembers = true;

    [Tooltip("체크하면 개별 멤버의 EnemySpriteFlip2D를 꺼서 패턴 방향 표시와 충돌하지 않게 합니다.")]
    [SerializeField] private bool disableEnemySpriteFlipOnMembers = true;

    [Header("자동 시작 설정")]
    [Tooltip("체크하면 생성 직후 자동으로 무리를 스폰하고 패턴을 시작합니다.")]
    [SerializeField] private bool autoStartOnEnable = true;

    [Header("디버그")]
    [Tooltip("체크하면 씬 뷰에 detectionRange와 현재 진행 방향을 기즈모로 표시합니다.")]
    [SerializeField] private bool drawDebugGizmos = true;

    [Tooltip("체크하면 주요 상태 전환을 콘솔에 출력합니다.")]
    [SerializeField] private bool debugLog = false;

    private readonly List<SwarmRushMember2D> _members = new List<SwarmRushMember2D>(32);

    private PatternPhase _phase = PatternPhase.Idle;
    private float _targetRefindTimer;
    private float _dashTimer;
    private float _telegraphTimer;

    private bool _hasSpawnedMembers;
    private bool _isFinishing;
    private bool _hasTriggeredTelegraph;

    private Vector2 _currentAimDirection = Vector2.right;
    private Vector2 _lockedDashDirection = Vector2.right;
    private Vector2 _currentVelocity = Vector2.zero;

    private HazardEventManager2D _hazardEventManager;

    private const float TargetRefindInterval = 0.5f;

    private void Reset()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
    }

    private void Awake()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        _hazardEventManager = FindFirstObjectByType<HazardEventManager2D>();
    }

    private void OnEnable()
    {
        _members.Clear();
        _phase = PatternPhase.Idle;
        _targetRefindTimer = 0f;
        _dashTimer = 0f;
        _telegraphTimer = 0f;
        _hasSpawnedMembers = false;
        _isFinishing = false;
        _hasTriggeredTelegraph = false;

        _currentVelocity = Vector2.zero;
        _currentAimDirection = Vector2.right;
        _lockedDashDirection = Vector2.right;

        if (autoStartOnEnable)
            BeginPattern();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        if (!_isFinishing && _phase != PatternPhase.Finished)
            SilentDespawnAllMembers();
    }

    private void OnValidate()
    {
        if (swarmSize < 1)
            swarmSize = 1;

        if (formationColumns < 1)
            formationColumns = 1;

        if (formationJitter < 0f)
            formationJitter = 0f;

        if (detectionRange < 0.01f)
            detectionRange = 0.01f;

        if (dashSpeed < 0.01f)
            dashSpeed = 0.01f;

        if (swarmLifeTime < 0.01f)
            swarmLifeTime = 0.01f;

        if (despawnViewportPadding < 0f)
            despawnViewportPadding = 0f;

        if (warningViewportMargin < 0.01f)
            warningViewportMargin = 0.01f;

        if (warningViewportMargin > 0.5f)
            warningViewportMargin = 0.5f;

        if (warningDuration < 0.01f)
            warningDuration = 0.01f;
    }

    [ContextMenu("Begin Pattern")]
    public void BeginPattern()
    {
        if (_hasSpawnedMembers)
            return;

        if (memberPrefab == null)
        {
            Debug.LogWarning("[SwarmRushPattern2D] Member Prefab이 비어 있습니다.", this);
            return;
        }

        ResolveTarget(force: true);
        SpawnMembers();

        _hasSpawnedMembers = _members.Count > 0;
        _phase = _hasSpawnedMembers ? PatternPhase.Aiming : PatternPhase.Finished;

        if (debugLog && _hasSpawnedMembers)
            Debug.Log($"[SwarmRushPattern2D] 패턴 시작. 멤버 수: {_members.Count}", this);
    }

    public void NotifyMemberDisabled(SwarmRushMember2D member)
    {
        _members.Remove(member);

        if (_isFinishing)
            return;

        if (GetAliveMemberCount() <= 0)
            FinishPattern();
    }

    private void Update()
    {
        if (!_hasSpawnedMembers || _phase == PatternPhase.Finished)
            return;

        CleanupDeadReferences();

        if (GetAliveMemberCount() <= 0)
        {
            FinishPattern();
            return;
        }

        ResolveTarget(force: false);

        switch (_phase)
        {
            case PatternPhase.Aiming:
                UpdateAimingPhase();
                break;

            case PatternPhase.Telegraphing:
                UpdateTelegraphingPhase();
                break;

            case PatternPhase.Dashing:
                UpdateDashingPhase();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (_phase != PatternPhase.Aiming && _phase != PatternPhase.Dashing)
            return;

        Vector2 facingDirection = _phase == PatternPhase.Dashing
            ? _lockedDashDirection
            : _currentAimDirection;

        for (int i = 0; i < _members.Count; i++)
        {
            SwarmRushMember2D member = _members[i];
            if (!IsUsableMember(member))
                continue;

            member.ApplyMovement(_currentVelocity, facingDirection);
        }
    }

    /// <summary>
    /// 1단계: 플레이어를 조준하며 접근하는 단계입니다.
    ///
    /// 변경된 핵심:
    /// - 더 이상 원형 거리로 "경고 시점"을 판정하지 않습니다.
    /// - 대신 무리 중심을 뷰포트 좌표로 변환하고,
    ///   "화면 내부는 아니지만, 화면 경계 바깥 warningViewportMargin 띠 안쪽"일 때
    ///   경고를 1회 발생시킵니다.
    /// - 카메라를 찾지 못하면 최소 동작 보장을 위해 detectionRange 기준 폴백을 사용합니다.
    /// </summary>
    private void UpdateAimingPhase()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            _currentVelocity = Vector2.zero;
            return;
        }

        Vector2 aliveCenter = GetAliveCenterPosition();
        Vector2 toTarget = (Vector2)target.position - aliveCenter;

        // 플레이어를 향한 조준 방향 갱신
        if (toTarget.sqrMagnitude > 0.0001f)
            _currentAimDirection = toTarget.normalized;

        _currentVelocity = _currentAimDirection * aimMoveSpeed;

        // 아직 경고를 한 번도 띄우지 않았다면, 뷰포트 기반으로 경고 시점을 체크한다.
        if (!_hasTriggeredTelegraph)
        {
            bool shouldTriggerWarning = false;

            if (IsInViewportWarningBand(aliveCenter, out _))
            {
                shouldTriggerWarning = true;
            }
            else
            {
                // 안전 장치(Fallback)
                // 카메라를 아예 찾지 못하는 상황이면 최소한 detectionRange에서 경고가 뜨게 한다.
                bool cameraUnavailable = !TryGetGameplayCamera(out _);
                if (cameraUnavailable)
                {
                    float distanceToTarget = Vector2.Distance(aliveCenter, target.position);
                    if (distanceToTarget <= detectionRange)
                        shouldTriggerWarning = true;
                }
            }

            if (shouldTriggerWarning)
            {
                _hasTriggeredTelegraph = true;
                _telegraphTimer = warningDuration;
                _phase = PatternPhase.Telegraphing;
                _currentVelocity = Vector2.zero;

                if (_hazardEventManager != null)
                    _hazardEventManager.ShowWarning(aliveCenter, warningDuration);

                if (debugLog)
                    Debug.Log("[SwarmRushPattern2D] 뷰포트 경고 발동. Telegraphing 상태 진입.", this);

                return;
            }
        }

        // 추가 안전장치:
        // 혹시라도 어떤 이유로 뷰포트 경고 없이 detectionRange까지 깊게 접근했다면
        // 상태 꼬임을 막기 위해 detectionRange에서라도 텔레그래프를 강제로 시작한다.
        if (!_hasTriggeredTelegraph)
        {
            float distanceToTarget = Vector2.Distance(aliveCenter, target.position);
            if (distanceToTarget <= detectionRange)
            {
                _hasTriggeredTelegraph = true;
                _telegraphTimer = warningDuration;
                _phase = PatternPhase.Telegraphing;
                _currentVelocity = Vector2.zero;

                if (_hazardEventManager != null)
                    _hazardEventManager.ShowWarning(aliveCenter, warningDuration);

                if (debugLog)
                    Debug.Log("[SwarmRushPattern2D] detectionRange 안전장치로 Telegraphing 진입.", this);

                return;
            }
        }
    }

    /// <summary>
    /// 2단계: 경고 UI를 보여 주는 예고 단계입니다.
    /// warningDuration 동안 잠깐 멈춘 뒤,
    /// 그 순간의 플레이어 방향을 다시 계산해서 돌진 방향을 고정합니다.
    /// </summary>
    private void UpdateTelegraphingPhase()
    {
        _currentVelocity = Vector2.zero;
        _telegraphTimer -= Time.deltaTime;

        if (_telegraphTimer > 0f)
            return;

        Vector2 aliveCenter = GetAliveCenterPosition();

        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector2 toTarget = (Vector2)target.position - aliveCenter;

            if (toTarget.sqrMagnitude > 0.0001f)
                _lockedDashDirection = toTarget.normalized;
            else
                _lockedDashDirection = _currentAimDirection.sqrMagnitude > 0.0001f
                    ? _currentAimDirection.normalized
                    : Vector2.right;
        }
        else
        {
            _lockedDashDirection = _currentAimDirection.sqrMagnitude > 0.0001f
                ? _currentAimDirection.normalized
                : Vector2.right;
        }

        _dashTimer = 0f;
        _phase = PatternPhase.Dashing;
        _currentVelocity = _lockedDashDirection * dashSpeed;

        if (debugLog)
            Debug.Log("[SwarmRushPattern2D] Telegraphing 종료. 방향 고정 후 돌진 시작.", this);
    }

    private void UpdateDashingPhase()
    {
        _dashTimer += Time.deltaTime;
        _currentVelocity = _lockedDashDirection * dashSpeed;

        if (_dashTimer >= swarmLifeTime)
        {
            if (debugLog)
                Debug.Log("[SwarmRushPattern2D] swarmLifeTime 종료. 남은 멤버 조용히 정리.", this);

            SilentDespawnAllMembers();
            return;
        }

        if (despawnWhenAllMembersLeaveView && AreAllMembersOutsideView())
        {
            if (debugLog)
                Debug.Log("[SwarmRushPattern2D] 무리 전체가 화면 밖으로 나감. 조용히 정리.", this);

            SilentDespawnAllMembers();
        }
    }

    private void SpawnMembers()
    {
        _members.Clear();

        int totalRows = Mathf.CeilToInt(swarmSize / (float)formationColumns);

        for (int i = 0; i < swarmSize; i++)
        {
            Vector2 localOffset = CalculateFormationOffset(i, totalRows);
            Vector3 worldPosition = transform.TransformPoint(localOffset);

            GameObject spawned = Instantiate(memberPrefab, worldPosition, transform.rotation, transform);
            spawned.name = $"{memberPrefab.name}_SwarmMember_{i:D2}";

            SwarmRushMember2D member = spawned.GetComponent<SwarmRushMember2D>();
            if (member == null)
                member = spawned.AddComponent<SwarmRushMember2D>();

            member.Initialize(
                this,
                memberOrientationMode,
                memberDefaultFacingDirection,
                disableEnemyChaserOnMembers,
                disableEnemyAutoDespawnOnMembers,
                disableEnemySpriteFlipOnMembers);

            _members.Add(member);
        }
    }

    private Vector2 CalculateFormationOffset(int index, int totalRows)
    {
        int column = index % formationColumns;
        int row = index / formationColumns;

        float width = (formationColumns - 1) * formationSpacing.x;
        float height = (totalRows - 1) * formationSpacing.y;

        float x = column * formationSpacing.x - width * 0.5f;
        float y = height * 0.5f - row * formationSpacing.y;

        Vector2 offset = new Vector2(x, y);

        if (formationJitter > 0f)
            offset += Random.insideUnitCircle * formationJitter;

        return offset;
    }

    /// <summary>
    /// gameplayCamera가 비어 있으면 Camera.main으로 한 번 복구를 시도합니다.
    /// 둘 다 없으면 false를 반환합니다.
    /// </summary>
    private bool TryGetGameplayCamera(out Camera resolvedCamera)
    {
        if (gameplayCamera != null)
        {
            resolvedCamera = gameplayCamera;
            return true;
        }

        gameplayCamera = Camera.main;
        if (gameplayCamera != null)
        {
            resolvedCamera = gameplayCamera;
            return true;
        }

        resolvedCamera = null;
        return false;
    }

    /// <summary>
    /// 현재 월드 좌표가 "화면에 보이기 직전" 경고 띠 영역에 들어왔는지 검사합니다.
    ///
    /// 구현 원리:
    /// 1. WorldToViewportPoint로 월드 좌표를 정규화된 뷰포트 좌표로 변환합니다.
    /// 2. 화면 내부는 x,y가 0~1 범위입니다.
    /// 3. warningViewportMargin만큼 확장한 직사각형(-m ~ 1+m)을 만듭니다.
    /// 4. 화면 내부는 아니지만, 확장 직사각형 안에는 들어온 상태라면
    ///    "화면 가장자리 바로 바깥"에 도달한 것으로 보고 경고를 켭니다.
    /// </summary>
    private bool IsInViewportWarningBand(Vector3 worldPosition, out Vector3 viewportPosition)
    {
        viewportPosition = default;

        if (!TryGetGameplayCamera(out Camera cam))
            return false;

        viewportPosition = cam.WorldToViewportPoint(worldPosition);

        // 카메라 뒤쪽은 경고 대상이 아닙니다.
        if (viewportPosition.z <= 0f)
            return false;

        bool isInsideVisibleRect =
            viewportPosition.x >= 0f && viewportPosition.x <= 1f &&
            viewportPosition.y >= 0f && viewportPosition.y <= 1f;

        if (isInsideVisibleRect)
            return false;

        float margin = warningViewportMargin;

        bool isInsideExpandedRect =
            viewportPosition.x >= -margin && viewportPosition.x <= 1f + margin &&
            viewportPosition.y >= -margin && viewportPosition.y <= 1f + margin;

        return isInsideExpandedRect;
    }

    private void ResolveTarget(bool force)
    {
        if (target != null && target.gameObject.activeInHierarchy)
            return;

        target = null;

        if (!force)
        {
            _targetRefindTimer -= Time.deltaTime;
            if (_targetRefindTimer > 0f)
                return;
        }

        _targetRefindTimer = TargetRefindInterval;

        if (string.IsNullOrWhiteSpace(playerTag))
            return;

        try
        {
            GameObject player = GameObject.FindWithTag(playerTag);
            if (player != null)
                target = player.transform;
        }
        catch (UnityException)
        {
            // 태그가 없으면 현재 방향 유지
        }
    }

    private bool AreAllMembersOutsideView()
    {
        if (!TryGetGameplayCamera(out Camera cam))
            return false;

        bool hasAlive = false;

        for (int i = 0; i < _members.Count; i++)
        {
            SwarmRushMember2D member = _members[i];
            if (!IsUsableMember(member))
                continue;

            hasAlive = true;

            Vector3 viewport = cam.WorldToViewportPoint(member.transform.position);

            bool isOutside =
                viewport.z < 0f ||
                viewport.x < -despawnViewportPadding ||
                viewport.x > 1f + despawnViewportPadding ||
                viewport.y < -despawnViewportPadding ||
                viewport.y > 1f + despawnViewportPadding;

            if (!isOutside)
                return false;
        }

        return hasAlive;
    }

    private int GetAliveMemberCount()
    {
        int count = 0;

        for (int i = 0; i < _members.Count; i++)
        {
            if (IsUsableMember(_members[i]))
                count++;
        }

        return count;
    }

    private Vector2 GetAliveCenterPosition()
    {
        Vector2 sum = Vector2.zero;
        int count = 0;

        for (int i = 0; i < _members.Count; i++)
        {
            SwarmRushMember2D member = _members[i];
            if (!IsUsableMember(member))
                continue;

            sum += member.Position;
            count++;
        }

        return count > 0 ? sum / count : (Vector2)transform.position;
    }

    private bool IsUsableMember(SwarmRushMember2D member)
    {
        return member != null && member.IsActiveMember;
    }

    private void CleanupDeadReferences()
    {
        for (int i = _members.Count - 1; i >= 0; i--)
        {
            if (_members[i] == null || !_members[i].gameObject.activeInHierarchy)
                _members.RemoveAt(i);
        }
    }

    private void SilentDespawnAllMembers()
    {
        if (_isFinishing)
            return;

        _isFinishing = true;
        _phase = PatternPhase.Finished;
        _currentVelocity = Vector2.zero;

        SwarmRushMember2D[] snapshot = _members.ToArray();

        for (int i = 0; i < snapshot.Length; i++)
        {
            SwarmRushMember2D member = snapshot[i];
            if (member == null || !member.gameObject.activeInHierarchy)
                continue;

            member.SilentDespawn();
        }

        _members.Clear();
        Destroy(gameObject);
    }

    private void FinishPattern()
    {
        if (_isFinishing)
            return;

        _isFinishing = true;
        _phase = PatternPhase.Finished;
        _currentVelocity = Vector2.zero;
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
            return;

        Vector3 center = Application.isPlaying
            ? (Vector3)GetAliveCenterPosition()
            : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, detectionRange);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(center, center + (Vector3)_currentAimDirection * 2f);
        }
    }
}