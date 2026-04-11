public enum QuestType
{
    /// <summary>적 처치 (targetCount마리)</summary>
    Kill = 0,

    /// <summary>생존 시간 (targetCount초)</summary>
    Survive = 1,

    /// <summary>아이템 수집 (targetCount개)</summary>
    Collect = 2,

    /// <summary>보스 처치 (targetCount마리)</summary>
    BossKill = 3,

    /// <summary>엘리트 처치 (targetCount마리)</summary>
    EliteKill = 4,

    /// <summary>특정 스킬로 처치 (targetCount마리)</summary>
    SkillKill = 5,
}