using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// - JsonIO2D(기술) 위에 "정책/참조/우선순위"만 올린 싱글톤.
/// - 스킬 밸런스는 (1) persistent override(핫튜닝) 우선, 없으면 (2) TextAsset 기본값.
/// - 세이브 저장/로드는 SaveManager가 책임지고, JsonManager는 밸런스 로드 정책만 책임.
/// </summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class JsonManager2D : MonoBehaviour
{
    public enum BalanceSource
    {
        PersistentOverride,
        DefaultTextAsset
    }

    public static JsonManager2D Instance { get; private set; }

    [Header("밸런스 JSON(TextAsset)")]
    [Tooltip("프로젝트 기본 스킬 밸런스 JSON(TextAsset)을 넣으세요.\n예: Assets/_Game/Balance/skill_balance.json")]
    [SerializeField] private TextAsset defaultSkillBalanceJson;

    [Header("persistent override(핫튜닝)")]
    [Tooltip("체크 시 persistentDataPath에 같은 파일명이 있으면 그걸 우선 로드합니다.\n개발 중 수치 핫튜닝용(에디터/빌드 공통).")]
    [SerializeField] private bool usePersistentOverride = false;

    [Tooltip("override 파일명(기본값: skill_balance.json)")]
    [SerializeField] private string skillBalanceOverrideFileName = "skill_balance.json";

    [Header("로그")]
    [Tooltip("로드 경로/우선순위를 로그로 출력합니다.")]
    [SerializeField] private bool log = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        if (log)
        {
            Debug.Log($"[JsonManager2D] Awake => name={gameObject.name}, scene={gameObject.scene.name}, " +
                      $"defaultNull={(defaultSkillBalanceJson == null)}, len={(defaultSkillBalanceJson ? defaultSkillBalanceJson.text.Length : -1)}", this);
        }
    }

    public string GetSkillBalanceOverridePath()
        => JsonIO2D.GetPersistentPath(skillBalanceOverrideFileName);

    // ----------------------------
    // 진단 유틸
    // ----------------------------
    private static string NormalizeJsonText(string raw)
    {
        if (raw == null) return null;

        // BOM 제거(가끔 persistent에 BOM 붙으면 JsonUtility가 터짐)
        raw = raw.Trim();
        if (raw.Length > 0 && raw[0] == '\ufeff')
            raw = raw.Substring(1);

        return raw;
    }

    private static string Head(string s, int len = 160)
    {
        if (string.IsNullOrEmpty(s)) return "(empty)";
        s = s.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        return s.Length <= len ? s : s.Substring(0, len) + "...";
    }

    /// <summary>
    /// 스킬 밸런스를 정책대로 로드한다.
    /// </summary>
    public bool TryLoadSkillBalance<T>(out T data, out string error, out BalanceSource source)
    {
        data = default;
        error = null;
        source = BalanceSource.DefaultTextAsset;

        // 1) persistent override 우선
        if (usePersistentOverride)
        {
            string path = GetSkillBalanceOverridePath();

            if (File.Exists(path))
            {
                string raw = File.ReadAllText(path, Encoding.UTF8);
                raw = NormalizeJsonText(raw);

                if (log) Debug.Log($"[JsonManager2D] persistent head: {Head(raw)}", this);

                try
                {
                    data = JsonUtility.FromJson<T>(raw);
                    if (data != null)
                    {
                        source = BalanceSource.PersistentOverride;
                        if (log) Debug.Log($"[JsonManager2D] SkillBalance 로드: persistent override => {path}", this);
                        return true;
                    }

                    error = "persistent JSON 파싱 결과가 null입니다.";
                    if (log) Debug.LogWarning($"[JsonManager2D] persistent 파싱 null(폴백): {error}", this);
                }
                catch (Exception ex)
                {
                    error = $"JSON parse error(persistent): {ex.Message} | head={Head(raw)}";
                    if (log) Debug.LogWarning($"[JsonManager2D] persistent 파싱 실패(폴백): {error}", this);
                }
            }
            else
            {
                if (log) Debug.LogWarning($"[JsonManager2D] persistent override 파일이 없습니다(폴백): {path}", this);
            }
        }

        // 2) TextAsset 폴백
        if (defaultSkillBalanceJson == null)
        {
            error = "Default Skill Balance Json(TextAsset)이 비어있습니다.";
            return false;
        }

        string textRaw = NormalizeJsonText(defaultSkillBalanceJson.text);
        if (log) Debug.Log($"[JsonManager2D] textasset head: {Head(textRaw)}", this);

        try
        {
            data = JsonUtility.FromJson<T>(textRaw);
            if (data == null)
            {
                error = "TextAsset JSON 파싱 결과가 null입니다.";
                return false;
            }

            source = BalanceSource.DefaultTextAsset;
            if (log) Debug.Log("[JsonManager2D] SkillBalance 로드: TextAsset", this);
            return true;
        }
        catch (Exception ex)
        {
            error = $"JSON parse error(textasset): {ex.Message} | head={Head(textRaw)}";
            return false;
        }
    }

    /// <summary>
    /// 개발 편의: 기본 TextAsset을 persistent override로 내보내기(파일이 없으면 생성).
    /// </summary>
    [ContextMenu("개발/SkillBalance 기본값을 persistent override로 내보내기(없으면 생성)")]
    private void ExportDefaultToOverride_IfMissing()
    {
        if (defaultSkillBalanceJson == null)
        {
            Debug.LogWarning("[JsonManager2D] Default Skill Balance Json이 비어있습니다.", this);
            return;
        }

        string path = GetSkillBalanceOverridePath();

        // 이미 있으면 건드리지 않음(실수 방지)
        if (File.Exists(path))
        {
            Debug.Log($"[JsonManager2D] 이미 override 파일이 존재합니다: {path}", this);
            return;
        }

        if (!JsonIO2D.TrySaveTextToPersistent(skillBalanceOverrideFileName, defaultSkillBalanceJson.text, out string err))
        {
            Debug.LogWarning($"[JsonManager2D] override 내보내기 실패: {err}", this);
            return;
        }

        Debug.Log($"[JsonManager2D] override 파일 생성 완료: {path}", this);
    }

    // ----------------------------
    // 하위 호환(과거 코드가 JsonManager2D.TryLoadFromPersistent 같은 호출을 하고 있을 수 있어서 제공)
    // ----------------------------
    public static bool TryLoadFromPersistent<T>(string fileName, out T data, out string error)
        => JsonIO2D.TryLoadFromPersistent(fileName, out data, out error);

    public static bool TrySaveToPersistent<T>(string fileName, T data, bool prettyPrint, out string error)
        => JsonIO2D.TrySaveToPersistent(fileName, data, prettyPrint, out error);

    public static bool TryDeletePersistent(string fileName, out string error)
        => JsonIO2D.TryDeletePersistent(fileName, out error);
}