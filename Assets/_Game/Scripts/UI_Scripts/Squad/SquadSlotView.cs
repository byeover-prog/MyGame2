using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 편성 화면 상단의 "큰 슬롯" 뷰.
/// 큰 초상화 + 캐릭터 이름 + 속성 + 기본/궁극 스킬 아이콘을 표시한다.
/// </summary>
public sealed class SquadSlotView : MonoBehaviour
{
    // ─── 슬롯 종류 ───
    public enum SlotKind { Support1, Main, Support2 }

    [Header("슬롯 종류")]
    [SerializeField] private SlotKind kind = SlotKind.Main;
    public SlotKind Kind => kind;

    // ─── UI 요소 (큰 슬롯용) ───
    [Header("UI — 큰 초상화 슬롯")]
    [SerializeField] private Button button;
    [SerializeField] private Button clearButton;

    [Header("캐릭터 표시")]
    [SerializeField] private Image portraitImage;          // 큰 초상화
    [SerializeField] private TMP_Text nameText;            // 캐릭터 이름
    [SerializeField] private TMP_Text attributeText;       // 속성(물리/빙결/화염 등)

    [Header("스킬 아이콘")]
    [SerializeField] private Image basicSkillIconImage;    // 기본 스킬 아이콘
    [SerializeField] private Image ultimateSkillIconImage; // 궁극기 아이콘

    [Header("상태 표시")]
    [SerializeField] private GameObject selectedOutline;   // 현재 선택된 슬롯 강조
    [SerializeField] private GameObject emptyLabel;        // "빈 슬롯" 표시용

    // ─── 외부 접근 ───
    public Button Button => button;

    private CharacterDefinitionSO _current;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
    }

    // ─── 캐릭터 배치 ───
    public void SetCharacter(CharacterDefinitionSO def)
    {
        _current = def;

        if (portraitImage != null)
        {
            portraitImage.sprite = def != null ? def.Portrait : null;
            portraitImage.enabled = def != null && def.Portrait != null;
        }

        if (nameText != null)
            nameText.text = def != null ? def.DisplayName : "";

        if (attributeText != null)
            attributeText.text = def != null ? def.Attribute.ToString() : "";

        // 기본 스킬 아이콘
        if (basicSkillIconImage != null)
        {
            basicSkillIconImage.sprite = def != null ? def.BasicSkillIcon : null;
            basicSkillIconImage.enabled = basicSkillIconImage.sprite != null;
        }

        // 궁극기 아이콘
        if (ultimateSkillIconImage != null)
        {
            ultimateSkillIconImage.sprite = def != null ? def.UltimateSkillIcon : null;
            ultimateSkillIconImage.enabled = ultimateSkillIconImage.sprite != null;
        }

        // 빈 슬롯 라벨
        if (emptyLabel != null)
            emptyLabel.SetActive(def == null);

        // 클리어 버튼은 캐릭터가 있을 때만
        if (clearButton != null)
            clearButton.gameObject.SetActive(def != null);
    }

    // ─── 빈 슬롯으로 초기화 ───
    public void SetEmpty()
    {
        SetCharacter(null);
    }

    // ─── 선택 강조 ───
    public void SetSelected(bool selected)
    {
        if (selectedOutline != null)
            selectedOutline.SetActive(selected);
    }

    // ─── 현재 배치된 캐릭터 ───
    public CharacterDefinitionSO Current => _current;
}