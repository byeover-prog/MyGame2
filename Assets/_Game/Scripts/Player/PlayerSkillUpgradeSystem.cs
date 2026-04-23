using System;
using System.Collections.Generic;
using UnityEngine;
using _Game.Skills;

// 무기 업그레이드 계산 + 에디터 툴용 SO 참조 컨테이너.
// 레벨업 카드 UI 흐름은 LevelUpOpenOnPlayerExp → LevelUpFlowCoordinator가 담당.

[DisallowMultipleComponent]
public sealed class PlayerSkillUpgradeSystem : MonoBehaviour
{
    [Header("공통 스킬(카탈로그)")]
    [SerializeField] private CommonSkillCatalogSO commonSkillCatalog;
    [SerializeField] private CommonSkillManager2D commonSkillManager;

    [Header("패시브(카탈로그)")]
    [SerializeField] private PassiveCatalogSO passiveCatalog;
    [SerializeField] private PassiveManager2D passiveManager;

    [Header("레벨업 소스")]
    [SerializeField] private PlayerExp playerExp;

    [Header("무기 덱(카드 풀) — 에디터 툴이 참조")]
    [SerializeField] private WeaponSkillDeckSO deck;

    [Header("적용 대상(현재 사용 중인 무기 슬롯 발사 시스템)")]
    [SerializeField] private WeaponShooterSystem2D shooter;

    [Header("무기 정의 DB — 에디터 툴이 참조")]
    [SerializeField] private WeaponDatabaseSO weaponDatabase;

    [Header("업그레이드 밸런스 테이블")]
    [SerializeField] private UpgradeBalanceTableSO upgradeBalanceTable;

    [Header("시작 무기 레벨 처리")]
    [SerializeField] private bool seedEquippedWeaponsAsLevel1 = true;

    [Header("임시 밸런스(테이블 미지정 시 사용)")]
    [Min(1)]
    [SerializeField] private int defaultMaxLevel = 8;
    [SerializeField] private int damageAddPerLevel = 1;
    [Range(0.5f, 1.2f)]
    [SerializeField] private float cooldownMulPerLevel = 0.95f;
    [SerializeField] private float rangeAddPerLevel = 0f;

    private readonly Dictionary<string, int> _levels = new Dictionary<string, int>(16);

    private void Awake()
    {
        if (playerExp == null) playerExp = FindFirstObjectByType<PlayerExp>();
        if (shooter == null) shooter = FindFirstObjectByType<WeaponShooterSystem2D>();
        if (weaponDatabase == null) weaponDatabase = FindFirstObjectByType<WeaponDatabaseSO>();
        if (commonSkillManager == null) commonSkillManager = FindFirstObjectByType<CommonSkillManager2D>();
        if (passiveManager == null) passiveManager = FindFirstObjectByType<PassiveManager2D>();

        if (seedEquippedWeaponsAsLevel1)
            SeedEquippedWeapons();
    }
    
    // 무기 업그레이드 적용

    public void ApplyUpgrade(WeaponDefinitionSO weapon)
    {
        if (weapon == null) return;
        if (string.IsNullOrWhiteSpace(weapon.weaponId)) return;

        int cur = GetLevel(weapon.weaponId);
        int max = GetMaxLevel(weapon);
        int next = Mathf.Clamp(cur + 1, 1, max);
        _levels[weapon.weaponId] = next;

        ApplyToShooterSlots(weapon, next);
    }

    private void ApplyToShooterSlots(WeaponDefinitionSO weaponDef, int nextLevel)
    {
        if (shooter == null || weaponDef == null) return;
        if (string.IsNullOrWhiteSpace(weaponDef.weaponId)) return;

        int desiredBonusDamage = CalculateDesiredBonusDamage(weaponDef, nextLevel);
        float desiredCooldownMul = CalculateDesiredCooldownMul(weaponDef, nextLevel);
        float desiredRangeAdd = CalculateDesiredRangeAdd(weaponDef, nextLevel);

        if (!shooter.TryGetSlotByWeaponId(weaponDef.weaponId, out int slotIndex))
        {
            shooter.AddSlot(
                weapon: weaponDef,
                enabled: true,
                bonusDamage: desiredBonusDamage,
                cooldownMul: desiredCooldownMul,
                rangeAdd: desiredRangeAdd
            );
            return;
        }

        if (!shooter.TryGetSlotStat(slotIndex, out int curBonusDamage, out float curCooldownMul, out float curRangeAdd, out bool curEnabled))
            return;

        int deltaDamage = desiredBonusDamage - curBonusDamage;
        if (deltaDamage != 0)
            shooter.ApplyUpgradeBySlotIndex(slotIndex, WeaponUpgradeType.DamageAdd, deltaDamage, 0f, false);

        if (curCooldownMul <= 0.0001f) curCooldownMul = 1f;
        float factor = desiredCooldownMul / curCooldownMul;
        if (Mathf.Abs(factor - 1f) > 0.0001f)
            shooter.ApplyUpgradeBySlotIndex(slotIndex, WeaponUpgradeType.CooldownMul, 0, factor, false);

        float deltaRange = desiredRangeAdd - curRangeAdd;
        if (Mathf.Abs(deltaRange) > 0.0001f)
            shooter.ApplyUpgradeBySlotIndex(slotIndex, WeaponUpgradeType.RangeAdd, 0, deltaRange, false);

        if (!curEnabled)
            shooter.ApplyUpgradeBySlotIndex(slotIndex, WeaponUpgradeType.ToggleEnabled, 0, 0f, true);
    }
    
    // 계산

    private int CalculateDesiredBonusDamage(WeaponDefinitionSO weapon, int nextLevel)
    {
        if (nextLevel <= 1) return 0;

        float scale = weapon != null ? Mathf.Max(0f, weapon.damageStepScale) : 1f;
        int sum = 0;

        if (upgradeBalanceTable != null)
        {
            for (int lv = 2; lv <= nextLevel; lv++)
            {
                int step = Mathf.Max(0, upgradeBalanceTable.GetDamageAdd(lv));
                sum += Mathf.Max(0, Mathf.RoundToInt(step * scale));
            }
        }
        else
        {
            sum = Mathf.Max(0, (nextLevel - 1) * Mathf.Max(0, Mathf.RoundToInt(damageAddPerLevel * scale)));
        }

        return sum;
    }

    private float CalculateDesiredCooldownMul(WeaponDefinitionSO weapon, int nextLevel)
    {
        if (nextLevel <= 1) return 1f;

        float expScale = weapon != null ? Mathf.Max(0f, weapon.cooldownStepScale) : 1f;
        float mul = 1f;

        if (upgradeBalanceTable != null)
        {
            for (int lv = 2; lv <= nextLevel; lv++)
            {
                float step = upgradeBalanceTable.GetCooldownMul(lv);
                if (step <= 0.0001f) step = 1f;
                float scaledStep = PowFloatSafe(step, expScale);
                mul *= scaledStep;
            }
        }
        else
        {
            float scaledStep = PowFloatSafe(cooldownMulPerLevel, expScale);
            mul = PowSafe(scaledStep, nextLevel - 1);
        }

        if (mul <= 0.0001f) mul = 1f;
        return Mathf.Clamp(mul, 0.05f, 10f);
    }

    private float CalculateDesiredRangeAdd(WeaponDefinitionSO weapon, int nextLevel)
    {
        if (nextLevel <= 1) return 0f;

        float scale = weapon != null ? Mathf.Max(0f, weapon.rangeStepScale) : 1f;
        float sum = 0f;

        if (upgradeBalanceTable != null)
        {
            for (int lv = 2; lv <= nextLevel; lv++)
                sum += upgradeBalanceTable.GetRangeAdd(lv) * scale;
        }
        else
        {
            sum = (nextLevel - 1) * (rangeAddPerLevel * scale);
        }

        return sum;
    }
    
    // 유틸

    private void SeedEquippedWeapons()
    {
        if (shooter == null) return;

        WeaponSaveData data = shooter.BuildSaveData();
        if (data == null || data.slots == null) return;

        for (int i = 0; i < data.slots.Count; i++)
        {
            var s = data.slots[i];
            if (s == null) continue;
            if (string.IsNullOrWhiteSpace(s.weaponId)) continue;

            if (!_levels.ContainsKey(s.weaponId))
                _levels.Add(s.weaponId, 1);
        }
    }

    private int GetLevel(string weaponId)
    {
        if (string.IsNullOrWhiteSpace(weaponId)) return 0;
        return _levels.TryGetValue(weaponId, out int lv) ? lv : 0;
    }

    private int GetMaxLevel(WeaponDefinitionSO w)
    {
        return Mathf.Max(1, defaultMaxLevel);
    }

    private static float PowSafe(float baseValue, int exp)
    {
        if (exp <= 0) return 1f;
        float v = 1f;
        for (int i = 0; i < exp; i++) v *= baseValue;
        return Mathf.Clamp(v, 0.05f, 10f);
    }

    private static float PowFloatSafe(float baseValue, float exp)
    {
        if (baseValue <= 0.0001f) return 1f;
        if (exp <= 0.0001f) return 1f;

        double r = Math.Pow(baseValue, exp);
        if (double.IsNaN(r) || double.IsInfinity(r)) return 1f;

        float f = (float)r;
        if (f <= 0.0001f) f = 1f;
        return Mathf.Clamp(f, 0.05f, 10f);
    }
}