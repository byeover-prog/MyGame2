using UnityEngine;
using _Game.Scripts.Core.Session;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class GameManager2D : MonoBehaviour
{
    public static GameManager2D Instance { get; private set; }

    [Header("Manager References")]
    [SerializeField] private SessionGameManager2D sessionManager;
    [SerializeField] private KillCountSource killCountSource;
    [SerializeField] private StageManager2D stageManager;

    [Tooltip("Scene_Game start-flow references. If empty, the runtime resolves this from the existing scene references.")]
    [SerializeField] private GameSceneContext sceneContext;

    [Header("Game Start")]
    [SerializeField] private bool autoStartOnAwake = true;

    [Header("Debug")]
    [SerializeField] private bool log = true;

    private GameSceneRuntime _runtime;

    public bool IsGameRunning => _runtime != null && _runtime.IsGameRunning;

    public int KillCount => _runtime != null ? _runtime.KillCount : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveLegacyReferences();
        ResolveRuntime();

        if (sessionManager == null)
            Debug.LogError("[GameManager2D] SessionGameManager2D is missing in Scene_Game.", this);
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(SquadLoadoutRuntime.MainId))
            SquadLoadoutRuntime.LoadFromSave();

        if (autoStartOnAwake)
            StartGame();
    }

    private void OnEnable()
    {
        RunSignals.PlayerDead += OnPlayerDead;
    }

    private void OnDisable()
    {
        RunSignals.PlayerDead -= OnPlayerDead;
    }

    private void OnClickRetry()
    {
        Instance?.RestartGame();
    }

    public void StartGame()
    {
        StartGame(RunSetupHolder.GetOrCreateFromCurrentState());
    }

    public void StartGame(RunSetup runSetup)
    {
        EnsureRuntime();
        _runtime.StartGame(runSetup);
    }

    public void EndGame_Defeat()
    {
        EnsureRuntime();
        _runtime.EndDefeat();
    }

    public void EndGame_Victory()
    {
        EnsureRuntime();
        _runtime.EndVictory();
    }

    public void RestartGame()
    {
        EnsureRuntime();
        _runtime.RestartCurrentScene();
    }

    private void OnPlayerDead()
    {
        if (log)
            GameLogger.Log("[GameManager2D] PlayerDead received. Ending run as defeat.", this);

        EndGame_Defeat();
    }

    private void EnsureRuntime()
    {
        if (_runtime == null)
            ResolveRuntime();
    }

    private void ResolveRuntime()
    {
        sceneContext = GameSceneContext.ResolveFor(this, sceneContext, sessionManager, killCountSource, stageManager);

        if (sceneContext != null)
        {
            sessionManager = sceneContext.SessionManager;
            killCountSource = sceneContext.KillCountSource;
            stageManager = sceneContext.StageManager;
        }

        _runtime = new GameSceneRuntime(sceneContext, this, log);
    }

    private void ResolveLegacyReferences()
    {
        if (sessionManager == null)
            sessionManager = FindFirstObjectByType<SessionGameManager2D>();

        if (killCountSource == null)
            killCountSource = FindFirstObjectByType<KillCountSource>();

        if (stageManager == null)
            stageManager = FindFirstObjectByType<StageManager2D>();
    }
}
