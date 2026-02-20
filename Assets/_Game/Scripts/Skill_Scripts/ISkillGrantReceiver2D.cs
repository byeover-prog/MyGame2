using UnityEngine;

public interface ISkillGrantReceiver2D
{
    // 시작 스킬/레벨업/로드아웃 등 "스킬을 부여"하는 공통 통로
    // - 반드시 "성공/실패"를 bool로 반환해서 디버깅 가능하게
    bool TryGrantSkill(ScriptableObject skill, int level);
}