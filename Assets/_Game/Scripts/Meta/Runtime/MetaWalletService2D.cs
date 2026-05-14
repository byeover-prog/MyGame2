using UnityEngine;

public sealed class MetaWalletService2D
{
    private readonly SaveManager2D _saveManager;

    public MetaWalletService2D(SaveManager2D saveManager = null)
    {
        _saveManager = saveManager != null ? saveManager : SaveManager2D.Instance;
    }

    public int Nyang
    {
        get
        {
            MetaProfileSaveData2D meta = EnsureMeta();
            return meta != null ? meta.nyang : 0;
        }
    }

    public int Soul
    {
        get
        {
            MetaProfileSaveData2D meta = EnsureMeta();
            return meta != null ? meta.soul : 0;
        }
    }

    public bool CanSpendNyang(int amount)
    {
        return amount >= 0 && Nyang >= amount;
    }

    public bool CanSpendSoul(int amount)
    {
        return amount >= 0 && Soul >= amount;
    }

    public bool SpendNyang(int amount, bool autoSave = true)
    {
        if (amount < 0) return false;

        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null || meta.nyang < amount) return false;

        meta.nyang -= amount;

        if (autoSave) Save();
        return true;
    }

    public bool SpendSoul(int amount, bool autoSave = true)
    {
        if (amount < 0) return false;

        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null || meta.soul < amount) return false;

        meta.soul -= amount;

        if (autoSave) Save();
        return true;
    }

    public void AddNyang(int amount, bool autoSave = true)
    {
        if (amount <= 0) return;

        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null) return;

        meta.nyang += amount;

        if (autoSave) Save();
    }

    public void AddSoul(int amount, bool autoSave = true)
    {
        if (amount <= 0) return;

        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null) return;

        meta.soul += amount;

        if (autoSave) Save();
    }

    public void DebugSetNyang(int value)
    {
        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null) return;

        meta.nyang = Mathf.Max(0, value);
        Save();
        GameLogger.Log($"[MetaWalletService2D] Debug: Nyang set to {meta.nyang}.");
    }

    public void DebugSetSoul(int value)
    {
        MetaProfileSaveData2D meta = EnsureMeta();
        if (meta == null) return;

        meta.soul = Mathf.Max(0, value);
        Save();
        GameLogger.Log($"[MetaWalletService2D] Debug: Soul set to {meta.soul}.");
    }

    private MetaProfileSaveData2D EnsureMeta()
    {
        if (_saveManager == null || _saveManager.Data == null) return null;

        _saveManager.Data.EnsureDefaults();
        return _saveManager.Data.metaProfile;
    }

    private void Save()
    {
        if (_saveManager != null)
            _saveManager.Save();
    }
}
