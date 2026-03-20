using System.Collections.Generic;
using UnityEngine;
using _Game.Player;

public sealed class CharacterProgressionService2D
{
    private readonly CharacterCatalogSO _catalog;
    private readonly SaveManager2D _saveManager;
    private readonly List<CharacterLevelUnlockEntry2D> _unlockBuffer = new List<CharacterLevelUnlockEntry2D>(8);

    public CharacterProgressionService2D(CharacterCatalogSO catalog, SaveManager2D saveManager = null)
    {
        _catalog = catalog;
        _saveManager = saveManager != null ? saveManager : SaveManager2D.Instance;
        EnsureMeta();
    }

    public CharacterCatalogSO Catalog => _catalog;

    public bool IsUnlocked(string characterId)
    {
        CharacterProgressionSaveData2D state = GetOrCreateState(characterId);
        return state != null && state.isUnlocked;
    }

    public void SetUnlocked(string characterId, bool unlocked, bool autoSave = true)
    {
        CharacterProgressionSaveData2D state = GetOrCreateState(characterId);
        if (state == null) return;

        state.isUnlocked = unlocked;
        Save(autoSave);
    }

    public int GetLevel(string characterId)
    {
        CharacterProgressionSaveData2D state = GetOrCreateState(characterId);
        return state != null ? state.level : 1;
    }

    public int GetCurrentXp(string characterId)
    {
        CharacterProgressionSaveData2D state = GetOrCreateState(characterId);
        return state != null ? state.currentXp : 0;
    }

    public int GetTotalXp(string characterId)
    {
        CharacterProgressionSaveData2D state = GetOrCreateState(characterId);
        return state != null ? state.totalXp : 0;
    }

    public int GetRequiredXpToNextLevel(string characterId)
    {
        CharacterDefinitionSO definition = GetDefinition(characterId);
        CharacterLevelCurveSO curve = definition != null ? definition.LevelCurve : null;
        int level = GetLevel(characterId);

        if (curve != null)
            return curve.GetRequiredXpToNextLevel(level);

        return CharacterLevelCurveSO.GetFallbackRequiredXpToNextLevel(level);
    }

    public PlayerStatSnapshot BuildLevelBonusSnapshot(string characterId)
    {
        int level = GetLevel(characterId);
        CharacterDefinitionSO definition = GetDefinition(characterId);
        CharacterLevelCurveSO curve = definition != null ? definition.LevelCurve : null;

        if (curve != null)
            return curve.BuildLevelBonusSnapshot(level);

        return CharacterLevelCurveSO.BuildFallbackBonusSnapshot(level);
    }

    public IReadOnlyList<CharacterLevelUnlockEntry2D> GetUnlockedEntries(string characterId)
    {
        _unlockBuffer.Clear();

        int level = GetLevel(characterId);
        CharacterDefinitionSO definition = GetDefinition(characterId);
        CharacterLevelCurveSO curve = definition != null ? definition.LevelCurve : null;

        if (curve != null)
            curve.CollectUnlocksToLevel(level, _unlockBuffer);

        return _unlockBuffer;
    }

    public CharacterProgressionResult2D AddXp(string characterId, int amount, bool autoSave = true)
    {
        CharacterProgressionResult2D result = new CharacterProgressionResult2D
        {
            characterId = characterId,
            gainedXp = Mathf.Max(0, amount),
            previousLevel = GetLevel(characterId),
            newLevel = GetLevel(characterId)
        };

        if (amount <= 0)
            return result;

        CharacterProgressionSaveData2D state = GetOrCreateState(characterId);
        CharacterDefinitionSO definition = GetDefinition(characterId);
        CharacterLevelCurveSO curve = definition != null ? definition.LevelCurve : null;
        int maxLevel = curve != null ? curve.MaxLevel : 50;

        if (state == null)
            return result;

        state.currentXp += amount;
        state.totalXp += amount;

        int previousLevel = state.level;
        while (state.level < maxLevel)
        {
            int required = curve != null
                ? curve.GetRequiredXpToNextLevel(state.level)
                : CharacterLevelCurveSO.GetFallbackRequiredXpToNextLevel(state.level, maxLevel: maxLevel);

            if (required <= 0) break;
            if (state.currentXp < required) break;

            state.currentXp -= required;
            state.level++;
        }

        result.previousLevel = previousLevel;
        result.newLevel = state.level;

        if (curve != null && state.level > previousLevel)
        {
            List<CharacterLevelUnlockEntry2D> unlocked = new List<CharacterLevelUnlockEntry2D>(4);
            curve.CollectUnlocksBetweenLevels(previousLevel, state.level, unlocked);
            result.unlockedEntries = unlocked;
        }
        else
        {
            result.unlockedEntries = new List<CharacterLevelUnlockEntry2D>(0);
        }

        Save(autoSave);
        return result;
    }

    private CharacterProgressionSaveData2D GetOrCreateState(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return null;
        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null) return null;

        CharacterDefinitionSO definition = GetDefinition(characterId);
        bool unlockedByDefault = definition == null || definition.UnlockedByDefault;
        return meta.progression.GetOrCreate(characterId, unlockedByDefault);
    }

    private CharacterDefinitionSO GetDefinition(string characterId)
    {
        if (_catalog == null || string.IsNullOrWhiteSpace(characterId)) return null;
        return _catalog.TryFindById(characterId, out CharacterDefinitionSO found) ? found : null;
    }

    private MetaProfileSaveData2D EnsureMeta()
    {
        if (_saveManager == null || _saveManager.Data == null) return null;
        _saveManager.Data.EnsureDefaults();
        return _saveManager.Data.metaProfile;
    }

    private void Save(bool autoSave)
    {
        if (autoSave && _saveManager != null)
            _saveManager.Save();
    }
}

public sealed class CharacterProgressionResult2D
{
    public string characterId;
    public int gainedXp;
    public int previousLevel;
    public int newLevel;
    public List<CharacterLevelUnlockEntry2D> unlockedEntries = new List<CharacterLevelUnlockEntry2D>(0);
}
