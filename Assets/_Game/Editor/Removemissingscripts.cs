// ============================================================================
// RemoveMissingScripts.cs
// 경로: Assets/_Game/Scripts/Editor/RemoveMissingScripts.cs
// 용도: 프리팹에서 Missing Script 컴포넌트를 일괄 제거하는 에디터 도구
// 
// [사용법]
// 1. Project 창에서 정리할 프리팹을 선택 (복수 선택 가능)
// 2. 상단 메뉴 → Tools → Remove Missing Scripts (Selected) 클릭
// 3. Console에서 초록색 결과 메시지 확인
//
// ⚠ Editor 폴더 안에 있어야 합니다. 빌드에 포함되지 않습니다.
// ============================================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RemoveMissingScripts
{
    [MenuItem("Tools/Remove Missing Scripts (Selected)")]
    static void Remove()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("[RemoveMissingScripts] 선택된 오브젝트가 없습니다. Project 창에서 프리팹을 선택해주세요.");
            return;
        }

        int totalRemoved = 0;

        foreach (var go in Selection.gameObjects)
        {
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

            // 자식 오브젝트도 전부 순회
            foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
            {
                count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            }

            totalRemoved += count;

            if (count > 0)
                Debug.Log($"<color=green>[RemoveMissingScripts] '{go.name}' 프리팹에서 Missing Script {count}개 제거 완료!</color>", go);
            else
                Debug.Log($"[RemoveMissingScripts] '{go.name}' → Missing Script 없음 (깨끗함)", go);
        }

        Debug.Log($"<color=green>[RemoveMissingScripts] 전체 작업 완료. 총 {totalRemoved}개 제거됨.</color>");
    }
}
#endif