using System;
using System.Collections.Generic;
using UnityEngine;
using _Game.Player;

[CreateAssetMenu(menuName = "혼령검/메타/캐릭터 레벨 곡선", fileName = "CharacterLevelCurve_")]
public sealed class CharacterLevelCurveSO : ScriptableObject
{
    [Header("레벨 범위")]
    [Tooltip("캐릭터 영구 레벨 최대치입니다. 기본 50 권장입니다.")]
    [Range(1, 50)]
    [SerializeField] private int maxLevel = 50;

    [Header("경험치 곡선")]
    [Tooltip("체크 시 아래 배열을 사용합니다. 끄면 기본 공식으로 계산합니다.")]
    [SerializeField] private bool useCustomRequiredXp = false;

    [Tooltip("현재 레벨에서 다음 레벨로 갈 때 필요한 경험치입니다. 길이는 최대레벨-1과 맞춰주세요.")]
    [SerializeField] private List<int> requiredXpByLevel = new List<int>(49);

    [Tooltip("기본 공식 사용 시 Lv1→2 기준 경험치입니다.")]
    [Min(1)]
    [SerializeField] private int baseRequiredXp = 100;

    [Tooltip("기본 공식 사용 시 레벨마다 곱해지는 증가율입니다.")]
    [Min(1.01f)]
    [SerializeField] private float requiredXpGrowth = 1.18f;

    [Header("레벨업 고정 성장")]
    [Tooltip("레벨이 1 오를 때마다 공격력에 더할 퍼센트입니다. 이동속도 같은 유틸은 여기서 올리지 않습니다.")]
    [SerializeField] private float attackPowerPercentPerLevel = 2f;

    [Tooltip("레벨이 1 오를 때마다 방어력에 더할 퍼센트입니다.")]
    [SerializeField] private float defensePercentPerLevel = 1f;

    [Tooltip("레벨이 1 오를 때마다 최대 체력에 더할 고정값입니다.")]
    [SerializeField] private int maxHpFlatPerLevel = 4;

    [Header("레벨 구간 해금")]
    [Tooltip("특정 레벨 도달 시 열리는 캐릭터 강화 해금 목록입니다.")]
    [SerializeField] private List<CharacterLevelUnlockEntry2D> unlocks = new List<CharacterLevelUnlockEntry2D>(8);

    public int MaxLevel => maxLevel;
    public IReadOnlyList<CharacterLevelUnlockEntry2D> Unlocks => unlocks;

    public int GetRequiredXpToNextLevel(int currentLevel)
    {
        currentLevel = Mathf.Clamp(currentLevel, 1, maxLevel);
        if (currentLevel >= maxLevel) return 0;

        if (useCustomRequiredXp && requiredXpByLevel != null && requiredXpByLevel.Count >= currentLevel)
        {
            return Mathf.Max(1, requiredXpByLevel[currentLevel - 1]);
        }

        return GetFallbackRequiredXpToNextLevel(currentLevel, baseRequiredXp, requiredXpGrowth, maxLevel);
    }

    public PlayerStatSnapshot BuildLevelBonusSnapshot(int level)
    {
        return BuildSnapshotFromRules(level, attackPowerPercentPerLevel, defensePercentPerLevel, maxHpFlatPerLevel);
    }

    public void CollectUnlocksBetweenLevels(int previousLevel, int newLevel, List<CharacterLevelUnlockEntry2D> results)
    {
        if (results == null) return;
        if (unlocks == null || unlocks.Count == 0) return;

        int minLevel = Mathf.Min(previousLevel, newLevel);
        int maxInclusive = Mathf.Max(previousLevel, newLevel);

        for (int i = 0; i < unlocks.Count; i++)
        {
            CharacterLevelUnlockEntry2D entry = unlocks[i];
            if (entry == null) continue;
            if (entry.level <= minLevel) continue;
            if (entry.level > maxInclusive) continue;
            results.Add(entry);
        }
    }

    public void CollectUnlocksToLevel(int level, List<CharacterLevelUnlockEntry2D> results)
    {
        if (results == null) return;
        if (unlocks == null || unlocks.Count == 0) return;

        for (int i = 0; i < unlocks.Count; i++)
        {
            CharacterLevelUnlockEntry2D entry = unlocks[i];
            if (entry == null) continue;
            if (entry.level > level) continue;
            results.Add(entry);
        }
    }

    public static int GetFallbackRequiredXpToNextLevel(int currentLevel, int baseXp = 100, float growth = 1.18f, int maxLevel = 50)
    {
        currentLevel = Mathf.Clamp(currentLevel, 1, maxLevel);
        if (currentLevel >= maxLevel) return 0;

        float raw = baseXp * Mathf.Pow(growth, currentLevel - 1);
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    public static PlayerStatSnapshot BuildFallbackBonusSnapshot(int level)
    {
        return BuildSnapshotFromRules(level, 2f, 1f, 4);
    }

    private static PlayerStatSnapshot BuildSnapshotFromRules(int level, float attackPerLevel, float defensePerLevel, int maxHpPerLevel)
    {
        int levelOffset = Mathf.Max(0, level - 1);

        return new PlayerStatSnapshot
        {
            AttackPowerPercent = attackPerLevel * levelOffset,
            DefensePercent = defensePerLevel * levelOffset,
            MaxHpFlat = Mathf.Max(0, maxHpPerLevel * levelOffset)
        };
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxLevel < 1) maxLevel = 1;
        if (maxLevel > 50) maxLevel = 50;
        if (baseRequiredXp < 1) baseRequiredXp = 1;
        if (requiredXpGrowth < 1.01f) requiredXpGrowth = 1.01f;
        if (maxHpFlatPerLevel < 0) maxHpFlatPerLevel = 0;

        if (requiredXpByLevel == null)
            requiredXpByLevel = new List<int>(maxLevel - 1);

        int targetCount = Mathf.Max(0, maxLevel - 1);
        while (requiredXpByLevel.Count < targetCount)
            requiredXpByLevel.Add(GetFallbackRequiredXpToNextLevel(requiredXpByLevel.Count + 1, baseRequiredXp, requiredXpGrowth, maxLevel));

        while (requiredXpByLevel.Count > targetCount)
            requiredXpByLevel.RemoveAt(requiredXpByLevel.Count - 1);

        if (unlocks == null)
            unlocks = new List<CharacterLevelUnlockEntry2D>(8);
    }
#endif
}

[Serializable]
public sealed class CharacterLevelUnlockEntry2D
{
    [Tooltip("이 레벨에 도달하면 해금됩니다.")]
    public int level = 5;

    [Tooltip("저장/체크용 고유 ID입니다.")]
    public string unlockId;

    [Tooltip("UI에 표시할 제목입니다.")]
    public string titleKr;

    [Tooltip("UI에 표시할 설명입니다.")]
    [TextArea]
    public string descriptionKr;

    [Tooltip("해금 종류입니다.")]
    public CharacterLevelUnlockKind2D kind = CharacterLevelUnlockKind2D.BasicSkillAugment;
}
