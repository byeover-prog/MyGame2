using System;
using UnityEngine;

[Serializable]
public struct UpgradeValue
{
    [Tooltip("정수 가산값")]
    public int addInt;

    [Tooltip("실수 가산값")]
    public float addFloat;

    [Tooltip("실수 배율값 (예: 0.92 = 8% 감소)")]
    public float mulFloat;

    [Tooltip("토글값")]
    public bool toggleBool;

    public static UpgradeValue Toggle(bool v)
    {
        return new UpgradeValue { toggleBool = v };
    }

    public static UpgradeValue AddInt(int v)
    {
        return new UpgradeValue { addInt = v };
    }

    public static UpgradeValue AddFloat(float v)
    {
        return new UpgradeValue { addFloat = v };
    }

    public static UpgradeValue MulFloat(float v)
    {
        return new UpgradeValue { mulFloat = v };
    }
}