using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSpirit2D : MonoBehaviour
{
    [Header("Current Soul")]
    [SerializeField] private int currentSpirit;

    [Header("Save Ownership")]
    [SerializeField] private bool persistToMetaWallet = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private MetaWalletService2D _wallet;

    public int CurrentSpirit => currentSpirit;

    public event Action<int, int> OnSpiritChanged;

    private void Awake()
    {
        EnsureWallet();
        SyncFromSave();
    }

    private void OnEnable()
    {
        EnsureWallet();
        SyncFromSave();
    }

    public void AddSpirit(int amount)
    {
        if (amount <= 0) return;

        int before = currentSpirit;

        if (_wallet != null)
        {
            _wallet.AddSoul(amount);
            currentSpirit = _wallet.Soul;
        }
        else
        {
            currentSpirit += amount;
        }

        int delta = currentSpirit - before;
        OnSpiritChanged?.Invoke(currentSpirit, delta);

        if (debugLog)
            GameLogger.Log($"[PlayerSpirit2D] Soul +{amount} => {currentSpirit}", this);
    }

    public bool SpendSpirit(int amount)
    {
        if (amount <= 0) return true;

        int before = currentSpirit;

        if (_wallet != null)
        {
            if (!_wallet.SpendSoul(amount))
            {
                if (debugLog)
                    GameLogger.Log($"[PlayerSpirit2D] Not enough Soul. Required={amount}, Current={currentSpirit}", this);

                return false;
            }

            currentSpirit = _wallet.Soul;
            OnSpiritChanged?.Invoke(currentSpirit, currentSpirit - before);

            if (debugLog)
                GameLogger.Log($"[PlayerSpirit2D] Soul -{amount} => {currentSpirit}", this);

            return true;
        }

        if (currentSpirit < amount)
        {
            if (debugLog)
                GameLogger.Log($"[PlayerSpirit2D] Not enough local Soul. Required={amount}, Current={currentSpirit}", this);

            return false;
        }

        currentSpirit -= amount;
        OnSpiritChanged?.Invoke(currentSpirit, -amount);

        if (debugLog)
            GameLogger.Log($"[PlayerSpirit2D] Soul -{amount} => {currentSpirit}", this);

        return true;
    }

    public bool CanAfford(int cost)
    {
        if (_wallet != null)
            return _wallet.CanSpendSoul(cost);

        return currentSpirit >= cost;
    }

    private void EnsureWallet()
    {
        if (!persistToMetaWallet) return;
        if (_wallet != null) return;

        _wallet = new MetaWalletService2D(SaveManager2D.Instance);
    }

    private void SyncFromSave()
    {
        if (_wallet == null) return;

        int savedSoul = _wallet.Soul;
        if (currentSpirit == savedSoul) return;

        int delta = savedSoul - currentSpirit;
        currentSpirit = savedSoul;
        OnSpiritChanged?.Invoke(currentSpirit, delta);
    }
}
