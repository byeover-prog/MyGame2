using UnityEngine;
using _Game.Player;

/// <summary>
/// 캐릭터 기본 능력치 SO + 메타 보정 + PlayerSkillLoadout 패시브 스냅샷을 합쳐서
/// 실제 런타임(PlayerCombatStats2D / PlayerHealth)에 반영합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerStatRuntimeApplier2D : MonoBehaviour
{
    [Header("=== 기본 데이터 ===")]
    [Tooltip("캐릭터 기본 능력치 SO. 메타 런타임에 선택된 캐릭터 프로필이 있으면 그 값을 우선 사용합니다.")]
    [SerializeField] private PlayerBaseStatProfileSO baseStatProfile;

    [Header("=== 참조 ===")]
    [SerializeField, Tooltip("새 스킬/패시브 런타임 상태를 보관하는 로드아웃")]
    private PlayerSkillLoadout loadout;

    [SerializeField, Tooltip("전투/이동/획득 배율이 저장될 런타임 스탯 컴포넌트")]
    private PlayerCombatStats2D combatStats;

    [SerializeField, Tooltip("최대 HP 보너스를 반영할 플레이어 체력 컴포넌트")]
    private PlayerHealth playerHealth;

    [Header("=== 동작 옵션 ===")]
    [Tooltip("이 값은 사용하지 않습니다. 최대 HP 증가분만큼만 회복하도록 고정합니다.")]
#pragma warning disable 0414
    [SerializeField] private bool healToFullWhenMaxHpChanges = false;
#pragma warning restore 0414

    [Tooltip("적용 로그를 보고 싶을 때만 켜세요")]
    [SerializeField] private bool debugLog = false;

    [ContextMenu("지금 스탯 다시 적용")]
    public void ReapplyFromLoadout()
    {
        PlayerBaseStatProfileSO resolvedBaseProfile = ResolveBaseProfile();
        PlayerStatSnapshot baseSnapshot = resolvedBaseProfile != null ? resolvedBaseProfile.BuildSnapshot() : default;
        PlayerStatSnapshot metaSnapshot = MetaBattleSnapshotRuntime2D.HasMainCharacter
            ? MetaBattleSnapshotRuntime2D.Current.mainBonus.coreStats
            : default;
        PlayerStatSnapshot passiveSnapshot = loadout != null ? loadout.BuildStatSnapshot() : default;

        baseSnapshot = Merge(baseSnapshot, metaSnapshot);

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

    private PlayerBaseStatProfileSO ResolveBaseProfile()
    {
        if (MetaBattleSnapshotRuntime2D.HasMainCharacter && MetaBattleSnapshotRuntime2D.Current.mainDefinition != null)
        {
            PlayerBaseStatProfileSO runtimeProfile = MetaBattleSnapshotRuntime2D.Current.mainDefinition.BaseStatProfile;
            if (runtimeProfile != null)
                return runtimeProfile;
        }

        return baseStatProfile;
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

        float attackMul = ToMul(baseSnapshot.AttackPowerPercent) * ToMul(passiveSnapshot.AttackPowerPercent);
        float moveMul = ToMul(baseSnapshot.MoveSpeedPercent) * ToMul(passiveSnapshot.MoveSpeedPercent);
        float pickupMul = ToMul(baseSnapshot.PickupRangePercent) * ToMul(passiveSnapshot.PickupRangePercent);
        float areaMul = ToMul(baseSnapshot.SkillAreaPercent) * ToMul(passiveSnapshot.SkillAreaPercent);
        float expMul = ToMul(baseSnapshot.ExpGainPercent) * ToMul(passiveSnapshot.ExpGainPercent);

        float totalHaste = Mathf.Max(0f, baseSnapshot.SkillHastePercent + passiveSnapshot.SkillHastePercent);
        float cooldownMul = 100f / (100f + totalHaste);
        cooldownMul = Mathf.Clamp(cooldownMul, 0.4f, 1f);

        float totalDefense = Mathf.Max(0f, baseSnapshot.DefensePercent + passiveSnapshot.DefensePercent);
        float incomingDamageMul = Mathf.Clamp(100f / (100f + totalDefense), 0.1f, 1f);

        combatStats.SetDamageMul(attackMul);
        combatStats.SetMoveSpeedMul(moveMul);
        combatStats.SetPickupRangeMul(pickupMul);
        combatStats.SetCooldownMul(cooldownMul);
        combatStats.SetAreaMul(areaMul);
        combatStats.SetExpGainMul(expMul);
        combatStats.SetIncomingDamageMul(incomingDamageMul);

        if (playerHealth != null)
        {
            int maxHpBonus = Mathf.Max(0, baseSnapshot.MaxHpFlat + passiveSnapshot.MaxHpFlat);
            int oldMaxHp = playerHealth.MaxHp;

            playerHealth.SetMaxHpBonus(maxHpBonus, healToFull: false);

            int newMaxHp = playerHealth.MaxHp;
            if (newMaxHp > oldMaxHp)
            {
                int hpGain = newMaxHp - oldMaxHp;
                playerHealth.Heal(hpGain);

                if (debugLog)
                    Debug.Log($"[StatApplier] MaxHP 증가: {oldMaxHp} → {newMaxHp} | 회복량: +{hpGain}", this);
            }
        }

        if (debugLog)
        {
            Debug.Log(
                $"[StatApplier] ATKx{attackMul:0.00} | MOVEx{moveMul:0.00} | PICKUPx{pickupMul:0.00} | " +
                $"DEF%={totalDefense:0.##} | HP+{baseSnapshot.MaxHpFlat + passiveSnapshot.MaxHpFlat} | " +
                $"HASTEx{cooldownMul:0.00}(가속합={totalHaste:0.#}) | AREAx{areaMul:0.00} | EXPx{expMul:0.00}",
                this);
        }
    }

    private static float ToMul(float percent)
    {
        return 1f + (percent / 100f);
    }

    private static PlayerStatSnapshot Merge(PlayerStatSnapshot a, PlayerStatSnapshot b)
    {
        a.AttackPowerPercent += b.AttackPowerPercent;
        a.PickupRangePercent += b.PickupRangePercent;
        a.MoveSpeedPercent += b.MoveSpeedPercent;
        a.DefensePercent += b.DefensePercent;
        a.MaxHpFlat += b.MaxHpFlat;
        a.SkillHastePercent += b.SkillHastePercent;
        a.SkillAreaPercent += b.SkillAreaPercent;
        a.ExpGainPercent += b.ExpGainPercent;
        return a;
    }
}
