using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SquadSlotView : MonoBehaviour
{
    public enum SlotKind { Support1, Main, Support2 }

    [Header("슬롯 종류")]
    [SerializeField] private SlotKind slotKind;

    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image basicSkillIcon;
    [SerializeField] private Image ultimateSkillIcon;
    [SerializeField] private TMP_Text attributeText;

    [Header("빈 슬롯 표시")]
    [SerializeField] private Sprite emptyPortrait;
    [SerializeField] private string emptyName = "비어있음";

    public SlotKind Kind => slotKind;
    public Button Button => button;

    public void SetEmpty()
    {
        if (portraitImage != null) portraitImage.sprite = emptyPortrait;
        if (nameText != null) nameText.text = emptyName;

        if (basicSkillIcon != null) { basicSkillIcon.sprite = null; basicSkillIcon.enabled = false; }
        if (ultimateSkillIcon != null) { ultimateSkillIcon.sprite = null; ultimateSkillIcon.enabled = false; }
        if (attributeText != null) attributeText.text = "속성: -";
    }

    public void SetCharacter(CharacterDefinitionSO def)
    {
        if (def == null) { SetEmpty(); return; }

        if (portraitImage != null) { portraitImage.sprite = def.Portrait; }
        if (nameText != null) nameText.text = string.IsNullOrWhiteSpace(def.DisplayName) ? def.name : def.DisplayName;

        if (basicSkillIcon != null)
        {
            basicSkillIcon.sprite = def.BasicSkillIcon;
            basicSkillIcon.enabled = def.BasicSkillIcon != null;
        }

        if (ultimateSkillIcon != null)
        {
            ultimateSkillIcon.sprite = def.UltimateSkillIcon;
            ultimateSkillIcon.enabled = def.UltimateSkillIcon != null;
        }

        if (attributeText != null) attributeText.text = $"속성: {def.Attribute.ToKorean()}";
    }
}
