using UnityEngine;

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

        // 몬스터 콜라이더가 자식에 있고, 체력/사망 스크립트가 루트에 있는 구조 대비
        _damageable = GetComponentInParent<IDamageable2D>();

        _rootId = transform.root.gameObject.GetInstanceID();
    }

    private void OnEnable()
    {
        EnemyRegistry2D.Register(this);
    }

    private void OnDisable()
    {
        EnemyRegistry2D.Unregister(this);
    }
}