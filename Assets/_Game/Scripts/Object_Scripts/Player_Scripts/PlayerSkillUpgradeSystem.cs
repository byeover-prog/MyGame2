using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSkillUpgradeSystem : MonoBehaviour
{
    [Header("레벨업 소스")]
    [SerializeField] private PlayerExp playerExp;

    [Header("UI")]
    [SerializeField] private LevelUpPanelController levelUpPanel;

    [Header("무기 덱(카드 풀)")]
    [SerializeField] private WeaponSkillDeckSO deck;

    [Header("적용 대상(현재 사용 중인 무기 슬롯 발사 시스템)")]
    [SerializeField] private WeaponShooterSystem2D shooter;

    [Header("무기 정의 DB(weaponId -> WeaponDefinitionSO)")]
    [SerializeField] private WeaponDatabaseSO weaponDatabase;

    [Header("업그레이드 밸런스 테이블")]
    [SerializeField] private UpgradeBalanceTableSO upgradeBalanceTable;

    [Header("시작 무기 레벨 처리")]
    [Tooltip("시작부터 슬롯에 장착된 무기들을 Lv1로 간주해서 '획득'이 아니라 '업그레이드'로 뜨게 함")]
    [SerializeField] private bool seedEquippedWeaponsAsLevel1 = true;

    [Header("레벨업 연속 처리(패널 순차 재생)")]
    [SerializeField, Tooltip("연속 레벨업이 들어올 때 패널을 순차로 띄우는 텀(초). TimeScale=0에서도 동작")]
    private float levelUpPanelInterval = 0.25f;

    [Header("임시 밸런스(테이블 미지정/비정상일 때만 사용)")]
    [Min(1)]
    [SerializeField] private int defaultMaxLevel = 8;

    [SerializeField] private int damageAddPerLevel = 1;

    [Range(0.5f, 1.2f)]
    [SerializeField] private float cooldownMulPerLevel = 0.95f;

    [SerializeField] private float rangeAddPerLevel = 0f;

    private readonly Dictionary<string, int> _levels = new Dictionary<string, int>(16);
    private readonly List<int> _candidate = new List<int>(64);

    private int _pendingLevelUpCount = 0;
    private bool _isLevelUpProcessing = false;

    private void Awake()
    {
        if (playerExp == null) playerExp = FindFirstObjectByType<PlayerExp>();
        if (levelUpPanel == null) levelUpPanel = FindFirstObjectByType<LevelUpPanelController>();
        if (shooter == null) shooter = FindFirstObjectByType<WeaponShooterSystem2D>();
        if (weaponDatabase == null) weaponDatabase = FindFirstObjectByType<WeaponDatabaseSO>();

        if (seedEquippedWeaponsAsLevel1)
            SeedEquippedWeapons();
    }

    private void OnEnable()
    {
        if (playerExp != null)
            playerExp.OnLevelUp += OnLevelUp;
    }

    private void OnDisable()
    {
        if (playerExp != null)
            playerExp.OnLevelUp -= OnLevelUp;
    }

    private void OnLevelUp(int level)
    {
        if (levelUpPanel == null) return;

        _pendingLevelUpCount++;

        if (!_isLevelUpProcessing)
            StartCoroutine(ProcessLevelUpQueue());
    }

    private IEnumerator ProcessLevelUpQueue()
    {
        _isLevelUpProcessing = true;

        while (_pendingLevelUpCount > 0)
        {
            while (levelUpPanel != null && levelUpPanel.IsOpen)
                yield return null;

            if (levelUpPanelInterval > 0f)
                yield return new WaitForSecondsRealtime(levelUpPanelInterval);

            var a = PickOneChoice(exclude0: null, exclude1: null);
            var b = PickOneChoice(exclude0: a, exclude1: null);
            var c = PickOneChoice(exclude0: a, exclude1: b);

            _pendingLevelUpCount = Mathf.Max(0, _pendingLevelUpCount - 1);

            // 리롤 콜백 포함 Open
            levelUpPanel.Open(
                a, b, c,
                OnChoicePicked,
                () =>
                {
                    var ra = PickOneChoice(exclude0: null, exclude1: null);
                    var rb = PickOneChoice(exclude0: ra, exclude1: null);
                    var rc = PickOneChoice(exclude0: ra, exclude1: rb);
                    return (ra, rb, rc);
                }
            );
        }

        _isLevelUpProcessing = false;
    }

    private void OnChoicePicked(LevelUpChoice choice)
    {
        if (choice == null) return;
        if (choice.Weapon == null) return;

        ApplyUpgrade(choice.Weapon);
    }

    private void ApplyUpgrade(WeaponDefinitionSO weapon)
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

    private IReadOnlyList<WeaponDefinitionSO> GetWeaponCandidates()
    {
        if (deck != null && !deck.IsEmpty)
            return deck.Weapons;

        if (weaponDatabase != null && weaponDatabase.Weapons != null && weaponDatabase.Weapons.Count > 0)
            return weaponDatabase.Weapons;

        return Array.Empty<WeaponDefinitionSO>();
    }

    private LevelUpChoice PickOneChoice(LevelUpChoice exclude0, LevelUpChoice exclude1)
    {
        var candidates = GetWeaponCandidates();
        if (candidates == null || candidates.Count == 0) return null;

        _candidate.Clear();

        for (int i = 0; i < candidates.Count; i++)
        {
            var w = candidates[i];
            if (w == null) continue;
            if (string.IsNullOrWhiteSpace(w.weaponId)) continue;

            if (exclude0 != null && exclude0.Weapon == w) continue;
            if (exclude1 != null && exclude1.Weapon == w) continue;

            if (IsMaxed(w)) continue;

            _candidate.Add(i);
        }

        if (_candidate.Count == 0)
            return null;

        int pick = _candidate[UnityEngine.Random.Range(0, _candidate.Count)];
        var weapon = candidates[pick];

        int curLevel = GetLevel(weapon.weaponId);
        int max = GetMaxLevel(weapon);
        int nextLevel = Mathf.Clamp(curLevel + 1, 1, max);

        string title = BuildCardTitle(weapon, curLevel, nextLevel, max);
        string desc = BuildCardDescriptionText(weapon, curLevel, nextLevel);

        return new LevelUpChoice(weapon, nextLevel, title, desc, weapon.tagKr, weapon.icon);
    }

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

    private bool IsMaxed(WeaponDefinitionSO w)
    {
        if (w == null) return true;
        int cur = GetLevel(w.weaponId);
        return cur >= GetMaxLevel(w);
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

    // "왜 2레벨부터?" 느낌 줄이기: 획득/강화 + 레벨 표기 개선
    private static string BuildCardTitle(WeaponDefinitionSO w, int cur, int next, int max)
    {
        string name = string.IsNullOrWhiteSpace(w.weaponNameKr) ? w.name : w.weaponNameKr;

        if (cur <= 0) return $"{name} (획득)";
        return $"{name} (Lv.{cur} → Lv.{next})";
    }

    // 수치 대신 문장형(네가 원하는 UX)
    private string BuildCardDescriptionText(WeaponDefinitionSO w, int curLevel, int nextLevel)
    {
        if (w == null) return string.Empty;

        // 지금 구조상 레벨업은 기본적으로 피해/쿨/범위를 건드림.
        // 당장 1줄 UX로 고정.
        if (curLevel <= 0)
            return "새 스킬을 획득합니다.";

        return "피해량이 증가하고 재사용 대기 시간이 감소합니다.";
    }
}