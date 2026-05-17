using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunConfigHolder : MonoBehaviour
{
    [Header("런 설정 데이터(SO)")]
    [SerializeField] private RunConfigSO run_config;

    public RunConfigSO Config => run_config;

    private void Awake()
    {
        ApplyToCurrentRunSetup();
        RunSignals.RaiseRunConfigChanged();
    }

    // 나중에 로비에서 모드 선택 붙일 때 사용
    public void SetConfig(RunConfigSO config)
    {
        run_config = config;
        ApplyToCurrentRunSetup();
        RunSignals.RaiseRunConfigChanged();
    }

    public static RunConfigSO FindSceneConfig()
    {
        RunConfigHolder holder = FindFirstObjectByType<RunConfigHolder>(FindObjectsInactive.Include);
        return holder != null ? holder.Config : null;
    }

    private void ApplyToCurrentRunSetup()
    {
        if (!RunSetupHolder.HasCurrent || RunSetupHolder.Current == null)
            return;

        RunSetupHolder.Current.runConfig = run_config;
    }
}
