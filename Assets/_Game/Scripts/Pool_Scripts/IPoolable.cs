using UnityEngine;

public interface IPoolable
{
    // 풀에서 꺼낼 때 호출
    void OnPoolGet();

    // 풀로 반납될 때 호출
    void OnPoolRelease();

    // 풀/프리팹키 바인딩(풀에서 1회 설정)
    void BindPool(ProjectilePool pool, GameObject prefabKey);

    // 안전 반납(외부에서 이거만 호출)
    void ReleaseToPool();
}