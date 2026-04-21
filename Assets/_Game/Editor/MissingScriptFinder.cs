using UnityEditor;
using UnityEngine;

/// <summary>
/// 씬의 Missing Script 오브젝트를 콘솔에 출력
/// </summary>
public static class MissingScriptFinder
{
    [MenuItem("Tools/Find Missing Scripts In Scene")]
    public static void Find()
    {
        foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            var comps = go.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
                if (comps[i] == null)
                    Debug.LogError($"[Missing] {GetPath(go)} (슬롯 {i})", go);
        }
    }
    static string GetPath(GameObject g) =>
        g.transform.parent ? GetPath(g.transform.parent.gameObject) + "/" + g.name : g.name;
}