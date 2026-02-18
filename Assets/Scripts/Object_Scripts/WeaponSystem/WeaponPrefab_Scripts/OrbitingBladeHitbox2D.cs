using UnityEngine;

[DisallowMultipleComponent]
public sealed class OrbitingBladeHitbox2D : MonoBehaviour
{
    private OrbitingBladeWeapon2D owner;

    public void BindOwner(OrbitingBladeWeapon2D weapon)
    {
        owner = weapon;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null) owner.TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (owner != null) owner.TryHit(other);
    }
}