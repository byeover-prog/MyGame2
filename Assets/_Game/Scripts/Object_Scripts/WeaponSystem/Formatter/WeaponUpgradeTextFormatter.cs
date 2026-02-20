using System;
using System.Reflection;

public sealed class WeaponUpgradeTextFormatter
{
    // WeaponUpgradeCardSO 안에 이미 "UI용 설명 문자열"이 있다고 문서에 적혀있음.
    // 프로젝트마다 필드명이 다를 수 있으니 후보 이름들로 안전하게 가져온다.
    public string BuildDescription(object weaponUpgradeCardSo)
    {
        if (weaponUpgradeCardSo == null) return string.Empty;

        // 가장 가능성 높은 후보들
        string desc =
            ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "descriptionKorean") ??
            ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "descKorean") ??
            ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "uiDescription") ??
            ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "uiDesc") ??
            ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "description") ??
            ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "desc") ??
            string.Empty;

        return desc;
    }

    private static class ReflectionValueReader
    {
        public static string TryGetString(object obj, string memberName)
        {
            var t = obj.GetType();

            // property
            var p = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(string))
            {
                return p.GetValue(obj) as string;
            }

            // field
            var f = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(string))
            {
                return f.GetValue(obj) as string;
            }

            return null;
        }
    }
}