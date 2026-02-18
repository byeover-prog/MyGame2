using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LevelUpCardPicker : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private LevelUpCardView[] cardViews;

    [Header("적용기")]
    [SerializeField] private WeaponShooterSlotUpgradeApplier2D applier;

    private Action<WeaponUpgradeCardSO> _onPicked;
    private Func<int, int> _getDisplayLevel;

    public void OpenWithOffers(
        List<WeaponUpgradeCardSO> offers,
        Action<WeaponUpgradeCardSO> onPicked,
        Func<int, int> getDisplayLevel = null)
    {
        if (applier == null)
            applier = FindFirstObjectByType<WeaponShooterSlotUpgradeApplier2D>();

        _onPicked = onPicked;
        _getDisplayLevel = getDisplayLevel;

        BindOffers(offers);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void BindOffers(List<WeaponUpgradeCardSO> offers)
    {
        if (cardViews == null || cardViews.Length == 0) return;

        for (int i = 0; i < cardViews.Length; i++)
        {
            var view = cardViews[i];
            if (view == null) continue;

            if (offers != null && i < offers.Count)
            {
                var card = offers[i];

                int curLv = 1;
                if (_getDisplayLevel != null)
                    curLv = Mathf.Max(1, _getDisplayLevel(card.slotIndex));

                view.BindWeaponUpgradeCard(card, curLv, showLevel: true, onPicked: () =>
                {
                    if (applier != null)
                        applier.Apply(card);

                    _onPicked?.Invoke(card);
                    Close();
                });
            }
            else
            {
                view.BindWeaponUpgradeCard(null, 1, false, null);
            }
        }
    }
}