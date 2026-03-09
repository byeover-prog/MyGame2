using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CommonSkillCardPicker2D : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CommonSkillManager2D manager;
    [SerializeField] private CommonSkillCardPoolSO pool;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CommonSkillCardView2D cardView1;
    [SerializeField] private CommonSkillCardView2D cardView2;
    [SerializeField] private CommonSkillCardView2D cardView3;

    [Header("옵션")]
    [SerializeField] private bool pauseGameWhileOpen = true;

    private bool _isOpen;
    private float _prevTimeScale;

    private readonly List<CommonSkillCardSO> _candidates = new List<CommonSkillCardSO>(32);
    private readonly List<CommonSkillCardSO> _picked = new List<CommonSkillCardSO>(3);
    private readonly HashSet<CommonSkillCardSO> _pickedSet = new HashSet<CommonSkillCardSO>();

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        _isOpen = false;
        _prevTimeScale = 1f;
    }

    public void Open()
    {
        if (_isOpen) return;
        if (manager == null || pool == null || pool.cards == null || pool.cards.Count == 0) return;

        BuildCandidates();
        if (_candidates.Count == 0) return;

        _picked.Clear();
        _pickedSet.Clear();

        for (int i = 0; i < 3; i++)
        {
            var c = PickWeightedNoDuplicate();
            if (c == null) c = _candidates[Random.Range(0, _candidates.Count)];
            _picked.Add(c);
            _pickedSet.Add(c);
        }

        BindViews();

        if (panelRoot != null) panelRoot.SetActive(true);

        // 시간 정지는 LevelUpOrchestrator에서만 처리한다.

        _isOpen = true;
    }

    public void Close()
    {
        if (!_isOpen) return;

        // 시간 정지는 LevelUpOrchestrator에서만 처리한다.

        if (panelRoot != null) panelRoot.SetActive(false);
        _isOpen = false;
    }

    public void Pick(CommonSkillCardSO card)
    {
        if (!_isOpen) return;
        if (card == null || card.skill == null || manager == null) { Close(); return; }

        manager.Upgrade(card.skill);
        Close();
    }

    private void BuildCandidates()
    {
        _candidates.Clear();

        for (int i = 0; i < pool.cards.Count; i++)
        {
            var c = pool.cards[i];
            if (c == null || c.skill == null) continue;

            if (manager.IsMaxLevel(c.skill))
                continue;

            _candidates.Add(c);
        }
    }

    private CommonSkillCardSO PickWeightedNoDuplicate()
    {
        // 후보가 적으므로 단순 반복(성능 충분)
        for (int retry = 0; retry < 20; retry++)
        {
            var c = PickWeighted();
            if (c == null) return null;
            if (_pickedSet.Contains(c)) continue;
            return c;
        }

        // fallback
        for (int i = 0; i < _candidates.Count; i++)
        {
            var c = _candidates[i];
            if (!_pickedSet.Contains(c)) return c;
        }

        return null;
    }

    private CommonSkillCardSO PickWeighted()
    {
        int total = 0;
        for (int i = 0; i < _candidates.Count; i++)
            total += Mathf.Max(1, _candidates[i].weight);

        int r = Random.Range(0, total);
        int acc = 0;

        for (int i = 0; i < _candidates.Count; i++)
        {
            acc += Mathf.Max(1, _candidates[i].weight);
            if (r < acc) return _candidates[i];
        }

        return _candidates[_candidates.Count - 1];
    }

    private void BindViews()
    {
        var c1 = _picked.Count > 0 ? _picked[0] : null;
        var c2 = _picked.Count > 1 ? _picked[1] : null;
        var c3 = _picked.Count > 2 ? _picked[2] : null;

        if (cardView1 != null && c1 != null) cardView1.Bind(this, c1, manager.GetLevel(c1.skill.kind));
        if (cardView2 != null && c2 != null) cardView2.Bind(this, c2, manager.GetLevel(c2.skill.kind));
        if (cardView3 != null && c3 != null) cardView3.Bind(this, c3, manager.GetLevel(c3.skill.kind));
    }
}
