using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/공통스킬/레벨업 카드", fileName = "CCard_")]
public sealed class CommonSkillCardSO : ScriptableObject
{
    public CommonSkillConfigSO skill;
    [Min(1)] public int weight = 10;
}