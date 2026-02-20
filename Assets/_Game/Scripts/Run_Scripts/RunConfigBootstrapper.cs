using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunConfigBootstrapper : MonoBehaviour
{
    [SerializeField] private RunConfigAsset run_config;
    public static RunConfigAsset Current { get; private set; }

    private void Awake()
    {
        if (Current != null) { Destroy(gameObject); return; }
        Current = run_config;
        DontDestroyOnLoad(gameObject);
    }
}
