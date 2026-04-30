using System;
using UnityEngine;

/// <summary>
/// 캐릭터 전용 스킬 1개의 원본 데이터입니다.
/// 구현 원리:
///  스킬 ID, 표시 정보, 프리팹, 속성, 레벨별 수치를 한 SO에서 관리합니다.
///  JSON은 이 SO에서 자동 생성되는 결과물로 사용합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "Skill_CharacterSkill",
    menuName = "혼령검/스킬/캐릭터 전용 스킬 정의",
    order = 100)]
public sealed class CharacterSkillDefinitionSO : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("스킬 고유 ID입니다. 예: weapon_bingju")]
    [SerializeField] private string skillId;

    [Tooltip("UI에 표시될 이름입니다. 예: 빙주")]
    [SerializeField] private string displayName;

    [Tooltip("이 스킬을 소유한 캐릭터 ID입니다. 예: yoonseol, harin, hayul")]
    [SerializeField] private string ownerCharacterId;

    [Tooltip("레벨업 카드와 HUD에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite icon;

    [Tooltip("이 스킬의 무기 프리팹입니다.")]
    [SerializeField] private GameObject weaponPrefab;

    [Tooltip("이 스킬의 피해 속성입니다.")]
    [SerializeField] private DamageElement2D element = DamageElement2D.Physical;

    [Header("레벨")]
    [Tooltip("최대 레벨입니다. 기본값은 8입니다.")]
    [SerializeField, Range(1, 20)] private int maxLevel = 8;

    [Tooltip("레벨별 밸런스 수치입니다.")]
    [SerializeField] private SkillLevelBalanceData2D[] levelBalances = new SkillLevelBalanceData2D[8];

    [Header("카드 설명")]
    [Tooltip("레벨별 카드 설명입니다. 0번 인덱스가 Lv.1.")]
    [SerializeField, TextArea] private string[] levelDescriptions = new string[8];

    [Tooltip("레벨별 추가 정보입니다. 피해량, 횟수, 범위 같은 짧은 설명을 넣습니다.")]
    [SerializeField, TextArea] private string[] levelAddInfos = new string[8];

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public string OwnerCharacterId => ownerCharacterId;
    public Sprite Icon => icon;
    public GameObject WeaponPrefab => weaponPrefab;
    public DamageElement2D Element => element;
    public int MaxLevel => maxLevel;
    public SkillLevelBalanceData2D[] LevelBalances => levelBalances;

    private void OnValidate()
    {
        maxLevel = Mathf.Clamp(maxLevel, 1, 20);
        EnsureLevelArray();
        EnsureTextArray(ref levelDescriptions);
        EnsureTextArray(ref levelAddInfos);
    }

    public SkillLevelBalanceData2D GetLevelBalance(int targetLevel)
    {
        if (levelBalances == null || levelBalances.Length == 0)
            return null;

        targetLevel = Mathf.Clamp(targetLevel, 1, maxLevel);

        for (int i = 0; i < levelBalances.Length; i++)
        {
            SkillLevelBalanceData2D data = levelBalances[i];
            if (data == null) continue;

            if (data.Level == targetLevel)
                return data;
        }

        int index = Mathf.Clamp(targetLevel - 1, 0, levelBalances.Length - 1);
        return levelBalances[index];
    }

    public string GetDescriptionForLevel(int targetLevel)
    {
        return GetTextWithFallback(levelDescriptions, targetLevel);
    }

    public string GetAddInfoForLevel(int targetLevel)
    {
        return GetTextWithFallback(levelAddInfos, targetLevel);
    }

    private void EnsureLevelArray()
    {
        if (levelBalances == null)
            levelBalances = new SkillLevelBalanceData2D[maxLevel];

        if (levelBalances.Length != maxLevel)
            Array.Resize(ref levelBalances, maxLevel);

        for (int i = 0; i < levelBalances.Length; i++)
        {
            if (levelBalances[i] == null)
                levelBalances[i] = new SkillLevelBalanceData2D();

            levelBalances[i].SetLevelForEditor(i + 1);
        }
    }

    private void EnsureTextArray(ref string[] target)
    {
        if (target == null)
            target = new string[maxLevel];

        if (target.Length != maxLevel)
            Array.Resize(ref target, maxLevel);
    }

    private string GetTextWithFallback(string[] source, int targetLevel)
    {
        if (source == null || source.Length == 0)
            return string.Empty;

        targetLevel = Mathf.Clamp(targetLevel, 1, Mathf.Min(maxLevel, source.Length));

        int index = targetLevel - 1;
        if (!string.IsNullOrWhiteSpace(source[index]))
            return source[index];

        for (int i = index; i >= 0; i--)
        {
            if (!string.IsNullOrWhiteSpace(source[i]))
                return source[i];
        }

        return string.Empty;
    }
}