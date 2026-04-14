using UnityEngine;

/// <summary>
/// 퀘스트 하나의 정의입니다.
/// 보상으로 스킬 각성 효과를 제공합니다.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/퀘스트/퀘스트 정의", fileName = "Quest_")]
public sealed class QuestDefinitionSO : ScriptableObject
{
    [Header("퀘스트 식별")]
    [Tooltip("퀘스트 고유 ID입니다.")]
    [SerializeField] private string questId;

    [Tooltip("퀘스트 이름 (한글)입니다.")]
    [SerializeField] private string displayName;

    [Tooltip("퀘스트 설명입니다.")]
    [TextArea(2, 4)]
    [SerializeField] private string description;

    [Tooltip("퀘스트 아이콘입니다.")]
    [SerializeField] private Sprite icon;

    [Header("목표")]
    [Tooltip("퀘스트 종류입니다.")]
    [SerializeField] private QuestType questType;

    [Tooltip("목표 달성 수입니다. (Kill=마리, Survive=초, Collect=개)")]
    [SerializeField] private int targetCount = 10;

    [Tooltip("특정 적 ID가 필요한 경우입니다. 빈칸이면 아무 적이나 OK.")]
    [SerializeField] private string targetEnemyId;

    [Header("보상")]
    [Tooltip("보상으로 주어지는 스킬 각성 효과입니다.")]
    [SerializeField] private SkillAwakeningSO awakeningReward;

    [Tooltip("추가 냥 보상입니다.")]
    [SerializeField] private int nyangReward;

    [Tooltip("추가 경험치 보상입니다.")]
    [SerializeField] private int expReward;

    [Header("출현 조건")]
    [Tooltip("이 퀘스트가 제안되려면 최소 게임 시간(초)이 필요합니다.")]
    [SerializeField] private float minGameTime;

    [Tooltip("이 퀘스트의 반복 가능 여부입니다.")]
    [SerializeField] private bool repeatable;

    // ─── 프로퍼티 ───
    public string QuestId => questId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public QuestType QuestType => questType;
    public int TargetCount => targetCount;
    public string TargetEnemyId => targetEnemyId;
    public SkillAwakeningSO AwakeningReward => awakeningReward;
    public int NyangReward => nyangReward;
    public int ExpReward => expReward;
    public float MinGameTime => minGameTime;
    public bool Repeatable => repeatable;

    /// <summary>목표 설명을 자동 생성합니다.</summary>
    public string FormatObjective()
    {
        switch (questType)
        {
            case QuestType.Kill:
                return string.IsNullOrWhiteSpace(targetEnemyId)
                    ? $"적 {targetCount}마리 처치"
                    : $"{targetEnemyId} {targetCount}마리 처치";
            case QuestType.Survive:
                return $"{targetCount}초 생존";
            case QuestType.Collect:
                return $"아이템 {targetCount}개 수집";
            case QuestType.BossKill:
                return $"보스 {targetCount}마리 처치";
            case QuestType.EliteKill:
                return $"엘리트 {targetCount}마리 처치";
            case QuestType.SkillKill:
                return $"스킬로 적 {targetCount}마리 처치";
            default:
                return $"목표 {targetCount} 달성";
        }
    }
}