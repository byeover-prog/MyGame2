using UnityEngine;

[System.Serializable]
public struct PassiveLevelParams
{
    [Tooltip("퍼센트 계열(0.05 = +5%)")]
    public float addPercent;

    [Tooltip("정수 가산(예: 최대체력 +10)")]
    public int addInt;
}