using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CommonSkillCatalog 진단 덤프 v2.
/// FindAssets가 실패해도 전체 SO 순회로 재시도하여 반드시 출력을 남김.
/// </summary>
public static class CommonSkillCatalogDumper
{
    [MenuItem("Tools/Common Skill/Catalog 덤프 v2")]
    public static void Dump()
    {
        // 생명 신호 — 메뉴 실행 여부 자체를 확인
        Debug.Log("[CatalogDumper] ▶ 실행 시작");

        try
        {
            // 1차: 타입명 필터로 Catalog 찾기
            var guids = AssetDatabase.FindAssets("t:CommonSkillCatalogSO");
            Debug.Log($"[CatalogDumper] FindAssets 결과: {guids.Length}개");

            // 1차 실패 시 전체 SO 순회로 폴백
            if (guids.Length == 0)
            {
                Debug.LogWarning("[CatalogDumper] 타입 검색 실패 → 전체 ScriptableObject 폴백 검색");
                var allGuids = AssetDatabase.FindAssets("t:ScriptableObject");
                int hit = 0;
                foreach (var g in allGuids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(p);
                    if (so == null) continue;
                    string typeName = so.GetType().Name;
                    if (typeName.Contains("Catalog") || typeName.Contains("CardPool"))
                    {
                        Debug.Log($"[CatalogDumper] 후보 발견: {typeName} @ {p}", so);
                        DumpSO(so);
                        hit++;
                    }
                }
                if (hit == 0)
                    Debug.LogError("[CatalogDumper] Catalog/CardPool 관련 SO 전혀 없음. 타입명 확인 필요.");
                return;
            }

            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (catalog == null)
                {
                    Debug.LogError($"[CatalogDumper] Load 실패: {path}");
                    continue;
                }
                DumpSO(catalog);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CatalogDumper] 예외 발생: {e}");
        }

        Debug.Log("[CatalogDumper] ◀ 실행 종료");
    }

    /// <summary>대상 SO의 모든 object reference 배열을 덤프</summary>
    private static void DumpSO(ScriptableObject target)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== [{target.name}] ({target.GetType().Name}) ===");

        var so = new SerializedObject(target);
        var it = so.GetIterator();
        bool enter = true;
        while (it.NextVisible(enter))
        {
            enter = false;

            if (it.isArray && it.propertyType == SerializedPropertyType.Generic)
            {
                sb.AppendLine($"[{it.name}] count={it.arraySize}");
                for (int i = 0; i < it.arraySize; i++)
                {
                    var e = it.GetArrayElementAtIndex(i);
                    if (e.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        var obj = e.objectReferenceValue;
                        sb.AppendLine($"  [{i}] {DescribeIfSkill(obj)}");
                    }
                }
            }
            else if (it.propertyType == SerializedPropertyType.ObjectReference)
            {
                sb.AppendLine($"[{it.name}] {(it.objectReferenceValue ? it.objectReferenceValue.name : "NULL")}");
            }
        }

        Debug.Log(sb.ToString(), target);
    }

    /// <summary>참조가 공통스킬 SO면 핵심 필드까지 같이 요약</summary>
    private static string DescribeIfSkill(Object obj)
    {
        if (obj == null) return "<NULL>";

        var so = new SerializedObject(obj);
        var kind   = so.FindProperty("kind");
        var weapon = so.FindProperty("weaponPrefab");
        var levels = so.FindProperty("levels");

        if (kind == null && weapon == null) return obj.name; // 공통스킬 아님

        string kindStr   = kind   != null && kind.propertyType == SerializedPropertyType.Enum
                           ? kind.enumDisplayNames[kind.enumValueIndex] : "?";
        string weaponStr = weapon != null ? (weapon.objectReferenceValue ? "OK" : "NULL!") : "?";
        int    lvCount   = levels != null ? levels.arraySize : 0;

        return $"{obj.name} | kind={kindStr} | weapon={weaponStr} | levels={lvCount}";
    }
}