using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterGridView : MonoBehaviour
{
    [Header("카탈로그")]
    [SerializeField] private CharacterCatalogSO catalog;

    [Header("프리팹/루트")]
    [SerializeField] private CharacterCardView cardPrefab;
    [SerializeField] private Transform contentRoot;

    private readonly List<CharacterCardView> _spawned = new List<CharacterCardView>(64);

    public event Action<CharacterDefinitionSO> OnClickCharacter;

    public void SetCatalog(CharacterCatalogSO newCatalog)
    {
        catalog = newCatalog;
    }

    public void Rebuild()
    {
        Clear();

        if (catalog == null || cardPrefab == null || contentRoot == null) return;

        var list = catalog.Characters;
        for (int i = 0; i < list.Count; i++)
        {
            var def = list[i];
            if (def == null) continue;

            var view = Instantiate(cardPrefab, contentRoot);
            view.Bind(def);

            if (view.Button != null)
            {
                var captured = def;
                view.Button.onClick.AddListener(() => OnClickCharacter?.Invoke(captured));
            }

            _spawned.Add(view);
        }
    }

    public void RefreshInteractable(Func<CharacterDefinitionSO, bool> canPick)
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            var v = _spawned[i];
            if (v == null || v.Bound == null) continue;
            v.SetInteractable(canPick == null || canPick(v.Bound));
        }
    }

    private void Clear()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();
    }
}
