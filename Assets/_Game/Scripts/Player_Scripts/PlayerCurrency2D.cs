using System;
using UnityEngine;

/// <summary>
/// 플레이 중 획득하는 재화를 관리합니다.
/// GoldGain 배율이 있으면 최종 획득량에 반영합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerCurrency2D : MonoBehaviour
{
    [Header("=== 현재 재화 ===")]
    [SerializeField] private int currentGold;

    [Header("=== 참조 ===")]
    [SerializeField] private PlayerCombatStats2D combatStats;

    [Header("=== 디버그 ===")]
    [SerializeField] private bool debugLog = false;

    public int CurrentGold => currentGold;
    public event Action<int, int> OnGoldChanged;

    private void Awake()
    {
        if (combatStats == null) combatStats = GetComponent<PlayerCombatStats2D>();
        if (combatStats == null) combatStats = GetComponentInParent<PlayerCombatStats2D>();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        int finalAmount = amount;
        if (combatStats != null)
            finalAmount = Mathf.Max(1, Mathf.RoundToInt(amount * combatStats.GoldGainMul));

        currentGold += finalAmount;
        OnGoldChanged?.Invoke(currentGold, finalAmount);

        if (debugLog)
            Debug.Log($"[PlayerCurrency2D] Gold +{finalAmount} (raw={amount}) => {currentGold}", this);
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (currentGold < amount) return false;

        currentGold -= amount;
        OnGoldChanged?.Invoke(currentGold, -amount);

        if (debugLog)
            Debug.Log($"[PlayerCurrency2D] Gold -{amount} => {currentGold}", this);

        return true;
    }
}