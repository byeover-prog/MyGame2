using UnityEngine;

public class PooledObject2D : MonoBehaviour
{
    private ProjectilePool2D _pool;

    internal void BindPool(ProjectilePool2D pool)
    {
        _pool = pool;
    }

    public void ReturnToPool()
    {
        if (_pool != null)
        {
            _pool.Return(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}