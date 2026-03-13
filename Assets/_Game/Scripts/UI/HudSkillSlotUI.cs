using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 HUD 슬롯 1칸 표시 전용.
/// 상태에 따라 프레임 / 아이콘 / 텍스트만 갱신한다.
/// 기존 호출부 호환을 위해 다양한 메서드 시그니처를 함께 제공한다.
/// </summary>
public sealed class HudSkillSlotUI : MonoBehaviour
{
    private enum SlotState
    {
        Empty,
        Filled,
        Locked,
        Placeholder
    }

    [Header("참조")]
    [Tooltip("슬롯 아이콘 이미지")]
    [SerializeField] private Image iconImage;

    [Tooltip("슬롯 프레임 이미지")]
    [SerializeField] private Image frameImage;

    [Tooltip("슬롯 이름 텍스트")]
    [SerializeField] private TMP_Text labelText;

    [Tooltip("슬롯 보조 텍스트(레벨, 설명, 개수 등)")]
    [SerializeField] private TMP_Text subLabelText;

    [Header("프레임 스프라이트")]
    [Tooltip("빈 슬롯 프레임")]
    [SerializeField] private Sprite emptyFrameSprite;

    [Tooltip("일반 슬롯 프레임")]
    [SerializeField] private Sprite filledFrameSprite;

    [Tooltip("잠김 슬롯 프레임")]
    [SerializeField] private Sprite lockedFrameSprite;

    [Header("아이콘 색상")]
    [Tooltip("빈 슬롯 아이콘 색상")]
    [SerializeField] private Color emptyIconColor = new Color(1f, 1f, 1f, 0.18f);

    [Tooltip("일반 슬롯 아이콘 색상")]
    [SerializeField] private Color filledIconColor = Color.white;

    [Tooltip("잠김 슬롯 아이콘 색상")]
    [SerializeField] private Color lockedIconColor = new Color(1f, 1f, 1f, 0.35f);

    private SlotState currentState = SlotState.Empty;

    /// <summary>
    /// 빈 슬롯으로 표시
    /// </summary>
    public void SetEmpty(string label = "")
    {
        currentState = SlotState.Empty;
        ApplyState(null, label, string.Empty);
    }

    /// <summary>
    /// 자리 표시 슬롯으로 표시
    /// </summary>
    public void SetPlaceholder(string label = "")
    {
        currentState = SlotState.Placeholder;
        ApplyState(null, label, string.Empty);
    }

    /// <summary>
    /// 자리 표시 슬롯으로 표시 (호환 오버로드)
    /// </summary>
    public void SetPlaceholder(Sprite iconSprite, string label)
    {
        currentState = SlotState.Placeholder;
        ApplyState(iconSprite, label, string.Empty);
    }

    /// <summary>
    /// 잠김 슬롯으로 표시
    /// </summary>
    public void SetLocked(string label = "")
    {
        currentState = SlotState.Locked;
        ApplyState(null, label, string.Empty);
    }

    /// <summary>
    /// 실제 스킬 슬롯으로 표시
    /// </summary>
    public void SetSkill(Sprite iconSprite, string label = "")
    {
        currentState = SlotState.Filled;
        ApplyState(iconSprite, label, string.Empty);
    }

    /// <summary>
    /// 실제 스킬 슬롯으로 표시 (호환 오버로드)
    /// </summary>
    public void SetSkill(Sprite iconSprite, string label, string subLabel)
    {
        currentState = SlotState.Filled;
        ApplyState(iconSprite, label, subLabel);
    }

    /// <summary>
    /// 이전 답변에서 제시한 이름도 같이 지원
    /// </summary>
    public void Set_Empty(string label = "")
    {
        SetEmpty(label);
    }

    /// <summary>
    /// 이전 답변에서 제시한 이름도 같이 지원
    /// </summary>
    public void Set_Placeholder(string label = "")
    {
        SetPlaceholder(label);
    }

    /// <summary>
    /// 이전 답변에서 제시한 이름도 같이 지원
    /// </summary>
    public void Set_Placeholder(Sprite iconSprite, string label)
    {
        SetPlaceholder(iconSprite, label);
    }

    /// <summary>
    /// 이전 답변에서 제시한 이름도 같이 지원
    /// </summary>
    public void Set_Filled(Sprite iconSprite, string label = "")
    {
        SetSkill(iconSprite, label);
    }

    /// <summary>
    /// 이전 답변에서 제시한 이름도 같이 지원
    /// </summary>
    public void Set_Filled(Sprite iconSprite, string label, string subLabel)
    {
        SetSkill(iconSprite, label, subLabel);
    }

    /// <summary>
    /// 슬롯 상태를 실제 UI에 반영
    /// </summary>
    private void ApplyState(Sprite iconSprite, string label, string subLabel)
    {
        if (labelText != null)
        {
            labelText.text = label;
        }

        if (subLabelText != null)
        {
            subLabelText.text = subLabel;
        }

        switch (currentState)
        {
            case SlotState.Empty:
            {
                ApplyFrame(emptyFrameSprite);
                ApplyIcon(null, emptyIconColor, false);
                break;
            }

            case SlotState.Placeholder:
            {
                ApplyFrame(emptyFrameSprite);
                ApplyIcon(iconSprite, emptyIconColor, iconSprite != null);
                break;
            }

            case SlotState.Locked:
            {
                ApplyFrame(lockedFrameSprite);
                ApplyIcon(null, lockedIconColor, false);
                break;
            }

            case SlotState.Filled:
            {
                ApplyFrame(filledFrameSprite);
                ApplyIcon(iconSprite, filledIconColor, iconSprite != null);
                break;
            }
        }
    }

    /// <summary>
    /// 프레임 갱신
    /// </summary>
    private void ApplyFrame(Sprite sprite)
    {
        if (frameImage == null) return;
        frameImage.sprite = sprite;
    }

    /// <summary>
    /// 아이콘 갱신
    /// </summary>
    private void ApplyIcon(Sprite sprite, Color color, bool enabledState)
    {
        if (iconImage == null) return;

        iconImage.sprite = sprite;
        iconImage.color = color;
        iconImage.enabled = enabledState;
    }
}