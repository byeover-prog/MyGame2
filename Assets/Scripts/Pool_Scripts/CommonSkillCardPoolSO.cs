using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/공통스킬/카드 풀", fileName = "CCardPool_")]
public sealed class CommonSkillCardPoolSO : ScriptableObject
{
    public List<CommonSkillCardSO> cards = new List<CommonSkillCardSO>(16);
}