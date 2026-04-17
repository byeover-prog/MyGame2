using UnityEngine;

/// <summary>
/// MonsterDefinitionSO의 값을 실제 몬스터 프리팹 내부 컴포넌트에 주입하는 연결층입니다.
///
/// 스포너는 생성만 담당하고,
/// 실제 체력 / 이동 / 공격 관련 런타임 값 적용은 이 컴포넌트가 담당합니다.
///
/// 현재 역할:
/// - 공통 수치(HP, 접촉 데미지)를 공통 컴포넌트에 적용
/// - behaviorType 기준으로 Chase / Ranged 행동 컴포넌트를 분기 적용
/// - 마지막으로 적용한 MonsterDefinitionSO를 보관해 사망 처리 등에서 재사용
///
/// 이번 단계의 변경점:
/// - 원거리 행동 구현체를 직접 EnemyRangedAttacker2D로 고정하지 않고,
///   EnemyRangedBehaviorBase2D 계약 기준으로 찾을 수 있게 확장합니다.
/// - 다만 기존 프리팹 연결을 최대한 안 깨기 위해
///   직렬화 필드는 유지하면서 내부에서는 base 계약 기준으로 사용합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterRuntimeApplier2D : MonoBehaviour
{
    [Header("1. 적용 대상 연결")]
    [SerializeField, Tooltip("체력 값을 적용할 대상입니다.\n"
                             + "보통 같은 프리팹 루트의 EnemyHealth2D를 연결합니다.")]
    private EnemyHealth2D health;

    [SerializeField, Tooltip("추적형 이동 값을 적용할 대상입니다.\n"
                             + "Chase 행동일 때 사용합니다.")]
    private EnemyChaser2D chaser;

    [SerializeField, Tooltip("접촉 데미지 값을 적용할 대상입니다.\n"
                             + "MonsterDefinitionSO의 contactDamage를 주입합니다.")]
    private EnemyContactDamage2D contactDamage;

    [SerializeField, Tooltip("원거리 행동 컴포넌트 연결용 필드입니다.\n"
                             + "앞으로는 EnemyRangedBehaviorBase2D 계약 기준으로 사용합니다.")]
    private EnemyRangedBehaviorBase2D rangedBehavior;

    [Header("2. 수동 검증용 설정")]
    [SerializeField, Tooltip("스포너 연결 전 프리팹 단독 테스트용 MonsterDefinitionSO입니다.\n"
                             + "실제 스포너 검증 단계에서는 비워두는 것을 권장합니다.")]
    private MonsterDefinitionSO testDefinition;

    [SerializeField, Tooltip("플레이 모드 활성화 시 testDefinition을 자동 적용할지 여부입니다.\n"
                             + "스포너 기반 테스트에서는 꺼두는 것이 안전합니다.")]
    private bool applyTestDefinitionOnEnable = false;

    [Header("3. 디버그 확인")]
    [SerializeField, Tooltip("마지막으로 적용한 MonsterDefinitionSO를 확인하기 위한 디버그용 필드입니다.")]
    private MonsterDefinitionSO appliedDefinition;

    [SerializeField, Tooltip("적용 로그를 출력할지 여부입니다.")]
    private bool debugLog = false;

    private EnemyRangedBehaviorBase2D cachedRangedBehavior;

    /// <summary>마지막으로 적용된 정의 데이터</summary>
    public MonsterDefinitionSO AppliedDefinition => appliedDefinition;

    private void Reset()
    {
        health = GetComponent<EnemyHealth2D>();
        chaser = GetComponent<EnemyChaser2D>();
        contactDamage = GetComponent<EnemyContactDamage2D>();
        rangedBehavior = GetComponent<EnemyRangedBehaviorBase2D>();
        cachedRangedBehavior = rangedBehavior;
    }

    private void Awake()
    {
        CacheRangedBehaviorIfNeeded();
    }

    private void OnEnable()
    {
        if (applyTestDefinitionOnEnable && testDefinition != null)
            ApplyDefinition(testDefinition);
    }

    /// <summary>
    /// MonsterDefinitionSO를 받아 프리팹 내부 컴포넌트에 런타임 값을 적용합니다.
    /// </summary>
    public void ApplyDefinition(MonsterDefinitionSO definition)
    {
        if (definition == null)
        {
            Debug.LogWarning("[MonsterRuntimeApplier2D] 적용할 MonsterDefinitionSO가 비어 있습니다.", this);
            return;
        }

        appliedDefinition = definition;

        ApplyHealth(definition);
        ApplyContactDamage(definition);
        ApplyBehavior(definition);

        if (debugLog)
        {
            Debug.Log(
                $"[MonsterRuntimeApplier2D] 적용 완료 | ID: {definition.MonsterId} | Behavior: {definition.BehaviorType}",
                this);
        }
    }

    /// <summary>
    /// SO의 maxHp를 EnemyHealth2D에 적용합니다.
    /// float -> int 변환은 이 지점에서 통일합니다.
    /// </summary>
    private void ApplyHealth(MonsterDefinitionSO definition)
    {
        if (health == null)
            return;

        int runtimeHp = Mathf.Max(1, Mathf.RoundToInt(definition.MaxHp));
        health.SetMaxAndFill(runtimeHp);
    }

    /// <summary>
    /// SO의 contactDamage를 EnemyContactDamage2D에 적용합니다.
    /// </summary>
    private void ApplyContactDamage(MonsterDefinitionSO definition)
    {
        if (contactDamage == null)
            return;

        contactDamage.SetDamage(definition.ContactDamage);
    }

    /// <summary>
    /// behaviorType 기준으로 행동 컴포넌트를 분기 적용합니다.
    /// </summary>
    private void ApplyBehavior(MonsterDefinitionSO definition)
    {
        switch (definition.BehaviorType)
        {
            case MonsterBehaviorType.Chase:
                ApplyChaseBehavior(definition);
                break;

            case MonsterBehaviorType.Ranged:
                ApplyRangedBehavior(definition);
                break;

            default:
                Debug.LogWarning(
                    $"[MonsterRuntimeApplier2D] 지원하지 않는 BehaviorType입니다: {definition.BehaviorType}",
                    this);
                break;
        }
    }

    /// <summary>
    /// 추적형 몬스터용 이동 설정을 적용합니다.
    /// 원거리 행동 컴포넌트는 충돌 방지를 위해 비활성화합니다.
    /// </summary>
    private void ApplyChaseBehavior(MonsterDefinitionSO definition)
    {
        EnemyRangedBehaviorBase2D resolvedRangedBehavior = ResolveRangedBehavior();
        if (resolvedRangedBehavior != null)
            resolvedRangedBehavior.enabled = false;

        if (chaser == null)
        {
            Debug.LogWarning("[MonsterRuntimeApplier2D] Chase 타입인데 EnemyChaser2D가 없습니다.", this);
            return;
        }

        chaser.ConfigureRuntime(definition.MoveSpeed, definition.DetectRange);
        chaser.enabled = true;
    }

    /// <summary>
    /// 원거리형 몬스터용 거리/공격 설정을 적용합니다.
    /// 추적형 컴포넌트는 충돌 방지를 위해 비활성화합니다.
    /// </summary>
    private void ApplyRangedBehavior(MonsterDefinitionSO definition)
    {
        if (chaser != null)
            chaser.enabled = false;

        EnemyRangedBehaviorBase2D resolvedRangedBehavior = ResolveRangedBehavior();
        if (resolvedRangedBehavior == null)
        {
            Debug.LogWarning("[MonsterRuntimeApplier2D] Ranged 타입인데 EnemyRangedBehaviorBase2D가 없습니다.", this);
            return;
        }

        resolvedRangedBehavior.ConfigureRuntime(
            definition.MoveSpeed,
            definition.DetectRange,
            definition.AttackRange,
            definition.RetreatRange,
            definition.AttackCooldown,
            definition.ChargeDuration,
            definition.ProjectileSpeed,
            definition.ProjectileCount,
            definition.SpreadAngle,
            definition.AttackDamage);

        resolvedRangedBehavior.enabled = true;
    }

    private void CacheRangedBehaviorIfNeeded()
    {
        if (cachedRangedBehavior != null)
            return;

        if (rangedBehavior != null)
        {
            cachedRangedBehavior = rangedBehavior;
            return;
        }

        cachedRangedBehavior = GetComponent<EnemyRangedBehaviorBase2D>();
    }

    private EnemyRangedBehaviorBase2D ResolveRangedBehavior()
    {
        CacheRangedBehaviorIfNeeded();
        return cachedRangedBehavior;
    }
}