using UnityEngine;

public interface IPooledProjectile2D
{
    // 풀 참조 주입
    void SetPool(ProjectilePool pool);

    // "어떤 프리팹에서 나온 인스턴스인지" 키 주입 (반납 시 필요)
    void SetOriginPrefab(GameObject originPrefab);

    // 발사
    void Launch(Vector2 dir, int damage, LayerMask targetMask);
}