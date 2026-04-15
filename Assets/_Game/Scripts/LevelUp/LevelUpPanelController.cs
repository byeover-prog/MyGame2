using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Game.LevelUp;
using _Game.Player;

public class LevelUpPanelController : MonoBehaviour
{
    [Header("카드 슬롯")]
    [SerializeField] private List<SkillCardUI> cardSlots;

    [Header("연결")]
    [SerializeField] private LevelUpRewardApplier rewardApplier;
    [SerializeField] private LevelUpFlowCoordinator flowCoordinator;
    [SerializeField] private LevelUpCardGenerator cardGenerator;
    [SerializeField] private PlayerSkillLoadout loadout;

    [Header("패널")]
    [SerializeField] private GameObject panelRoot;

    [Header("최소화")]
    [SerializeField] private Button btnMinimize;
    [SerializeField] private GameObject cardArea;      // CardRow + 타이틀 등 숨길 영역
    [SerializeField] private TextMeshProUGUI txtMinimize;

    [Header("새로고침")]
    [SerializeField] private Button btnReroll;
    [SerializeField] private TextMeshProUGUI txtReroll;
    [SerializeField, Min(0)] private int rerollMaxCount = 3;

    private List<LevelUpCardData> _currentCards;
    private bool _isMinimized;
    private int _rerollsLeft;

    public bool IsOpen { get; private set; }

    public void Open(List<LevelUpCardData> cards)
    {
        if (cards == null || cards.Count == 0) return;

        _currentCards = cards;
        IsOpen = true;
        _rerollsLeft = rerollMaxCount;
        _isMinimized = false;

        panelRoot.SetActive(true);
        if (cardArea != null) cardArea.SetActive(true);
        if (txtMinimize != null) txtMinimize.text = "최소화";

        BindCards(cards);
        UpdateRerollUI();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        panelRoot.SetActive(false);
        flowCoordinator?.NotifyPanelClosed();
    }

    private void Start()
    {
        btnMinimize?.onClick.AddListener(OnMinimizeClicked);
        btnReroll?.onClick.AddListener(OnRerollClicked);
    }

    private void BindCards(List<LevelUpCardData> cards)
    {
        for (int i = 0; i < cardSlots.Count; i++)
        {
            bool hasCard = i < cards.Count;
            cardSlots[i].gameObject.SetActive(hasCard);
            if (hasCard)
                cardSlots[i].Setup(cards[i], OnCardSelected);
        }
    }

    private void OnCardSelected(LevelUpCardData data)
    {
        bool applied = rewardApplier != null && rewardApplier.Apply(data);
        if (applied) Close();
    }

    private void OnMinimizeClicked()
    {
        _isMinimized = !_isMinimized;
        if (cardArea != null) cardArea.SetActive(!_isMinimized);
        if (txtMinimize != null) txtMinimize.text = _isMinimized ? "▲ 스킬 선택" : "최소화";
    }

    private void OnRerollClicked()
    {
        if (_rerollsLeft <= 0) return;
        if (cardGenerator == null || loadout == null) return;

        _rerollsLeft--;
        cardGenerator.NotifyReroll();

        var newCards = cardGenerator.Generate(loadout);
        if (newCards == null || newCards.Count == 0)
        {
            UpdateRerollUI();
            return;
        }

        _currentCards = newCards;
        BindCards(newCards);
        UpdateRerollUI();
    }

    private void UpdateRerollUI()
    {
        if (btnReroll != null)
            btnReroll.interactable = _rerollsLeft > 0;
        if (txtReroll != null)
            txtReroll.text = $"새로고침 ({_rerollsLeft})";
    }
}