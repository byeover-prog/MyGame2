using UnityEngine;
using System.Diagnostics;

/// <summary>
/// 전투 중 반복되는 Debug.Log의 성능 문제를 해결하는 유틸리티.
/// 
/// ★ 문제:
/// Debug.Log()는 매 호출마다 string 할당 + 스택 트레이스 + 콘솔 UI 갱신이 발생.
/// 초당 수십 회 호출되면 그것만으로 FPS가 10~30 이상 떨어질 수 있음.
/// 
/// ★ 해결:
/// 1) 에디터에서도 전투 로그를 끌 수 있는 전역 토글
/// 2) [Conditional] 어트리뷰트로 빌드 시 자동 제거
/// 3) 기존 GameLogger.Log 호출을 CombatLog.Log로 교체
/// 
/// ★ 사용법 (세팅):
/// 1. 이 파일을 Assets/_Game/Scripts/Core/ 폴더에 넣기
/// 2. Hierarchy → 아무 오브젝트에도 붙일 필요 없음 (static 클래스)
/// 3. 에디터 상단 메뉴 → 혼령검 → 전투 로그 ON/OFF 로 토글
/// 
/// ★ 사용법 (코드 교체):
/// 기존: Debug.Log("[ThunderWeapon] 부적 발사 ...");
/// 변경: CombatLog.Log("[ThunderWeapon] 부적 발사 ...");
/// 
/// 또는 기존: GameLogger.Log("[좌격요세] 횡베기! ...");
/// 변경: CombatLog.Log("[좌격요세] 횡베기! ...");
/// </summary>
public static class CombatLog
{
    /// <summary>
    /// 전투 로그 출력 여부. false면 모든 전투 로그가 무시됨.
    /// 에디터 메뉴 또는 Inspector에서 토글 가능.
    /// </summary>
    public static bool Enabled { get; set; } = false; // 기본: 꺼짐

    /// <summary>
    /// 전투 중 반복되는 로그 출력. Enabled가 false면 아무것도 안 함.
    /// 릴리스 빌드에서는 [Conditional]로 호출 자체가 제거됨.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        if (!Enabled) return;
        UnityEngine.Debug.Log(message);
    }

    /// <summary>
    /// Object 컨텍스트 포함 버전
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, Object context)
    {
        if (!Enabled) return;
        UnityEngine.Debug.Log(message, context);
    }

    /// <summary>
    /// 경고 로그 (이건 항상 출력 — 경고는 숨기면 안 됨)
    /// </summary>
    public static void Warn(string message)
    {
        UnityEngine.Debug.LogWarning(message);
    }
}

#if UNITY_EDITOR
/// <summary>
/// 에디터 메뉴에서 전투 로그를 켜고 끄는 토글.
/// 메뉴: 혼령검 → 전투 로그 ON/OFF
/// </summary>
public static class CombatLogMenuToggle
{
    private const string MENU_PATH = "혼령검/전투 로그 ON-OFF";
    private const string PREF_KEY = "CombatLog_Enabled";

    [UnityEditor.MenuItem(MENU_PATH, false, 100)]
    private static void ToggleCombatLog()
    {
        bool current = UnityEditor.EditorPrefs.GetBool(PREF_KEY, false);
        bool next = !current;
        UnityEditor.EditorPrefs.SetBool(PREF_KEY, next);
        CombatLog.Enabled = next;
        UnityEngine.Debug.Log($"[CombatLog] 전투 로그 {(next ? "ON ✅" : "OFF ❌")}");
    }

    [UnityEditor.MenuItem(MENU_PATH, true)]
    private static bool ToggleCombatLogValidate()
    {
        UnityEditor.Menu.SetChecked(MENU_PATH,
            UnityEditor.EditorPrefs.GetBool(PREF_KEY, false));
        return true;
    }

    /// <summary>
    /// 에디터 시작 시 저장된 설정을 복원
    /// </summary>
    [UnityEditor.InitializeOnLoadMethod]
    private static void RestoreState()
    {
        CombatLog.Enabled = UnityEditor.EditorPrefs.GetBool(PREF_KEY, false);
    }
}
#endif