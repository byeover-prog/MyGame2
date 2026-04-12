// UTF-8
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/Enemies/Enemy Root", fileName = "EnemyRoot")]
public sealed class EnemyRootSO : ScriptableObject
{
    [Serializable]
    public sealed class EnemyEntry
    {
        public string Id;
        public GameObject Prefab;
        [Min(0f)] public float Weight = 1f;

        [Min(1)] public int BaseHP = 10;
        [Min(0f)] public float BaseMoveSpeed = 2.5f;
        [Min(0)] public int BaseContactDamage = 1;
    }

    public List<EnemyEntry> Enemies = new List<EnemyEntry>(16);
}