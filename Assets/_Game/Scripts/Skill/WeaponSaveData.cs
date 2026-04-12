using System;
using System.Collections.Generic;

[Serializable]
public sealed class WeaponSaveData
{
    public List<WeaponSlotSave> slots = new List<WeaponSlotSave>();
}

[Serializable]
public sealed class WeaponSlotSave
{
    public string weaponId;
    public bool enabled = true;

    public int bonusDamage = 0;
    public float cooldownMul = 1f;
    public float rangeAdd = 0f;
}
