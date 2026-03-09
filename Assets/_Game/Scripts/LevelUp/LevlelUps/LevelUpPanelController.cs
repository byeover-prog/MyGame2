using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// [참고] 슬롯 상한/시간 정지 필드는 LevelUpOrchestrator로 이관 예정. 현재 미사용이지만 인스펙터 설정값 보존용.
#pragma warning disable CS0414
public sealed class LevelUpPanelController : MonoBehaviour
{
    [Header("Common Skills")]
    [SerializeField] private CommonSkillCatalogSO commonSkillCatalog;
    [SerializeField] private CommonSkillManager2D commonSkillManager;

    [Header("Passives")]
    [SerializeField] private PassiveCatalogSO passiveCatalog;
    [SerializeField] private PassiveManager2D passiveManager;

    [Header("Slot Limits")]
    [SerializeField] private int maxSkillSlots = 8;
    [SerializeField] private int maxPassiveSlots = 8;
    [SerializeField] private bool useTotalSlotCap = true;
    [SerializeField] private int maxTotalSlots = 16;

    [Header("Panel Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Cards")]
    [SerializeField] private SkillCardView cardA;
    [SerializeField] private SkillCardView cardB;
    [SerializeField] private SkillCardView cardC;

    [Header("Time")]
    [SerializeField] private bool pauseTimeWhenOpen = true;

    [Header("Reroll")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollCountText;
    [SerializeField, Min(0)] private int rerollMaxCount = 100;

    private int _rerollRemaining;
    private Func<(LevelUpChoice a, LevelUpChoice b, LevelUpChoice c)> _onRerollRequest;

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
        if (_isOpen)
        {
            Debug.LogWarning("[LevelUpPanelController] Already open, ignoring duplicate Open() call.", this);
            return;
        }

        _a = a;
        _b = b;
        _c = c;
        _onPick = onPick;
        _onRerollRequest = onRerollRequest;

        _rerollRemaining = rerollMaxCount;
        RefreshRerollUI();

        _isOpen = true;

        // 시간 정지는 LevelUpOrchestrator에서만 처리한다.
        // (이 패널 컨트롤러는 구 구조이며, 프로토타입에서는 Disable/대체 대상)

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
        // 시간 정지는 LevelUpOrchestrator에서만 처리한다.
        _didPause = false;
    }

    private void RefreshRerollUI()
    {
        if (rerollCountText != null)
            rerollCountText.text = $"Reroll {_rerollRemaining}/{rerollMaxCount}";

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
            card.SetData("", "EMPTY", "No upgrades available.", "", null);
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
#pragma warning restore CS0414