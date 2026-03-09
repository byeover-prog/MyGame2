using System;
using UnityEngine;

public static class SquadLoadoutRuntime
{
    public struct Loadout
    {
        public string support1Id;
        public string mainId;
        public string support2Id;

        public bool HasMain => !string.IsNullOrWhiteSpace(mainId);
    }

    private static Loadout _current;

    /// <summary>
    /// 편성이 바뀌면 호출됩니다.
    /// </summary>
    public static event Action<Loadout> OnChanged;

    public static Loadout Current => _current;

    public static void SetSupport1(string characterId)
    {
        _current.support1Id = characterId;
        OnChanged?.Invoke(_current);
    }

    public static void SetMain(string characterId)
    {
        _current.mainId = characterId;
        OnChanged?.Invoke(_current);
    }

    public static void SetSupport2(string characterId)
    {
        _current.support2Id = characterId;
        OnChanged?.Invoke(_current);
    }

    public static void ClearSupport1()
    {
        _current.support1Id = null;
        OnChanged?.Invoke(_current);
    }

    public static void ClearMain()
    {
        _current.mainId = null;
        OnChanged?.Invoke(_current);
    }

    public static void ClearSupport2()
    {
        _current.support2Id = null;
        OnChanged?.Invoke(_current);
    }

    public static bool Contains(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return false;
        return _current.support1Id == characterId || _current.mainId == characterId || _current.support2Id == characterId;
    }

    public static void ClearAll()
    {
        _current = default;
        OnChanged?.Invoke(_current);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnDomainReload()
    {
        // 플레이 모드 재시작 시 이전 값이 남는 걸 방지
        _current = default;
        OnChanged = null;
    }
}
