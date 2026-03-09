// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - "발사 순간 1회 탐색" 기본값을 지키기 위해 간단/안전한 타겟 탐색 유틸 제공.
// - 물리 OverlapCircleNonAlloc로 GC 최소화.
public static class Targeting2D
{
    // 적 수가 많으면 128 -> 256으로 늘려도 됨(프로토타입 기본값)
    private static readonly Collider2D[] _buffer = new Collider2D[128];

    public static bool TryGetClosestEnemy(Vector2 origin, float radius, LayerMask enemyMask, int excludeRootId, out Transform target)
    {
        target = null;

        int count = Physics2DCompat.OverlapCircleNonAlloc(origin, radius, _buffer, enemyMask);
        if (count <= 0) return false;

        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            var col = _buffer[i];
            if (col == null) continue;

            int rootId = DamageUtil2D.GetRootInstanceId(col);
            if (excludeRootId != 0 && rootId == excludeRootId)
                continue;

            Transform t = col.attachedRigidbody != null ? col.attachedRigidbody.transform : col.transform;
            float sqr = ((Vector2)t.position - origin).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                target = t;
            }
        }

        return target != null;
    }
}