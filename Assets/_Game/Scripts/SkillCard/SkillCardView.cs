using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public sealed class SkillCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI 레퍼런스")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI tagText;

    [Header("선택 강조(없어도 동작)")]
    [SerializeField] private GameObject selectedOutline;

    [Header("마우스 오버 강조(비우면 선택 강조를 재사용)")]
    [SerializeField] private GameObject hoverOutline;

    [Header("클릭 버튼(없으면 자동 탐색)")]
    [SerializeField] private Button button;

    [Header("동작 옵션")]
    [SerializeField, Tooltip("마우스를 올리면 카드가 강조됩니다.")]
    private bool enableHoverHighlight = true;

    // 현재 카드에 들어있는 데이터 (컨트롤러가 조회)
    public string CurrentId { get; private set; }

    // 컨트롤러가 구독할 클릭 콜백
    private System.Action<SkillCardView> onClicked;

    // 외부(컨트롤러)가 관리하는 '선택 상태'를 내부에 저장해서
    // Hover가 선택 표시를 덮어쓰지 않게 함.
    private bool _isSelected;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(HandleClick);

        // hoverOutline이 비어있으면 selectedOutline을 재사용(옵션)
        if (hoverOutline == null) hoverOutline = selectedOutline;

        // 기본은 선택/호버 해제
        ApplySelected(false);
        ApplyHover(false);
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
            iconImage.enabled = (icon != null);
        }

        // 데이터 교체 시 기본: 선택 해제
        SetSelected(false);
    }

    // 컨트롤러가 클릭 이벤트를 연결
    public void BindClick(System.Action<SkillCardView> onClick)
    {
        onClicked = onClick;
    }

    // 선택 강조 표시 (컨트롤러가 호출)
    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;
        ApplySelected(isSelected);

        // 선택 해제되면, 현재 마우스 오버 중이면 hover가 다시 보이게 할 수도 있지만
        // 여기서는 단순화: 선택이 우선.
        if (_isSelected)
            ApplyHover(false);
    }

    // 마우스 오버 반응
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enableHoverHighlight) return;
        if (_isSelected) return; // 선택이 우선
        ApplyHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!enableHoverHighlight) return;
        if (_isSelected) return;
        ApplyHover(false);
    }

    private void ApplySelected(bool on)
    {
        if (selectedOutline != null)
            selectedOutline.SetActive(on);
    }

    private void ApplyHover(bool on)
    {
        // hoverOutline이 selectedOutline과 동일하면, "선택이 아닐 때만" 켜지도록 위에서 제어함.
        if (hoverOutline != null)
            hoverOutline.SetActive(on);
    }
}