using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class LevelUpPanelController : MonoBehaviour
{
    [Header("공통 스킬(11 스킬 중 8개)")]
    [SerializeField] private CommonSkillCatalogSO commonSkillCatalog;
    [SerializeField] private CommonSkillManager2D commonSkillManager;

    [Header("패시브(8개)")]
    [SerializeField] private PassiveCatalogSO passiveCatalog;
    [SerializeField] private PassiveManager2D passiveManager;

    [Header("슬롯 제한(결정 전이니 인스펙터로 바꿀 수 있게)")]
    [SerializeField] private int maxSkillSlots = 8;
    [SerializeField] private int maxPassiveSlots = 8;
    [SerializeField] private bool useTotalSlotCap = true;
    [SerializeField] private int maxTotalSlots = 16;

    [Header("패널 루트(없으면 자기 자신)")]
    [SerializeField] private GameObject panelRoot;

    [Header("카드 3장")]
    [SerializeField] private SkillCardView cardA;
    [SerializeField] private SkillCardView cardB;
    [SerializeField] private SkillCardView cardC;

    [Header("시간 정지")]
    [SerializeField] private bool pauseTimeWhenOpen = true;

    [Header("리롤(새로고침)")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollCountText;
    [SerializeField, Min(0)] private int rerollMaxCount = 100;

    private int _rerollRemaining;
    private Func<(LevelUpChoice a, LevelUpChoice b, LevelUpChoice c)> _onRerollRequest;

    // TimeScale 복구 안전장치
    private float _prevTimeScale = 1f;
    private bool _didPause = false;

    private bool _isOpen;
    private LevelUpChoice _a;
    private LevelUpChoice _b;
    private LevelUpChoice _c;
    private Action<LevelUpChoice> _onPick;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;

        if (commonSkillManager == null) commonSkillManager = FindFirstObjectByType<CommonSkillManager2D>();
        if (passiveManager == null) passiveManager = FindFirstObjectByType<PassiveManager2D>();

        if (cardA != null) cardA.BindClick(OnCardClicked);
        if (cardB != null) cardB.BindClick(OnCardClicked);
        if (cardC != null) cardC.BindClick(OnCardClicked);

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(OnRerollClicked);
            rerollButton.onClick.AddListener(OnRerollClicked);
        }

        Close();
    }

    private void OnDestroy()
    {
        if (rerollButton != null)
            rerollButton.onClick.RemoveListener(OnRerollClicked);

        RestoreTimeScaleIfNeeded();
    }

    private void OnDisable()
    {
        RestoreTimeScaleIfNeeded();
    }

    public void Open(
        LevelUpChoice a,
        LevelUpChoice b,
        LevelUpChoice c,
        Action<LevelUpChoice> onPick,
        Func<(LevelUpChoice a, LevelUpChoice b, LevelUpChoice c)> onRerollRequest)
    {
        _a = a;
        _b = b;
        _c = c;
        _onPick = onPick;
        _onRerollRequest = onRerollRequest;

        _rerollRemaining = rerollMaxCount;
        RefreshRerollUI();

        _isOpen = true;

        if (pauseTimeWhenOpen)
        {
            // 이미 0이면 “이전 값”이 0으로 덮여 영원히 복구 못 하는 사고 방지
            _prevTimeScale = Time.timeScale;
            if (_prevTimeScale <= 0f) _prevTimeScale = 1f;

            Time.timeScale = 0f;
            _didPause = true;
        }

        if (panelRoot != null) panelRoot.SetActive(true);

        ApplyToCard(cardA, _a);
        ApplyToCard(cardB, _b);
        ApplyToCard(cardC, _c);

        if (cardA != null) cardA.SetSelected(false);
        if (cardB != null) cardB.SetSelected(false);
        if (cardC != null) cardC.SetSelected(false);
    }

    public void Close()
    {
        _isOpen = false;

        RestoreTimeScaleIfNeeded();
        if (panelRoot != null) panelRoot.SetActive(false);

        _a = null;
        _b = null;
        _c = null;
        _onPick = null;

        _onRerollRequest = null;
        _rerollRemaining = 0;
        RefreshRerollUI();
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (!pauseTimeWhenOpen) return;
        if (!_didPause) return;

        Time.timeScale = (_prevTimeScale <= 0f) ? 1f : _prevTimeScale;
        _didPause = false;
    }

    private void RefreshRerollUI()
    {
        if (rerollCountText != null)
            rerollCountText.text = $"새로고침 {_rerollRemaining}/{rerollMaxCount}";

        if (rerollButton != null)
            rerollButton.interactable = (_isOpen && _rerollRemaining > 0 && _onRerollRequest != null);
    }

    private void OnRerollClicked()
    {
        if (!_isOpen) return;
        if (_rerollRemaining <= 0) return;
        if (_onRerollRequest == null) return;

        var tuple = _onRerollRequest.Invoke();
        if (tuple.a == null && tuple.b == null && tuple.c == null) return;

        _a = tuple.a;
        _b = tuple.b;
        _c = tuple.c;

        _rerollRemaining--;
        RefreshRerollUI();

        ApplyToCard(cardA, _a);
        ApplyToCard(cardB, _b);
        ApplyToCard(cardC, _c);

        if (cardA != null) cardA.SetSelected(false);
        if (cardB != null) cardB.SetSelected(false);
        if (cardC != null) cardC.SetSelected(false);
    }

    private void ApplyToCard(SkillCardView card, LevelUpChoice choice)
    {
        if (card == null) return;

        if (choice == null)
        {
            card.SetData("", "EMPTY", "선택 가능한 업그레이드가 없습니다.", "", null);
            return;
        }

        card.SetData(choice.Id, choice.Title, choice.Description, choice.Tag, choice.Icon);
    }

    public void OnClickReroll()
    {
        OnRerollClicked();
    }

    private void OnCardClicked(SkillCardView clicked)
    {
        if (!_isOpen) return;
        if (clicked == null) return;

        LevelUpChoice choice = null;

        if (clicked == cardA) choice = _a;
        else if (clicked == cardB) choice = _b;
        else if (clicked == cardC) choice = _c;

        if (choice == null) return;

        if (cardA != null) cardA.SetSelected(cardA == clicked);
        if (cardB != null) cardB.SetSelected(cardB == clicked);
        if (cardC != null) cardC.SetSelected(cardC == clicked);

        _onPick?.Invoke(choice);
        Close();
    }
}