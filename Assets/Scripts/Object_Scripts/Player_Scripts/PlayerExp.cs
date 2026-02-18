// ============================================================
// 파일: Assets/Scripts/Player_Scripts/PlayerExp.cs
// 역할: 경험치 누적 + 레벨업 트리거(통합본)
// - 레벨업 시 이벤트 2종을 모두 쏨(구버전 호환 + 신규 권장)
// - 한 번에 여러 레벨업 처리
// - (선택) 디버그 로그
// ============================================================

using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerExp : MonoBehaviour
{
    [Header("레벨/경험치")]
    [Min(1)]
    [SerializeField] private int startLevel = 1;

    [Tooltip("레벨업에 필요한 기본 EXP(레벨이 오르면 증가)")]
    [Min(1)]
    [SerializeField] private int baseExpToLevelUp = 10;

    [Tooltip("레벨당 추가 필요 EXP")]
    [Min(0)]
    [SerializeField] private int expIncreasePerLevel = 5;

    [Header("디버그")]
    [SerializeField] private bool verboseLog = false;

    // 구버전 호환: 레벨업 발생만 알림
    public event Action OnLevelup;

    // 권장: 레벨 값을 함께 전달
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
                Debug.Log($"[PlayerExp] 레벨업! => Lv.{_level}");

            // 둘 다 쏴서 어디서 구독하든 동작하게
            OnLevelup?.Invoke();
            OnLevelUp?.Invoke(_level);
        }
    }

    private int CalcRequiredExp(int lv)
    {
        // 선형 증가(안정적/디버깅 쉬움)
        return baseExpToLevelUp + (lv - 1) * expIncreasePerLevel;
    }
}