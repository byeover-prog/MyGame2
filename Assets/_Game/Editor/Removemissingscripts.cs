// UTF-8
// Assets/_Game/Scripts/Editor/RemoveMissingScripts.cs
//
// [사용법]
// 1. 이 파일을 Assets/_Game/Scripts/Editor/ 폴더에 넣는다 (Editor 폴더 없으면 만든다)
// 2. Project 창에서 DarkOrb 프리팹을 클릭해서 선택
// 3. 상단 메뉴 → Tools → Remove Missing Scripts (Selected)
// 4. Console에 제거 결과가 출력됨
// 5. 다른 프리팹(ThunderTalisman_Weapon 등)도 같은 방식으로 선택 후 실행
//
// ★ 이 스크립트는 Editor 전용이므로 빌드에 포함되지 않음

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class RemoveMissingScripts
{
    [MenuItem("Tools/Remove Missing Scripts (Selected)")]
    public static void Run()
    {
        // ── 선택된 오브젝트 확인 ──
        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("[RemoveMissingScripts] Project 창에서 프리팹을 먼저 선택하세요.");
            return;
        }

        int totalRemoved = 0;

        foreach (GameObject go in selected)
        {
            // 프리팹인 경우 프리팹 내용물을 열어서 수정
            string path = AssetDatabase.GetAssetPath(go);
            if (!string.IsNullOrEmpty(path))
            {
                // Project 창에서 선택한 프리팹 에셋
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                int count = CleanGameObject(prefabRoot);
                totalRemoved += count;

                if (count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                    Debug.Log($"<color=lime>[RemoveMissingScripts] '{go.name}' 프리팹에서 Missing Script {count}개 제거 완료!</color>");
                }
                else
                {
                    Debug.Log($"[RemoveMissingScripts] '{go.name}' 프리팹에는 Missing Script가 없습니다.");
                }

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            else
            {
                // Hierarchy(씬)에서 선택한 오브젝트
                int count = CleanGameObject(go);
                totalRemoved += count;

                if (count > 0)
                    Debug.Log($"<color=lime>[RemoveMissingScripts] '{go.name}' 씬 오브젝트에서 Missing Script {count}개 제거 완료!</color>");
                else
                    Debug.Log($"[RemoveMissingScripts] '{go.name}' 씬 오브젝트에는 Missing Script가 없습니다.");
            }
        }

        Debug.Log($"<color=cyan>[RemoveMissingScripts] 전체 작업 완료: 총 {totalRemoved}개 Missing Script 제거</color>");
    }

    /// <summary>
    /// 대상 GameObject와 모든 자식에서 Missing Script를 제거한다.
    /// </summary>
    private static int CleanGameObject(GameObject root)
    {
        int removed = 0;
        Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in allTransforms)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            if (count > 0)
            {
                Undo.RegisterCompleteObjectUndo(t.gameObject, "Remove Missing Scripts");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                removed += count;
                Debug.Log($"  → '{t.gameObject.name}'에서 {count}개 제거");
            }
        }

        return removed;
    }
}
#endif