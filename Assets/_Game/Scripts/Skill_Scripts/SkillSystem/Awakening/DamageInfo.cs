// DamageInfo.cs
using UnityEngine;

public readonly struct DamageInfo
{
    public readonly float Amount;
    public readonly Transform Source;

    public DamageInfo(float amount, Transform source)
    {
        Amount = amount;
        Source = source;
    }
}