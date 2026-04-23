using System;
using UnityEngine;

[Serializable]
public struct EquipmentEffect
{
    [Header("효과 종류")]
    [Tooltip("이 효과가 어떤 스탯/시스템에 붙는지")]
    public EquipmentEffectType type;

    [Header("효과 수치")]
    [Tooltip("효과 값. %는 정수로 (예: 15%면 15), 가속/정수 효과는 그대로 입력")]
    public float value;
}