using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponDatabaseSO : ScriptableObject
{
    [SerializeField] private List<WeaponDefinitionSO> weapons = new List<WeaponDefinitionSO>();

    public IReadOnlyList<WeaponDefinitionSO> Weapons => weapons;

    public bool TryGet(string weaponId, out WeaponDefinitionSO def)
    {
        def = null;
        if (string.IsNullOrWhiteSpace(weaponId)) return false;

        for (int i = 0; i < weapons.Count; i++)
        {
            var w = weapons[i];
            if (w == null) continue;
            if (w.weaponId == weaponId)
            {
                def = w;
                return true;
            }
        }
        return false;
    }
}