// 스테이지 클리어 조건입니다.

public enum StageClearCondition
{
    /// <summary>제한 시간 동안 생존</summary>
    SurviveTime = 0,

    /// <summary>보스 처치</summary>
    BossKill = 1,

    /// <summary>모든 적 처치 (웨이브 완료)</summary>
    ClearAllWaves = 2,

    /// <summary>특정 HP 도달 시 연출 진입 (두억시니 등)</summary>
    BossHPThreshold = 3,
}