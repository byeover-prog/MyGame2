using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - JSON(TextAsset)에서 SkillBalanceDB2D를 파싱한다.
// - id 기준으로 딕셔너리 인덱싱하여 빠르게 조회한다.
// - 무기/스킬은 "기본값"을 만든 뒤, Apply로 덮어쓰기만 받는다.
public static class SkillBalanceService2D
{
    private static readonly Dictionary<string, SkillBalanceDB2D.SkillRow2D> _map = new(64);
    private static bool _loaded;

    public static bool IsLoaded => _loaded;

    public static void LoadFromText(string json)
    {
        _map.Clear();
        _loaded = false;

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[Balance] JSON이 비어있습니다.");
            return;
        }

        SkillBalanceDB2D db = null;
        try
        {
            db = JsonUtility.FromJson<SkillBalanceDB2D>(json);
        }
        catch
        {
            Debug.LogWarning("[Balance] JSON 파싱 실패(형식 확인 필요).");
            return;
        }

        if (db == null || db.skills == null)
        {
            Debug.LogWarning("[Balance] DB 구조가 비정상입니다.");
            return;
        }

        for (int i = 0; i < db.skills.Length; i++)
        {
            var row = db.skills[i];
            if (row == null || !row.HasId()) continue;

            // 중복 id는 마지막 값이 우선(덮어쓰기)
            _map[row.id] = row;
        }

        _loaded = true;
        Debug.Log($"[Balance] 로드 완료: {_map.Count}개 스킬 오버라이드");
    }

    public static bool TryGet(string id, out SkillBalanceDB2D.SkillRow2D row)
    {
        row = null;
        if (!_loaded) return false;
        if (string.IsNullOrEmpty(id)) return false;
        return _map.TryGetValue(id, out row);
    }

    // 공통 수치 적용(너 프로젝트의 "P" 구조체/클래스에 맞춰서 연결하면 됨)
    // 여기서는 '대표 필드 이름'으로만 작성해두고,
    // 실제 적용 지점(공통 Weapon 베이스)에서 row 값을 P에 덮어쓰면 된다.

    /// <summary>
    /// SkillBalanceBootstrap2D에서 이미 파싱된 DB를 직접 넘겨받는다.
    /// LoadFromText()와 동일하게 _map을 채우고 _loaded를 true로 전환.
    /// </summary>
    public static void LoadFromDB(SkillBalanceDB2D db)
    {
        _map.Clear();
        _loaded = false;

        if (db == null || db.skills == null || db.skills.Length == 0)
        {
            Debug.LogWarning("[Balance] LoadFromDB: DB가 비어있습니다.");
            return;
        }

        for (int i = 0; i < db.skills.Length; i++)
        {
            var row = db.skills[i];
            if (row == null || !row.HasId()) continue;
            _map[row.id] = row;
        }

        _loaded = true;
        Debug.Log($"[Balance] LoadFromDB 완료: {_map.Count}개 스킬 오버라이드");
    }
}