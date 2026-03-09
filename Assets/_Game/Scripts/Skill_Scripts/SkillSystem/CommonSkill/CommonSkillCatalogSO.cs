using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 프로젝트에 존재하는 모든 공통 스킬(CommonSkillConfigSO)의 목록을 보관합니다.
/// SkillRootSO에 연결되어, 런타임에서 카드 뽑기/UI 표시 등에 사용됩니다.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/공통스킬/카탈로그", fileName = "CommonSkillCatalog")]
public sealed class CommonSkillCatalogSO : ScriptableObject
{
    [Header("공통 스킬 목록")]
    [Tooltip("이 카탈로그에 등록된 모든 공통 스킬 설정(CommonSkillConfigSO)입니다.")]
    public List<CommonSkillConfigSO> skills = new List<CommonSkillConfigSO>(16);

    [Header("카드 풀")]
    [Tooltip("레벨업 시 뽑기에 사용할 카드 풀입니다. SkillRootSO에도 같은 풀을 연결해야 합니다.")]
    public CommonSkillCardPoolSO cardPool;
}