using UnityEngine;

public static class GameplayCollisionPolicy2D
{
    private static bool _warnedMissingLayers;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyBeforeSceneLoad()
    {
        Apply();
    }

    public static void Apply()
    {
        int player = GameplayCollisionLayers2D.PlayerLayer;
        int playerBody = GameplayCollisionLayers2D.PlayerBodyLayer;
        int enemy = GameplayCollisionLayers2D.EnemyLayer;
        int obstacle = GameplayCollisionLayers2D.ObstacleLayer;
        int projectile = GameplayCollisionLayers2D.ProjectileLayer;

        if (!GameplayCollisionLayers2D.HasRequiredLayers)
        {
            if (!_warnedMissingLayers)
            {
                _warnedMissingLayers = true;
                GameLogger.LogWarning("[CollisionPolicy] Required layers are missing. Add PlayerBody and Obstacle layers.");
            }

            return;
        }

        SetCollision(playerBody, obstacle, true);
        SetCollision(enemy, obstacle, true);
        SetCollision(enemy, enemy, true);
        SetCollision(player, enemy, true);

        SetCollision(playerBody, enemy, false);
        SetCollision(playerBody, player, false);

        if (projectile >= 0)
            SetCollision(playerBody, projectile, false);
    }

    private static void SetCollision(int a, int b, bool shouldCollide)
    {
        if (a < 0 || b < 0) return;
        Physics2D.IgnoreLayerCollision(a, b, !shouldCollide);
    }
}
