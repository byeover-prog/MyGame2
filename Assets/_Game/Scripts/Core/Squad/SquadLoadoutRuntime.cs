using System;
using UnityEngine;

/// <summary>
/// 스쿼드 편성 런타임 상태를 관리하는 정적 클래스입니다.
/// SquadFormationController(UGUI)와 FormationService2D(아웃게임) 양쪽에서 사용합니다.
/// </summary>
public static class SquadLoadoutRuntime
{
    // ─── 내부 구조체 ───────────────────────────────────

    /// <summary>
    /// 현재 편성 상태를 담는 구조체입니다.
    /// </summary>
    public struct Loadout
    {
        public string support1Id;
        public string mainId;
        public string support2Id;

        /// <summary>메인 슬롯에 캐릭터가 배치되어 있는지 여부입니다.</summary>
        public bool HasMain => !string.IsNullOrWhiteSpace(mainId);

        /// <summary>해당 캐릭터 ID가 어느 슬롯이든 배치되어 있는지 확인합니다.</summary>
        public bool Contains(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return false;
            return support1Id == characterId
                || mainId == characterId
                || support2Id == characterId;
        }

        /// <summary>
        /// FormationSaveData2D 형태로 변환합니다.
        /// CharacterMetaResolver2D 등에서 세이브 데이터 형태가 필요할 때 사용합니다.
        /// </summary>
        public FormationSaveData2D ToSaveData()
        {
            return new FormationSaveData2D
            {
                support1Id = this.support1Id,
                mainId = this.mainId,
                support2Id = this.support2Id
            };
        }
    }

    // ─── 상태 ──────────────────────────────────────────

    private static Loadout _current;

    /// <summary>현재 편성 상태입니다.</summary>
    public static Loadout Current => _current;

    /// <summary>편성이 변경될 때마다 호출되는 이벤트입니다.</summary>
    public static event Action<Loadout> OnChanged;

    // ─── 프로퍼티 (하위 호환) ──────────────────────────

    /// <summary>현재 메인 캐릭터 ID입니다.</summary>
    public static string MainId => _current.mainId;

    /// <summary>현재 지원1 캐릭터 ID입니다.</summary>
    public static string Support1Id => _current.support1Id;

    /// <summary>현재 지원2 캐릭터 ID입니다.</summary>
    public static string Support2Id => _current.support2Id;

    // ─── 개별 슬롯 설정 ───────────────────────────────

    /// <summary>메인 슬롯에 캐릭터를 배치합니다.</summary>
    public static void SetMain(string characterId)
    {
        _current.mainId = characterId;
        NotifyAndSave();
    }

    /// <summary>지원1 슬롯에 캐릭터를 배치합니다.</summary>
    public static void SetSupport1(string characterId)
    {
        _current.support1Id = characterId;
        NotifyAndSave();
    }

    /// <summary>지원2 슬롯에 캐릭터를 배치합니다.</summary>
    public static void SetSupport2(string characterId)
    {
        _current.support2Id = characterId;
        NotifyAndSave();
    }

    /// <summary>해당 캐릭터 ID가 어느 슬롯이든 배치되어 있는지 확인합니다.</summary>
    public static bool Contains(string characterId)
    {
        return _current.Contains(characterId);
    }

    /// <summary>모든 슬롯을 비웁니다.</summary>
    public static void ClearAll()
    {
        _current = default;
        NotifyAndSave();
    }

    // ─── 아웃게임 브릿지 ──────────────────────────────

    /// <summary>
    /// FormationSaveData2D에서 런타임 값을 복사합니다.
    /// FormationService2D가 편성 변경 시 자동으로 호출합니다.
    /// </summary>
    public static void CopyFromSave(FormationSaveData2D data)
    {
        if (data == null)
        {
            _current = default;
        }
        else
        {
            _current.mainId = data.mainId;
            _current.support1Id = data.support1Id;
            _current.support2Id = data.support2Id;
        }

        Debug.Log($"[SquadLoadoutRuntime] 편성 동기화 완료 — 메인:{_current.mainId ?? "없음"}, " +
                  $"지원1:{_current.support1Id ?? "없음"}, 지원2:{_current.support2Id ?? "없음"}");

        OnChanged?.Invoke(_current);
    }

    /// <summary>
    /// 게임 시작 시 세이브에서 한 번 로드합니다.
    /// </summary>
    public static void LoadFromSave()
    {
        if (SaveManager2D.Instance == null || SaveManager2D.Instance.Data == null) return;
        SaveManager2D.Instance.Data.EnsureDefaults();

        if (SaveManager2D.Instance.Data.metaProfile != null
            && SaveManager2D.Instance.Data.metaProfile.formation != null)
        {
            CopyFromSave(SaveManager2D.Instance.Data.metaProfile.formation);
        }
    }

    // ─── 내부 ─────────────────────────────────────────

    private static void NotifyAndSave()
    {
        if (SaveManager2D.Instance != null
            && SaveManager2D.Instance.Data != null)
        {
            SaveManager2D.Instance.Data.EnsureDefaults();
            FormationSaveData2D formation = SaveManager2D.Instance.Data.metaProfile.formation;
            if (formation != null)
            {
                formation.mainId = _current.mainId;
                formation.support1Id = _current.support1Id;
                formation.support2Id = _current.support2Id;
                SaveManager2D.Instance.Save();
            }
        }

        OnChanged?.Invoke(_current);
    }
}