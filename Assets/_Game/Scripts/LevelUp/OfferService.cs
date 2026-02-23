using System.Collections.Generic;
using UnityEngine;

public sealed class OfferService : MonoBehaviour
{
    [Header("오퍼 원본 목록(카탈로그 1개 or 프리팹 목록)")]
    [SerializeField] private Offer[] catalog;

    private SkillRuntimeState _state;

    public void Bind(SkillRuntimeState state)
    {
        _state = state;
    }

    public Offer[] BuildOffers(int count = 3)
    {
        if (_state == null)
        {
            Debug.LogError("[OfferService] Bind(state)가 호출되지 않았습니다.");
            return System.Array.Empty<Offer>();
        }

        var candidates = new List<Offer>(catalog.Length);

        for (int i = 0; i < catalog.Length; i++)
        {
            var offer = catalog[i];
            if (string.IsNullOrEmpty(offer.id)) continue;

            // 이미 획득한 스킬은 제외(정책 변경 가능)
            if (_state.HasSkill(offer.id)) continue;

            candidates.Add(offer);
        }

        int take = Mathf.Min(count, candidates.Count);
        var result = new Offer[take];

        for (int i = 0; i < take; i++)
        {
            int idx = Random.Range(0, candidates.Count);
            result[i] = candidates[idx];
            candidates.RemoveAt(idx); // 중복 방지
        }

        return result;
    }

    public void RequestOffers(int count = 3)
    {
        var offers = BuildOffers(count);
        GameSignals.RaiseOffersReady(offers);
    }
}