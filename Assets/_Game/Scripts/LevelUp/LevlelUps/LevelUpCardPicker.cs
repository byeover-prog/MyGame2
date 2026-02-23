using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelUpCardPicker : MonoBehaviour
{
    [Header("패널 루트(선택)")]
    [SerializeField] private GameObject panelRoot;

    [Header("카드 뷰 3개 고정")]
    [SerializeField] private LevelUpCardView[] cardViews = new LevelUpCardView[3];

    [Header("선택 중 입력 잠금")]
    [SerializeField] private bool lockInputWhileResolving = true;

    [Header("디버그")]
    [SerializeField] private bool enableLogs = true;

    public event Action<WeaponUpgradeCardSO> OnPickWeaponUpgrade;
    public event Action<CommonSkillCardSO> OnPickCommonSkill;
    public event Action OnClosed;

    private readonly List<Button> _cachedButtons = new List<Button>(3);

    private bool _isOpen;
    private bool _isResolving;

    private WeaponUpgradeCardSO[] _currentWeaponCards;
    private CommonSkillCardSO[] _currentCommonCards;
    private ILevelUpCardData[] _currentDataCards;

    private void Awake()
    {
        CacheButtons();
        SetOpen(false);
    }

    private void CacheButtons()
    {
        _cachedButtons.Clear();

        for (int i = 0; i < cardViews.Length; i++)
        {
            var view = cardViews[i];
            if (view == null)
            {
                _cachedButtons.Add(null);
                continue;
            }

            // 카드 오브젝트에 Button이 붙어있다고 가정 (없으면 null로 둠)
            var btn = view.GetComponent<Button>();
            _cachedButtons.Add(btn);
        }
    }

    public bool IsOpen => _isOpen;

    // 무기 업그레이드 카드 3장 열기
    public void OpenWeaponUpgrades(IReadOnlyList<WeaponUpgradeCardSO> offers)
    {
        if (offers == null)
        {
            Log("OpenWeaponUpgrades: offers == null");
            return;
        }

        if (offers.Count == 0)
        {
            Log("OpenWeaponUpgrades: offers.Count == 0");
            return;
        }

        EnsureViewCount(3);

        _currentWeaponCards = new WeaponUpgradeCardSO[cardViews.Length];
        _currentCommonCards = null;
        _currentDataCards = null;

        for (int i = 0; i < cardViews.Length; i++)
        {
            var card = (i < offers.Count) ? offers[i] : null;
            _currentWeaponCards[i] = card;

            var view = cardViews[i];
            if (view != null)
                view.BindWeaponUpgradeCard(card);

            var btn = _cachedButtons.Count > i ? _cachedButtons[i] : null;
            if (btn != null)
                btn.interactable = (card != null);
        }

        HookButtonsWeapon();
        SetOpen(true);
    }

    // 공통 스킬 카드 3장 열기
    public void OpenCommonSkills(IReadOnlyList<CommonSkillCardSO> offers)
    {
        if (offers == null)
        {
            Log("OpenCommonSkills: offers == null");
            return;
        }

        if (offers.Count == 0)
        {
            Log("OpenCommonSkills: offers.Count == 0");
            return;
        }

        EnsureViewCount(3);

        _currentCommonCards = new CommonSkillCardSO[cardViews.Length];
        _currentWeaponCards = null;
        _currentDataCards = null;

        for (int i = 0; i < cardViews.Length; i++)
        {
            var card = (i < offers.Count) ? offers[i] : null;
            _currentCommonCards[i] = card;

            var view = cardViews[i];
            if (view != null)
                view.BindCommonSkillCard(card);

            var btn = _cachedButtons.Count > i ? _cachedButtons[i] : null;
            if (btn != null)
                btn.interactable = (card != null);
        }

        HookButtonsCommon();
        SetOpen(true);
    }

    public void Close()
    {
        if (!_isOpen) return;

        UnhookButtons();
        _isResolving = false;
        _currentWeaponCards = null;
        _currentCommonCards = null;
        _currentDataCards = null;

        SetOpen(false);
        OnClosed?.Invoke();
    }

    /// <summary>
    /// (권장) 인터페이스 기반 카드 3장 열기.
    /// - 무기/공통스킬/패시브 등 모든 카드 타입을 한 UI로 통합하기 위한 진입점.
    /// - LevelUpSystem2D가 이 메서드를 사용합니다.
    /// </summary>
    public void OpenCards(IReadOnlyList<ILevelUpCardData> offers)
    {
        if (offers == null)
        {
            Log("OpenCards: offers == null");
            return;
        }

        if (offers.Count == 0)
        {
            Log("OpenCards: offers.Count == 0");
            return;
        }

        EnsureViewCount(3);

        _currentDataCards = new ILevelUpCardData[cardViews.Length];
        _currentWeaponCards = null;
        _currentCommonCards = null;

        for (int i = 0; i < cardViews.Length; i++)
        {
            var data = (i < offers.Count) ? offers[i] : null;
            _currentDataCards[i] = data;

            var view = cardViews[i];
            if (view != null)
                view.Bind(data);

            // 버튼 비활성(선택 불가 카드면 클릭 차단)
            var btn = _cachedButtons.Count > i ? _cachedButtons[i] : null;
            if (btn != null)
                btn.interactable = (data != null && data.CanPick());
        }

        HookButtonsData();
        SetOpen(true);
    }

    private void HookButtonsWeapon()
    {
        UnhookButtons();

        for (int i = 0; i < cardViews.Length; i++)
        {
            int index = i;
            var btn = _cachedButtons.Count > i ? _cachedButtons[i] : null;
            if (btn == null) continue;

            btn.onClick.AddListener(() => TryPickWeapon(index));
        }
    }

    private void HookButtonsCommon()
    {
        UnhookButtons();

        for (int i = 0; i < cardViews.Length; i++)
        {
            int index = i;
            var btn = _cachedButtons.Count > i ? _cachedButtons[i] : null;
            if (btn == null) continue;

            btn.onClick.AddListener(() => TryPickCommon(index));
        }
    }

    private void HookButtonsData()
    {
        UnhookButtons();

        for (int i = 0; i < cardViews.Length; i++)
        {
            int index = i;
            var btn = _cachedButtons.Count > i ? _cachedButtons[i] : null;
            if (btn == null) continue;

            btn.onClick.AddListener(() => TryPickData(index));
        }
    }

    private void UnhookButtons()
    {
        for (int i = 0; i < _cachedButtons.Count; i++)
        {
            var btn = _cachedButtons[i];
            if (btn == null) continue;
            btn.onClick.RemoveAllListeners();
        }
    }

    private void TryPickWeapon(int index)
    {
        if (!_isOpen) return;
        if (lockInputWhileResolving && _isResolving) return;

        if (_currentWeaponCards == null)
        {
            Log("TryPickWeapon: _currentWeaponCards == null");
            return;
        }

        if (index < 0 || index >= _currentWeaponCards.Length) return;

        var picked = _currentWeaponCards[index];
        if (picked == null)
        {
            Log($"TryPickWeapon: picked is null at index {index}");
            return;
        }

        _isResolving = true;
        Log($"Pick WeaponUpgradeCardSO: {picked.name}");

        OnPickWeaponUpgrade?.Invoke(picked);
        Close();
    }

    private void TryPickCommon(int index)
    {
        if (!_isOpen) return;
        if (lockInputWhileResolving && _isResolving) return;

        if (_currentCommonCards == null)
        {
            Log("TryPickCommon: _currentCommonCards == null");
            return;
        }

        if (index < 0 || index >= _currentCommonCards.Length) return;

        var picked = _currentCommonCards[index];
        if (picked == null)
        {
            Log($"TryPickCommon: picked is null at index {index}");
            return;
        }

        _isResolving = true;
        Log($"Pick CommonSkillCardSO: {picked.name}");

        OnPickCommonSkill?.Invoke(picked);
        Close();
    }

    private void TryPickData(int index)
    {
        if (!_isOpen) return;
        if (lockInputWhileResolving && _isResolving) return;

        if (_currentDataCards == null)
        {
            Log("TryPickData: _currentDataCards == null");
            return;
        }

        if (index < 0 || index >= _currentDataCards.Length) return;

        var picked = _currentDataCards[index];
        if (picked == null)
        {
            Log($"TryPickData: picked is null at index {index}");
            return;
        }

        if (!picked.CanPick())
        {
            Log($"TryPickData: CanPick() == false at index {index}");
            return;
        }

        _isResolving = true;

        try
        {
            Log($"Pick ILevelUpCardData: {picked.TitleKorean}");
            picked.Apply();
        }
        catch (Exception e)
        {
            Debug.LogException(e, this);
        }

        Close();
    }

    private void SetOpen(bool open)
    {
        _isOpen = open;

        if (panelRoot != null)
            panelRoot.SetActive(open);
        else
            gameObject.SetActive(open);
    }

    private void EnsureViewCount(int required)
    {
        if (cardViews == null || cardViews.Length < required)
        {
            Log($"cardViews 길이가 {required}보다 작음. 인스펙터에서 3개 연결 필요.");
        }
    }

    private void Log(string msg)
    {
        if (!enableLogs) return;
        Debug.Log($"[LevelUpCardPicker] {msg}", this);
    }
}
