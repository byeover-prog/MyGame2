using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct QuestEntity : IComponentData
{
    public float startTime;
    public int questId;
}
