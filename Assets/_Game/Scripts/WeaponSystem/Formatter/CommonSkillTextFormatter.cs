using System;
using System.Reflection;

public sealed class CommonSkillTextFormatter
{
    // 공통 스킬은 기존에 View에서 switch로 desc를 만들고 있었음.
    // Phase1에서는 "View 밖"으로만 빼고, 데이터 구조는 건드리지 않는다.
    // 그래서 1차는 "CommonSkillConfigSO 또는 CommonSkillCardSO에 이미 있는 설명"을 우선 사용한다.
    public string BuildDescription(object commonSkillConfigOrCardSo)
    {
        if (commonSkillConfigOrCardSo == null) return string.Empty;

        string desc =
            ReflectionValueReader.TryGetString(commonSkillConfigOrCardSo, "descriptionKorean") ??
            ReflectionValueReader.TryGetString(commonSkillConfigOrCardSo, "descKorean") ??
            ReflectionValueReader.TryGetString(commonSkillConfigOrCardSo, "uiDescription") ??
            ReflectionValueReader.TryGetString(commonSkillConfigOrCardSo, "uiDesc") ??
            ReflectionValueReader.TryGetString(commonSkillConfigOrCardSo, "description") ??
            ReflectionValueReader.TryGetString(commonSkillConfigOrCardSo, "desc");

        if (!string.IsNullOrEmpty(desc))
            return desc;

        // 마지막 fallback: 진짜 아무것도 없으면 최소 문구라도 제공
        return "효과가 강화 됩니다.";
    }

    private static class ReflectionValueReader
    {
        public static string TryGetString(object obj, string memberName)
        {
            var t = obj.GetType();

            var p = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(string))
                return p.GetValue(obj) as string;

            var f = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(string))
                return f.GetValue(obj) as string;

            return null;
        }
    }
}