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
                // 잠긴 슬롯
                slots[i].fill.fillAmount = 0f;
                continue;
            }

            if (i < currentCount)
            {
                // 충전됨 - 초기화 후 첫 Refresh는 애니메이션 없이
                if (!_initialized)
                {
                    slots[i].fill.fillAmount = 1f;
                }
                // 이전에 비어있다가 충전 완료된 경우 팝 효과
                else if (i >= prevCount)
                {
                    slots[i].fill.fillAmount = 1f;
                    PlayPopAnim(i);
                }
            }
            else
            {
                // 소진됨
                if (!_initialized)
                    slots[i].fill.fillAmount = 0f;
            }
        }

        _initialized = true;
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