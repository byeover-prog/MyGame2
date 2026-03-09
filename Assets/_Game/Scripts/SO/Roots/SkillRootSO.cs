using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "스킬 데이터"의 단일 진입점.
/// 
/// 포함 범위
/// - 공통 스킬(카탈로그/카드풀)
/// - 무기(슬롯 기반) 레벨 트랙(1~8)
/// 
/// 주의
/// - 이 Root는 "데이터 참조"만 가진다.
/// - 런타임 상태(현재 레벨, 쿨타임 등)는 별도의 Runtime 컴포넌트가 가진다.
/// </summary>
[CreateAssetMenu(menuName = "그날이후/Roots/SkillRoot", fileName = "Root_Skill")]
public sealed class SkillRootSO : ScriptableObject
{
    [Header("공통 스킬")]
    [Tooltip("공통 스킬 카탈로그(원본 목록)")]
    public CommonSkillCatalogSO commonSkillCatalog;

    [Tooltip("레벨업 뽑기용 카드 풀")]
    public CommonSkillCardPoolSO commonSkillCardPool;

    [Header("무기(슬롯 기반) 레벨 트랙")]
    [Tooltip("WeaponDefinitionSO.weaponId와 매칭되는 트랙들")]
    public List<SkillLevelTrackSO> weaponSkillTracks = new List<SkillLevelTrackSO>(16);
}