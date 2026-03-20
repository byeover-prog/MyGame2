using System;
using System.Collections.Generic;

public sealed class FormationService2D
{
    private readonly CharacterCatalogSO _catalog;
    private readonly SaveManager2D _saveManager;
    private readonly CharacterProgressionService2D _progressionService;

    public event Action<FormationSaveData2D> OnChanged;

    public FormationService2D(CharacterCatalogSO catalog, SaveManager2D saveManager = null)
    {
        _catalog = catalog;
        _saveManager = saveManager != null ? saveManager : SaveManager2D.Instance;
        _progressionService = new CharacterProgressionService2D(catalog, _saveManager);
        EnsureData();
    }

    public IReadOnlyList<CharacterDefinitionSO> Characters => _catalog != null ? _catalog.Characters : Array.Empty<CharacterDefinitionSO>();

    public FormationSaveData2D Current => EnsureData();

    public bool IsUnlocked(string characterId)
    {
        return _progressionService.IsUnlocked(characterId);
    }

    public bool IsSelected(string characterId)
    {
        FormationSaveData2D data = EnsureData();
        return data != null && data.Contains(characterId);
    }

    public bool CanAssign(string characterId, FormationSlotType2D targetSlot, out string reason)
    {
        reason = string.Empty;

        if (string.IsNullOrWhiteSpace(characterId))
        {
            reason = "비어 있는 캐릭터는 배치할 수 없습니다.";
            return false;
        }

        if (_catalog == null || !_catalog.TryFindById(characterId, out CharacterDefinitionSO definition) || definition == null)
        {
            reason = "카탈로그에 없는 캐릭터입니다.";
            return false;
        }

        if (!_progressionService.IsUnlocked(characterId))
        {
            reason = "아직 해금되지 않은 캐릭터입니다.";
            return false;
        }

        FormationSaveData2D data = EnsureData();
        if (data == null)
        {
            reason = "세이브 데이터가 준비되지 않았습니다.";
            return false;
        }

        string existing = data.GetCharacterId(targetSlot);
        if (existing == characterId)
            return true;

        if (data.Contains(characterId))
        {
            reason = "이미 다른 슬롯에 배치된 캐릭터입니다.";
            return false;
        }

        return true;
    }

    public bool TryAssign(FormationSlotType2D slot, string characterId, out string reason)
    {
        if (!CanAssign(characterId, slot, out reason))
            return false;

        FormationSaveData2D data = EnsureData();
        data.SetCharacterId(slot, characterId);
        SaveAndSync();
        return true;
    }

    public void ClearSlot(FormationSlotType2D slot)
    {
        FormationSaveData2D data = EnsureData();
        if (data == null) return;

        data.ClearSlot(slot);
        SaveAndSync();
    }

    public void ClearAll()
    {
        FormationSaveData2D data = EnsureData();
        if (data == null) return;

        data.ClearAll();
        SaveAndSync();
    }

    public bool CanStart(bool requireMain)
    {
        FormationSaveData2D data = EnsureData();
        if (data == null) return false;
        return !requireMain || data.HasMain;
    }

    public CharacterDefinitionSO GetCharacter(FormationSlotType2D slot)
    {
        FormationSaveData2D data = EnsureData();
        if (data == null || _catalog == null) return null;

        return _catalog.TryFindById(data.GetCharacterId(slot), out CharacterDefinitionSO found) ? found : null;
    }

    private FormationSaveData2D EnsureData()
    {
        if (_saveManager == null || _saveManager.Data == null) return null;
        _saveManager.Data.EnsureDefaults();
        return _saveManager.Data.metaProfile.formation;
    }

    private void SaveAndSync()
    {
        FormationSaveData2D data = EnsureData();
        if (data == null) return;

        if (_saveManager != null)
            _saveManager.Save();

        SquadLoadoutRuntime.CopyFromSave(data);
        MetaAutoBootstrap2D.RebuildBattleSnapshotIfPossible();
        OnChanged?.Invoke(data);
    }
}
