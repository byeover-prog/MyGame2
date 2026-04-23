using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 레벨업 카드 1장의 데이터입니다.
/// 어떤 공통 스킬(CommonSkillConfigSO)을 가리키는지, 뽑기 가중치는 얼마인지를 정의합니다.
/// CommonSkillCardPoolSO에 등록해야 실제 뽑기 대상이 됩니다.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/공통스킬/레벨업 카드", fileName = "CCard_")]
public sealed class CommonSkillCardSO : ScriptableObject
{
    [Header("연결된 스킬")]
    [Tooltip("이 카드가 부여/업그레이드할 공통 스킬 설정입니다.")]
    public CommonSkillConfigSO skill;

    [Header("뽑기 가중치")]
    [Tooltip("숫자가 클수록 뽑기에서 더 자주 등장합니다. 기본값 10.")]
    [Min(1)] public int weight = 10;
}