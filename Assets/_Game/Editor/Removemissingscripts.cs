#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RemoveMissingScripts
{
    [MenuItem("Tools/Remove Missing Scripts (Selected)")]
    static void RemoveSelected()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("[RemoveMissingScripts] 선택된 오브젝트가 없습니다.");
            return;
        }

        int totalRemoved = 0;
        foreach (var go in Selection.gameObjects)
        {
            totalRemoved += CleanGameObject(go);
        }

        Debug.Log($"<color=green>[RemoveMissingScripts] 선택 완료. 총 {totalRemoved}개 제거됨.</color>");
    }

    [MenuItem("Tools/Remove Missing Scripts (All Prefabs)")]
    static void RemoveAllPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int totalRemoved = 0;
        int prefabsFixed = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            int count = CleanGameObject(prefab);
            if (count > 0)
            {
                prefabsFixed++;
                totalRemoved += count;
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"<color=green>[RemoveMissingScripts] '{path}' → {count}개 제거</color>");
            }

            if (i % 50 == 0)
                EditorUtility.DisplayProgressBar("Missing Script 제거", path, (float)i / guids.Length);
        }

        EditorUtility.ClearProgressBar();
        Debug.Log($"<color=green>[RemoveMissingScripts] 전체 프리팹 스캔 완료. {prefabsFixed}개 프리팹에서 총 {totalRemoved}개 제거됨.</color>");
    }

    [MenuItem("Tools/Remove Missing Scripts (Current Scene)")]
    static void RemoveCurrentScene()
    {
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int totalRemoved = 0;

        foreach (var go in allObjects)
        {
            totalRemoved += CleanGameObject(go);
        }

        Debug.Log($"<color=green>[RemoveMissingScripts] 현재 씬 완료. 총 {totalRemoved}개 제거됨.</color>");
    }

    private static int CleanGameObject(GameObject go)
    {
        int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
        {
            count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }
        return count;
    }
}
#endif