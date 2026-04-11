using UnityEngine;
using UnityEngine.UIElements;
using _Game.Skills;
using _Game.Player;

[RequireComponent(typeof(UIDocument))]
public sealed class LevelUpUIController : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private PlayerSkillLoadout skillLoadout;

    // 카드 수
    private const int CARD_COUNT = 4;

    // 현재 표시 중인 카드 후보
    private SkillDefinitionSO[] _candidates = new SkillDefinitionSO[CARD_COUNT];

    // UI 요소 캐시
    private VisualElement   root;
    private VisualElement   levelUpRoot;
    private VisualElement[] cardEls   = new VisualElement[CARD_COUNT];
    private Label[]         cardNames = new Label[CARD_COUNT];
    private Label[]         cardDescs = new Label[CARD_COUNT];
    private Label[]         cardStats = new Label[CARD_COUNT];
    private VisualElement[] cardIcons = new VisualElement[CARD_COUNT];
    private Button          btnMinimize;
    private Button          btnReroll;

    void OnEnable()
    {
        root       = GetComponent<UIDocument>().rootVisualElement;
        levelUpRoot = root.Q<VisualElement>("LevelUpRoot");

        for (int i = 0; i < CARD_COUNT; i++)
        {
            int idx = i; // 클로저 캡처용
            cardEls[i]   = root.Q<VisualElement>($"Card{i}");
            cardNames[i] = root.Q<Label>($"Card{i}Name");
            cardDescs[i] = root.Q<Label>($"Card{i}Desc");
            cardStats[i] = root.Q<Label>($"Card{i}Stat");
            cardIcons[i] = root.Q<VisualElement>($"Card{i}Icon");

            // 카드 클릭 이벤트
            cardEls[i]?.RegisterCallback<ClickEvent>(_ => OnCardClicked(idx));
        }

        btnMinimize = root.Q<Button>("BtnMinimize");
        btnReroll   = root.Q<Button>("BtnReroll");

        btnMinimize?.RegisterCallback<ClickEvent>(_ => Hide());
        btnReroll?.RegisterCallback<ClickEvent>(_ => Reroll());

        // 시작 시 숨김
        Hide();
    }

    // 외부 호출 API
    // 레벨업 창을 열고 후보 스킬을 표시합니다.
    // LevelSystem 쪽에서 호출하세요.
    public void Show(SkillDefinitionSO[] candidates)
    {
        if (levelUpRoot == null) return;

        // 후보 저장
        for (int i = 0; i < CARD_COUNT; i++)
            _candidates[i] = i < candidates.Length ? candidates[i] : null;

        RefreshCards();
        levelUpRoot.style.display = DisplayStyle.Flex;
        Time.timeScale = 0f; // 일시정지
    }
    
    public void Hide()
    {
        if (levelUpRoot == null) return;
        levelUpRoot.style.display = DisplayStyle.None;
        GamePauseGate2D.Release(this);
    }

    // 카드 갱신
    private void RefreshCards()
    {
        for (int i = 0; i < CARD_COUNT; i++)
        {
            var skill = _candidates[i];
            bool hasSkill = skill != null;

            if (cardEls[i] != null)
                cardEls[i].style.display = hasSkill
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            if (!hasSkill) continue;

            // 이름
            if (cardNames[i] != null)
                cardNames[i].text = skill.DisplayName;

            // 설명
            if (cardDescs[i] != null)
            {
                string desc = skillLoadout != null
                    ? skillLoadout.BuildCardDescription(skill)
                    : skill.GetDescriptionForLevel(1);
                cardDescs[i].text = desc;
            }

            // 수치 텍스트 (SO에 있으면 사용)
            if (cardStats[i] != null)
                cardStats[i].text = ""; // 필요 시 스탯 표시

            // 아이콘 (Sprite 할당)
            if (cardIcons[i] != null && skill.Icon != null)
                cardIcons[i].style.backgroundImage =
                    new StyleBackground(skill.Icon);
        }
    }

    // 카드 선택
    private void OnCardClicked(int index)
    {
        var skill = _candidates[index];
        if (skill == null || skillLoadout == null) return;

        // 이미 보유 -> 레벨업 / 미보유 -> 신규 추가
        if (skillLoadout.HasSkill(skill.SkillId))
            skillLoadout.TryUpgradeSkill(skill.SkillId);
        else
            skillLoadout.TryAddSkill(skill);

        GameLogger.Log($"[LevelUpUI] 선택: {skill.DisplayName}");
        Hide();
    }

    // 새로고침
    private void Reroll()
    {
        // TODO: 새로고침 비용 차감 후 후보 재추첨
        // 지금은 카드만 셔플
        GameLogger.Log("[LevelUpUI] 새로고침");
    }
}