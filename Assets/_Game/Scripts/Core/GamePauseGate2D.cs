// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 UI가 동시에 게임 일시정지를 요청해도 timeScale이 꼬이지 않도록 관리합니다.
/// 레벨업창, 옵션창처럼 서로 다른 패널이 겹쳐도 마지막 요청이 해제될 때만 원래 배속으로 복구합니다.
/// </summary>
public static class GamePauseGate2D
{
    private static readonly HashSet<int> Owners = new HashSet<int>();

    public static bool IsPaused => Owners.Count > 0;
    public static int OwnerCount => Owners.Count;

    public static void Acquire(Object owner)
    {
        if (owner == null)
            return;

        Owners.Add(owner.GetInstanceID());
        Time.timeScale = 0f;
    }

    public static void Release(Object owner)
    {
        if (owner == null)
            return;

        Owners.Remove(owner.GetInstanceID());

        if (Owners.Count <= 0)
        {
            Owners.Clear();
            Time.timeScale = ResolvePlayableTimeScale();
        }
    }

    public static void ClearAll()
    {
        Owners.Clear();
        Time.timeScale = ResolvePlayableTimeScale();
    }

    private static float ResolvePlayableTimeScale()
    {
        if (GameSettingsRuntime.HasInstance)
            return Mathf.Max(0.01f, GameSettingsRuntime.Instance.TimeScale);

        return 1f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnDomainReload()
    {
        Owners.Clear();
    }
}