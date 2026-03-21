using UnityEngine;

[CreateAssetMenu(menuName = "혼령검/메타/런 보상 설정", fileName = "CharacterRunRewardConfig")]
public sealed class CharacterRunRewardConfigSO : ScriptableObject
{
    [Header("스토리 모드 XP")]
    [Tooltip("스토리 승리 시 메인 캐릭터 XP입니다.")]
    [Min(0)] [SerializeField] private int storyVictoryMainXp = 120;
    [Tooltip("스토리 패배 시 메인 캐릭터 XP입니다.")]
    [Min(0)] [SerializeField] private int storyDefeatMainXp = 70;

    [Header("캐주얼 모드 XP")]
    [Tooltip("캐주얼 승리 시 메인 캐릭터 XP입니다.")]
    [Min(0)] [SerializeField] private int casualVictoryMainXp = 80;
    [Tooltip("캐주얼 패배 시 메인 캐릭터 XP입니다.")]
    [Min(0)] [SerializeField] private int casualDefeatMainXp = 45;

    [Header("지원 캐릭터 분배")]
    [Tooltip("지원 캐릭터는 메인 대비 몇 퍼센트 XP를 받을지 설정합니다.")]
    [Range(0f, 100f)] [SerializeField] private float supportXpSharePercent = 60f;

    [Header("냥 보상")]
    [Tooltip("스토리 승리 시 획득 냥입니다.")]
    [Min(0)] [SerializeField] private int storyVictoryNyang = 140;
    [Tooltip("스토리 패배 시 획득 냥입니다.")]
    [Min(0)] [SerializeField] private int storyDefeatNyang = 70;
    [Tooltip("캐주얼 승리 시 획득 냥입니다.")]
    [Min(0)] [SerializeField] private int casualVictoryNyang = 90;
    [Tooltip("캐주얼 패배 시 획득 냥입니다.")]
    [Min(0)] [SerializeField] private int casualDefeatNyang = 40;

    public int GetCharacterXp(CharacterRunMode2D mode, RunResultType2D result, bool isMain)
    {
        int mainXp = mode switch
        {
            CharacterRunMode2D.Story => result == RunResultType2D.Victory ? storyVictoryMainXp : storyDefeatMainXp,
            _ => result == RunResultType2D.Victory ? casualVictoryMainXp : casualDefeatMainXp,
        };

        if (isMain)
            return Mathf.Max(0, mainXp);

        return Mathf.Max(0, Mathf.RoundToInt(mainXp * (supportXpSharePercent / 100f)));
    }

    public int GetNyang(CharacterRunMode2D mode, RunResultType2D result)
    {
        return mode switch
        {
            CharacterRunMode2D.Story => result == RunResultType2D.Victory ? storyVictoryNyang : storyDefeatNyang,
            _ => result == RunResultType2D.Victory ? casualVictoryNyang : casualDefeatNyang,
        };
    }

    public static CharacterRunRewardConfigSO CreateRuntimeFallback()
    {
        CharacterRunRewardConfigSO config = CreateInstance<CharacterRunRewardConfigSO>();
        config.hideFlags = HideFlags.DontSave;
        return config;
    }
}

public enum CharacterRunMode2D
{
    Story = 0,
    Casual = 1,
}

public enum RunResultType2D
{
    Defeat = 0,
    Victory = 1,
}
