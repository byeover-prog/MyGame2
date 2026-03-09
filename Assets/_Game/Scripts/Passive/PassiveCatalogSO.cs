using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/패시브/카탈로그", fileName = "PassiveCatalog_")]
public sealed class PassiveCatalogSO : ScriptableObject
{
    public List<PassiveConfigSO> passives = new List<PassiveConfigSO>();
}