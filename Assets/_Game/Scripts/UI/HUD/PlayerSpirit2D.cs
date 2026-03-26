using System;
using UnityEngine;

/// <summary>
/// 혼령(Spirit) 재화를 관리합니다.
/// - 인게임에서 획득, 스킬트리에서 소비합니다.
/// - PlayerCurrency2D(냥)와 독립적으로 동작합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerSpirit2D : MonoBehaviour
{
    [Header("=== 현재 혼령 ===")]
    [SerializeField] private int currentSpirit;

    [Header("=== 디버그 ===")]
    [SerializeField] private bool debugLog = false;

    public int CurrentSpirit => currentSpirit;

    /// <summary>혼령이 변화할 때 발행합니다. (총량, 변화량)</summary>
    public event Action<int, int> OnSpiritChanged;

    // ── 획득 ─────────────────────────────────────
    public void AddSpirit(int amount)
    {
        if (amount <= 0) return;

        currentSpirit += amount;
        OnSpiritChanged?.Invoke(currentSpirit, amount);

        if (debugLog)
            GameLogger.Log($"[PlayerSpirit2D] Spirit +{amount} => {currentSpirit}", this);
    }

    // ── 소비 (스킬트리) ───────────────────────────
    /// <summary>
    /// 혼령을 소비합니다. 잔액 부족 시 false 반환.
    /// 스킬트리 구매 시 호출하세요.
    /// </summary>
    public bool SpendSpirit(int amount)
    {
        if (amount <= 0) return true;
        if (currentSpirit < amount)
        {
            if (debugLog)
                GameLogger.Log($"[PlayerSpirit2D] 혼령 부족 — 필요 {amount}, 보유 {currentSpirit}", this);
            return false;
        }

        currentSpirit -= amount;
        OnSpiritChanged?.Invoke(currentSpirit, -amount);

        if (debugLog)
            GameLogger.Log($"[PlayerSpirit2D] Spirit -{amount} => {currentSpirit}", this);

        return true;
    }

    // ── 조회 ─────────────────────────────────────
    /// <summary>스킬트리에서 구매 가능 여부 확인용</summary>
    public bool CanAfford(int cost) => currentSpirit >= cost;
}
