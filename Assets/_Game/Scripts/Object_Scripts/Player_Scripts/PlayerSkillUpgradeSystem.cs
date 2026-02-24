using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSkillUpgradeSystem : MonoBehaviour
{
    [Header("Legacy LevelUp Flow")]
    [Tooltip("구 레벨업 시스템(이 스크립트)로 패널을 띄우고 싶을 때만 켜세요.\n신규 구조: LevelUpOpenOnPlayerExp + LevelUpOrchestrator + OfferService + LevelUpOfferPanelView")]
    [SerializeField] private bool enableLegacyLevelUpFlow = false;

    [Header("공통 스킬(카탈로그)")]
    [SerializeField] private CommonSkillCatalogSO commonSkillCatalog;
    [SerializeField] private CommonSkillManager2D commonSkillManager;

    [Header("패시브(카탈로그)")]
    [SerializeField] private PassiveCatalogSO passiveCatalog;
    [SerializeField] private PassiveManager2D passiveManager;

    [Header("슬롯 제한(인스펙터에서 조절)")]
    [SerializeField] private int maxSkillSlots = 8;
    [SerializeField] private int maxPassiveSlots = 8;
    [SerializeField] private bool useTotalSlotCap = true;
    [SerializeField] private int maxTotalSlots = 16;

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

        if (commonSkillManager == null) commonSkillManager = FindFirstObjectByType<CommonSkillManager2D>();
        if (passiveManager == null) passiveManager = FindFirstObjectByType<PassiveManager2D>();

        if (seedEquippedWeaponsAsLevel1)
            SeedEquippedWeapons();
    }

    private void OnEnable()
    {
        if (!enableLegacyLevelUpFlow) return;

        if (playerExp != null)
            playerExp.OnLevelUp += OnLevelUp;
    }

    private void OnDisable()
    {
        if (!enableLegacyLevelUpFlow) return;

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

            // ✅ 3장 고정: 스킬 2장 + 패시브 1장
            var a = PickOneSkillChoice(null, null);
            var b = PickOneSkillChoice(a, null);
            var c = PickOnePassiveChoice();

            // 패시브 후보가 0이면 fallback: 스킬로 채움(카드 3장 보장)
            if (c == null)
                c = PickOneSkillChoice(a, b);

            _pendingLevelUpCount = Mathf.Max(0, _pendingLevelUpCount - 1);

            // ✅ 리롤도 동일 규칙으로
            levelUpPanel.Open(
                a, b, c,
                OnChoicePicked,
                () =>
                {
                    var ra = PickOneSkillChoice(null, null);
                    var rb = PickOneSkillChoice(ra, null);
                    var rc = PickOnePassiveChoice();
                    if (rc == null) rc = PickOneSkillChoice(ra, rb);
                    return (ra, rb, rc);
                }
            );
        }

        _isLevelUpProcessing = false;
    }

    private void OnChoicePicked(LevelUpChoice choice)
    {
        if (choice == null) return;

        switch (choice.Type)
        {
            case LevelUpChoiceType.WeaponSkill:
                if (choice.Weapon != null) ApplyUpgrade(choice.Weapon);
                break;

            case LevelUpChoiceType.CommonSkill:
                if (commonSkillManager != null && choice.CommonSkill != null)
                    commonSkillManager.Upgrade(choice.CommonSkill);
                break;

            case LevelUpChoiceType.Passive:
                if (passiveManager != null && choice.Passive != null)
                    passiveManager.Upgrade(choice.Passive);
                break;
        }
    }

    // ============================
    // 슬롯/보유 개수 계산
    // ============================

    private int GetAcquiredSkillCount()
    {
        int weaponCount = _levels.Count; // 무기 스킬 보유 수
        int commonCount = 0;

        if (commonSkillCatalog != null && commonSkillManager != null && commonSkillCatalog.skills != null)
        {
            foreach (var s in commonSkillCatalog.skills)
                if (s != null && commonSkillManager.GetLevel(s.kind) > 0)
                    commonCount++;
        }

        return weaponCount + commonCount;
    }

    private bool HasFreeSkillSlot()
    {
        int skills = GetAcquiredSkillCount();
        int passives = passiveManager != null ? passiveManager.AcquiredCount : 0;

        if (skills >= maxSkillSlots) return false;
        if (useTotalSlotCap && (skills + passives) >= maxTotalSlots) return false;
        return true;
    }

    private bool HasFreePassiveSlot()
    {
        int skills = GetAcquiredSkillCount();
        int passives = passiveManager != null ? passiveManager.AcquiredCount : 0;

        if (passives >= maxPassiveSlots) return false;
        if (useTotalSlotCap && (skills + passives) >= maxTotalSlots) return false;
        return true;
    }

    // ============================
    // 카드 후보 뽑기
    // ============================

    private LevelUpChoice PickOneSkillChoice(LevelUpChoice exclude0, LevelUpChoice exclude1)
    {
        _candidate.Clear();

        // 1) 무기 후보
        var weapons = GetWeaponCandidates();
        for (int i = 0; i < weapons.Count; i++)
        {
            var w = weapons[i];
            if (w == null) continue;
            if (string.IsNullOrWhiteSpace(w.weaponId)) continue;

            if (exclude0 != null && exclude0.Type == LevelUpChoiceType.WeaponSkill && exclude0.Weapon == w) continue;
            if (exclude1 != null && exclude1.Type == LevelUpChoiceType.WeaponSkill && exclude1.Weapon == w) continue;

            if (IsMaxed(w)) continue;

            bool acquired = GetLevel(w.weaponId) > 0;
            if (!acquired && !HasFreeSkillSlot()) continue;

            _candidate.Add(i); // 무기는 i 그대로 사용
        }

        // 2) 공통스킬 후보(무기 인덱스와 구분하기 위해 음수 인덱스 사용)
        if (commonSkillCatalog != null && commonSkillCatalog.skills != null && commonSkillManager != null)
        {
            for (int i = 0; i < commonSkillCatalog.skills.Count; i++)
            {
                var s = commonSkillCatalog.skills[i];
                if (s == null) continue;

                if (exclude0 != null && exclude0.Type == LevelUpChoiceType.CommonSkill && exclude0.CommonSkill == s) continue;
                if (exclude1 != null && exclude1.Type == LevelUpChoiceType.CommonSkill && exclude1.CommonSkill == s) continue;

                if (commonSkillManager.IsMaxLevel(s)) continue;

                bool acquired = commonSkillManager.GetLevel(s.kind) > 0;
                if (!acquired && !HasFreeSkillSlot()) continue;

                _candidate.Add(-(i + 1)); // ✅ 공통스킬은 -(i+1)
            }
        }

        if (_candidate.Count == 0) return null;

        int pick = _candidate[UnityEngine.Random.Range(0, _candidate.Count)];

        // pick >= 0 => 무기
        if (pick >= 0)
        {
            var w = weapons[pick];
            int curLevel = GetLevel(w.weaponId);
            int max = GetMaxLevel(w);
            int nextLevel = Mathf.Clamp(curLevel + 1, 1, max);

            string title = BuildCardTitle(w, curLevel, nextLevel, max);
            string desc = BuildCardDescriptionSimple(w, nextLevel);

            return new LevelUpChoice(w, nextLevel, title, desc, w.tagKr, w.icon);
        }
        else
        {
            int idx = (-pick) - 1;
            if (commonSkillCatalog == null || commonSkillCatalog.skills == null) return null;
            if (idx < 0 || idx >= commonSkillCatalog.skills.Count) return null;

            var s = commonSkillCatalog.skills[idx];

            int curLevel = commonSkillManager != null ? commonSkillManager.GetLevel(s.kind) : 0;
            int max = Mathf.Max(1, (int)s.maxLevel);
            int nextLevel = Mathf.Clamp(curLevel + 1, 1, max);

            string skillName = string.IsNullOrWhiteSpace(s.displayName) ? s.name : s.displayName;

            // [Issue 4 수정] "Lv." 표기 제거 + 신규 획득/업그레이드 구분 타이틀
            // 이미 보유 중이면 "스킬명 강화", 신규면 "스킬명 획득"
            string title = curLevel <= 0 ? $"{skillName} (획득)" : $"{skillName} (강화)";

            // 카드 설명: 공격 방식 + 다음 레벨 수치 요약
            string desc = BuildCommonSkillDesc(s, nextLevel);

            return new LevelUpChoice(s, nextLevel, title, desc, "공통 스킬", s.icon);
        }
    }

    private LevelUpChoice PickOnePassiveChoice()
    {
        if (passiveCatalog == null || passiveCatalog.passives == null || passiveCatalog.passives.Count == 0)
            return null;

        if (passiveManager == null)
            return null;

        _candidate.Clear();

        for (int i = 0; i < passiveCatalog.passives.Count; i++)
        {
            var p = passiveCatalog.passives[i];
            if (p == null) continue;

            if (passiveManager.IsMaxLevel(p)) continue;

            int cur = passiveManager.GetLevel(p.kind);
            bool acquired = cur > 0;
            if (!acquired && !HasFreePassiveSlot()) continue;

            _candidate.Add(i);
        }

        if (_candidate.Count == 0) return null;

        int pick = _candidate[UnityEngine.Random.Range(0, _candidate.Count)];
        var cfg = passiveCatalog.passives[pick];

        int curLv = passiveManager.GetLevel(cfg.kind);
        int maxLv = Mathf.Max(1, (int)cfg.maxLevel);
        int nextLv = Mathf.Clamp(curLv + 1, 1, maxLv);

        string passiveName = string.IsNullOrWhiteSpace(cfg.displayName) ? cfg.name : cfg.displayName;

        // [Issue 5 수정] "Lv." 표기 제거: 패시브 이름만 타이틀로 사용
        string title = passiveName;

        // [Issue 5] 수치 표기 필수: SO의 다음 레벨 params에서 자동 생성
        // PassiveAutoBuilder struct 버그 수정 후 0%가 아닌 실제 값이 표시됨
        var lp = cfg.GetLevelParams(nextLv);
        string desc = BuildPassiveDesc(cfg.kind, passiveName, lp);

        return new LevelUpChoice(cfg, nextLv, title, desc, "패시브", cfg.icon);
    }

    // [Issue 5] 패시브 카드 설명: 수치 명시, Lv. 표기 없음
    private static string BuildPassiveDesc(PassiveKind kind, string kindName, PassiveLevelParams lp)
    {
        switch (kind)
        {
            case PassiveKind.AttackDamage:
                return $"{kindName} +{Mathf.RoundToInt(lp.addPercent * 100f)}%";
            case PassiveKind.Defense:
                return $"피해 감소 +{Mathf.RoundToInt(lp.addPercent * 100f)}%";
            case PassiveKind.CooldownReduction:
                return $"쿨타임 -{Mathf.RoundToInt(lp.addPercent * 100f)}%";
            case PassiveKind.MoveSpeed:
                return $"이동속도 +{Mathf.RoundToInt(lp.addPercent * 100f)}%";
            case PassiveKind.PickupRange:
                return $"아이템 획득 범위 +{Mathf.RoundToInt(lp.addPercent * 100f)}%";
            case PassiveKind.MaxHp:
                return $"최대 체력 +{lp.addInt}";
            case PassiveKind.ElementDamage:
                return $"속성 피해 +{Mathf.RoundToInt(lp.addPercent * 100f)}%";
            case PassiveKind.SkillArea:
                return $"스킬 범위 +{Mathf.RoundToInt(lp.addPercent * 100f)}%";
            default:
                // 범용 처리: addInt가 0이면 퍼센트, 0이 아니면 정수
                if (lp.addInt != 0)
                    return $"{kindName} +{lp.addInt}";
                return $"{kindName} +{Mathf.RoundToInt(lp.addPercent * 100f)}%";
        }
    }

    // ============================
    // 무기 업그레이드 적용
    // ============================

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

    // ============================
    // 후보 풀(무기)
    // ============================

    private IReadOnlyList<WeaponDefinitionSO> GetWeaponCandidates()
    {
        if (deck != null && !deck.IsEmpty)
            return deck.Weapons;

        if (weaponDatabase != null && weaponDatabase.Weapons != null && weaponDatabase.Weapons.Count > 0)
            return weaponDatabase.Weapons;

        return Array.Empty<WeaponDefinitionSO>();
    }

    // (구버전 호환) - 남겨두되, 현재 리롤에서는 사용 안 함
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

    // [Issue 4] 공통 스킬 카드 설명: 공격 방식 + 다음 레벨 수치 요약(Lv. 표기 없음)
    private static string BuildCommonSkillDesc(CommonSkillConfigSO s, int nextLevel)
    {
        if (s == null) return string.Empty;

        // 1) 공격 방식 설명
        string visual = !string.IsNullOrWhiteSpace(s.visualDescriptionKr)
            ? s.visualDescriptionKr
            : "자동으로 적을 공격합니다.";

        // 2) 다음 레벨 핵심 수치 요약
        var p = s.GetLevelParams(nextLevel);
        string effect;

        switch (s.kind)
        {
            case CommonSkillKind.OrbitingBlade:
                effect = $"피해 {p.damage} | 검 {p.projectileCount}개 | 틱 {p.hitInterval:0.##}s";
                break;
            case CommonSkillKind.Boomerang:
                effect = $"피해 {p.damage} | 투사체 {p.projectileCount}개 | 쿨 {p.cooldown:0.##}s";
                break;
            case CommonSkillKind.PiercingBullet:
                effect = $"피해 {p.damage} | 투사체 {p.projectileCount}개 | 쿨 {p.cooldown:0.##}s";
                break;
            case CommonSkillKind.HomingMissile:
                effect = $"피해 {p.damage} | 연쇄 {p.chainCount}회 | 쿨 {p.cooldown:0.##}s";
                break;
            case CommonSkillKind.DarkOrb:
                effect = $"피해 {p.damage} | 분열 {p.splitCount}개 | 폭발반경 {p.explosionRadius:0.#}";
                break;
            case CommonSkillKind.Shuriken:
                effect = $"피해 {p.damage} | 튕김 {p.bounceCount}회 | 쿨 {p.cooldown:0.##}s";
                break;
            case CommonSkillKind.ArrowShot:
                effect = $"피해 {p.damage} | 화살 {p.projectileCount}발 | 쿨 {p.cooldown:0.##}s";
                break;
            case CommonSkillKind.ArrowRain:
                effect = $"피해 {p.damage} | 반경 {p.explosionRadius:0.#} | 쿨 {p.cooldown:0.##}s";
                break;
            default:
                effect = $"피해 {p.damage} | 쿨 {p.cooldown:0.##}s";
                break;
        }

        return $"{visual}\n효과: {effect}";
    }

    // "왜 2레벨부터?" 느낌 줄이기: 획득/강화 + 레벨 표기 개선
    private static string BuildCardTitle(WeaponDefinitionSO w, int cur, int next, int max)
    {
        string name = string.IsNullOrWhiteSpace(w.weaponNameKr) ? w.name : w.weaponNameKr;

        if (cur <= 0) return $"{name} (획득)";
        return $"{name} (Lv.{cur} → Lv.{next})";
    }

    // 무기 설명(간단): 기존 문장형 유지
    private string BuildCardDescriptionSimple(WeaponDefinitionSO w, int nextLevel)
    {
        int cur = Mathf.Max(0, nextLevel - 1);
        return BuildCardDescriptionText(w, cur, nextLevel);
    }

    // 수치 대신 문장형(네가 원하는 UX)
    private string BuildCardDescriptionText(WeaponDefinitionSO w, int curLevel, int nextLevel)
    {
        if (w == null) return string.Empty;

        if (curLevel <= 0)
            return "새 스킬을 획득합니다.";

        return "피해량이 증가하고 재사용 대기 시간이 감소합니다.";
    }
}