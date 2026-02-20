using UnityEngine;

public static class DamageUtil2D
{
    public static bool TryApplyDamage(Collider2D other, int damage)
    {
        if (other == null) return false;
        if (damage <= 0) return false;

        // 1) collider 자체
        if (other.TryGetComponent<IDamageable2D>(out var d0))
        {
            if (!d0.IsDead) d0.TakeDamage(damage);
            return true;
        }

        // 2) 상위(자식 콜라이더 대비)
        var d1 = other.GetComponentInParent<IDamageable2D>();
        if (d1 != null)
        {
            if (!d1.IsDead) d1.TakeDamage(damage);
            return true;
        }

        return false;
    }

    public static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return ((1 << layer) & mask.value) != 0;
    }

    public static int GetRootId(Collider2D col)
    {
        if (col == null) return 0;
        var rb = col.attachedRigidbody;
        if (rb != null) return rb.gameObject.GetInstanceID();
        return col.transform.root.gameObject.GetInstanceID();
    }
}