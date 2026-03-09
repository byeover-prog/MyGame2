using UnityEngine;

public abstract class PooledObject : MonoBehaviour, IPoolable
{
    private ProjectilePool _pool;
    private GameObject _prefabKey;
    private bool _isInPool;

    public void BindPool(ProjectilePool pool, GameObject prefabKey)
    {
        _pool = pool;
        _prefabKey = prefabKey;
    }

    public void ReleaseToPool()
    {
        if (_isInPool) return;            // 중복 반납 방지
        if (_pool == null || _prefabKey == null)
        {
            // 풀에 안 묶인 오브젝트면 파괴(누수 방지)
            Destroy(gameObject);
            return;
        }

        _pool.ReleaseInternal(_prefabKey, gameObject);
    }

    // 풀에서 꺼낼 때 내부에서 호출됨
    internal void MarkOutOfPool()
    {
        _isInPool = false;
    }

    // 풀로 들어갈 때 내부에서 호출됨
    internal void MarkInPool()
    {
        _isInPool = true;
    }

    public virtual void OnPoolGet() { }
    public virtual void OnPoolRelease() { }
}