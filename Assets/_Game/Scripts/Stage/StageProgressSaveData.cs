using System;
using System.Collections.Generic;

// 스토리 모드 진행 상태 저장 데이터입니다.
// MetaProfileSaveData2D.stageProgress에 포함됩니다.

[Serializable]
public sealed class StageProgressSaveData
{
    /// <summary>클리어한 스테이지 인덱스 목록입니다.</summary>
    public List<int> clearedStages = new List<int>(13);

    /// <summary>현재 도달한 최대 스테이지 인덱스입니다.</summary>
    public int maxReachedStage;

    /// <summary>강화 시스템 해금 여부입니다. (St.2 클리어 시 해금)</summary>
    public bool upgradeSystemUnlocked;

    /// <summary>해금된 캐릭터 ID 목록입니다.</summary>
    public List<string> unlockedCharacterIds = new List<string>(8);

    /// <summary>해당 스테이지가 클리어되었는지 확인합니다.</summary>
    public bool IsCleared(int stageIndex)
    {
        return clearedStages.Contains(stageIndex);
    }

    /// <summary>스테이지 클리어를 기록합니다.</summary>
    public void MarkCleared(int stageIndex)
    {
        if (!clearedStages.Contains(stageIndex))
            clearedStages.Add(stageIndex);

        if (stageIndex + 1 > maxReachedStage)
            maxReachedStage = stageIndex + 1;
    }

    /// <summary>캐릭터 해금을 기록합니다.</summary>
    public void UnlockCharacter(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return;
        if (!unlockedCharacterIds.Contains(characterId))
            unlockedCharacterIds.Add(characterId);
    }

    /// <summary>해당 캐릭터가 해금되었는지 확인합니다.</summary>
    public bool IsCharacterUnlocked(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return false;
        return unlockedCharacterIds.Contains(characterId);
    }

    /// <summary>해당 스테이지에 진입 가능한지 확인합니다.</summary>
    public bool CanEnter(int stageIndex)
    {
        if (stageIndex <= 0) return true;
        return stageIndex <= maxReachedStage;
    }
}