using UnityEngine;
using _Game.Player;

/// <summary>
/// 캐릭터 기본 능력치 SO + PlayerSkillLoadout 패시브 스냅샷을 합쳐서
/// 실제 런타임(PlayerCombatStats2D / PlayerHealth)에 반영합니다.
///
/// 공식:
///   최종 배율 = (1 + 캐릭터%/100) × (1 + 패시브%/100)
///   최종 방어 = 100 / (100 + 총방어력)             ← LoL 유효체력 공식
///   스킬 가속 = 100 / (100 + 총가속)               ← LoL 스킬 가속 공식 (상한 60% 감소)
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
    [Tooltip("★ false 권장! true면 어떤 패시브를 배워도 풀피 회복되는 버그 발생.\n최대 HP 패시브를 배울 때는 증가분만큼만 회복됩니다.")]
    [SerializeField] private bool healToFullWhenMaxHpChanges = false;

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

        // ── 공격력 / 이동속도 / 픽업 / 범위 / 경험치: 곱연산 ──
        float attackMul  = ToMul(baseSnapshot.AttackPowerPercent) * ToMul(passiveSnapshot.AttackPowerPercent);
        float moveMul    = ToMul(baseSnapshot.MoveSpeedPercent) * ToMul(passiveSnapshot.MoveSpeedPercent);
        float pickupMul  = ToMul(baseSnapshot.PickupRangePercent) * ToMul(passiveSnapshot.PickupRangePercent);
        float areaMul    = ToMul(baseSnapshot.SkillAreaPercent) * ToMul(passiveSnapshot.SkillAreaPercent);
        float expMul     = ToMul(baseSnapshot.ExpGainPercent) * ToMul(passiveSnapshot.ExpGainPercent);

        // [핵심 수정] 스킬 가속: LoL 공식 — 100/(100+가속)
        // 기존 코드는 ToMul()로 곱연산하여 쿨다운이 '증가'하는 버그가 있었음.
        // 예: 가속 10이면 기존=1.1배(쿨다운 10% 증가), 수정=0.909배(쿨다운 9.1% 감소)
        float totalHaste = Mathf.Max(0f, baseSnapshot.SkillHastePercent + passiveSnapshot.SkillHastePercent);
        float cooldownMul = 100f / (100f + totalHaste);
        // 상한: 최대 60% 쿨다운 감소 (설계 문서 기준)
        cooldownMul = Mathf.Clamp(cooldownMul, 0.4f, 1f);

        // LoL 유효체력 공식: 받는피해 = 초기피해 × 100/(100+방어력)
        float totalDefense = Mathf.Max(0f, baseSnapshot.DefensePercent + passiveSnapshot.DefensePercent);
        float incomingDamageMul = Mathf.Clamp(100f / (100f + totalDefense), 0.1f, 1f);

        combatStats.SetDamageMul(attackMul);
        combatStats.SetMoveSpeedMul(moveMul);
        combatStats.SetPickupRangeMul(pickupMul);
        combatStats.SetCooldownMul(cooldownMul);
        combatStats.SetAreaMul(areaMul);
        combatStats.SetExpGainMul(expMul);
        combatStats.SetIncomingDamageMul(incomingDamageMul);

        // [핵심 수정] 체력 회복 버그 수정
        // 기존: healToFullWhenMaxHpChanges = true → 어떤 패시브든 배우면 풀피 회복
        // 수정: MaxHp가 실제로 '증가'한 경우에만 증가분만큼 현재 HP 회복
        if (playerHealth != null)
        {
            int maxHpBonus = Mathf.Max(0, baseSnapshot.MaxHpFlat + passiveSnapshot.MaxHpFlat);
            int oldMaxHp = playerHealth.MaxHp;

            // healToFull은 항상 false로 호출 (풀피 회복 방지)
            playerHealth.SetMaxHpBonus(maxHpBonus, healToFull: false);

            int newMaxHp = playerHealth.MaxHp;

            // MaxHp가 실제로 증가한 경우에만, 증가분만큼 현재 HP도 회복
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
}