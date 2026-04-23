using System;

public static class RunSignals
{
    /// <summary>
    /// 스테이지 시작
    /// </summary>
    public static event Action StageStarted;

    /// <summary>
    /// 런 설정 변경
    /// </summary>
    public static event Action RunConfigChanged;

    /// <summary>
    /// 플레이어 사망
    /// </summary>
    public static event Action PlayerDead;

    /// <summary>
    /// 궁극기 사용 (R키 또는 T키 궁극기 발동 시)
    /// </summary>
    public static event Action UltimateUsed;

    // ====== 발행 함수 ======

    public static void RaiseStageStarted()
    {
        StageStarted?.Invoke();
#if UNITY_EDITOR
        if (StageStarted == null)
            GameLogger.LogWarning("[RunSignals] StageStarted 발행됨(구독자 없음)");
        else
            GameLogger.Log("[RunSignals] StageStarted 발행");
#endif
    }

    public static void RaiseRunConfigChanged()
    {
        RunConfigChanged?.Invoke();
#if UNITY_EDITOR
        if (RunConfigChanged == null)
            GameLogger.LogWarning("[RunSignals] RunConfigChanged 발행됨(구독자 없음)");
        else
            GameLogger.Log("[RunSignals] RunConfigChanged 발행");
#endif
    }

    public static void RaisePlayerDead()
    {
#if UNITY_EDITOR
        GameLogger.Log("[RunSignals] PlayerDead 발행");
#endif

        PlayerDead?.Invoke();

#if UNITY_EDITOR
        if (PlayerDead == null)
            GameLogger.LogWarning("[RunSignals] PlayerDead 구독자가 없습니다.");
#endif
    }

    public static void RaiseUltimateUsed()
    {
        UltimateUsed?.Invoke();
#if UNITY_EDITOR
        GameLogger.Log("[RunSignals] UltimateUsed 발행");
#endif
    }

    /// <summary>
    /// 모든 이벤트 구독 제거 (플레이 리셋 시 안전장치)
    /// </summary>
    public static void ClearAllSubscribers()
    {
        StageStarted = null;
        RunConfigChanged = null;
        PlayerDead = null;
        UltimateUsed = null;

#if UNITY_EDITOR
        GameLogger.Log("[RunSignals] 모든 구독자 제거됨");
#endif
    }
}