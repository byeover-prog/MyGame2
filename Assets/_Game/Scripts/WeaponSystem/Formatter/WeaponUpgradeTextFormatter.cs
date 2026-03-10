using System;
using System.Reflection;

// [요약] 레벨업 시 화면에 표시되는 스킬 카드 텍스트(이름, 설명)를 기획에 맞게 변환하는 도우미 클래스
public sealed class WeaponUpgradeTextFormatter
{
    /// <summary>
    /// 카드 UI의 제목(스킬 이름)을 반환합니다.
    /// 기획 요구사항에 맞춰 이름 옆에 (Lv.X)가 붙지 않도록 문자열을 정리합니다.
    /// </summary>
    public string BuildTitle(string originalName)
    {
        if (string.IsNullOrEmpty(originalName)) return string.Empty;

        // 원본 이름에 "(Lv." 같은 글자가 있다면 그 앞까지만 잘라서 깔끔하게 반환
        int lvIndex = originalName.IndexOf("(Lv.", StringComparison.OrdinalIgnoreCase);
        if (lvIndex >= 0)
        {
            return originalName.Substring(0, lvIndex).Trim();
        }

        // 패시브 등 원래 레벨 표기가 없는 경우 원본 그대로 반환
        return originalName;
    }

    /// <summary>
    /// 카드 UI의 설명을 반환합니다.
    /// 패시브인 경우 수치가 0으로 나오지 않도록 SO 데이터에서 직접 수치를 읽어옵니다.
    /// </summary>
    public string BuildDescription(object weaponUpgradeCardSo)
    {
        if (weaponUpgradeCardSo == null) return string.Empty;

        // 1) 패시브인 경우 특수 처리 (PassiveConfigSO)
        // 리플렉션을 통해 타입을 확인하고 수치를 추출합니다. (0 방지)
        if (weaponUpgradeCardSo.GetType().Name.Contains("PassiveConfigSO"))
        {
            // 설명 원본 텍스트 가져오기
            string baseDesc = ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "descriptionKr") 
                           ?? ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "description") 
                           ?? "패시브 효과";

            // 실제 수치(StatValue) 가져오기 (배열이든 단일 필드든 유연하게 대처)
            object levelsArray = ReflectionValueReader.TryGetPropertyOrField(weaponUpgradeCardSo, "levels");
            float realStatValue = 0f;

            if (levelsArray is Array arr && arr.Length > 0)
            {
                // levels[0].statValue를 가져옵니다.
                object levelParams = arr.GetValue(0);
                object valObj = ReflectionValueReader.TryGetPropertyOrField(levelParams, "statValue");
                if (valObj is float fVal) realStatValue = fVal;
            }
            else
            {
                // 만약 단일 필드로 존재한다면
                object valObj = ReflectionValueReader.TryGetPropertyOrField(weaponUpgradeCardSo, "StatValue") 
                             ?? ReflectionValueReader.TryGetPropertyOrField(weaponUpgradeCardSo, "statValue");
                if (valObj is float fVal) realStatValue = fVal;
            }

            // 예: "공격력을 증가시킵니다. (효과: 10%)"
            return $"{baseDesc} (효과: {realStatValue}%)";
        }

        // 2) 일반 스킬인 경우 (기존 로직 유지)
        string desc =
            ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "descriptionKorean") ??
            ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "descKorean") ??
            ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "descriptionKr") ??
            ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "uiDescription") ??
            ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "uiDesc") ??
            ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "description") ??
            ReflectionValueReader.TryGetString(weaponUpgradeCardSo, "desc") ??
            string.Empty;

        return desc;
    }

    private static class ReflectionValueReader
    {
        // 문자열(String) 전용 추출기
        public static string TryGetString(object obj, string memberName)
        {
            var val = TryGetPropertyOrField(obj, memberName);
            return val as string;
        }

        // 범용(Object) 추출기 (배열이나 float 수치를 꺼낼 때 사용)
        public static object TryGetPropertyOrField(object obj, string memberName)
        {
            if (obj == null) return null;
            var t = obj.GetType();

            // property (프로퍼티 접근)
            var p = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null)
            {
                return p.GetValue(obj);
            }

            // field (필드 변수 접근)
            var f = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                return f.GetValue(obj);
            }

            return null;
        }
    }
}