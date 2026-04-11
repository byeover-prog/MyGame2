using System.Collections.Generic;
using UnityEngine;

// 적 등록부. 모든 활성 적을 추적하여 Physics 쿼리 없이 타겟팅할 수 있게 한다.

public static class EnemyRegistry2D
{
    private static readonly List<EnemyRegistryMember2D> _enemies = new List<EnemyRegistryMember2D>(256);

    public static int Count => _enemies.Count;
    
    // 등록된 적 목록 읽기 전용 접근.
    // WeaponShooterSystem2D.TryPickTargetFromRegistry()에서 사용.

    public static IReadOnlyList<EnemyRegistryMember2D> Members => _enemies;

    public static void Register(EnemyRegistryMember2D enemy)
    {
        if (enemy == null) return;
        if (_enemies.Contains(enemy)) return;
        _enemies.Add(enemy);
    }

   
    // SwapBack O(1) 제거. 기존 List.Remove()는 O(N)이라 적 수가 많을 때 비쌈.
    // 순서가 바뀌지만, 레지스트리는 순서를 보장할 필요가 없으므로 안전함.
    public static void Unregister(EnemyRegistryMember2D enemy)
    {
        if (enemy == null) return;

        int idx = _enemies.IndexOf(enemy);
        if (idx < 0) return;

        int last = _enemies.Count - 1;
        if (idx < last)
            _enemies[idx] = _enemies[last];
        _enemies.RemoveAt(last);
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
