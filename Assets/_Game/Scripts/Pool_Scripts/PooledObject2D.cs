// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 풀링 반납 중에는 재귀(ReturnToPool->Return->SetActive(false)->OnDisable->ReturnToPool...)를 차단한다.
// - 투사체 쪽에서 OnDisable/OnEnable에 ReturnToPool이 있어도 에디터 먹통을 막는 '안전장치'다.
// - 권장: 투사체 스크립트에서 OnDisable에 ReturnToPool을 두지 말고, 수명/충돌 시점에만 명시적으로 호출.

public class PooledObject2D : MonoBehaviour
{
    private ProjectilePool2D _pool;

    // 재귀 반납 차단 플래그
    private bool _isReturning;

    internal void BindPool(ProjectilePool2D pool)
    {
        _pool = pool;
    }

    // 풀에서 꺼내질 때(재사용) 호출해도 됨
    internal void ClearReturningFlag()
    {
        _isReturning = false;
    }

    public void ReturnToPool()
    {
        // ★ 핵심: OnDisable/중복 호출로 인한 무한 재귀 차단
        if (_isReturning) return;
        _isReturning = true;

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