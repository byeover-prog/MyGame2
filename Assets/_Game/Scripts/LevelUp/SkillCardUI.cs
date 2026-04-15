using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using _Game.LevelUp;

public class SkillCardUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI text_Name;
    [SerializeField] private TextMeshProUGUI text_Tag;
    [SerializeField] private TextMeshProUGUI text_Desc;
    [SerializeField] private Button button;

    private LevelUpCardData _data;
    private Action<LevelUpCardData> _onSelected;

    public void Setup(LevelUpCardData data, Action<LevelUpCardData> onSelected)
    {
        _data = data;
        _onSelected = onSelected;

        text_Name.text = data.Title ?? "";
        text_Tag.text  = data.Tag ?? "";
        text_Desc.text = data.Description ?? "";

        if (icon != null && data.Icon != null)
            icon.sprite = data.Icon;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => _onSelected?.Invoke(_data));
    }
}