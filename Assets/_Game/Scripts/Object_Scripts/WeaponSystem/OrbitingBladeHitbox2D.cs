using UnityEngine;

// [구현 원리 요약]
// 이전 버전에서는 물리 충돌(Collider)을 감지해 데미지를 주었으나,
// 물리 충돌 버그(끼임, 튕김)를 없애기 위해 코드로 직접 거리를 계산하는 방식으로 리빌딩되었습니다.
// 따라서 이 스크립트는 더 이상 충돌 로직을 수행하지 않습니다. (컴파일 에러 방지용)
[DisallowMultipleComponent]
public sealed class OrbitingBladeHitbox2D : MonoBehaviour
{
    

    private void Awake()
    {
        
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

   
    public void BindOwner(OrbitingBladeWeapon2D owner)
    {
        // 더 이상 바인딩이 필요 없습니다.
    }
}