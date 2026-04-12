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

    [Header("추가 스탯")]
    [Tooltip("경험치 획득 배율을 읽을 전투 스탯 컴포넌트입니다.")]
    [SerializeField] private PlayerCombatStats2D combatStats;

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
        if (combatStats == null) combatStats = GetComponent<PlayerCombatStats2D>();
        if (combatStats == null) combatStats = GetComponentInParent<PlayerCombatStats2D>();

        Initialize(startLevel);
    }

    public void Initialize(int level)
    {
        _level = Mathf.Max(1, level);
        _currentExp = 0;
        _requiredExp = CalcRequiredExp(_level);

        if (verboseLog)
            GameLogger.Log($"[PlayerExp] Init => Lv.{_level}, exp 0/{_requiredExp}");
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        // ★ 경험치 배율 적용 (패시브 + 기본 능력치)
        int finalAmount = ApplyExpMultiplier(amount);
        _currentExp += finalAmount;

        if (verboseLog)
            GameLogger.Log($"[PlayerExp] EXP +{finalAmount} (raw={amount}) => {_currentExp}/{_requiredExp}");

        while (_currentExp >= _requiredExp)
        {
            _currentExp -= _requiredExp;
            _level++;

            _requiredExp = CalcRequiredExp(_level);

            if (verboseLog)
                GameLogger.Log($"[PlayerExp] LevelUp => Lv.{_level}");

            if (fireLegacyEvent)
                OnLevelup?.Invoke();

            OnLevelUp?.Invoke(_level);
        }
    }

    private int ApplyExpMultiplier(int rawAmount)
    {
        if (combatStats == null)
            return rawAmount;

        return Mathf.Max(1, Mathf.RoundToInt(rawAmount * combatStats.ExpGainMul));
    }

    private int CalcRequiredExp(int lv)
    {
        return baseExpToLevelUp + (lv - 1) * expIncreasePerLevel;
    }
}