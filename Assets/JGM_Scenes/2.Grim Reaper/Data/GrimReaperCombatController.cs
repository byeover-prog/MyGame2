// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 저승사자 전투의 상위 판단기다.
// 직접 피해를 주지 않고, 기본 공격 실행기에게 시작 요청만 보낸다.
// 지금 단계에서는 기본 공격만 연결하고, 특수 패턴은 나중에 이 구조 안에 추가한다.

[DisallowMultipleComponent]
public sealed class GrimReaperCombatController : MonoBehaviour
{
    [Header("전투 설정")]

    [Tooltip("저승사자 전투 전체 설정 SO")]
    [SerializeField] private GrimReaperCombatConfigSO combatConfig;


    [Header("공통 참조")]

    [Tooltip("보스 공용 타겟 제공 컴포넌트")]
    [SerializeField] private BossTargetProvider targetProvider;

    [Tooltip("보스 공용 추적 이동 컴포넌트")]
    [SerializeField] private BossChaseMovementController chaseMovementController;


    [Header("실행기 참조")]

    [Tooltip("저승사자 기본 공격 실행기")]
    [SerializeField] private GrimReaperBasicAttackController basicAttackController;


    [Header("전투 상태")]

    [Tooltip("전투 활성 여부")]
    [SerializeField] private bool isBattleActive = true;

    [Tooltip("디버그 로그 출력 여부")]
    [SerializeField] private bool debugLog = false;


    private float thinkTimer = 0f;
    private float battleStartTimer = 0f;

    private bool hasAppliedConfig = false;


    private void Reset()
    {
        targetProvider = GetComponent<BossTargetProvider>();
        chaseMovementController = GetComponent<BossChaseMovementController>();
        basicAttackController = GetComponent<GrimReaperBasicAttackController>();
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
        InitializeBattleStartDelay();

        if (isBattleActive)
        {
            SetChaseEnabled(CanChaseOnEnable());
        }
    }

    private void OnDisable()
    {
        ForceStopAllActions();
    }

    private void Update()
    {
        if (!CanRunCombat())
        {
            return;
        }

        UpdateBattleStartDelay();

        if (IsWaitingBattleStart())
        {
            return;
        }

        if (IsAnyActionRunning())
        {
            return;
        }

        UpdateThinkTimer();
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
            basicAttackController = GetComponent<GrimReaperBasicAttackController>();
        }
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
                Debug.LogWarning("[GrimReaperCombatController] BasicAttackConfig가 비어 있습니다.", this);
            }
        }
        else if (basicAttackController != null)
        {
            basicAttackController.SetExternalConfig(combatConfig.BasicAttackConfig);
        }

        hasAppliedConfig = true;

        if (debugLog)
        {
            Debug.Log("[GrimReaperCombatController] CombatConfig 설정 주입 완료", this);
        }
    }

    private void InitializeBattleStartDelay()
    {
        if (combatConfig == null)
        {
            battleStartTimer = 0f;
            return;
        }

        battleStartTimer = Mathf.Max(0f, combatConfig.BattleStartDelay);
    }

    private bool CanChaseOnEnable()
    {
        if (combatConfig == null)
        {
            return true;
        }

        if (battleStartTimer <= 0f)
        {
            return true;
        }

        return combatConfig.ChaseDuringStartDelay;
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
                Debug.LogWarning("[GrimReaperCombatController] CombatConfig가 비어 있습니다.", this);
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

    private void UpdateBattleStartDelay()
    {
        if (battleStartTimer <= 0f)
        {
            return;
        }

        battleStartTimer -= Time.deltaTime;

        if (battleStartTimer <= 0f)
        {
            battleStartTimer = 0f;
            SetChaseEnabled(true);

            if (debugLog)
            {
                Debug.Log("[GrimReaperCombatController] 전투 시작 대기 종료", this);
            }
        }
    }

    private bool IsWaitingBattleStart()
    {
        return battleStartTimer > 0f;
    }

    private void UpdateThinkTimer()
    {
        thinkTimer -= Time.deltaTime;
        if (thinkTimer > 0f)
        {
            return;
        }

        thinkTimer = Mathf.Max(0.02f, combatConfig.ThinkInterval);

        Transform target = targetProvider.GetTarget();
        if (target == null)
        {
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        TryExecuteCombatDecision(target, distanceToTarget);
    }

    private void TryExecuteCombatDecision(Transform target, float distanceToTarget)
    {
        if (target == null)
        {
            return;
        }

        TryUseBasicAttack(target, distanceToTarget);
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
            Debug.Log("[GrimReaperCombatController] 기본 공격 선택", this);
        }

        return started;
    }

    private bool IsAnyActionRunning()
    {
        if (basicAttackController != null && basicAttackController.IsRunningAttack())
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
            ForceStopAllActions();
            SetChaseEnabled(false);
            return;
        }

        thinkTimer = 0f;
        InitializeBattleStartDelay();
        ApplyCombatConfigToControllers();
        SetChaseEnabled(CanChaseOnEnable());
    }

    public void ForceStopAllActions()
    {
        if (basicAttackController != null)
        {
            basicAttackController.ForceStopAttack();
        }

        SetChaseEnabled(true);
    }
}