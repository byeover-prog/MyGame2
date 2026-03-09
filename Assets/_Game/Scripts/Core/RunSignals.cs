// UTF-8
using System;

/// <summary>
/// 런(한 판)에서 쓰는 이벤트 모음
/// - 서로 직접 참조하지 않고 신호로만 연결
/// - 디버그 추적 가능하도록 발행 로그 포함
/// </summary>
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

    // ====== 발행 함수 ======

    public static void RaiseStageStarted()
    {
        StageStarted?.Invoke();
#if UNITY_EDITOR
        if (StageStarted == null)
            UnityEngine.Debug.LogWarning("[RunSignals] StageStarted 발행됨(구독자 없음)");
        else
            UnityEngine.Debug.Log("[RunSignals] StageStarted 발행");
#endif
    }

    public static void RaiseRunConfigChanged()
    {
        RunConfigChanged?.Invoke();
#if UNITY_EDITOR
        if (RunConfigChanged == null)
            UnityEngine.Debug.LogWarning("[RunSignals] RunConfigChanged 발행됨(구독자 없음)");
        else
            UnityEngine.Debug.Log("[RunSignals] RunConfigChanged 발행");
#endif
    }

    public static void RaisePlayerDead()
    {
#if UNITY_EDITOR
        UnityEngine.Debug.Log("[RunSignals] PlayerDead 발행");
#endif

        PlayerDead?.Invoke();

#if UNITY_EDITOR
        if (PlayerDead == null)
            UnityEngine.Debug.LogWarning("[RunSignals] PlayerDead 구독자가 없습니다.");
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

#if UNITY_EDITOR
        UnityEngine.Debug.Log("[RunSignals] 모든 구독자 제거됨");
#endif
    }
}