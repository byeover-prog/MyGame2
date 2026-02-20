using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerExp : MonoBehaviour
{
    [Header("레벨/경험치")]
    [Min(1)]
    [SerializeField] private int startLevel = 1;

    [Min(1)]
    [SerializeField] private int baseExpToLevelUp = 10;

    [Min(0)]
    [SerializeField] private int expIncreasePerLevel = 5;

    [Header("구버전 이벤트")]
    [SerializeField] private bool fireLegacyEvent = false;

    [Header("디버그")]
    [SerializeField] private bool verboseLog = false;

    public event Action OnLevelup;
    public event Action<int> OnLevelUp;

    private int _level;
    private int _currentExp;
    private int _requiredExp;

    public int Level => _level;
    public int CurrentExp => _currentExp;
    public int RequiredExp => _requiredExp;

    private void Awake()
    {
        Initialize(startLevel);
    }

    public void Initialize(int level)
    {
        _level = Mathf.Max(1, level);
        _currentExp = 0;
        _requiredExp = CalcRequiredExp(_level);

        if (verboseLog)
            Debug.Log($"[PlayerExp] Init => Lv.{_level}, exp 0/{_requiredExp}");
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        _currentExp += amount;

        if (verboseLog)
            Debug.Log($"[PlayerExp] EXP +{amount} => {_currentExp}/{_requiredExp}");

        while (_currentExp >= _requiredExp)
        {
            _currentExp -= _requiredExp;
            _level++;

            _requiredExp = CalcRequiredExp(_level);

            if (verboseLog)
                Debug.Log($"[PlayerExp] LevelUp => Lv.{_level}");

            if (fireLegacyEvent)
                OnLevelup?.Invoke();

            OnLevelUp?.Invoke(_level);
        }
    }

    private int CalcRequiredExp(int lv)
    {
        return baseExpToLevelUp + (lv - 1) * expIncreasePerLevel;
    }
}