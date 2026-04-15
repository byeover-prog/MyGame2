using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Game.LevelUp;
using _Game.Player;
using DG.Tweening;

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
    [SerializeField] private GameObject dim;
    [SerializeField] private Image panelBG;

    [Header("새로고침")]
    [SerializeField] private Button btnReroll;
    [SerializeField] private TextMeshProUGUI txtReroll;
    [SerializeField, Min(0)] private int rerollMaxCount = 3;
    
    [Header("버튼 위치")]
    [SerializeField] private RectTransform btnMinimizeRect;
    [SerializeField] private RectTransform btnRerollRect;
    [SerializeField] private float btnMinimizedY = 300f;
    [SerializeField] private HoverMoveUI hoverMinimize;
    [SerializeField] private HoverMoveUI hoverReroll;
    
    private Vector2 _btnMinimizeOrigin;
    private Vector2 _btnRerollOrigin;

    private List<LevelUpCardData> _currentCards;
    private bool _isMinimized;
    private int _rerollsLeft;

    public bool IsOpen { get; private set; }

    private bool _originSaved = false;
    
    public void Open(List<LevelUpCardData> cards)
    {
        if (cards == null || cards.Count == 0) return;

        panelRoot.SetActive(true);

        // 첫 Open 시 위치 저장
        if (!_originSaved)
        {
            _btnMinimizeOrigin = btnMinimizeRect.anchoredPosition;
            _btnRerollOrigin   = btnRerollRect.anchoredPosition;
            _originSaved = true;
        }
        
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
        if (dim != null) dim.SetActive(!_isMinimized);
        if (txtMinimize != null) txtMinimize.text = _isMinimized ? "▲ 스킬 선택" : "최소화";

        if (_isMinimized)
        {
            if (hoverMinimize != null) hoverMinimize.enabled = false;
            if (hoverReroll != null) hoverReroll.enabled = false;
    
            btnMinimizeRect?.DOAnchorPosY(btnMinimizedY, 0.2f)
                .SetUpdate(true)
                .OnComplete(() => {
                    if (hoverMinimize != null)
                    {
                        hoverMinimize.UpdateOrigin();
                        hoverMinimize.enabled = true;
                    }
                });
            btnRerollRect?.DOAnchorPosY(btnMinimizedY, 0.2f)
                .SetUpdate(true)
                .OnComplete(() => {
                    if (hoverReroll != null)
                    {
                        hoverReroll.UpdateOrigin();
                        hoverReroll.enabled = true;
                    }
                });
            var c = panelBG.color;
            c.a = 0f;
            panelBG.color = c;
        }
        else
        {
            if (hoverMinimize != null) hoverMinimize.enabled = false;
            if (hoverReroll != null) hoverReroll.enabled = false;

            btnMinimizeRect?.DOAnchorPosY(_btnMinimizeOrigin.y, 0.2f)
                .SetUpdate(true)
                .OnComplete(() => {
                    if (hoverMinimize != null)
                    {
                        hoverMinimize.UpdateOrigin();
                        hoverMinimize.enabled = true;
                    }
                });
            btnRerollRect?.DOAnchorPosY(_btnRerollOrigin.y, 0.2f)
                .SetUpdate(true)
                .OnComplete(() => {
                    if (hoverReroll != null)
                    {
                        hoverReroll.UpdateOrigin();
                        hoverReroll.enabled = true;
                    }
                });
            var c = panelBG.color;
            c.a = 150f / 255f;
            panelBG.color = c;
        }
        
        if (btnReroll != null)
        {
            btnReroll.interactable = !_isMinimized;
            if (txtReroll != null)
            {
                var c = txtReroll.color;
                c.a = _isMinimized ? 0.3f : 1f;
                txtReroll.color = c;
            }
        }
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