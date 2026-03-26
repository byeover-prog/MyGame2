#if UNITY_EDITOR
// ──────────────────────────────────────────────
// DebugLogReplacer.cs  (v3 — UnityEngine.Debug.Log 패턴 수정)
// Debug.Log / Debug.LogWarning → GameLogger.Log / GameLogger.LogWarning 일괄 교체 도구
//
// [사용법]
// Unity 에디터 상단 메뉴 → Tools → 그날이후 → 디버그 로그 일괄 교체 (미리보기)
//   → Console에 교체 대상 목록이 출력됩니다. 실제 파일은 변경되지 않습니다.
//
// Unity 에디터 상단 메뉴 → Tools → 그날이후 → 디버그 로그 일괄 교체 (실행)
//   → 실제 파일이 변경됩니다. 변경 전 반드시 Git 커밋하세요!
//
// [교체 규칙]
// - Debug.Log(              → GameLogger.Log(
// - Debug.LogWarning(       → GameLogger.LogWarning(
// - UnityEngine.Debug.Log(  → GameLogger.Log(          ← v3 추가
// - UnityEngine.Debug.LogWarning( → GameLogger.LogWarning( ← v3 추가
// - Debug.LogError는 교체하지 않음 (릴리스에서도 필요)
// - Editor/ 폴더 안의 스크립트는 건드리지 않음
// - Debug/ 폴더 안의 스크립트는 건드리지 않음
// - GameLogger.cs, DebugLogReplacer.cs 자체는 건드리지 않음
//
// [변경 이력]
// v2: GameLogger.cs 제외 (무한재귀 방지)
// v3: UnityEngine.Debug.Log( 패턴 처리 (UnityEngine.GameLogger 컴파일 에러 방지)
//     이미 잘못 교체된 UnityEngine.GameLogger. 자동 복구 메뉴 추가
// ──────────────────────────────────────────────

using System.IO;
using UnityEditor;
using UnityEngine;

public static class DebugLogReplacer
{
    private const string GAME_SCRIPTS_ROOT = "Assets/_Game/Scripts";

    // ★ v3: 교체 순서가 중요!
    // UnityEngine.Debug.Log( 를 먼저 교체해야 Debug.Log( 교체 시 이중 치환이 안 됨
    private static readonly (string from, string to)[] Replacements =
    {
        // 1단계: 정규화된 네임스페이스 호출 (먼저 처리)
        ("UnityEngine.Debug.Log(",        "GameLogger.Log("),
        ("UnityEngine.Debug.LogWarning(", "GameLogger.LogWarning("),

        // 2단계: 일반 호출
        ("Debug.Log(",        "GameLogger.Log("),
        ("Debug.LogWarning(", "GameLogger.LogWarning("),
    };

    // ★ v3 추가: 이전 버전에서 잘못 교체된 패턴 자동 복구
    private static readonly (string from, string to)[] AutoFix =
    {
        ("UnityEngine.GameLogger.Log(",        "GameLogger.Log("),
        ("UnityEngine.GameLogger.LogWarning(", "GameLogger.LogWarning("),
    };

    // 제외 폴더 (경로에 포함되면 스킵)
    private static readonly string[] ExcludeFolders =
    {
        "/Editor/",
        "/Debug/",
    };

    // 제외 파일명 (파일명이 일치하면 스킵)
    private static readonly string[] ExcludeFileNames =
    {
        "Gamelogger.cs",
        "GameLogger.cs",
        "Debuglogreplacer.cs",
        "DebugLogReplacer.cs",
    };

    // ═══════════════════════════════════════════════════════
    //  미리보기 (파일 변경 없음)
    // ═══════════════════════════════════════════════════════

    [MenuItem("Tools/그날이후/디버그 로그 일괄 교체 (미리보기)")]
    public static void PreviewReplace()
    {
        ProcessAll(dryRun: true);
    }

    // ═══════════════════════════════════════════════════════
    //  실행 (파일 변경됨)
    // ═══════════════════════════════════════════════════════

    [MenuItem("Tools/그날이후/디버그 로그 일괄 교체 (실행)")]
    public static void ExecuteReplace()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "디버그 로그 일괄 교체 (v3)",
            "Debug.Log / Debug.LogWarning을 GameLogger로 교체합니다.\n" +
            "UnityEngine.Debug.Log 패턴도 올바르게 처리됩니다.\n\n" +
            "⚠ 변경 전 반드시 Git 커밋하세요!\n" +
            "⚠ Editor/, Debug/ 폴더는 제외됩니다.\n" +
            "⚠ GameLogger.cs 자체는 제외됩니다.\n" +
            "⚠ Debug.LogError는 교체하지 않습니다.\n\n" +
            "계속하시겠습니까?",
            "실행", "취소");

        if (!confirm) return;

        int totalReplacements = ProcessAll(dryRun: false);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "완료",
            $"총 {totalReplacements}건 교체 완료.\n\nConsole에서 상세 내역을 확인하세요.",
            "확인");
    }

    // ═══════════════════════════════════════════════════════
    //  자동 복구 (이전 버전 오류 수정)
    // ═══════════════════════════════════════════════════════

    [MenuItem("Tools/그날이후/UnityEngine.GameLogger 자동 복구")]
    public static void FixBrokenReplacements()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "자동 복구",
            "이전 버전의 교체 도구로 인해 발생한\n" +
            "UnityEngine.GameLogger 컴파일 에러를 수정합니다.\n\n" +
            "UnityEngine.GameLogger.Log → GameLogger.Log\n" +
            "UnityEngine.GameLogger.LogWarning → GameLogger.LogWarning\n\n" +
            "계속하시겠습니까?",
            "실행", "취소");

        if (!confirm) return;

        string fullRoot = Path.GetFullPath(GAME_SCRIPTS_ROOT).Replace("\\", "/");
        string[] files = Directory.GetFiles(fullRoot, "*.cs", SearchOption.AllDirectories);

        int fixedCount = 0;
        int fixedOccurrences = 0;

        foreach (string filePath in files)
        {
            string content = File.ReadAllText(filePath);
            string original = content;

            foreach (var (from, to) in AutoFix)
            {
                int count = CountOccurrences(content, from);
                if (count > 0)
                {
                    content = content.Replace(from, to);
                    fixedOccurrences += count;
                }
            }

            if (content != original)
            {
                File.WriteAllText(filePath, content);
                fixedCount++;

                string rel = filePath.Replace("\\", "/");
                int idx = rel.IndexOf("Assets/");
                if (idx >= 0) rel = rel.Substring(idx);
                Debug.Log($"  [복구] {rel}");
            }
        }

        AssetDatabase.Refresh();

        Debug.Log($"[DebugLogReplacer] 자동 복구 완료: {fixedCount}개 파일 / {fixedOccurrences}건 수정");
        EditorUtility.DisplayDialog("완료", $"{fixedCount}개 파일, {fixedOccurrences}건 복구 완료.", "확인");
    }

    // ═══════════════════════════════════════════════════════
    //  내부 처리
    // ═══════════════════════════════════════════════════════

    private static int ProcessAll(bool dryRun)
    {
        string fullRoot = Path.GetFullPath(GAME_SCRIPTS_ROOT).Replace("\\", "/");

        if (!Directory.Exists(fullRoot))
        {
            Debug.LogError($"[DebugLogReplacer] 폴더를 찾을 수 없습니다: {fullRoot}");
            return 0;
        }

        string[] files = Directory.GetFiles(fullRoot, "*.cs", SearchOption.AllDirectories);

        int totalFiles = 0;
        int totalReplacements = 0;
        int skippedFiles = 0;
        int autoFixed = 0;

        string mode = dryRun ? "미리보기" : "실행";
        Debug.Log($"[DebugLogReplacer v3] === {mode} 시작 ===");

        foreach (string filePath in files)
        {
            string normalizedPath = filePath.Replace("\\", "/");

            // 제외 폴더 체크
            bool excluded = false;
            foreach (string ex in ExcludeFolders)
            {
                if (normalizedPath.Contains(ex))
                {
                    excluded = true;
                    break;
                }
            }
            if (excluded) continue;

            // 제외 파일명 체크
            string fileName = Path.GetFileName(filePath);
            foreach (string exFile in ExcludeFileNames)
            {
                if (string.Equals(fileName, exFile, System.StringComparison.OrdinalIgnoreCase))
                {
                    excluded = true;
                    break;
                }
            }
            if (excluded)
            {
                skippedFiles++;
                continue;
            }

            string content = File.ReadAllText(filePath);
            string original = content;
            int fileReplacements = 0;

            // ★ v3: 먼저 이전 버전 오류 자동 복구
            foreach (var (from, to) in AutoFix)
            {
                int fixCount = CountOccurrences(content, from);
                if (fixCount > 0)
                {
                    content = content.Replace(from, to);
                    autoFixed += fixCount;
                }
            }

            // 본 교체 (순서대로: UnityEngine.Debug.Log 먼저, 그 다음 Debug.Log)
            foreach (var (from, to) in Replacements)
            {
                int count = CountOccurrences(content, from);
                if (count > 0)
                {
                    content = content.Replace(from, to);
                    fileReplacements += count;
                }
            }

            if (content != original)
            {
                totalFiles++;
                totalReplacements += fileReplacements;

                string relativePath = normalizedPath;
                int assetsIdx = relativePath.IndexOf("Assets/");
                if (assetsIdx >= 0) relativePath = relativePath.Substring(assetsIdx);

                Debug.Log($"  [{fileReplacements}건] {relativePath}");

                if (!dryRun)
                {
                    File.WriteAllText(filePath, content);
                }
            }
        }

        string summary = $"파일 {totalFiles}개 / 교체 {totalReplacements}건 / 제외 {skippedFiles}개";
        if (autoFixed > 0) summary += $" / 자동복구 {autoFixed}건";

        Debug.Log($"[DebugLogReplacer v3] === {mode} 완료 === {summary}");

        return totalReplacements;
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int idx = 0;
        while ((idx = text.IndexOf(pattern, idx, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }
}
#endif