// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 두억시니 전투의 상위 코디네이터다.
// 전투 설정 진입점은 DuryoksiniCombatConfigSO 하나만 사용한다.
// 실제 공격 실행은 각 실행기가 맡고, 여기서는 어떤 행동을 시도할지만 결정한다.
// 전투 판단 흐름은 "설정 주입 -> 상태 확인 -> 기본 공격/특수 패턴 선택" 순서로 고정한다.

[DisallowMultipleComponent]
public sealed class DuryoksiniCombatController : MonoBehaviour
{
    [Header("전투 설정")]

    [Tooltip("두억시니 전투 전체 설정 SO")]
    [SerializeField] private DuryoksiniCombatConfigSO combatConfig;

    [Header("공통 참조")]

    [Tooltip("보스 공용 타겟 제공 컴포넌트")]
    [SerializeField] private BossTargetProvider targetProvider;

    [Tooltip("보스 공용 추적 이동 컴포넌트")]
    [SerializeField] private BossChaseMovementController chaseMovementController;

    [Header("실행기 참조")]

    [Tooltip("두억시니 기본 공격 실행기")]
    [SerializeField] private DuryoksiniBasicAttackController basicAttackController;

    [Tooltip("두억시니 파쇄 돌진 실행기")]
    [SerializeField] private DuryoksiniCrushChargeController crushChargeController;

    [Tooltip("두억시니 대지 분쇄 실행기")]
    [SerializeField] private DuryoksiniGroundSmashController groundSmashController;

    [Tooltip("두억시니 분노 포효 낙석 실행기")]
    [SerializeField] private DuryoksiniRoarRockfallController roarRockfallController;

    [Header("전투 상태")]

    [Tooltip("전투 활성 여부")]
    [SerializeField] private bool isBattleActive = true;

    [Tooltip("디버그 로그 출력 여부")]
    [SerializeField] private bool debugLog = false;


    private float thinkTimer = 0f;
    private bool hasAppliedConfig = false;


    private void Reset()
    {
        targetProvider = GetComponent<BossTargetProvider>();
        chaseMovementController = GetComponent<BossChaseMovementController>();
        basicAttackController = GetComponent<DuryoksiniBasicAttackController>();
        crushChargeController = GetComponent<DuryoksiniCrushChargeController>();
        groundSmashController = GetComponent<DuryoksiniGroundSmashController>();
        roarRockfallController = GetComponent<DuryoksiniRoarRockfallController>();
    }

    private void Awake()
    {
        CacheLocalReferences();
        ApplyCombatConfigToControllers();
    }

    private void OnEnable()
    {
        thinkTimer = 0f;

        CacheLocalReferences();
        ApplyCombatConfigToControllers();

        if (isBattleActive)
        {
            SetChaseEnabled(true);
        }
    }

    private void OnDisable()
    {
        ForceStopAllPatterns();
    }

    private void CacheLocalReferences()
    {
        if (targetProvider == null)
        {
            targetProvider = GetComponent<BossTargetProvider>();
        }

        if (chaseMovementController == null)
        {
            chaseMovementController = GetComponent<BossChaseMovementController>();
        }

        if (basicAttackController == null)
        {
            basicAttackController = GetComponent<DuryoksiniBasicAttackController>();
        }

        if (crushChargeController == null)
        {
            crushChargeController = GetComponent<DuryoksiniCrushChargeController>();
        }

        if (groundSmashController == null)
        {
            groundSmashController = GetComponent<DuryoksiniGroundSmashController>();
        }

        if (roarRockfallController == null)
        {
            roarRockfallController = GetComponent<DuryoksiniRoarRockfallController>();
        }
    }

    private void Update()
    {
        if (!CanRunCombat())
        {
            return;
        }

        if (IsAnyActionRunning())
        {
            return;
        }

        thinkTimer -= Time.deltaTime;
        if (thinkTimer > 0f)
        {
            return;
        }

        thinkTimer = combatConfig.ThinkInterval;

        Transform target = targetProvider.GetTarget();
        if (target == null)
        {
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        TryExecuteCombatDecision(target, distanceToTarget);
    }

    private bool CanRunCombat()
    {
        if (!isBattleActive)
        {
            return false;
        }

        if (combatConfig == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("[DuryoksiniCombatController] CombatConfig가 비어 있습니다.", this);
            }

            return false;
        }

        if (!hasAppliedConfig)
        {
            ApplyCombatConfigToControllers();
        }

        if (targetProvider == null || !targetProvider.HasTarget())
        {
            return false;
        }

        return true;
    }

    private void TryExecuteCombatDecision(Transform target, float distanceToTarget)
    {
        if (combatConfig == null || target == null)
        {
            return;
        }

        if (combatConfig.PreferBasicAttackInRange)
        {
            if (TryUseBasicAttack(target, distanceToTarget))
            {
                return;
            }

            TryUseSpecialPatternByPriority(target, distanceToTarget);
            return;
        }

        if (TryUseSpecialPatternByPriority(target, distanceToTarget))
        {
            return;
        }

        TryUseBasicAttack(target, distanceToTarget);
    }

    private void ApplyCombatConfigToControllers()
    {
        hasAppliedConfig = false;

        if (combatConfig == null)
        {
            return;
        }

        if (combatConfig.BasicAttackConfig == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("[DuryoksiniCombatController] BasicAttackConfig가 비어 있습니다.", this);
            }
        }
        else if (basicAttackController != null)
        {
            basicAttackController.SetExternalConfig(combatConfig.BasicAttackConfig);
        }

        if (combatConfig.PatternCatalog == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("[DuryoksiniCombatController] PatternCatalog가 비어 있습니다.", this);
            }
        }
        else
        {
            if (crushChargeController != null)
            {
                crushChargeController.SetExternalPatternCatalog(combatConfig.PatternCatalog);
            }

            if (groundSmashController != null)
            {
                groundSmashController.SetExternalPatternCatalog(combatConfig.PatternCatalog);
            }

            if (roarRockfallController != null)
            {
                roarRockfallController.SetExternalPatternCatalog(combatConfig.PatternCatalog);
            }
        }

        hasAppliedConfig = true;

        if (debugLog)
        {
            Debug.Log("[DuryoksiniCombatController] CombatConfig 설정 주입 완료", this);
        }
    }

    private bool TryUseBasicAttack(Transform target, float distanceToTarget)
    {
        if (basicAttackController == null || target == null)
        {
            return false;
        }

        if (!basicAttackController.CanStartAttackByDistance(distanceToTarget))
        {
            return false;
        }

        bool started = basicAttackController.TryStartAttack(target);

        if (started && debugLog)
        {
            Debug.Log("[DuryoksiniCombatController] 기본 공격 선택", this);
        }

        return started;
    }

    private bool TryUseSpecialPatternByPriority(Transform target, float distanceToTarget)
    {
        if (combatConfig == null || target == null)
        {
            return false;
        }

        if (TryUsePattern(combatConfig.FirstPattern, target, distanceToTarget))
        {
            return true;
        }

        if (TryUsePattern(combatConfig.SecondPattern, target, distanceToTarget))
        {
            return true;
        }

        if (TryUsePattern(combatConfig.ThirdPattern, target, distanceToTarget))
        {
            return true;
        }

        return false;
    }

    private bool TryUsePattern(DuryoksiniCombatConfigSO.PatternType patternType, Transform target, float distanceToTarget)
    {
        switch (patternType)
        {
            case DuryoksiniCombatConfigSO.PatternType.RoarRockfall:
                return TryUseRoarRockfall(target, distanceToTarget);

            case DuryoksiniCombatConfigSO.PatternType.CrushCharge:
                return TryUseCrushCharge(target, distanceToTarget);

            case DuryoksiniCombatConfigSO.PatternType.GroundSmash:
                return TryUseGroundSmash(target, distanceToTarget);
        }

        return false;
    }

    private bool TryUseRoarRockfall(Transform target, float distanceToTarget)
    {
        if (roarRockfallController == null)
        {
            return false;
        }

        if (!roarRockfallController.CanStartPatternByDistance(distanceToTarget))
        {
            return false;
        }

        bool started = roarRockfallController.TryStartPattern(target);

        if (started && debugLog)
        {
            Debug.Log("[DuryoksiniCombatController] 분노 포효 낙석 선택", this);
        }

        return started;
    }

    private bool TryUseCrushCharge(Transform target, float distanceToTarget)
    {
        if (crushChargeController == null)
        {
            return false;
        }

        if (!crushChargeController.CanStartPatternByDistance(distanceToTarget))
        {
            return false;
        }

        bool started = crushChargeController.TryStartPattern(target);

        if (started && debugLog)
        {
            Debug.Log("[DuryoksiniCombatController] 파쇄 돌진 선택", this);
        }

        return started;
    }

    private bool TryUseGroundSmash(Transform target, float distanceToTarget)
    {
        if (groundSmashController == null)
        {
            return false;
        }

        if (!groundSmashController.CanStartPatternByDistance(distanceToTarget))
        {
            return false;
        }

        bool started = groundSmashController.TryStartPattern(target);

        if (started && debugLog)
        {
            Debug.Log("[DuryoksiniCombatController] 대지 분쇄 선택", this);
        }

        return started;
    }

    private bool IsAnyActionRunning()
    {
        if (basicAttackController != null && basicAttackController.IsRunningAttack())
        {
            return true;
        }

        if (crushChargeController != null && crushChargeController.IsRunningPattern())
        {
            return true;
        }

        if (groundSmashController != null && groundSmashController.IsRunningPattern())
        {
            return true;
        }

        if (roarRockfallController != null && roarRockfallController.IsRunningPattern())
        {
            return true;
        }

        return false;
    }

    private void SetChaseEnabled(bool value)
    {
        if (chaseMovementController != null)
        {
            chaseMovementController.SetCanChase(value);
        }
    }

    public void SetBattleActive(bool value)
    {
        isBattleActive = value;

        if (!isBattleActive)
        {
            ForceStopAllPatterns();
            SetChaseEnabled(false);
            return;
        }

        thinkTimer = 0f;
        ApplyCombatConfigToControllers();
        SetChaseEnabled(true);
    }

    public void ForceStopAllPatterns()
    {
        if (basicAttackController != null)
        {
            basicAttackController.ForceStopAttack();
        }

        if (crushChargeController != null)
        {
            crushChargeController.ForceStopPattern();
        }

        if (groundSmashController != null)
        {
            groundSmashController.ForceStopPattern();
        }

        if (roarRockfallController != null)
        {
            roarRockfallController.ForceStopPattern();
        }

        SetChaseEnabled(true);
    }
}