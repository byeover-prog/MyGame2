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

    [Header("시작 무기 레벨 처리")]
    [Tooltip("시작부터 슬롯에 장착된 무기들을 Lv1로 간주해서 '획득'이 아니라 '업그레이드'로 뜨게 함")]
    [SerializeField] private bool seedEquippedWeaponsAsLevel1 = true;

    // id 기반 레벨 관리(카드 뽑기/표시용)
    private readonly Dictionary<string, int> _levels = new Dictionary<string, int>(16);
    private readonly List<int> _candidate = new List<int>(32);

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
        if (deck == null || levelUpPanel == null) return;

        var a = PickOneChoice(exclude0: null, exclude1: null);
        var b = PickOneChoice(exclude0: a?.Skill, exclude1: null);
        var c = PickOneChoice(exclude0: a?.Skill, exclude1: b?.Skill);

        levelUpPanel.Open(a, b, c, OnChoicePicked);
    }

    private void OnChoicePicked(LevelUpChoice choice)
    {
        if (choice == null || choice.Skill == null) return;
        ApplyUpgrade(choice.Skill);
    }

    private void ApplyUpgrade(WeaponSkillSO skill)
    {
        if (skill == null) return;
        if (!skill.IsUsable()) return;

        int cur = GetLevel(skill.Id);
        int next = Mathf.Clamp(cur + 1, 1, skill.MaxLevel);
        _levels[skill.Id] = next;

        // 실제 발사 시스템(WeaponShooterSystem2D)에 반영
        ApplyToShooterSlots(skill, next);
    }

    private void ApplyToShooterSlots(WeaponSkillSO skill, int nextLevel)
    {
        if (shooter == null || weaponDatabase == null) return;
        if (string.IsNullOrWhiteSpace(skill.Id)) return;

        if (!weaponDatabase.TryGet(skill.Id, out var weaponDef) || weaponDef == null)
        {
            Debug.LogWarning($"[PlayerSkillUpgradeSystem] WeaponDatabaseSO에서 weaponId='{skill.Id}' 무기를 찾지 못했습니다. WeaponSkillSO.id와 WeaponDefinitionSO.weaponId가 정확히 같아야 합니다.");
            return;
        }

        var lv = skill.GetLevelData(nextLevel);

        // 목표(절대값): WeaponDefinitionSO의 base를 기준으로 "최종 데미지/최종 발사간격" 맞추기
        int desiredBonusDamage = Mathf.Max(0, lv.damage - weaponDef.baseDamage);

        float desiredInterval = Mathf.Max(0.05f, lv.cooldown);
        float baseInterval = Mathf.Max(0.05f, weaponDef.baseFireInterval);
        float desiredCooldownMul = Mathf.Clamp(desiredInterval / baseInterval, 0.1f, 10f);

        // 슬롯이 없으면 신규 장착(자동 슬롯 추가)
        if (!shooter.TryGetSlotByWeaponId(skill.Id, out int slotIndex))
        {
            shooter.AddSlot(
                weapon: weaponDef,
                enabled: true,
                bonusDamage: desiredBonusDamage,
                cooldownMul: desiredCooldownMul,
                rangeAdd: 0f
            );
            return;
        }

        // 슬롯이 있으면 현재 값에서 목표 값으로 보정(기존 API로 delta 적용)
        if (!shooter.TryGetSlotStat(slotIndex, out int curBonusDamage, out float curCooldownMul, out float curRangeAdd, out bool curEnabled))
            return;

        int deltaDamage = desiredBonusDamage - curBonusDamage;
        if (deltaDamage != 0)
            shooter.ApplyUpgradeBySlotIndex(slotIndex, WeaponUpgradeType.DamageAdd, deltaDamage, 0f, false);

        if (curCooldownMul <= 0.0001f) curCooldownMul = 1f;
        float factor = desiredCooldownMul / curCooldownMul;
        if (Mathf.Abs(factor - 1f) > 0.0001f)
            shooter.ApplyUpgradeBySlotIndex(slotIndex, WeaponUpgradeType.CooldownMul, 0, factor, false);

        if (!curEnabled)
            shooter.ApplyUpgradeBySlotIndex(slotIndex, WeaponUpgradeType.ToggleEnabled, 0, 0f, true);
    }

    private void SeedEquippedWeapons()
    {
        if (shooter == null) return;

        // shooter 슬롯에 이미 장착된 weaponId들은 Lv1로 간주
        // (카드에서 다시 '획득'이 아니라 'Lv2'부터 뜨게)
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

    private int GetLevel(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        return _levels.TryGetValue(id, out int lv) ? lv : 0;
    }

    private bool IsMaxed(WeaponSkillSO s)
    {
        if (s == null) return true;
        int cur = GetLevel(s.Id);
        return cur >= s.MaxLevel;
    }

    private LevelUpChoice PickOneChoice(WeaponSkillSO exclude0, WeaponSkillSO exclude1)
    {
        var skills = deck.Skills;
        if (skills == null || skills.Count == 0) return null;

        _candidate.Clear();

        for (int i = 0; i < skills.Count; i++)
        {
            var s = skills[i];
            if (s == null) continue;
            if (!s.IsUsable()) continue;
            if (s == exclude0 || s == exclude1) continue;
            if (IsMaxed(s)) continue;

            _candidate.Add(i);
        }

        if (_candidate.Count == 0)
            return null;

        int pick = _candidate[Random.Range(0, _candidate.Count)];
        var skill = skills[pick];

        int curLevel = GetLevel(skill.Id);
        int nextLevel = Mathf.Clamp(curLevel + 1, 1, skill.MaxLevel);

        string title = skill.BuildCardTitle(curLevel, nextLevel);
        string desc = skill.BuildCardDescription(curLevel, nextLevel);

        return new LevelUpChoice(skill, nextLevel, title, desc, skill.TagKr, skill.Icon);
    }
}