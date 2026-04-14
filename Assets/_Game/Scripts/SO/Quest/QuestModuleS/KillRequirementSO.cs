using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "혼령검/퀘스트/퀘스트 정의/퀘스트 모듈", menuName = "Quest_KillRequirement")]
public class KillRequirementSO : QuestModuleSO
{
    // Entity에 KillCount를 주입하기 위한 SO
    [Tooltip("퀘스트 달성을 위해 필요한 처치 수 입니다")]
    public int requiredKillCount;
    
    public override void AddComponents(Entity entity, EntityManager em)
    {
        em.AddComponentData(entity, new KillCount { CurrentKillCount = 0, TargetKillCount = requiredKillCount });
    }
}
