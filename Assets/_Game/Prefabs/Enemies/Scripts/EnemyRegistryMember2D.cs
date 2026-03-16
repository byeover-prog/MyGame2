// UTF-8
// [구현 원리 요약]
// - 적 프리팹이 활성/비활성 될 때 레지스트리에 등록/해제된다.
// - 체력 컴포넌트를 캐싱해서 타겟 판단 비용을 줄인다.
using UnityEngine;

/// <summary>
/// 적 프리팹 루트에 부착하는 레지스트리 멤버.
/// OnEnable/OnDisable 시점에 EnemyRegistry2D에 자동 등록/해제된다.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyRegistryMember2D : MonoBehaviour
{
    private Transform _tr;
    private IDamageable2D _damageable;
    private EnemyHealth2D _health;
    private int _rootId;

    /// <summary>현재 위치 (캐싱된 Transform 우선)</summary>
    public Vector2 Position => _tr != null ? (Vector2)_tr.position : (Vector2)transform.position;

    /// <summary>캐싱된 Transform</summary>
    public Transform Transform => _tr != null ? _tr : transform;

    /// <summary>루트 오브젝트의 InstanceID (동일 적 판별용)</summary>
    public int RootInstanceId => _rootId;

    /// <summary>체력 컴포넌트 보유 여부</summary>
    public bool HasHealth => _health != null;

    /// <summary>현재 체력</summary>
    public int CurrentHp => _health != null ? _health.CurrentHp : 0;

    /// <summary>최대 체력</summary>
    public int MaxHp => _health != null ? _health.MaxHp : 0;

    /// <summary>유효한 타겟인지 (활성 + 생존 상태)</summary>
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
        Cache();
    }

    private void OnEnable()
    {
        Cache();
        EnemyRegistry2D.Register(this);
    }

    private void OnDisable()
    {
        EnemyRegistry2D.Unregister(this);
    }

    private void Cache()
    {
        _tr = transform;
        _damageable = GetComponentInParent<IDamageable2D>();
        _health = GetComponentInParent<EnemyHealth2D>();
        _rootId = transform.root.gameObject.GetInstanceID();
    }
}
