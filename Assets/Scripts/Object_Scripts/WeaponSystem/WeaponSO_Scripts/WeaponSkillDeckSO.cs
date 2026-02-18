using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon Skill Deck", fileName = "WeaponSkillDeck_")]
public sealed class WeaponSkillDeckSO : ScriptableObject
{
    [SerializeField] private List<WeaponSkillSO> skills = new List<WeaponSkillSO>();

    public IReadOnlyList<WeaponSkillSO> Skills => skills;
}