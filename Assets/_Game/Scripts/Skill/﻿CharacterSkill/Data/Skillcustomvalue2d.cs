using System;
using UnityEngine;

/// <summary>
/// 스킬별 특수 수치를 담는 키-값 데이터입니다.
/// 구현 원리:
///  공통 수치(damage/cooldown/radius 등)로 표현하기 어려운 값은 key로 조회합니다.
///  예: hitRadius, frostDuration, attachDelay, awakeningExtraCasts
/// </summary>
public enum SkillCustomValueType2D
{
    Float,
    Int,
    Bool,
    String
}

[Serializable]
public sealed class SkillCustomValue2D
{
    [Header("키")]
    [Tooltip("스크립트에서 조회할 이름입니다. 예: hitRadius, frostDuration, attachDelay")]
    [SerializeField] private string key;

    [Tooltip("이 값의 자료형입니다.")]
    [SerializeField] private SkillCustomValueType2D valueType = SkillCustomValueType2D.Float;

    [Header("값")]
    [Tooltip("실수 값입니다.")]
    [SerializeField] private float floatValue;

    [Tooltip("정수 값입니다.")]
    [SerializeField] private int intValue;

    [Tooltip("참/거짓 값입니다.")]
    [SerializeField] private bool boolValue;

    [Tooltip("문자열 값입니다.")]
    [SerializeField] private string stringValue;

    public string Key => key;
    public SkillCustomValueType2D ValueType => valueType;
    public float FloatValue => floatValue;
    public int IntValue => intValue;
    public bool BoolValue => boolValue;
    public string StringValue => stringValue;

    public bool IsKey(string targetKey)
    {
        return Normalize(key) == Normalize(targetKey);
    }

    public bool TryGetFloat(string targetKey, out float value)
    {
        value = 0f;

        if (!IsKey(targetKey))
            return false;

        if (valueType == SkillCustomValueType2D.Float)
        {
            value = floatValue;
            return true;
        }

        if (valueType == SkillCustomValueType2D.Int)
        {
            value = intValue;
            return true;
        }

        return false;
    }

    public bool TryGetInt(string targetKey, out int value)
    {
        value = 0;

        if (!IsKey(targetKey))
            return false;

        if (valueType == SkillCustomValueType2D.Int)
        {
            value = intValue;
            return true;
        }

        if (valueType == SkillCustomValueType2D.Float)
        {
            value = Mathf.RoundToInt(floatValue);
            return true;
        }

        return false;
    }

    public bool TryGetBool(string targetKey, out bool value)
    {
        value = false;

        if (!IsKey(targetKey))
            return false;

        if (valueType != SkillCustomValueType2D.Bool)
            return false;

        value = boolValue;
        return true;
    }

    public bool TryGetString(string targetKey, out string value)
    {
        value = string.Empty;

        if (!IsKey(targetKey))
            return false;

        if (valueType != SkillCustomValueType2D.String)
            return false;

        value = stringValue;
        return true;
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .Trim()
            .Replace("_", "")
            .Replace("-", "")
            .Replace(" ", "")
            .ToLowerInvariant();
    }
}