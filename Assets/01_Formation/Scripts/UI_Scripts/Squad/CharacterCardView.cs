using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterCardView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text attributeText;
    [SerializeField] private Image basicSkillIcon;
    [SerializeField] private Image ultimateSkillIcon;

    [Header("상태 표현")]
    [SerializeField] private GameObject selectedOverlay;
    [SerializeField] private CanvasGroup canvasGroup;

    public Button Button => button;

    public CharacterDefinitionSO Bound { get; private set; }

    public void Bind(CharacterDefinitionSO def)
    {
        Bound = def;

        if (portraitImage != null) portraitImage.sprite = def != null ? def.Portrait : null;
        if (nameText != null) nameText.text = def != null ? (string.IsNullOrWhiteSpace(def.DisplayName) ? def.name : def.DisplayName) : "-";
        if (attributeText != null) attributeText.text = def != null ? def.Attribute.ToKorean() : "-";

        if (basicSkillIcon != null)
        {
            basicSkillIcon.sprite = def != null ? def.BasicSkillIcon : null;
            basicSkillIcon.enabled = def != null && def.BasicSkillIcon != null;
        }

        if (ultimateSkillIcon != null)
        {
            ultimateSkillIcon.sprite = def != null ? def.UltimateSkillIcon : null;
            ultimateSkillIcon.enabled = def != null && def.UltimateSkillIcon != null;
        }

        SetSelected(false);
        SetInteractable(true);
    }

    public void SetSelected(bool selected)
    {
        if (selectedOverlay != null) selectedOverlay.SetActive(selected);
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null) button.interactable = interactable;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = interactable ? 1f : 0.35f;
            canvasGroup.blocksRaycasts = interactable;
        }
    }
}
