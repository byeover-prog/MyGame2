using System;

public static class GameSignals
{
    // 레벨업 열기 요청
    public static event Action LevelUpOpenRequested;

    // 리롤 요청
    public static event Action RerollRequested;

    // 레벨업 종료
    public static event Action LevelUpClosed;

    // 스킬 레벨 변경
    public static event Action<string, int> SkillLevelChanged;

    public static void RaiseLevelUpOpenRequested() => LevelUpOpenRequested?.Invoke();
    public static void RaiseRerollRequested() => RerollRequested?.Invoke();
    public static void RaiseLevelUpClosed() => LevelUpClosed?.Invoke();
    public static void RaiseSkillLevelChanged(string id, int level) => SkillLevelChanged?.Invoke(id, level);
}