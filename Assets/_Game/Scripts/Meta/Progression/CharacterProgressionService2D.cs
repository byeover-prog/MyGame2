using System.Collections.Generic;
using UnityEngine;
using _Game.Player;

/// <summary>
/// 캐릭터 해금 여부, 레벨, 경험치를 조회·변경하는 서비스입니다.
/// SaveManager2D.Data.metaProfile.progression을 읽고 씁니다.
/// </summary>
public sealed class CharacterProgressionService2D
{
    private readonly CharacterCatalogSO _catalog;
    private readonly SaveManager2D _saveManager;

    /// <summary>레벨 N→N+1에 필요한 경험치를 계산합니다.</summary>
    private static int RequiredXpForLevel(int currentLevel)
    {
        return Mathf.Max(50, currentLevel * 100);
    }

    public CharacterProgressionService2D(CharacterCatalogSO catalog, SaveManager2D saveManager = null)
    {
        _catalog = catalog;
        _saveManager = saveManager != null ? saveManager : SaveManager2D.Instance;
    }

    // ─── 해금 ──────────────────────────────────────────

    /// <summary>해당 캐릭터가 해금되어 있는지 확인합니다.</summary>
    public bool IsUnlocked(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return false;

        if (_catalog != null && _catalog.TryFindById(characterId, out CharacterDefinitionSO def) && def != null)
        {
            if (def.UnlockedByDefault) return true;
        }

        CharacterProgressionEntrySaveData2D entry = GetEntry(characterId);
        return entry != null && entry.unlocked;
    }

    /// <summary>캐릭터를 해금합니다.</summary>
    public void Unlock(string characterId)
    {
        CharacterProgressionEntrySaveData2D entry = GetOrCreateEntry(characterId);
        if (entry == null) return;
        entry.unlocked = true;
        Save();
    }

    /// <summary>
    /// 해당 캐릭터의 레벨 해금 항목 목록을 반환합니다.
    /// TODO: CharacterDefinitionSO 또는 CharacterLevelCurveSO에서 해금 목록을 연결하세요.
    /// </summary>
    public IReadOnlyList<CharacterLevelUnlockEntry2D> GetUnlockedEntries(string characterId)
    {
        return System.Array.Empty<CharacterLevelUnlockEntry2D>();
    }

    // ─── 레벨 ──────────────────────────────────────────

    /// <summary>해당 캐릭터의 현재 레벨을 반환합니다. (최소 1, 최대 50)</summary>
    public int GetLevel(string characterId)
    {
        CharacterProgressionEntrySaveData2D entry = GetEntry(characterId);
        return entry != null ? Mathf.Clamp(entry.level, 1, 50) : 1;
    }

    /// <summary>해당 캐릭터의 현재 경험치(레벨 내 누적)를 반환합니다.</summary>
    public int GetCurrentXp(string characterId)
    {
        CharacterProgressionEntrySaveData2D entry = GetEntry(characterId);
        return entry != null ? Mathf.Max(0, entry.totalExp) : 0;
    }

    /// <summary>다음 레벨까지 필요한 총 경험치를 반환합니다. 최대 레벨이면 0을 반환합니다.</summary>
    public int GetRequiredXpToNextLevel(string characterId)
    {
        CharacterProgressionEntrySaveData2D entry = GetEntry(characterId);
        if (entry == null) return RequiredXpForLevel(1);
        if (entry.level >= 50) return 0;
        return RequiredXpForLevel(entry.level);
    }

    /// <summary>캐릭터 레벨을 직접 설정합니다. (1~50 범위)</summary>
    public void SetLevel(string characterId, int level)
    {
        CharacterProgressionEntrySaveData2D entry = GetOrCreateEntry(characterId);
        if (entry == null) return;
        entry.level = Mathf.Clamp(level, 1, 50);
        Save();
    }

    // ─── 경험치 ────────────────────────────────────────

    /// <summary>
    /// 경험치를 추가하고 레벨업 결과를 반환합니다.
    /// </summary>
    public CharacterProgressionResult2D AddXp(string characterId, int amount, bool autoSave = true)
    {
        CharacterProgressionResult2D result = new CharacterProgressionResult2D();

        if (amount <= 0 || string.IsNullOrWhiteSpace(characterId))
            return result;

        CharacterProgressionEntrySaveData2D entry = GetOrCreateEntry(characterId);
        if (entry == null) return result;

        result.previousLevel = entry.level;

        if (entry.level >= 50)
        {
            result.newLevel = 50;
            return result;
        }

        entry.totalExp += amount;
        result.xpAdded = amount;

        while (entry.level < 50)
        {
            int required = RequiredXpForLevel(entry.level);
            if (entry.totalExp < required) break;
            entry.totalExp -= required;
            entry.level++;
            result.levelsGained++;
        }

        result.newLevel = entry.level;

        if (autoSave) Save();

        return result;
    }

    /// <summary>경험치를 추가합니다. (간단 버전 — 하위 호환)</summary>
    public void AddExp(string characterId, int amount)
    {
        AddXp(characterId, amount, autoSave: true);
    }

    // ─── 레벨 보너스 스냅샷 ────────────────────────────

    /// <summary>
    /// 해당 캐릭터의 현재 레벨에 따른 보너스 스탯 스냅샷을 빌드합니다.
    /// PlayerBaseStatProfileSO.BuildSnapshot()을 기반으로 레벨 비례 보정합니다.
    /// </summary>
    public PlayerStatSnapshot BuildLevelBonusSnapshot(string characterId)
    {
        PlayerStatSnapshot snapshot = default;

        if (_catalog == null || string.IsNullOrWhiteSpace(characterId))
            return snapshot;

        if (!_catalog.TryFindById(characterId, out CharacterDefinitionSO def) || def == null)
            return snapshot;

        PlayerBaseStatProfileSO profile = def.BaseStatProfile;
        if (profile == null)
            return snapshot;

        // PlayerBaseStatProfileSO에 저장된 값은 캐릭터의 기본 보너스입니다.
        // 레벨에 비례해 스케일링합니다: (레벨-1) 만큼 기본 보너스를 누적.
        // 예: 공격력보너스 1%, 레벨 10이면 → 9% 누적
        int level = GetLevel(characterId);
        int bonusLevels = Mathf.Max(0, level - 1);

        if (bonusLevels <= 0)
            return snapshot;

        // 기본 프로필에서 1레벨분의 보너스를 읽어 레벨 수만큼 곱합니다.
        PlayerStatSnapshot baseSnapshot = profile.BuildSnapshot();

        snapshot.AttackPowerPercent = baseSnapshot.AttackPowerPercent * bonusLevels;
        snapshot.DefensePercent = baseSnapshot.DefensePercent * bonusLevels;
        snapshot.MaxHpFlat = baseSnapshot.MaxHpFlat * bonusLevels;
        snapshot.MoveSpeedPercent = baseSnapshot.MoveSpeedPercent * bonusLevels;
        snapshot.PickupRangePercent = baseSnapshot.PickupRangePercent * bonusLevels;
        snapshot.SkillHastePercent = baseSnapshot.SkillHastePercent * bonusLevels;
        snapshot.SkillAreaPercent = baseSnapshot.SkillAreaPercent * bonusLevels;
        snapshot.ExpGainPercent = baseSnapshot.ExpGainPercent * bonusLevels;

        return snapshot;
    }

    // ─── 내부 ──────────────────────────────────────────

    private CharacterProgressionEntrySaveData2D GetEntry(string characterId)
    {
        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null || meta.progression == null) return null;

        for (int i = 0; i < meta.progression.entries.Count; i++)
        {
            CharacterProgressionEntrySaveData2D e = meta.progression.entries[i];
            if (e != null && e.characterId == characterId) return e;
        }
        return null;
    }

    private CharacterProgressionEntrySaveData2D GetOrCreateEntry(string characterId)
    {
        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null) return null;
        return meta.progression.GetOrCreate(characterId);
    }

    private MetaProfileSaveData2D EnsureMeta()
    {
        if (_saveManager == null || _saveManager.Data == null) return null;
        _saveManager.Data.EnsureDefaults();
        return _saveManager.Data.metaProfile;
    }

    private void Save()
    {
        if (_saveManager != null) _saveManager.Save();
    }
}