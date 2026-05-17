using UnityEngine;

public sealed class GameBootstrapper : MonoBehaviour
{
    [Header("Compatibility")]
    [SerializeField] private GameSceneContext sceneContext;
    [SerializeField] private Transform player;
    [SerializeField] private Transform systemsRoot;

    private void Awake()
    {
        if (sceneContext == null)
            sceneContext = FindFirstObjectByType<GameSceneContext>(FindObjectsInactive.Include);

        if (sceneContext != null)
            return;

        PrepareLegacyPlayerLoadout();
    }

    private void PrepareLegacyPlayerLoadout()
    {
        if (player == null)
        {
            Debug.LogError("[GameBootstrapper] player is missing. Assign the player Transform or use GameSceneContext.", this);
            return;
        }

        if (systemsRoot == null)
            Debug.LogWarning("[GameBootstrapper] systemsRoot is missing. GameSceneContext should own Scene_Game roots.", this);

        WeaponShooterSystem2D shooter = player.GetComponentInChildren<WeaponShooterSystem2D>(true);
        if (shooter == null)
        {
            Debug.LogError("[GameBootstrapper] WeaponShooterSystem2D is missing on the player prefab.", this);
            return;
        }

        shooter.EnsureDefaultLoadoutIfEmpty();
    }
}
