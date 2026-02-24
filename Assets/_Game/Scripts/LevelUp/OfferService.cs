using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 오퍼(카드) 생성 전담.
/// - SO 없이, "프리팹 참조 + 문자열/아이콘"만으로 카드 데이터를 생성
/// - 런타임 상태(SkillRuntimeState)를 보고
///   - 미보유면 획득 카드(다음 레벨=1)
///   - 보유 중이면 업그레이드 카드(다음 레벨=현재+1)
///   - maxLevel 도달 시 후보 제외
///   - 슬롯 제한 초과 시(미보유 획득) 후보 제외
/// </summary>
[DisallowMultipleComponent]
public sealed class OfferService : MonoBehaviour
{
    [Serializable]
    public struct OfferCatalogItem
    {
        [Header("식별")]
        public string id;
        public OfferKind kind;

        [Header("카드 표시")]
        public Sprite icon;
        public string titleKr;
        public string tagKr;

        [Header("프리팹")]
        public GameObject prefab;

        [Header("정책")]
        [Tooltip("런타임 카드 후보에 포함할지")]
        public bool showInRunOffers;

        [Tooltip("이 스킬/패시브의 최대 레벨(0이면 8으로 간주)")]
        [Range(0, 8)]
        public int maxLevel;

        [Header("레벨별 설명(레벨 표기 X, 문장만)")]
        [TextArea] public string descLv1;
        [TextArea] public string descLv2;
        [TextArea] public string descLv3;
        [TextArea] public string descLv4;
        [TextArea] public string descLv5;
        [TextArea] public string descLv6;
        [TextArea] public string descLv7;
        [TextArea] public string descLv8;

        public int GetMaxLevelOrDefault()
        {
            return (maxLevel <= 0) ? 8 : Mathf.Clamp(maxLevel, 1, 8);
        }

        public string GetDescriptionForLevel(int nextLevel)
        {
            nextLevel = Mathf.Clamp(nextLevel, 1, 8);

            // nextLevel에 해당하는 설명이 비어있으면, 아래로 내려가며 마지막으로 채워진 설명을 사용한다.
            string d = nextLevel switch
            {
                1 => descLv1,
                2 => descLv2,
                3 => descLv3,
                4 => descLv4,
                5 => descLv5,
                6 => descLv6,
                7 => descLv7,
                8 => descLv8,
                _ => descLv1
            };

            if (!string.IsNullOrWhiteSpace(d))
                return d;

            // fallback
            for (int lv = nextLevel; lv >= 1; lv--)
            {
                string dd = lv switch
                {
                    1 => descLv1,
                    2 => descLv2,
                    3 => descLv3,
                    4 => descLv4,
                    5 => descLv5,
                    6 => descLv6,
                    7 => descLv7,
                    8 => descLv8,
                    _ => descLv1
                };

                if (!string.IsNullOrWhiteSpace(dd))
                    return dd;
            }

            return "";
        }
    }

    [Header("카탈로그(프리팹 참조)")]
    [SerializeField] private OfferCatalogItem[] catalog = Array.Empty<OfferCatalogItem>();

    [Header("생성 옵션")]
    [SerializeField, Min(1)] private int offerCount = 3;

    [Header("디버그")]
    [SerializeField] private bool enableLogs = false;

    private SkillRuntimeState _state;

    // 재사용 버퍼(할당 최소화)
    private readonly List<int> _candidateIndices = new List<int>(64);
    private readonly HashSet<string> _dedupIds = new HashSet<string>();

    private void OnEnable()
    {
        GameSignals.RerollRequested += HandleReroll;
    }

    private void OnDisable()
    {
        GameSignals.RerollRequested -= HandleReroll;
    }

    public void Bind(SkillRuntimeState state)
    {
        _state = state;
    }

    public Offer[] BuildOffers(int count)
    {
        if (_state == null)
        {
            Debug.LogError("[OfferService] Bind(state)가 호출되지 않았습니다.");
            return Array.Empty<Offer>();
        }

        int wanted = Mathf.Max(1, count);

        _candidateIndices.Clear();
        _dedupIds.Clear();

        // 1) 후보 수집
        for (int i = 0; i < catalog.Length; i++)
        {
            var def = catalog[i];
            if (!def.showInRunOffers) continue;

            if (string.IsNullOrWhiteSpace(def.id))
                continue;

            // 같은 id가 카탈로그에 중복으로 들어가면(실수) 1개만 살린다.
            if (!_dedupIds.Add(def.id))
                continue;

            int max = def.GetMaxLevelOrDefault();
            int cur = _state.GetLevel(def.kind, def.id);
            if (cur >= max)
                continue;

            // 미보유 획득은 슬롯 제한 체크
            if (cur <= 0 && !_state.CanAcquire(def.kind))
                continue;

            _candidateIndices.Add(i);
        }

        if (_candidateIndices.Count <= 0)
        {
            if (enableLogs)
                Debug.Log("[OfferService] 후보가 없습니다(모두 maxLevel or 슬롯 full).", this);

            return Array.Empty<Offer>();
        }

        // 2) 부분 셔플(Fisher-Yates)로 중복 없는 랜덤 선택
        int take = Mathf.Min(wanted, _candidateIndices.Count);
        var result = new Offer[take];

        for (int i = 0; i < take; i++)
        {
            int j = UnityEngine.Random.Range(i, _candidateIndices.Count);
            ( _candidateIndices[i], _candidateIndices[j] ) = ( _candidateIndices[j], _candidateIndices[i] );

            int idx = _candidateIndices[i];
            var def = catalog[idx];

            int cur = _state.GetLevel(def.kind, def.id);
            int next = Mathf.Clamp(cur + 1, 1, def.GetMaxLevelOrDefault());

            result[i] = new Offer
            {
                id = def.id,
                kind = def.kind,
                icon = def.icon,
                titleKr = string.IsNullOrWhiteSpace(def.titleKr) ? def.id : def.titleKr,
                tagKr = string.IsNullOrWhiteSpace(def.tagKr) ? "" : def.tagKr,
                descKr = def.GetDescriptionForLevel(next),
                prefab = def.prefab,
                previewLevel = next
            };
        }

        return result;
    }

    public void RequestOffers(int? countOverride = null)
    {
        int count = countOverride.HasValue ? Mathf.Max(1, countOverride.Value) : Mathf.Max(1, offerCount);

        var offers = BuildOffers(count);
        if (enableLogs)
            Debug.Log($"[OfferService] OffersReady => {offers.Length}장", this);

        GameSignals.RaiseOffersReady(offers);
    }

    private void HandleReroll()
    {
        RequestOffers();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 카탈로그 입력 실수 방지(필드 자동 채움)
        if (catalog == null) return;

        for (int i = 0; i < catalog.Length; i++)
        {
            var def = catalog[i];

            if (string.IsNullOrWhiteSpace(def.id) && def.prefab != null)
                def.id = def.prefab.name;

            if (def.maxLevel < 0) def.maxLevel = 0;
            if (def.maxLevel > 8) def.maxLevel = 8;

            catalog[i] = def;
        }

        if (offerCount < 1) offerCount = 1;
    }
#endif
}
