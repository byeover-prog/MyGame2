using UnityEngine;

public static class Physics2DCompat
{
    // int layerMask 버전
    public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results, int layerMask)
    {
        var filter = CreateFilter((LayerMask)layerMask);
        return Physics2D.OverlapCircle(point, radius, filter, results);
    }

    // LayerMask 버전
    public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results, LayerMask layerMask)
    {
        var filter = CreateFilter(layerMask);
        return Physics2D.OverlapCircle(point, radius, filter, results);
    }

    private static ContactFilter2D CreateFilter(LayerMask layerMask)
    {
        var filter = new ContactFilter2D();

        // 레이어 필터
        filter.useLayerMask = true;
        filter.SetLayerMask(layerMask);

        // 트리거 포함 여부(프로젝트 전역 설정에 맞춤)
        filter.useTriggers = Physics2D.queriesHitTriggers;

        // Depth/Angle 필터는 사용 안 함
        filter.useDepth = false;
        filter.useOutsideDepth = false;
        filter.useNormalAngle = false;
        filter.useOutsideNormalAngle = false;

        return filter;
    }
}