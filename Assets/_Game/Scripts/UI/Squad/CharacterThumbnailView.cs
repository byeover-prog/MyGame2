using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterThumbnailView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;

    [Header("상태 표현(선택)")]
    [SerializeField] private GameObject selectedOverlay;
    [SerializeField] private CanvasGroup canvasGroup;

    public Button Button => button;
    public CharacterDefinitionSO Bound { get; private set; }

    public void Bind(CharacterDefinitionSO def)
    {
        Bound = def;

        if (portraitImage != null) portraitImage.sprite = def != null ? def.Portrait : null;
        if (nameText != null) nameText.text = def != null
            ? (string.IsNullOrWhiteSpace(def.DisplayName) ? def.name : def.DisplayName)
            : "-";

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