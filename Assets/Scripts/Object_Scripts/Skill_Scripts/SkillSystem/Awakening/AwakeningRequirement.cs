// AwakeningRequirement.cs
using System;
using UnityEngine;

[Serializable]
public struct AwakeningRequirement
{
    [SerializeField] private string weaponId;
    [SerializeField] private int minLevel;

    public string WeaponId => weaponId;
    public int MinLevel => minLevel;
}