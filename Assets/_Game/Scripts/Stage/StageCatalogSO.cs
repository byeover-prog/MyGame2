using System.Collections.Generic;
using UnityEngine;

// 스토리 모드의 전체 스테이지 목록을 관리하는 카탈로그입니다.
// 프로젝트에 하나만 만들어 두면 됩니다.

[CreateAssetMenu(menuName = "혼령검/스테이지/스테이지 카탈로그", fileName = "StageCatalog")]
public sealed class StageCatalogSO : ScriptableObject
{
    [Header("스테이지 목록")]
    [Tooltip("인덱스 순서대로 스테이지 정의를 넣으세요. (0~12)")]
    [SerializeField] private List<StageDefinitionSO> stages = new List<StageDefinitionSO>(13);

    /// <summary>등록된 스테이지 목록입니다.</summary>
    public IReadOnlyList<StageDefinitionSO> Stages => stages;

    /// <summary>총 스테이지 수입니다.</summary>
    public int Count => stages != null ? stages.Count : 0;

    /// <summary>인덱스로 스테이지 정의를 가져옵니다.</summary>
    public StageDefinitionSO GetByIndex(int stageIndex)
    {
        if (stages == null) return null;

        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i] != null && stages[i].StageIndex == stageIndex)
                return stages[i];
        }
        return null;
    }

    /// <summary>다음 스테이지 정의를 가져옵니다.</summary>
    public StageDefinitionSO GetNext(int currentStageIndex)
    {
        return GetByIndex(currentStageIndex + 1);
    }

    /// <summary>마지막 스테이지인지 확인합니다.</summary>
    public bool IsLastStage(int stageIndex)
    {
        return GetByIndex(stageIndex + 1) == null;
    }
}