using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class OrbitingBladeHitbox2D : MonoBehaviour
{
    private OrbitingBladeWeapon2D _owner;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void BindOwner(OrbitingBladeWeapon2D owner)
    {
        _owner = owner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_owner == null) return;
        _owner.TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (_owner == null) return;
        _owner.TryHit(other);
    }
}