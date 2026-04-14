using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 퀘스트의 생성 및 진행, 달성 관리 역활을 하는 매니져입니다. 
/// </summary>
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
    [Tooltip("퀘스트 생성 가능 지점의 리스트입니다")]
    // todo 스테이지에 대응하는 좌표리스트의 리스트를 노출하고 스테이지 로드 시 할당하는 구조로 변경 
    [SerializeField] private List<float3> spawnPoints = new List<float3>(16);
    

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

    // 퀘스트 엔티티 생성
    private void SpawnQuest(QuestDefinitionSO def, float3 pos)
    {
        // 엔티티 생성 
        Entity qeust = ECSCore.EM.CreateEntity();
        
        // 공통 컴포넌트 주입
        ECSCore.EM.AddComponentData(qeust, new QuestBase 
            { QuestId = def.QuestId, Progress = 0f });
        // 반지름은 연산 효율을 위해 제곱으로 관리
        ECSCore.EM.AddComponentData(qeust, new QuestZone
            { Center = pos, RadiusSq = def.ZoneRadius * def.ZoneRadius});
        // 개별 컴포넌트 주입
        foreach (var module in def.Modules )
        {
            module.AddComponents(qeust, ECSCore.EM);
        }
    }

    private void ApplyRewards(QuestDefinitionSO definition)
    { 
        SkillAwakeningApplier applier = FindFirstObjectByType<SkillAwakeningApplier>(); 
        // if (applier != null)
            // applier.ApplyAwakening(); todo 각성 스킬 관련 정보는 퀘스트 매니져의 책임이 아님
    }
}