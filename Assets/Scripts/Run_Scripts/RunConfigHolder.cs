using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunConfigHolder : MonoBehaviour
{
    [Header("런 설정 데이터(SO)")]
    [SerializeField] private RunConfigSO run_config;

    public static RunConfigSO Current { get; private set; }

    private void Awake()
    {
        if (Current != null)
        {
            Destroy(gameObject);
            return;
        }

        Current = run_config;
        DontDestroyOnLoad(gameObject);

        // 시작 시 1회 발행(스포너가 자동 갱신할 수 있게)
        RunSignals.RaiseRunConfigChanged();
    }

    // 나중에 로비에서 모드 선택 붙일 때 사용
    public void SetConfig(RunConfigSO config)
    {
        run_config = config;
        Current = config;
        RunSignals.RaiseRunConfigChanged();
    }
}
