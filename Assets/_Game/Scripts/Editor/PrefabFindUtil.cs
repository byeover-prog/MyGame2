#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PrefabFindUtil
{
    public static GameObject FindPrefabWithComponent<T>() where T : Component
    {
        var guids = AssetDatabase.FindAssets("t:prefab");
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            if (go.GetComponentInChildren<T>(true) != null)
                return go;
        }
        return null;
    }
}
#endif