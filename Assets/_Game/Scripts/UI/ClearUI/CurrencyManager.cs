using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    private MetaWalletService2D _wallet;

    public int BaseNyang { get; private set; }
    public int BaseSpirit { get; private set; }

    public int StagedNyang { get; private set; }
    public int StagedSpirit { get; private set; }

    public int TotalNyang => BaseNyang + StagedNyang;
    public int TotalSpirit => BaseSpirit + StagedSpirit;

    public event System.Action OnCurrencyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _wallet = new MetaWalletService2D();
        LoadCurrency();
    }

    private void LoadCurrency()
    {
        _wallet ??= new MetaWalletService2D();
        BaseNyang = _wallet.Nyang;
        BaseSpirit = _wallet.Soul;
        StagedNyang = 0;
        StagedSpirit = 0;
    }

    public void ResetStageRewards()
    {
        StagedNyang = 0;
        StagedSpirit = 0;
        OnCurrencyChanged?.Invoke();
    }

    public void AddNyang(int amount)
    {
        if (amount <= 0) return;

        StagedNyang += amount;
        OnCurrencyChanged?.Invoke();
    }

    public void AddSpirit(int amount)
    {
        if (amount <= 0) return;

        StagedSpirit += amount;
        OnCurrencyChanged?.Invoke();
    }

    public void SaveStageClearRewards()
    {
        _wallet ??= new MetaWalletService2D();
        _wallet.AddNyang(StagedNyang, autoSave: false);
        _wallet.AddSoul(StagedSpirit, autoSave: false);

        SaveManager2D.Instance?.Save();

        BaseNyang = _wallet.Nyang;
        BaseSpirit = _wallet.Soul;

        OnCurrencyChanged?.Invoke();

        GameLogger.Log($"[CurrencyManager] Saved rewards - Nyang: {BaseNyang}, Soul: {BaseSpirit}");
    }

    public bool SpendNyang(int amount)
    {
        _wallet ??= new MetaWalletService2D();
        if (!_wallet.SpendNyang(amount))
        {
            GameLogger.LogWarning("[CurrencyManager] Not enough Nyang.");
            return false;
        }

        BaseNyang = _wallet.Nyang;
        OnCurrencyChanged?.Invoke();
        return true;
    }

    public bool SpendSpirit(int amount)
    {
        _wallet ??= new MetaWalletService2D();
        if (!_wallet.SpendSoul(amount))
        {
            GameLogger.LogWarning("[CurrencyManager] Not enough Soul.");
            return false;
        }

        BaseSpirit = _wallet.Soul;
        OnCurrencyChanged?.Invoke();
        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Add 100 Nyang")]
    private void DebugAddNyang() => AddNyang(100);

    [ContextMenu("Debug/Add 10 Spirit")]
    private void DebugAddSpirit() => AddSpirit(10);

    [ContextMenu("Debug/Reset All Currency")]
    private void DebugReset()
    {
        _wallet ??= new MetaWalletService2D();
        _wallet.DebugSetNyang(0);
        _wallet.DebugSetSoul(0);
        LoadCurrency();
        OnCurrencyChanged?.Invoke();
        GameLogger.Log("[CurrencyManager] Currency reset complete.");
    }
#endif
}
