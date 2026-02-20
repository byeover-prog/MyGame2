using System;

/// <summary>
/// 런(한 판)에서 쓰는 이벤트 모음(델리게이트)
/// - 서로 직접 참조하지 않고 신호로만 연결하기 위해 사용
/// - "한번 쓰고 잊어버리기" 목적이면 여기 이벤트만 최소로 유지하면 됨
/// </summary>
public static class RunSignals
{
    /// <summary>
    /// 스테이지 시작(시간 기준점 확정)
    /// elapsed_seconds 기준이 되는 "스테이지 시작 시각"을 확정할 때 사용
    /// </summary>
    public static event Action StageStarted;

    /// <summary>
    /// 런 설정이 바뀜(예: 모드 선택 후)
    /// </summary>
    public static event Action RunConfigChanged;

    /// <summary>
    /// 플레이어 사망
    /// </summary>
    public static event Action PlayerDead;

    // ====== 발행(Invoke) 함수: 이벤트 발행은 이 함수로만 ======
    public static void RaiseStageStarted() => StageStarted?.Invoke();
    public static void RaiseRunConfigChanged() => RunConfigChanged?.Invoke();
    public static void RaisePlayerDead() => PlayerDead?.Invoke();

    /// <summary>
    /// (디버그용) 모든 이벤트 구독 제거
    /// - 에디터 플레이 중 반복 재생성으로 꼬일 때 강제 초기화 용도
    /// </summary>
    public static void ClearAllSubscribers()
    {
        StageStarted = null;
        RunConfigChanged = null;
        PlayerDead = null;
    }
}
