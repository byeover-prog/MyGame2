using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 퀘스트 엔티티의 생성 관리 역활을 하는 매니져입니다.
/// 퀘스트 자체의 로직은 ECS시스템에서 처리됩니다. 
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
    private float _nextSpawnTime;
    private EntityQuery _timeQuery;

    // ─── 이벤트 ───
    public event Action<string> OnQuestStarted;
    public event Action<string, int, int> OnQuestProgressUpdated;
    public event Action<string> OnQuestCompleted;


    void Start()
    {
        _nextSpawnTime = _firstQuestStartTime;
        _timeQuery = ECSCore.EM.CreateEntityQuery(typeof(SessionTimeData));
    }
    void Update()
    {
        //ECS 브릿지에서 관리하는 시간 데이터를 바탕으로 캐싱
        if (_timeQuery.IsEmpty) return;
        var timeData = _timeQuery.GetSingleton<SessionTimeData>();
        float currentTime = timeData.Time;
        
        if (currentTime >= _nextSpawnTime)
        {
            TriggerRandomQuest();
            // 다음 스폰 시간 갱신
            _nextSpawnTime += offerInterval;
        }
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
    
    // 랜덤 퀘스트 생성
    private void TriggerRandomQuest()
    {
        if (questPool.Count == 0 || spawnPoints.Count == 0) return;

        // 랜덤 퀘스트 및 위치 선정, 추후 가중치 적용시 구조 변경 필요( 가중치를 반영하는 새로운 함수)
        var randomDef = questPool[UnityEngine.Random.Range(0, 
            questPool.Count)];
        var randomPos = spawnPoints[UnityEngine.Random.Range(0, 
            spawnPoints.Count)];

        SpawnQuest(randomDef, randomPos);

        // 이벤트 발행
        OnQuestStarted?.Invoke(randomDef.name);
        Debug.Log($"[Quest] 퀘스트 생성: {randomDef.name} _ {randomPos}");
    }

    private void ApplyRewards(QuestDefinitionSO definition)
    { 
        SkillAwakeningApplier applier = FindFirstObjectByType<SkillAwakeningApplier>(); 
        // if (applier != null)
            // applier.ApplyAwakening(); todo 각성 스킬 관련 정보는 퀘스트 매니져의 책임이 아님
    }
}