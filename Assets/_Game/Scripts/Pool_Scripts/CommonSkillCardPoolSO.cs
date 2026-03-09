using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 레벨업 뽑기 대상 카드(CommonSkillCardSO)들의 풀입니다.
/// SkillRootSO와 CommonSkillCatalogSO 양쪽에 같은 인스턴스를 연결해야 합니다.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/공통스킬/카드 풀", fileName = "CCardPool_")]
public sealed class CommonSkillCardPoolSO : ScriptableObject
{
    [Header("카드 목록")]
    [Tooltip("레벨업 뽑기에 포함될 카드들입니다. 각 카드에는 가중치와 스킬 참조가 있습니다.")]
    public List<CommonSkillCardSO> cards = new List<CommonSkillCardSO>(16);
}