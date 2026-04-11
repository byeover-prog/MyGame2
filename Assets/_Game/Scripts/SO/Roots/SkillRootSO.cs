using UnityEngine;

// 스킬 시스템의 글로벌 설정입니다.

[CreateAssetMenu(menuName = "혼령검/시스템/스킬 루트 설정", fileName = "SkillRoot")]
public sealed class SkillRootSO : ScriptableObject
{
    [Header("공통 스킬")]
    [Tooltip("공통 스킬 카드풀 SO입니다.")]
    public CommonSkillCardPoolSO commonSkillCardPool;
}