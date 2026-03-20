using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 시스템의 글로벌 설정입니다.
/// RootBootstrapper에 연결하면 LevelUpSystem2D 등이 자동으로 읽어갑니다.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/시스템/스킬 루트 설정", fileName = "SkillRoot")]
public sealed class SkillRootSO : ScriptableObject
{
    [Header("공통 스킬")]
    [Tooltip("공통 스킬 카드풀 SO입니다. LevelUpSystem2D에서 후보 생성 시 사용합니다.")]
    public CommonSkillCardPoolSO commonSkillCardPool;

    [Header("무기 스킬 트랙")]
    [Tooltip("무기 스킬 업그레이드 트랙 목록입니다.")]
    public List<SkillLevelTrackSO> weaponSkillTracks;
}