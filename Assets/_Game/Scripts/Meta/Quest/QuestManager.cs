using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class QuestManager : MonoBehaviour
{
    [Header("퀘스트 풀")]
    [Tooltip("제안 가능한 전체 퀘스트 목록입니다.")]
    [SerializeField] private List<QuestDefinitionSO> questPool = new List<QuestDefinitionSO>(16);

    [Header("설정")] 
    [Tooltip("첫 퀘스트 시작 시간입니다")] 
    [SerializeField] private float _firstQuestStartTime = 180f;
    [Tooltip("새 퀘스트 제안 간격(초)입니다.")]
    [SerializeField] private float offerInterval = 120f;

    // ─── 런타임 상태 ───
    private float _timeSinceLastOffer;
    private float _gameElapsed;

    // ─── 이벤트 ───
    public event Action<string> OnQuestStarted;
    public event Action<string, int, int> OnQuestProgressUpdated;
    public event Action<string> OnQuestCompleted;


    void Update()
    {
        
    }

    private void ApplyRewards(QuestDefinitionSO definition)
    {
        if (definition.AwakeningReward != null)
        {
            SkillAwakeningApplier applier = FindFirstObjectByType<SkillAwakeningApplier>();
            if (applier != null)
                applier.ApplyAwakening(definition.AwakeningReward);
        }
    }
}