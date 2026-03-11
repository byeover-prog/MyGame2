using UnityEngine;
using _Game.Player;

/// <summary>
/// 캐릭터 기본 능력치 SO + PlayerSkillLoadout 패시브 스냅샷을 합쳐서
/// 실제 런타임(PlayerCombatStats2D / PlayerHealth)에 반영합니다.
///
/// 공식:
///   최종 배율 = (1 + 캐릭터%/100) × (1 + 패시브%/100)
///   최종 방어 = clamp(1 - 총방어%/100, 0.1, 1.0)
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerStatRuntimeApplier2D : MonoBehaviour
{
    [Header("=== 기본 데이터 ===")]
    [Tooltip("캐릭터 기본 능력치 SO. 비우면 0 보정으로 처리합니다.")]
    [SerializeField] private PlayerBaseStatProfileSO baseStatProfile;

    [Header("=== 참조 ===")]
    [SerializeField, Tooltip("새 스킬/패시브 런타임 상태를 보관하는 로드아웃")]
    private PlayerSkillLoadout loadout;

    [SerializeField, Tooltip("전투/이동/획득 배율이 저장될 런타임 스탯 컴포넌트")]
    private PlayerCombatStats2D combatStats;

    [SerializeField, Tooltip("최대 HP 보너스를 반영할 플레이어 체력 컴포넌트")]
    private PlayerHealth playerHealth;

    [Header("=== 동작 옵션 ===")]
    [Tooltip("최대 HP가 바뀔 때 현재 HP도 최대치로 맞출지")]
    [SerializeField] private bool healToFullWhenMaxHpChanges = true;

    [Tooltip("적용 로그를 보고 싶을 때만 켜세요")]
    [SerializeField] private bool debugLog = false;

    [ContextMenu("지금 스탯 다시 적용")]
    public void ReapplyFromLoadout()
    {
        PlayerStatSnapshot baseSnapshot = baseStatProfile != null ? baseStatProfile.BuildSnapshot() : default;
        PlayerStatSnapshot passiveSnapshot = loadout != null ? loadout.BuildStatSnapshot() : default;

        EnsureTargets();
        ApplyRuntime(baseSnapshot, passiveSnapshot);
    }

    private void Awake()
    {
        EnsureTargets();
    }

    private void Start()
    {
        ReapplyFromLoadout();
    }

    private void EnsureTargets()
    {
        if (loadout == null) loadout = GetComponent<PlayerSkillLoadout>();
        if (loadout == null) loadout = GetComponentInParent<PlayerSkillLoadout>();
        if (loadout == null) loadout = FindFirstObjectByType<PlayerSkillLoadout>();

        if (combatStats == null) combatStats = GetComponent<PlayerCombatStats2D>();
        if (combatStats == null) combatStats = GetComponentInParent<PlayerCombatStats2D>();
        if (combatStats == null) combatStats = gameObject.AddComponent<PlayerCombatStats2D>();

        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null) playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void ApplyRuntime(PlayerStatSnapshot baseSnapshot, PlayerStatSnapshot passiveSnapshot)
    {
        if (combatStats == null) return;

        float attackMul  = ToMul(baseSnapshot.AttackPowerPercent) * ToMul(passiveSnapshot.AttackPowerPercent);
        float moveMul    = ToMul(baseSnapshot.MoveSpeedPercent) * ToMul(passiveSnapshot.MoveSpeedPercent);
        float pickupMul  = ToMul(baseSnapshot.PickupRangePercent) * ToMul(passiveSnapshot.PickupRangePercent);
        float elementMul = ToMul(baseSnapshot.ElementDamagePercent) * ToMul(passiveSnapshot.ElementDamagePercent);
        float goldMul    = ToMul(baseSnapshot.GoldGainPercent) * ToMul(passiveSnapshot.GoldGainPercent);
        float expMul     = ToMul(baseSnapshot.ExpGainPercent) * ToMul(passiveSnapshot.ExpGainPercent);

        float totalDefensePercent = Mathf.Max(0f, baseSnapshot.DefensePercent + passiveSnapshot.DefensePercent);
        float incomingDamageMul = Mathf.Clamp(1f - (totalDefensePercent / 100f), 0.1f, 1f);

        combatStats.SetDamageMul(attackMul);
        combatStats.SetMoveSpeedMul(moveMul);
        combatStats.SetPickupRangeMul(pickupMul);
        combatStats.SetElementDamageMul(elementMul);
        combatStats.SetGoldGainMul(goldMul);
        combatStats.SetExpGainMul(expMul);
        combatStats.SetIncomingDamageMul(incomingDamageMul);

        // 쿨타임/범위는 현재 패시브에 없으므로 기본값 유지
        combatStats.SetCooldownMul(1f);
        combatStats.SetAreaMul(1f);

        if (playerHealth != null)
        {
            int maxHpBonus = Mathf.Max(0, baseSnapshot.MaxHpFlat + passiveSnapshot.MaxHpFlat);
            playerHealth.SetMaxHpBonus(maxHpBonus, healToFullWhenMaxHpChanges);
        }

        if (debugLog)
        {
            Debug.Log(
                $"[StatApplier] ATKx{attackMul:0.00} | MOVEx{moveMul:0.00} | PICKUPx{pickupMul:0.00} | " +
                $"DEF%={totalDefensePercent:0.##} | HP+{baseSnapshot.MaxHpFlat + passiveSnapshot.MaxHpFlat} | " +
                $"ELEMx{elementMul:0.00} | GOLDx{goldMul:0.00} | EXPx{expMul:0.00}",
                this);
        }
    }

    private static float ToMul(float percent)
    {
        return 1f + (percent / 100f);
    }
}