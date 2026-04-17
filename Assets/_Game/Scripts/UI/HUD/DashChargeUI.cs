using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public sealed class DashChargeUI : MonoBehaviour
{
    [System.Serializable]
    public struct DashSlot
    {
        public Image fill;
        public RectTransform icon;
    }

    [Header("대쉬 슬롯")]
    [SerializeField] private DashSlot[] slots;

    private int _currentCount;
    private int _maxCount;
    private bool _initialized;

    public void Refresh(int currentCount, int maxCount)
    {
        int prevCount = _currentCount;
        _currentCount = currentCount;
        _maxCount = maxCount;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].fill == null) continue;

            if (i >= maxCount)
            {
                slots[i].fill.fillAmount = 0f;
                continue;
            }

            if (i < currentCount)
            {
                if (!_initialized)
                {
                    slots[i].fill.fillAmount = 1f;
                }
                else if (i >= prevCount)
                {
                    slots[i].fill.fillAmount = 1f;
                    PlayPopAnim(i);
                }
            }
            else
            {
                // 소진된 슬롯 → 즉시 0으로
                slots[i].fill.fillAmount = 0f;
            }
        }

        _initialized = true;
    }
    
    public void ResetSlotFill(int index)
    {
        if (index < 0 || index >= slots.Length) return;
        if (slots[index].fill == null) return;
        slots[index].fill.fillAmount = 0f;
    }

    public void UpdateChargingFill(float rechargeTimer, float dashCooldown)
    {
        if (_currentCount >= _maxCount) return;

        int chargingIndex = _currentCount;
        Debug.Log($"충전중 슬롯: {chargingIndex}, timer: {rechargeTimer}, cooldown: {dashCooldown}");
    
        if (chargingIndex >= slots.Length) return;
        if (slots[chargingIndex].fill == null) return;

        float ratio = rechargeTimer > 0f ? 1f - (rechargeTimer / dashCooldown) : 1f;
        Debug.Log($"fillAmount: {ratio}");
        slots[chargingIndex].fill.fillAmount = ratio;
    }

    private void PlayPopAnim(int index)
    {
        if (slots[index].icon == null) return;

        slots[index].icon.DOKill();
        slots[index].icon.localScale = Vector3.one;
        slots[index].icon
            .DOScale(1.25f, 0.1f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
                slots[index].icon
                    .DOScale(1f, 0.15f)
                    .SetEase(Ease.InQuad));
    }
}