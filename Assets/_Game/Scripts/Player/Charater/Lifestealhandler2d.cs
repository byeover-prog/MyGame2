using UnityEngine;

/// <summary>
/// 피해 적용 시 PlayerCombatStats2D.LifestealPercent만큼 체력을 회복합니다.
/// DamageEvents2D.OnEnemyDamageApplied 이벤트를 구독하여 동작합니다.
///
/// [동작 원리]
/// 1. 무기/스킬이 적에게 피해를 입힘
/// 2. DamageUtil2D → DamageEvents2D.RaiseEnemyDamageApplied() 발생
/// 3. 이 컴포넌트가 이벤트 수신 → LifestealPercent 확인 → 회복량 계산 → PlayerHealth.Heal()
///
/// [흡혈 공식]
/// 회복량 = 피해량 × (LifestealPercent / 100)
/// 최소 1 (퍼센트가 0보다 크고 피해가 있으면 최소 1 회복)
///
/// [Hierarchy / Inspector 설정]
/// - Player 오브젝트에 컴포넌트 부착
/// - Combat Stats, Player Health: 비워두면 자동 탐색
/// </summary>
[DisallowMultipleComponent]
public sealed class LifestealHandler2D : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("전투 스탯 (LifestealPercent 읽기). 비워두면 자동 탐색합니다.")]
    [SerializeField] private PlayerCombatStats2D combatStats;

    [Tooltip("플레이어 체력 (회복 적용). 비워두면 자동 탐색합니다.")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private void Awake()
    {
        if (combatStats == null) combatStats = GetComponent<PlayerCombatStats2D>();
        if (combatStats == null) combatStats = GetComponentInParent<PlayerCombatStats2D>();
        if (combatStats == null) combatStats = FindFirstObjectByType<PlayerCombatStats2D>();

        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null) playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void OnEnable()
    {
        DamageEvents2D.OnEnemyDamageApplied += HandleEnemyDamageApplied;
    }

    private void OnDisable()
    {
        DamageEvents2D.OnEnemyDamageApplied -= HandleEnemyDamageApplied;
    }

    private void HandleEnemyDamageApplied(DamageEvents2D.EnemyDamageAppliedInfo info)
    {
        if (combatStats == null || playerHealth == null) return;

        float lifestealPercent = combatStats.LifestealPercent;
        if (lifestealPercent <= 0f) return;
        if (info.Amount <= 0) return;

        // 회복량 계산
        float healRaw = info.Amount * (lifestealPercent / 100f);
        int healAmount = Mathf.Max(1, Mathf.RoundToInt(healRaw));

        // 이미 풀피면 스킵
        if (playerHealth.CurrentHp >= playerHealth.MaxHp) return;

        playerHealth.Heal(healAmount);

        if (debugLog)
            GameLogger.Log($"[흡혈] 피해={info.Amount} × {lifestealPercent}% → 회복 +{healAmount} " +
                      $"(현재 HP={playerHealth.CurrentHp}/{playerHealth.MaxHp})");
    }
}