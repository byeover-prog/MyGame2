using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon Skill Deck", fileName = "WeaponSkillDeck_")]
public sealed class WeaponSkillDeckSO : ScriptableObject
{
    [Header("카드 풀(무기 후보)")]
    [Tooltip("레벨업 카드로 '등장 가능한' 무기 정의(WeaponDefinitionSO) 목록")]
    [SerializeField] private List<WeaponDefinitionSO> weapons = new List<WeaponDefinitionSO>();

    public IReadOnlyList<WeaponDefinitionSO> Weapons => weapons;

    public bool IsEmpty => weapons == null || weapons.Count == 0;
}