using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// UpgradeValue의 멤버명이 프로젝트마다 다를 수 있어서(예: AddInt/AddFloat/MulFloat/ToggleBool vs 다른 이름),
/// 컴파일을 깨지지 않게 "호환 읽기"를 제공한다.
/// - 우선순위: (필드/프로퍼티) 이름 후보를 순서대로 탐색
/// - 못 찾으면 0/false 반환
///
/// 빌드 안정성:
/// - 리플렉션 탐색은 최초 1회만 수행하고, 이후 델리게이트로 접근한다.
/// </summary>
public static class UpgradeValueCompat
{
    private static bool _initialized;

    private static Func<UpgradeValue, int> _getAddInt;
    private static Func<UpgradeValue, float> _getAddFloat;
    private static Func<UpgradeValue, float> _getMulFloat;
    private static Func<UpgradeValue, bool> _getBool;

    private static bool _warned;

    public static int GetAddInt(UpgradeValue v)
    {
        EnsureInit();
        return _getAddInt != null ? _getAddInt(v) : 0;
    }

    public static float GetAddFloat(UpgradeValue v)
    {
        EnsureInit();
        return _getAddFloat != null ? _getAddFloat(v) : 0f;
    }

    public static float GetMulFloat(UpgradeValue v)
    {
        EnsureInit();
        return _getMulFloat != null ? _getMulFloat(v) : 0f;
    }

    public static bool GetBool(UpgradeValue v)
    {
        EnsureInit();
        return _getBool != null ? _getBool(v) : false;
    }

    private static void EnsureInit()
    {
        if (_initialized) return;
        _initialized = true;

        Type t = typeof(UpgradeValue);

        // int(가산)
        _getAddInt = BuildGetterInt(t,
            "AddInt", "addInt",
            "IntValue", "intValue",
            "ValueInt", "valueInt",
            "AmountInt", "amountInt"
        );

        // float(가산)
        _getAddFloat = BuildGetterFloat(t,
            "AddFloat", "addFloat",
            "FloatValue", "floatValue",
            "ValueFloat", "valueFloat",
            "AmountFloat", "amountFloat"
        );

        // float(배율)
        _getMulFloat = BuildGetterFloat(t,
            "MulFloat", "mulFloat",
            "Multiplier", "multiplier",
            "Mul", "mul"
        );

        // bool(토글)
        _getBool = BuildGetterBool(t,
            "ToggleBool", "toggleBool",
            "BoolValue", "boolValue",
            "ValueBool", "valueBool",
            "Enabled", "enabled"
        );

        if (!_warned && (_getAddInt == null || _getAddFloat == null || _getMulFloat == null || _getBool == null))
        {
            _warned = true;
            Debug.LogWarning(
                "[UpgradeValueCompat] UpgradeValue에서 일부 필드/프로퍼티를 찾지 못했습니다. " +
                "해당 값은 0/false로 처리될 수 있습니다. UpgradeValue 정의 파일의 멤버명을 확인하세요."
            );
        }
    }

    private static Func<UpgradeValue, int> BuildGetterInt(Type t, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (TryBuildFieldGetterInt(t, names[i], out var gf)) return gf;
            if (TryBuildPropGetterInt(t, names[i], out var gp)) return gp;
        }
        return null;
    }

    private static Func<UpgradeValue, float> BuildGetterFloat(Type t, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (TryBuildFieldGetterFloat(t, names[i], out var gf)) return gf;
            if (TryBuildPropGetterFloat(t, names[i], out var gp)) return gp;
        }
        return null;
    }

    private static Func<UpgradeValue, bool> BuildGetterBool(Type t, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (TryBuildFieldGetterBool(t, names[i], out var gf)) return gf;
            if (TryBuildPropGetterBool(t, names[i], out var gp)) return gp;
        }
        return null;
    }

    private static bool TryBuildFieldGetterInt(Type t, string name, out Func<UpgradeValue, int> getter)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo f = t.GetField(name, flags);
        if (f != null && f.FieldType == typeof(int))
        {
            getter = (UpgradeValue v) => (int)f.GetValue(v);
            return true;
        }

        getter = null;
        return false;
    }

    private static bool TryBuildPropGetterInt(Type t, string name, out Func<UpgradeValue, int> getter)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        PropertyInfo p = t.GetProperty(name, flags);
        if (p != null && p.PropertyType == typeof(int) && p.GetGetMethod(true) != null)
        {
            getter = (UpgradeValue v) => (int)p.GetValue(v, null);
            return true;
        }

        getter = null;
        return false;
    }

    private static bool TryBuildFieldGetterFloat(Type t, string name, out Func<UpgradeValue, float> getter)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo f = t.GetField(name, flags);
        if (f != null && f.FieldType == typeof(float))
        {
            getter = (UpgradeValue v) => (float)f.GetValue(v);
            return true;
        }

        getter = null;
        return false;
    }

    private static bool TryBuildPropGetterFloat(Type t, string name, out Func<UpgradeValue, float> getter)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        PropertyInfo p = t.GetProperty(name, flags);
        if (p != null && p.PropertyType == typeof(float) && p.GetGetMethod(true) != null)
        {
            getter = (UpgradeValue v) => (float)p.GetValue(v, null);
            return true;
        }

        getter = null;
        return false;
    }

    private static bool TryBuildFieldGetterBool(Type t, string name, out Func<UpgradeValue, bool> getter)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo f = t.GetField(name, flags);
        if (f != null && f.FieldType == typeof(bool))
        {
            getter = (UpgradeValue v) => (bool)f.GetValue(v);
            return true;
        }

        getter = null;
        return false;
    }

    private static bool TryBuildPropGetterBool(Type t, string name, out Func<UpgradeValue, bool> getter)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        PropertyInfo p = t.GetProperty(name, flags);
        if (p != null && p.PropertyType == typeof(bool) && p.GetGetMethod(true) != null)
        {
            getter = (UpgradeValue v) => (bool)p.GetValue(v, null);
            return true;
        }

        getter = null;
        return false;
    }
}
