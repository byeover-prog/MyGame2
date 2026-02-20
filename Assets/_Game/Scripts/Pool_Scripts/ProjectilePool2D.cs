using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProjectilePool2D : MonoBehaviour
{
    [SerializeField] private PooledObject2D prefab;
    [Min(0)] [SerializeField] private int prewarmCount = 20;
    [Min(1)] [SerializeField] private int maxCount = 200;

    private readonly Queue<PooledObject2D> _pool = new Queue<PooledObject2D>(256);
    private int _created;

    private void Awake()
    {
        if (prefab == null) return;

        int target = Mathf.Clamp(prewarmCount, 0, maxCount);
        for (int i = 0; i < target; i++)
        {
            var obj = CreateNew();
            Return(obj);
        }
    }

    public T Get<T>(Vector3 pos, Quaternion rot) where T : PooledObject2D
    {
        var obj = GetRaw();
        obj.transform.SetPositionAndRotation(pos, rot);
        obj.gameObject.SetActive(true);
        return (T)obj;
    }

    public PooledObject2D GetRaw()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();

        if (_created >= maxCount)
            return CreateNew(); // max 넘어도 “멈춤” 대신 생성(프로토타입 안전성 우선)

        return CreateNew();
    }

    private PooledObject2D CreateNew()
    {
        _created++;
        var obj = Instantiate(prefab, transform);
        obj.BindPool(this);
        obj.gameObject.SetActive(false);
        return obj;
    }

    public void Return(PooledObject2D obj)
    {
        if (obj == null) return;
        obj.gameObject.SetActive(false);
        _pool.Enqueue(obj);
    }
}