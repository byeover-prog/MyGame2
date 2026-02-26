// UTF-8
// 요약: SkillRunner가 스킬 프리팹에서 찾는 최소 인터페이스(정본 1개만 유지)

using UnityEngine;

public interface ILevelableSkill
{
    // 장착 직후 1회 호출(오너 전달)
    void OnAttached(Transform owner);

    // 레벨업 시 호출
    void ApplyLevel(int newLevel);
    
    void OnAttaced(Transform newOwner);
}