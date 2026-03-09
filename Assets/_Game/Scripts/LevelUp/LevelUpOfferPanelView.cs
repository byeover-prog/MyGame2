using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨업 UI 표시/입력만 담당(SRP).
/// - OffersReady를 받으면 패널을 열고 3장 카드에 바인딩
/// - 카드 클릭 시 GameSignals.RaiseOfferPicked(offer)
/// - 리롤 클릭 시 GameSignals.RaiseRerollRequested()
/// </summary>
[DisallowMultipleComponent]
public sealed class LevelUpOfferPanelView : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject panelRoot;

    [Header("카드(3장 고정)")]
    [SerializeField] private SkillCardView[] cardViews = new SkillCardView[3];

    [Header("리롤")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollCountText;
    [SerializeField, Min(0)] private int rerollMaxCount = 1;

    [Header("디버그")]
    [SerializeField] private bool enableLogs = false;

    private Offer[] _currentOffers = Array.Empty<Offer>();
    private int _rerollsLeft;

    private void Awake()
    {
        // 패널을 껐다 켜도, 이 스크립트가 붙은 오브젝트는 살아있어야(=활성) 이벤트를 받는다.
        // 따라서 panelRoot는 보통 자식 오브젝트(패널 루트)를 지정하는 것을 권장.
        if (panelRoot == null && transform.childCount > 0)
            panelRoot = transform.GetChild(0).gameObject;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        _rerollsLeft = rerollMaxCount;
        UpdateRerollUI();

        // 카드 클릭 바인딩
        if (cardViews != null)
        {
            for (int i = 0; i < cardViews.Length; i++)
            {
                if (cardViews[i] == null) continue;
                cardViews[i].BindClick(OnCardClicked);
            }
        }

        if (rerollButton != null)
            rerollButton.onClick.AddListener(OnRerollClicked);
    }

    private void OnEnable()
    {
        GameSignals.OffersReady += HandleOffersReady;
        GameSignals.LevelUpClosed += HandleClosed;
    }

    private void OnDisable()
    {
        GameSignals.OffersReady -= HandleOffersReady;
        GameSignals.LevelUpClosed -= HandleClosed;
    }

    private void HandleOffersReady(Offer[] offers)
    {
        _currentOffers = offers ?? Array.Empty<Offer>();
        _rerollsLeft = rerollMaxCount;
        UpdateRerollUI();

        if (_currentOffers.Length <= 0)
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            return;
        }

        if (panelRoot != null) panelRoot.SetActive(true);

        // 3장 고정: 부족하면 남는 카드는 비활성
        for (int i = 0; i < cardViews.Length; i++)
        {
            var view = cardViews[i];
            if (view == null) continue;

            if (i < _currentOffers.Length)
            {
                var o = _currentOffers[i];
                view.gameObject.SetActive(true);
                view.SetData(o.id, o.titleKr, o.descKr, o.tagKr, o.icon);
            }
            else
            {
                view.gameObject.SetActive(false);
            }
        }

        if (enableLogs)
            Debug.Log($"[LevelUpUI] Show {_currentOffers.Length} offers", this);
    }

    private void HandleClosed()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        _currentOffers = Array.Empty<Offer>();
    }

    private void OnCardClicked(SkillCardView view)
    {
        if (view == null) return;
        if (_currentOffers == null || _currentOffers.Length <= 0) return;

        int idx = IndexOfView(view);
        if (idx < 0 || idx >= _currentOffers.Length) return;

        var picked = _currentOffers[idx];
        GameSignals.RaiseOfferPicked(picked);
    }

    private int IndexOfView(SkillCardView view)
    {
        if (cardViews == null) return -1;
        for (int i = 0; i < cardViews.Length; i++)
        {
            if (cardViews[i] == view) return i;
        }
        return -1;
    }

    private void OnRerollClicked()
    {
        if (_rerollsLeft <= 0)
            return;

        _rerollsLeft--;
        UpdateRerollUI();
        GameSignals.RaiseRerollRequested();

        if (enableLogs)
            Debug.Log($"[LevelUpUI] Reroll => left {_rerollsLeft}", this);
    }

    private void UpdateRerollUI()
    {
        if (rerollCountText != null)
            rerollCountText.text = _rerollsLeft.ToString();

        if (rerollButton != null)
            rerollButton.interactable = _rerollsLeft > 0;
    }
}
