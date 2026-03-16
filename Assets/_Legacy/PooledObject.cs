// UTF-8
// [구현 원리 요약]
// - 구형 PooledObject 상속 파일이 계속 동작하도록 최소 공통 반납 로직만 유지한다.
// - 중복 반납을 막고, 풀 정보가 없으면 안전하게 파괴한다.
using UnityEngine;

/// <summary>
/// 풀링 가능한 오브젝트의 기본 클래스.
/// ProjectilePool과 연동하여 Get/Release 사이클을 관리한다.
/// </summary>
public class PooledObject : MonoBehaviour, IPoolable
{
    private ProjectilePool _pool;
    private GameObject _prefabKey;
    private bool _isReturning;

    /// <summary>풀에서 꺼내질 때 호출된다.</summary>
    public virtual void OnPoolGet()
    {
        _isReturning = false;
    }

    /// <summary>풀에 반납될 때 호출된다.</summary>
    public virtual void OnPoolRelease()
    {
    }

    /// <summary>풀 참조를 바인딩한다. (최초 생성 시 1회)</summary>
    public void BindPool(ProjectilePool pool, GameObject prefabKey)
    {
        _pool = pool;
        _prefabKey = prefabKey;
    }

    /// <summary>
    /// 이 오브젝트를 풀에 반납한다.
    /// 풀 정보가 없으면 Destroy로 안전하게 제거한다.
    /// </summary>
    public void ReleaseToPool()
    {
        if (_isReturning) return;
        _isReturning = true;

        if (_pool != null && _prefabKey != null)
        {
            _pool.Release(_prefabKey, gameObject);
            return;
        }

        Destroy(gameObject);
    }
}