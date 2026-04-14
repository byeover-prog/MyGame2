using UnityEngine;

/// <summary>
/// 퀘스트 하나의 정의입니다.
/// 보상으로 스킬 각성 효과를 제공합니다.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/퀘스트/퀘스트 정의", fileName = "Quest_")]
public sealed class QuestDefinitionSO : ScriptableObject
{
    [Header("퀘스트 식별")]
    [Tooltip("퀘스트 이름 (한글)입니다.")]
    [SerializeField] private string displayName;
    
    [Tooltip("퀘스트 고유 ID입니다.")]
    [SerializeField] private int questId;
    
    [Tooltip("퀘스트의 판정 범위입니다")]
    [SerializeField] private float zoneRadius;
    
    [Tooltip("퀘스트 모듈 리스트입니다.")]
    [SerializeField] private QuestModuleSO[] modules;
    
    // 프로퍼티
    public int QuestId => questId;
    public float ZoneRadius => zoneRadius;
    public string DisplayName => displayName;
    public QuestModuleSO[] Modules => modules;
}