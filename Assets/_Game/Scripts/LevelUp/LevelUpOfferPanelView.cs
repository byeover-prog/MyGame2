// UTF-8
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Game.LevelUp;
using _Game.LevelUp.UI;

/// <summary>
/// Scene_Game의 기존 LevelUpPanel 구조를 그대로 사용하면서 4장 카드를 런타임 생성합니다.
/// - 현재 씬에 cardViews 직렬화가 전혀 안 들어가 있으므로 자동 생성 방식으로 고정
/// - SkillCard.prefab(LevelUpNewCardView)을 CardRow 아래에 4개 생성
/// - 레벨업창 표시/닫힘과 pause gate를 같이 처리
/// </summary>
[DisallowMultipleComponent]
public sealed class LevelUpOfferPanelView : MonoBehaviour
{
    [Header("루트")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform cardRowRoot;

    [Header("카드 프리팹")]
    [SerializeField] private LevelUpNewCardView cardPrefab;
    [SerializeField, Min(1)] private int cardCount = 4;

    [Header("리롤")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text rerollCountText;
    [SerializeField, Min(0)] private int rerollMaxCount = 1;

    [Header("디버그")]
    [SerializeField] private bool enableLogs = true;

    private readonly List<LevelUpNewCardView> runtimeCards = new List<LevelUpNewCardView>(4);
    private Offer[] currentOffers = System.Array.Empty<Offer>();
    private int rerollsLeft;
    private bool clickLocked;
    private bool pauseAcquired;

    private void Awake()
    {
        if (panelRoot == null)
        {
            Transform found = transform.Find("LevelUpPanel");
            if (found != null)
                panelRoot = found.gameObject;
        }

        if (panelRoot != null && cardRowRoot == null)
        {
            Transform row = panelRoot.transform.Find("CardRow");
            if (row != null)
                cardRowRoot = row as RectTransform;
        }

        if (panelRoot != null && rerollButton == null)
        {
            Transform reroll = panelRoot.transform.Find("Rellol");
            if (reroll != null)
                rerollButton = reroll.GetComponent<Button>();
        }

        if (panelRoot != null && rerollCountText == null && rerollButton != null)
            rerollCountText = rerollButton.GetComponentInChildren<TMP_Text>(true);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (rerollButton != null)
            rerollButton.onClick.AddListener(OnRerollClicked);

        EnsureCardInstances();
        rerollsLeft = rerollMaxCount;
        UpdateRerollUI();
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
        ReleasePause();
    }

    private void OnDestroy()
    {
        if (rerollButton != null)
            rerollButton.onClick.RemoveListener(OnRerollClicked);
    }

    private void EnsureCardInstances()
    {
        if (cardRowRoot == null)
        {
            Debug.LogError("[LevelUpUI] CardRow 참조가 없습니다. Scene_Game > LevelUpPanel > CardRow를 확인하세요.", this);
            return;
        }

        if (cardPrefab == null)
        {
            Debug.LogError("[LevelUpUI] cardPrefab이 없습니다. Prefabs/Card_Prefab/SkillCard.prefab의 LevelUpNewCardView를 연결하세요.", this);
            return;
        }

        if (runtimeCards.Count > 0)
            return;

        for (int i = 0; i < cardCount; i++)
        {
            LevelUpNewCardView instance = Instantiate(cardPrefab, cardRowRoot);
            instance.name = $"RuntimeSkillCard_{i + 1:00}";
            instance.gameObject.SetActive(false);
            runtimeCards.Add(instance);
        }
    }

    private void HandleOffersReady(Offer[] offers)
    {
        currentOffers = offers ?? System.Array.Empty<Offer>();
        rerollsLeft = rerollMaxCount;
        clickLocked = false;

        if (currentOffers.Length <= 0)
        {
            HidePanel();
            return;
        }

        EnsureCardInstances();
        BindCards();
        ShowPanel();
        UpdateRerollUI();

        if (enableLogs)
            Debug.Log($"[LevelUpUI] Show => {currentOffers.Length}장", this);
    }

    private void HandleClosed()
    {
        currentOffers = System.Array.Empty<Offer>();
        clickLocked = false;
        HidePanel();
    }

    private void BindCards()
    {
        for (int i = 0; i < runtimeCards.Count; i++)
        {
            LevelUpNewCardView view = runtimeCards[i];
            if (view == null)
                continue;

            bool hasData = i < currentOffers.Length;
            view.gameObject.SetActive(hasData);

            if (!hasData)
                continue;

            Offer offer = currentOffers[i];
            LevelUpCardData cardData = new LevelUpCardData
            {
                Title = offer.titleKr,
                Description = offer.descKr,
                Tag = offer.tagKr,
                Icon = offer.icon
            };

            view.Bind(cardData, i, OnCardIndexClicked);
            view.SetInteractable(true);
            view.SetSelected(false);
        }
    }

    private void OnCardIndexClicked(int index)
    {
        if (clickLocked)
            return;

        if (index < 0 || index >= currentOffers.Length)
            return;

        clickLocked = true;
        SetCardInteractable(false);
        GameSignals.RaiseOfferPicked(currentOffers[index]);
    }

    private void OnRerollClicked()
    {
        if (clickLocked)
            return;

        if (rerollsLeft <= 0)
            return;

        rerollsLeft--;
        UpdateRerollUI();
        GameSignals.RaiseRerollRequested();

        if (enableLogs)
            Debug.Log($"[LevelUpUI] Reroll => left {rerollsLeft}", this);
    }

    private void SetCardInteractable(bool value)
    {
        for (int i = 0; i < runtimeCards.Count; i++)
        {
            if (runtimeCards[i] == null)
                continue;

            runtimeCards[i].SetInteractable(value);
        }
    }

    private void ShowPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        AcquirePause();
    }

    private void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        ReleasePause();
        UpdateRerollUI();
    }

    private void UpdateRerollUI()
    {
        if (rerollCountText != null)
            rerollCountText.text = rerollsLeft.ToString();

        if (rerollButton != null)
            rerollButton.interactable = rerollsLeft > 0 && currentOffers.Length > 0 && !clickLocked;
    }

    private void AcquirePause()
    {
        if (pauseAcquired)
            return;

        GamePauseGate2D.Acquire(this);
        pauseAcquired = true;
    }

    private void ReleasePause()
    {
        if (!pauseAcquired)
            return;

        GamePauseGate2D.Release(this);
        pauseAcquired = false;
    }
}