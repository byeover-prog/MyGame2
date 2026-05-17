using UnityEngine;

public static class GameplayCollisionLayers2D
{
    public const string Player = "Player";
    public const string PlayerBody = "PlayerBody";
    public const string Enemy = "Enemy";
    public const string Obstacle = "Obstacle";
    public const string Projectile = "Projectile";

    public static int PlayerLayer => LayerMask.NameToLayer(Player);
    public static int PlayerBodyLayer => LayerMask.NameToLayer(PlayerBody);
    public static int EnemyLayer => LayerMask.NameToLayer(Enemy);
    public static int ObstacleLayer => LayerMask.NameToLayer(Obstacle);
    public static int ProjectileLayer => LayerMask.NameToLayer(Projectile);

    public static bool HasRequiredLayers =>
        PlayerLayer >= 0
        && PlayerBodyLayer >= 0
        && EnemyLayer >= 0
        && ObstacleLayer >= 0;
}
