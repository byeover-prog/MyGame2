using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIRaycastProbe : MonoBehaviour
{
    [Header("테스트 키(마우스 좌클릭)")]
    [SerializeField] private int mouseButton = 0;

    [Header("로그 최대 출력 개수")]
    [SerializeField] private int maxLogCount = 15;

    private readonly List<RaycastResult> _results = new List<RaycastResult>(64);
    private PointerEventData _ped;

    private void Awake()
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("[UIRaycastProbe] EventSystem이 없습니다. UI 입력이 동작하지 않습니다.");
            enabled = false;
            return;
        }

        _ped = new PointerEventData(EventSystem.current);
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(mouseButton)) return;

        _results.Clear();
        _ped.position = Input.mousePosition;

        EventSystem.current.RaycastAll(_ped, _results);

        var sb = new StringBuilder(512);
        sb.AppendLine($"[UIRaycastProbe] Raycast hits: {_results.Count}");

        int count = Mathf.Min(_results.Count, maxLogCount);
        for (int i = 0; i < count; i++)
        {
            var r = _results[i];
            string goPath = GetPath(r.gameObject.transform);
            sb.AppendLine($"  {i:00}. {goPath}  (module:{r.module}, depth:{r.depth}, sortOrder:{r.sortingOrder}, distance:{r.distance:0.00})");
        }

        Debug.Log(sb.ToString());
    }

    private static string GetPath(Transform t)
    {
        if (t == null) return "(null)";
        var sb = new StringBuilder(128);
        while (t != null)
        {
            if (sb.Length == 0) sb.Append(t.name);
            else sb.Insert(0, t.name + "/");
            t = t.parent;
        }
        return sb.ToString();
    }
}