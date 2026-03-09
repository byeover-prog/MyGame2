using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/업그레이드/무기 업그레이드 풀", fileName = "WeaponSkillDeck_")]
public sealed class WeaponSkillDeckSO : ScriptableObject
{
    [SerializeField] private List<WeaponDefinitionSO> weapons = new List<WeaponDefinitionSO>();

    public IReadOnlyList<WeaponDefinitionSO> Weapons => weapons;
    public bool IsEmpty => weapons == null || weapons.Count == 0;
}