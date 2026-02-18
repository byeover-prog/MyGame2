using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class SkillCardView : MonoBehaviour
{
    [Header("UI 레퍼런스")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI tagText;

    [Header("선택 강조(없어도 동작)")]
    [SerializeField] private GameObject selectedOutline;

    [Header("클릭 버튼(없으면 자동 탐색)")]
    [SerializeField] private Button button;

    // 현재 카드에 들어있는 데이터 (컨트롤러가 조회)
    public string CurrentId { get; private set; }

    // 컨트롤러가 구독할 클릭 콜백
    private System.Action<SkillCardView> onClicked;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }

        // 기본은 선택 해제
        SetSelected(false);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        onClicked?.Invoke(this);
    }

    // 카드 표시 데이터 주입
    public void SetData(string id, string displayName, string description, string tag, Sprite icon)
    {
        CurrentId = id;

        if (nameText != null) nameText.text = displayName;
        if (descText != null) descText.text = description;
        if (tagText != null) tagText.text = tag;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = (icon != null); // 아이콘 없으면 숨김
        }

        SetSelected(false);
    }

    // 컨트롤러가 클릭 이벤트를 연결
    public void BindClick(System.Action<SkillCardView> onClick)
    {
        onClicked = onClick;
    }

    // 선택 강조 표시
    public void SetSelected(bool isSelected)
    {
        if (selectedOutline != null)
            selectedOutline.SetActive(isSelected);
    }
}