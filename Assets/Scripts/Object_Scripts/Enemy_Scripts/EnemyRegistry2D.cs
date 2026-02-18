using System.Collections.Generic;
using UnityEngine;

public static class EnemyRegistry2D
{
    private static readonly List<EnemyRegistryMember2D> _enemies = new List<EnemyRegistryMember2D>(256);

    public static void Register(EnemyRegistryMember2D enemy)
    {
        if (enemy == null) return;
        if (_enemies.Contains(enemy)) return;
        _enemies.Add(enemy);
    }

    public static void Unregister(EnemyRegistryMember2D enemy)
    {
        if (enemy == null) return;
        _enemies.Remove(enemy);
    }

    // 2인자 기본 버전(이게 있어야 다른 코드들이 다 살아남)
    public static bool TryGetNearest(Vector2 from, out EnemyRegistryMember2D result)
    {
        result = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < _enemies.Count; i++)
        {
            var e = _enemies[i];
            if (e == null || !e.IsValidTarget) continue;

            float sqr = (e.Position - from).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                result = e;
            }
        }

        return result != null;
    }

    // 3인자: 최대 거리 제한 버전(0 이하이면 제한 없음)
    public static bool TryGetNearest(Vector2 from, float maxDistance, out EnemyRegistryMember2D result)
    {
        if (!TryGetNearest(from, out result) || result == null)
            return false;

        if (maxDistance <= 0f)
            return true;

        float maxSqr = maxDistance * maxDistance;
        if ((result.Position - from).sqrMagnitude > maxSqr)
        {
            result = null;
            return false;
        }

        return true;
    }

    public static bool TryGetFarthest(Vector2 from, out EnemyRegistryMember2D result)
    {
        result = null;
        float bestSqr = 0f;

        for (int i = 0; i < _enemies.Count; i++)
        {
            var e = _enemies[i];
            if (e == null || !e.IsValidTarget) continue;

            float sqr = (e.Position - from).sqrMagnitude;
            if (result == null || sqr > bestSqr)
            {
                bestSqr = sqr;
                result = e;
            }
        }

        return result != null;
    }

    public static bool TryGetNearestExcluding(Vector2 from, HashSet<int> excludeIds, out EnemyRegistryMember2D result)
    {
        result = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < _enemies.Count; i++)
        {
            var e = _enemies[i];
            if (e == null || !e.IsValidTarget) continue;

            int id = e.RootInstanceId;
            if (excludeIds != null && excludeIds.Contains(id)) continue;

            float sqr = (e.Position - from).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                result = e;
            }
        }

        return result != null;
    }
}
