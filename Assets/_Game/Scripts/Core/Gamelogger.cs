using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class GameLogger
{
    // 에디터 런타임 토글. false면 Log/LogWarning이 즉시 반환.
    // 기본값 OFF — 성능 테스트할 때 GC 부하를 없앰.
    // 메뉴 혼령검 → 게임 로그 ON/OFF 로 전환 가능.
    public static bool Enabled = false;
    
    //  일반 로그

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        if (!Enabled) return;
        Debug.Log(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, Object context)
    {
        if (!Enabled) return;
        Debug.Log(message, context);
    }
    
    //  경고 로그

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message)
    {
        if (!Enabled) return;
        Debug.LogWarning(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message, Object context)
    {
        if (!Enabled) return;
        Debug.LogWarning(message, context);
    }
    
    //  에러 로그 (항상 출력 — 토글 무시)

    public static void LogError(string message)
    {
        Debug.LogError(message);
    }

    public static void LogError(string message, Object context)
    {
        Debug.LogError(message, context);
    }
    
    //  에디터 메뉴 토글
    
#if UNITY_EDITOR
    private const string MENU_PATH = "혼령검/게임 로그 ON-OFF %#l"; // Ctrl+Shift+L

    [UnityEditor.MenuItem(MENU_PATH, false, 200)]
    private static void ToggleLog()
    {
        Enabled = !Enabled;
        Debug.Log($"[GameLogger] 게임 로그 {(Enabled ? "ON ✅" : "OFF ❌")}");
    }

    [UnityEditor.MenuItem(MENU_PATH, true)]
    private static bool ToggleLogValidate()
    {
        UnityEditor.Menu.SetChecked(MENU_PATH, Enabled);
        return true;
    }
#endif
}