using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class LevelUpCardView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;

    [Header("옵션 UI(있으면 연결)")]
    [SerializeField] private TMP_Text tagText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image frameImage;

    private bool _pickedOnce;
    private Action _onPicked;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        _pickedOnce = false;
        if (button != null) button.interactable = true;
    }

    public void BindWeaponUpgradeCard(WeaponUpgradeCardSO card, int currentLevel, bool showLevel, Action onPicked)
    {
        _pickedOnce = false;
        _onPicked = onPicked;

        if (titleText != null) titleText.text = (card != null) ? card.GetTitleForUI() : "";
        if (descText != null) descText.text = (card != null) ? card.GetDescriptionForUI() : "";

        if (iconImage != null)
        {
            bool hasIcon = (card != null && card.icon != null);
            iconImage.enabled = hasIcon;
            iconImage.sprite = hasIcon ? card.icon : null;
        }

        if (tagText != null)
        {
            tagText.text = (card != null) ? card.GetTagsForUI() : "";
        }

        if (levelText != null)
        {
            if (showLevel)
            {
                int cur = Mathf.Max(1, currentLevel);
                levelText.text = $"Lv {cur} → {cur + 1}";
            }
            else
            {
                levelText.text = "";
            }
        }

        if (frameImage != null)
        {
            // 필요하면 프레임 색/희귀도 규칙 추가
        }

        if (button != null)
            button.interactable = (card != null);
    }

    private void OnClick()
    {
        if (_pickedOnce) return;
        if (_onPicked == null) return;

        _pickedOnce = true;
        if (button != null) button.interactable = false;

        _onPicked.Invoke();
    }
}
