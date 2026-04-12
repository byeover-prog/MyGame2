using UnityEngine;

[System.Serializable]
public struct WeaponFinalStats2D
{
    public int damage;
    public float fireInterval;
    public float range;
    public float projectileSpeed;
    public float lifetime;

    public int pierce;
    public int split;
    public int shotCount;

    public bool homing;
    public bool boomerang;
    public bool rotate;
}