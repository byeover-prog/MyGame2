using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/공통스킬/카탈로그", fileName = "CommonSkillCatalog")]
public sealed class CommonSkillCatalogSO : ScriptableObject
{
    public List<CommonSkillConfigSO> skills = new List<CommonSkillConfigSO>(16);
    public CommonSkillCardPoolSO cardPool;
}