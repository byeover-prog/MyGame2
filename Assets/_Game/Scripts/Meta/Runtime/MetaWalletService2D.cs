using System;
using UnityEngine;

public sealed class MetaWalletService2D
{
    private readonly SaveManager2D _saveManager;

    public event Action<int> OnNyangChanged;

    public MetaWalletService2D(SaveManager2D saveManager = null)
    {
        _saveManager = saveManager != null ? saveManager : SaveManager2D.Instance;
        EnsureData();
    }

    public int Nyang
    {
        get
        {
            WalletSaveData2D data = EnsureData();
            return data != null ? data.nyang : 0;
        }
    }

    public void AddNyang(int amount, bool autoSave = true)
    {
        if (amount <= 0) return;

        WalletSaveData2D data = EnsureData();
        if (data == null) return;

        data.nyang += amount;
        NotifyChanged(autoSave);
    }

    public bool CanSpendNyang(int amount)
    {
        if (amount <= 0) return true;
        return Nyang >= amount;
    }

    public bool SpendNyang(int amount, bool autoSave = true)
    {
        if (amount <= 0) return true;

        WalletSaveData2D data = EnsureData();
        if (data == null) return false;
        if (data.nyang < amount) return false;

        data.nyang -= amount;
        NotifyChanged(autoSave);
        return true;
    }

    public void SetNyang(int amount, bool autoSave = true)
    {
        WalletSaveData2D data = EnsureData();
        if (data == null) return;

        data.nyang = Mathf.Max(0, amount);
        NotifyChanged(autoSave);
    }

    private WalletSaveData2D EnsureData()
    {
        if (_saveManager == null || _saveManager.Data == null) return null;

        _saveManager.Data.EnsureDefaults();
        return _saveManager.Data.metaProfile.wallet;
    }

    private void NotifyChanged(bool autoSave)
    {
        if (autoSave && _saveManager != null)
            _saveManager.Save();

        OnNyangChanged?.Invoke(Nyang);
    }
}
