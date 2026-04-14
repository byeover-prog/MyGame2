using Unity.Entities;
using UnityEngine;

public abstract class QuestModuleSO : ScriptableObject
{
    // 퀘스트 정의 SO에 필요한 정보만 남기기 위한 모듈화
    // 필요한 데이터 별로 모듈 SO를 상속 받아 데이터 관리
    
    // 엔티티에 모듈을 주입을 위임하는 코드
    public abstract void AddComponents(Entity entity, EntityManager em);
}
