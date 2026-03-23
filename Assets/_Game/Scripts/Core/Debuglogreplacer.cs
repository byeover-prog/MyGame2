#if UNITY_EDITOR
// ──────────────────────────────────────────────
// DebugLogReplacer.cs
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
// - Debug.Log(     → GameLogger.Log(
// - Debug.LogWarning( → GameLogger.LogWarning(
// - Debug.LogError는 교체하지 않음 (릴리스에서도 필요)
// - Editor/ 폴더 안의 스크립트는 건드리지 않음
// - Debug/ 폴더 안의 스크립트는 건드리지 않음
// ──────────────────────────────────────────────

using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class DebugLogReplacer
{
    private const string GAME_SCRIPTS_ROOT = "Assets/_Game/Scripts";

    // 교체 대상 패턴 (Debug.Log와 Debug.LogWarning만)
    // Debug.LogError는 의도적으로 제외
    private static readonly (string from, string to)[] Replacements =
    {
        ("Debug.Log(", "GameLogger.Log("),
        ("Debug.LogWarning(", "GameLogger.LogWarning("),
    };

    // 제외 폴더 (경로에 포함되면 스킵)
    private static readonly string[] ExcludeFolders =
    {
        "/Editor/",
        "/Debug/",
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
            "디버그 로그 일괄 교체",
            "Debug.Log / Debug.LogWarning을 GameLogger로 교체합니다.\n\n" +
            "⚠ 변경 전 반드시 Git 커밋하세요!\n" +
            "⚠ Editor/ 폴더와 Debug/ 폴더는 제외됩니다.\n" +
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

        string mode = dryRun ? "미리보기" : "실행";
        Debug.Log($"[DebugLogReplacer] === {mode} 시작 === (대상 폴더: {GAME_SCRIPTS_ROOT})");

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

            string content = File.ReadAllText(filePath);
            string original = content;
            int fileReplacements = 0;

            foreach (var (from, to) in Replacements)
            {
                int count = CountOccurrences(content, from);
                if (count > 0)
                {
                    content = content.Replace(from, to);
                    fileReplacements += count;
                }
            }

            if (fileReplacements > 0)
            {
                totalFiles++;
                totalReplacements += fileReplacements;

                // 상대 경로로 표시
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

        Debug.Log($"[DebugLogReplacer] === {mode} 완료 === " +
                  $"파일 {totalFiles}개 / 교체 {totalReplacements}건");

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