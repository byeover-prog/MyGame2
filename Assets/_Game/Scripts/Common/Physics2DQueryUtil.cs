using System.Collections.Generic;
using UnityEngine;

public static class Physics2DQueryUtil
{
    // 재사용 리스트(단일 스레드/메인스레드 전제)
    private static readonly List<Collider2D> _results = new List<Collider2D>(64);

    // 원형 오버랩: NonAlloc 대체 (GC 최소화)
    public static int OverlapCircle(
        Vector2 center,
        float radius,
        int layerMask,
        List<Collider2D> outResults,
        bool useTriggers = true)
    {
        outResults.Clear();

        var filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = layerMask;

        // 트리거 포함 여부
        filter.useTriggers = useTriggers;

        Physics2D.OverlapCircle(center, radius, filter, outResults);
        return outResults.Count;
    }

    // 편의: 내부 static 리스트를 쓰고 싶을 때
    public static IReadOnlyList<Collider2D> OverlapCircle(
        Vector2 center,
        float radius,
        int layerMask,
        out int count,
        bool useTriggers = true)
    {
        count = OverlapCircle(center, radius, layerMask, _results, useTriggers);
        return _results;
    }
}