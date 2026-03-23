// ──────────────────────────────────────────────
// GameLogger.cs
// 릴리스 빌드에서 자동 제거되는 디버그 로그 유틸리티
//
// [구현 원리]
// System.Diagnostics.Conditional 어트리뷰트를 사용하면
// 해당 심볼이 정의되지 않은 빌드에서 **호출 자체가 컴파일러에 의해 제거**됩니다.
// 즉 문자열 보간($"...")이나 string.Format도 생성되지 않아 GC 부하가 0입니다.
//
// [사용법]
// 기존:  Debug.Log($"[무기] 데미지={damage}");
// 변경:  GameLogger.Log($"[무기] 데미지={damage}");
//
// 릴리스 빌드에서는 GameLogger.Log() 호출 줄 자체가 사라집니다.
// LogWarning, LogError도 동일하게 동작합니다.
//
// [주의]
// - LogError는 릴리스에서도 남기고 싶다면 Debug.LogError를 직접 사용하세요.
// - 이 스크립트는 Editor 폴더가 아닌 일반 폴더에 넣어야 합니다.
//
// [Hierarchy / Inspector]
// 컴포넌트가 아닙니다. static 클래스이므로 어디에도 붙일 필요 없습니다.
// ──────────────────────────────────────────────

using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// 릴리스 빌드에서 호출이 완전히 제거되는 디버그 로그 래퍼입니다.
/// Debug.Log 대신 GameLogger.Log를 사용하면 빌드 시 GC 부하가 0이 됩니다.
/// </summary>
public static class GameLogger
{
    // ═══════════════════════════════════════════════════════
    //  일반 로그 (에디터 전용)
    // ═══════════════════════════════════════════════════════

    /// <summary>에디터에서만 출력되는 일반 로그입니다.</summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        Debug.Log(message);
    }

    /// <summary>에디터에서만 출력되는 일반 로그입니다. (context 포함)</summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, Object context)
    {
        Debug.Log(message, context);
    }

    // ═══════════════════════════════════════════════════════
    //  경고 로그 (에디터 전용)
    // ═══════════════════════════════════════════════════════

    /// <summary>에디터에서만 출력되는 경고 로그입니다.</summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }

    /// <summary>에디터에서만 출력되는 경고 로그입니다. (context 포함)</summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message, Object context)
    {
        Debug.LogWarning(message, context);
    }

    // ═══════════════════════════════════════════════════════
    //  에러 로그 (항상 출력 — 릴리스에서도 필요)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 에러 로그는 릴리스에서도 출력됩니다.
    /// 치명적 문제를 추적해야 하므로 Conditional을 걸지 않습니다.
    /// </summary>
    public static void LogError(string message)
    {
        Debug.LogError(message);
    }

    /// <summary>에러 로그입니다. (context 포함)</summary>
    public static void LogError(string message, Object context)
    {
        Debug.LogError(message, context);
    }
}