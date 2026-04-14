using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct QuestEntity : IComponentData
{
    public QuestDefinitionSO definition;
    public float startTime;
    public int questId;
}
