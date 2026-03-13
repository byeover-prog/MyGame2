using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대쉬 충전 아이콘 표시 전용 UI.
/// 로직은 하지 않고, 현재 충전 수와 최대 칸 수만 받아서 표시한다.
/// </summary>
public sealed class DashChargeUI : MonoBehaviour
{
    [Header("대쉬 아이콘")]
    [Tooltip("대쉬 칸 이미지들. 0~2 순서로 넣기")]
    [SerializeField] private Image[] dashIcons;

    [Header("스프라이트")]
    [Tooltip("충전 완료 상태 이미지")]
    [SerializeField] private Sprite chargedSprite;

    [Tooltip("비어 있는 상태 이미지")]
    [SerializeField] private Sprite emptySprite;

    [Tooltip("잠긴 칸 상태 이미지")]
    [SerializeField] private Sprite lockedSprite;

    /// <summary>
    /// 현재 UI 반영
    /// </summary>
    public void Refresh(int currentCount, int maxCount)
    {
        if (dashIcons == null) return;

        for (int i = 0; i < dashIcons.Length; i++)
        {
            if (dashIcons[i] == null) continue;

            if (i >= maxCount)
            {
                dashIcons[i].sprite = lockedSprite;
                dashIcons[i].color = new Color(1f, 1f, 1f, 0.35f);
                continue;
            }

            if (i < currentCount)
            {
                dashIcons[i].sprite = chargedSprite;
                dashIcons[i].color = Color.white;
            }
            else
            {
                dashIcons[i].sprite = emptySprite;
                dashIcons[i].color = new Color(1f, 1f, 1f, 0.6f);
            }
        }
    }
}