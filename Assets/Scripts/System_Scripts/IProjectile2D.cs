using UnityEngine;

/// <summary>
/// "발사 가능한 투사체" 계약
/// - Shooter는 이 인터페이스만 호출 (직선/호밍/폭발/체인 등으로 확장 가능)
/// </summary>
public interface IProjectile2D
{
    void Launch(Vector2 dir, int damage, LayerMask targetMask);
}
