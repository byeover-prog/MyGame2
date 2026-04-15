using System.Collections.Generic;
using UnityEngine;
using _Game.LevelUp;

public class LevelUpPanelController : MonoBehaviour
{
    [Header("카드 슬롯")]
    [SerializeField] private List<SkillCardUI> cardSlots; // CardRow 안의 SkillCard 4개

    [Header("연결")]
    [SerializeField] private LevelUpRewardApplier rewardApplier;
    [SerializeField] private LevelUpFlowCoordinator flowCoordinator;

    [Header("패널")]
    [SerializeField] private GameObject panelRoot; // LEVELUP 루트 오브젝트

    private List<LevelUpCardData> _currentCards;
    public bool IsOpen { get; private set; }

    public void Open(List<LevelUpCardData> cards)
    {
        if (cards == null || cards.Count == 0) return;

        _currentCards = cards;
        IsOpen = true;
        panelRoot.SetActive(true);

        for (int i = 0; i < cardSlots.Count; i++)
        {
            bool hasCard = i < cards.Count;
            cardSlots[i].gameObject.SetActive(hasCard);
            if (hasCard)
                cardSlots[i].Setup(cards[i], OnCardSelected);
        }
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        panelRoot.SetActive(false);
        flowCoordinator?.NotifyPanelClosed();
    }

    private void OnCardSelected(LevelUpCardData data)
    {
        bool applied = rewardApplier != null && rewardApplier.Apply(data);
        if (applied) Close();
    }
}