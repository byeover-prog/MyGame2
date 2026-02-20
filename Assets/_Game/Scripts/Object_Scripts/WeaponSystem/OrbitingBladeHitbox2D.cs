using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class OrbitingBladeHitbox2D : MonoBehaviour
{
    private OrbitingBladeWeapon2D _owner;

    private void Awake()
    {
        // 회전검은 '접촉 판정'이 핵심이므로 트리거 강제
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void BindOwner(OrbitingBladeWeapon2D owner)
    {
        _owner = owner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        _owner?.TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Stay도 같이 처리하면 이동이 느릴 때도 타격이 안정적임
        _owner?.TryHit(other);
    }
}