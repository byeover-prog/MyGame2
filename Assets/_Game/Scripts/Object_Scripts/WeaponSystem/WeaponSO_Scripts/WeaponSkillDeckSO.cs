using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 플레이어가 사용할 수 있는 무기 목록(덱)입니다.
/// WeaponDatabaseSO는 "전체 무기 DB"이고, 이 SO는 "이번 런에서 뽑기 대상이 될 무기"를 관리합니다.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/무기덱/무기 스킬 덱", fileName = "WeaponSkillDeck_")]
public sealed class WeaponSkillDeckSO : ScriptableObject
{
    [Header("덱에 포함된 무기")]
    [Tooltip("이번 게임(런)에서 레벨업 카드에 등장할 수 있는 무기 목록입니다.")]
    [SerializeField] private List<WeaponDefinitionSO> weapons = new List<WeaponDefinitionSO>();

    public IReadOnlyList<WeaponDefinitionSO> Weapons => weapons;
    public bool IsEmpty => weapons == null || weapons.Count == 0;
}