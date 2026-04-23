using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 편성 화면 하단 "보유 캐릭터 목록"의 소형 카드.
/// 작은 썸네일 + 이름만 표시. 클릭하면 위쪽 편성 슬롯에 배치된다.
/// </summary>
public sealed class CharacterCardView : MonoBehaviour
{
    [Header("UI — 소형 썸네일 카드")]
    [SerializeField] private Button button;
    [SerializeField] private Image thumbnailImage;   // 작은 썸네일(아이콘 크기)
    [SerializeField] private TMP_Text nameText;      // 캐릭터 이름

    [Header("상태 표시")]
    [SerializeField] private GameObject selectedMark;    // 이미 편성된 캐릭터 표시
    [SerializeField] private GameObject disabledOverlay; // 선택 불가 오버레이

    private CharacterDefinitionSO _data;
    public CharacterDefinitionSO Data => _data;

    private System.Action<CharacterDefinitionSO> _onClick;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(HandleClick);
    }

    // ─── 데이터 바인딩 ───
    public void Bind(CharacterDefinitionSO data, System.Action<CharacterDefinitionSO> onClick)
    {
        _data = data;
        _onClick = onClick;

        // 소형 카드: 썸네일 + 이름만
        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = data != null ? data.Portrait : null;
            thumbnailImage.enabled = data != null && data.Portrait != null;
        }

        if (nameText != null)
            nameText.text = data != null ? data.DisplayName : "";
    }

    // ─── 상태 갱신 (편성 여부 등) ───
    public void SetState(bool selected, bool disabled)
    {
        if (selectedMark != null) selectedMark.SetActive(selected);
        if (disabledOverlay != null) disabledOverlay.SetActive(disabled);
        if (button != null) button.interactable = !disabled;
    }

    private void HandleClick()
    {
        if (_data == null) return;
        _onClick?.Invoke(_data);
    }
}