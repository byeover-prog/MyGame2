using UnityEngine;
using _Game.Scripts.Core.Session;

[DefaultExecutionOrder(-120)]
[DisallowMultipleComponent]
public sealed class GameSceneContext : MonoBehaviour
{
    [Header("Scene runtime owners")]
    [SerializeField] private GameManager2D gameManager;
    [SerializeField] private SessionGameManager2D sessionManager;
    [SerializeField] private KillCountSource killCountSource;
    [SerializeField] private StageManager2D stageManager;
    [SerializeField] private EnemySpawner2D enemySpawner;

    [Header("Scene roots")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform systemsRoot;

    [Header("Resolve")]
    [SerializeField] private bool resolveMissingReferencesOnAwake = true;

    public GameManager2D GameManager => gameManager;
    public SessionGameManager2D SessionManager => sessionManager;
    public KillCountSource KillCountSource => killCountSource;
    public StageManager2D StageManager => stageManager;
    public EnemySpawner2D EnemySpawner => enemySpawner;
    public Transform Player => player;
    public Transform SystemsRoot => systemsRoot;

    private void Awake()
    {
        if (resolveMissingReferencesOnAwake)
            ResolveMissingReferences();
    }

    public static GameSceneContext ResolveFor(
        GameManager2D owner,
        GameSceneContext explicitContext,
        SessionGameManager2D sessionFallback,
        KillCountSource killCountFallback,
        StageManager2D stageFallback)
    {
        GameSceneContext context = explicitContext;

        if (context == null && owner != null)
            context = owner.GetComponent<GameSceneContext>();

        if (context == null)
            context = FindFirstObjectByType<GameSceneContext>(FindObjectsInactive.Include);

        if (context == null && owner != null)
            context = owner.gameObject.AddComponent<GameSceneContext>();

        if (context == null)
            return null;

        context.ApplyFallbacks(owner, sessionFallback, killCountFallback, stageFallback);
        context.ResolveMissingReferences();
        return context;
    }

    public void ResolveMissingReferences()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager2D>(FindObjectsInactive.Include);

        if (sessionManager == null)
            sessionManager = FindFirstObjectByType<SessionGameManager2D>(FindObjectsInactive.Include);

        if (killCountSource == null)
            killCountSource = FindFirstObjectByType<KillCountSource>(FindObjectsInactive.Include);

        if (stageManager == null)
            stageManager = FindFirstObjectByType<StageManager2D>(FindObjectsInactive.Include);

        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner2D>(FindObjectsInactive.Include);

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (systemsRoot == null)
        {
            GameObject systemsObject = GameObject.Find("%Systems");
            if (systemsObject != null)
                systemsRoot = systemsObject.transform;
        }
    }

    public bool TryValidateForStart(out string reason)
    {
        if (sessionManager == null)
        {
            reason = "GameSceneContext requires SessionGameManager2D before the run starts.";
            return false;
        }

        if (killCountSource == null)
        {
            reason = "GameSceneContext requires KillCountSource before the run starts.";
            return false;
        }

        if (stageManager == null)
        {
            reason = "GameSceneContext requires StageManager2D before the run starts.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool TryPreparePlayerForStart(out string reason)
    {
        ResolveMissingReferences();

        if (player == null)
        {
            reason = "GameSceneContext requires a player Transform before the run starts.";
            return false;
        }

        WeaponShooterSystem2D shooter = player.GetComponentInChildren<WeaponShooterSystem2D>(true);
        if (shooter == null)
        {
            reason = "Player requires WeaponShooterSystem2D before the run starts.";
            return false;
        }

        PlayerCollisionBody2D collisionBody = player.GetComponentInChildren<PlayerCollisionBody2D>(true);
        if (collisionBody == null)
            collisionBody = player.gameObject.AddComponent<PlayerCollisionBody2D>();

        collisionBody.EnsureBody();
        shooter.EnsureDefaultLoadoutIfEmpty();
        reason = string.Empty;
        return true;
    }

    public T GetPlayerComponent<T>() where T : Component
    {
        ResolveMissingReferences();
        return player != null ? player.GetComponentInChildren<T>(true) : null;
    }

    public T GetSystemsComponent<T>() where T : Component
    {
        ResolveMissingReferences();
        return systemsRoot != null ? systemsRoot.GetComponentInChildren<T>(true) : null;
    }

    private void ApplyFallbacks(
        GameManager2D owner,
        SessionGameManager2D sessionFallback,
        KillCountSource killCountFallback,
        StageManager2D stageFallback)
    {
        if (gameManager == null)
            gameManager = owner;

        if (sessionManager == null)
            sessionManager = sessionFallback;

        if (killCountSource == null)
            killCountSource = killCountFallback;

        if (stageManager == null)
            stageManager = stageFallback;
    }
}
