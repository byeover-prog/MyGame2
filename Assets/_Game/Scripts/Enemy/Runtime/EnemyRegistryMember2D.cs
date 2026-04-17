using UnityEngine;

// 적 프리팹에 부착. 활성화 시 EnemyRegistry2D + EnemyRegistryExtensions에 등록.

[DisallowMultipleComponent]
public sealed class EnemyRegistryMember2D : MonoBehaviour
{
    private Transform _tr;
    private IDamageable2D _damageable;
    private int _rootId;

    public Vector2 Position => _tr != null ? (Vector2)_tr.position : (Vector2)transform.position;
    public Transform Transform => _tr != null ? _tr : transform;

    public int RootInstanceId => _rootId;

    public bool IsValidTarget
    {
        get
        {
            if (!isActiveAndEnabled) return false;
            if (_damageable == null) return true;
            return !_damageable.IsDead;
        }
    }

    private void Awake()
    {
        _tr = transform;
        _damageable = GetComponentInParent<IDamageable2D>();
        _rootId = transform.root.gameObject.GetInstanceID();
    }

    private void OnEnable()
    {
        // 풀에서 재활성화될 때 캐시 갱신
        // (풀링된 오브젝트는 Awake가 다시 호출되지 않으므로 여기서 재확인)
        if (_tr == null) _tr = transform;
        if (_damageable == null) _damageable = GetComponentInParent<IDamageable2D>();
        _rootId = transform.root.gameObject.GetInstanceID();

        EnemyRegistry2D.Register(this);

        // CentralProjectileManager의 InstanceID 기반 O(1) 조회를 위해 등록
        // 키: RootInstanceId와 동일한 값을 사용해야 역참조가 일치함
        EnemyRegistryExtensions.RegisterById(this);
    }

    private void OnDisable()
    {
        EnemyRegistry2D.Unregister(this);
        EnemyRegistryExtensions.UnregisterById(this);
    }
}