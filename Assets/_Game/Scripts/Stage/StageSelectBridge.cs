using UnityEngine;

public static class StageSelectBridge
{
    public static int SelectedStageIndex { get; private set; }
    public static bool HasSelection { get; private set; }

    public static void Select(int stageIndex)
    {
        SelectedStageIndex = stageIndex;
        HasSelection = true;
        RunSetupHolder.Set(RunSetupFactory.CreateFromCurrentState(stageIndexOverride: stageIndex));
    }

    public static void Clear()
    {
        HasSelection = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        SelectedStageIndex = 0;
        HasSelection = false;
    }
}
