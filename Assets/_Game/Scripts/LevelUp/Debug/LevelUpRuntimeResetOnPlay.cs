// UTF-8
// 플레이 시작마다 런타임 레벨 상태를 강제로 초기화(Reset/Clear/Initialize류 메서드 자동 탐색)
using System;
using System.Reflection;
using UnityEngine;

public sealed class LevelUpRuntimeResetOnPlay : MonoBehaviour
{
    [SerializeField] private Component runtimeState; // 비우면 자동 탐색

    private void Awake()
    {
        if (runtimeState == null)
        {
            // 이름이 달라도 타입명에 RuntimeState 들어간 컴포넌트를 우선 탐색
            var all = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var c in all)
            {
                if (c == null) continue;
                string tn = c.GetType().Name.ToLowerInvariant();
                if (tn.Contains("runtimestate"))
                {
                    runtimeState = c;
                    break;
                }
            }
        }

        if (runtimeState == null) return;

        var t = runtimeState.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // 가능한 초기화 메서드 이름들
        string[] names = { "ResetAll", "Reset", "Clear", "Initialize", "Init" };

        foreach (var n in names)
        {
            var m = t.GetMethod(n, flags);
            if (m == null) continue;

            var ps = m.GetParameters();
            if (ps.Length == 0)
            {
                m.Invoke(runtimeState, null);
                Debug.Log($"[LevelUpRuntimeResetOnPlay] {t.Name}.{n}() 호출", this);
                return;
            }
        }

        Debug.LogWarning($"[LevelUpRuntimeResetOnPlay] {t.Name}에서 Reset/Clear 계열 메서드를 찾지 못했습니다. (수동 초기화 필요)", this);
    }
}