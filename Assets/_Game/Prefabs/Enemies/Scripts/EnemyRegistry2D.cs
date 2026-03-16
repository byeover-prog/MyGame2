// UTF-8
// [구현 원리 요약]
// - 전투 중 매번 물리 전체 탐색을 하지 않도록 살아 있는 적 목록을 순회한다.
// - 가장 가까운 적 / 가장 먼 적 / 체력이 가장 높은 적 선택을 한 곳에서 처리한다.
// - Register 시 HashSet으로 O(1) 중복 체크를 하여 적이 수백 마리여도 병목이 없다.
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 살아 있는 적 전체 목록을 관리하는 정적 레지스트리.
/// 모든 스킬이 타겟 탐색 시 이 레지스트리를 사용한다.
/// </summary>
public static class EnemyRegistry2D
{
    private static readonly List<EnemyRegistryMember2D> _enemies = new List<EnemyRegistryMember2D>(256);
    private static readonly HashSet<EnemyRegistryMember2D> _set = new HashSet<EnemyRegistryMember2D>();

    /// <summary>현재 등록된 적 수</summary>
    public static int Count => _enemies.Count;

    /// <summary>적 등록 (OnEnable에서 호출)</summary>
    public static void Register(EnemyRegistryMember2D enemy)
    {
        if (enemy == null) return;
        if (!_set.Add(enemy)) return;
        _enemies.Add(enemy);
    }

    /// <summary>적 해제 (OnDisable에서 호출)</summary>
    public static void Unregister(EnemyRegistryMember2D enemy)
    {
        if (enemy == null) return;
        if (!_set.Remove(enemy)) return;
        _enemies.Remove(enemy);
    }

    /// <summary>씬 전환 시 전체 초기화</summary>
    public static void Clear()
    {
        _enemies.Clear();
        _set.Clear();
    }

    /// <summary>가장 가까운 적 탐색 (거리 제한 없음)</summary>
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

    /// <summary>가장 가까운 적 탐색 (거리 제한 있음)</summary>
    public static bool TryGetNearest(Vector2 from, float maxDistance, out EnemyRegistryMember2D result)
    {
        result = null;
        float bestSqr = float.PositiveInfinity;
        float maxSqr = maxDistance > 0f ? maxDistance * maxDistance : float.PositiveInfinity;

        for (int i = 0; i < _enemies.Count; i++)
        {
            var e = _enemies[i];
            if (e == null || !e.IsValidTarget) continue;

            float sqr = (e.Position - from).sqrMagnitude;
            if (sqr > maxSqr) continue;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                result = e;
            }
        }

        return result != null;
    }

    /// <summary>가장 먼 적 탐색 (거리 제한 없음)</summary>
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

    /// <summary>가장 먼 적 탐색 (거리 제한 있음)</summary>
    public static bool TryGetFarthest(Vector2 from, float maxDistance, out EnemyRegistryMember2D result)
    {
        result = null;
        float bestSqr = -1f;
        float maxSqr = maxDistance > 0f ? maxDistance * maxDistance : float.PositiveInfinity;

        for (int i = 0; i < _enemies.Count; i++)
        {
            var e = _enemies[i];
            if (e == null || !e.IsValidTarget) continue;

            float sqr = (e.Position - from).sqrMagnitude;
            if (sqr > maxSqr) continue;

            if (sqr > bestSqr)
            {
                bestSqr = sqr;
                result = e;
            }
        }

        return result != null;
    }

    /// <summary>최대 체력이 가장 높은 적 탐색 (같으면 거리 가까운 쪽 우선)</summary>
    public static bool TryGetHighestHp(Vector2 from, float maxDistance, out EnemyRegistryMember2D result)
    {
        result = null;
        int bestHp = int.MinValue;
        float bestSqr = float.PositiveInfinity;
        float maxSqr = maxDistance > 0f ? maxDistance * maxDistance : float.PositiveInfinity;

        for (int i = 0; i < _enemies.Count; i++)
        {
            var e = _enemies[i];
            if (e == null || !e.IsValidTarget) continue;
            if (!e.HasHealth) continue;

            float sqr = (e.Position - from).sqrMagnitude;
            if (sqr > maxSqr) continue;

            int hp = e.CurrentHp;
            if (hp > bestHp || (hp == bestHp && sqr < bestSqr))
            {
                bestHp = hp;
                bestSqr = sqr;
                result = e;
            }
        }

        return result != null;
    }

    /// <summary>특정 ID를 제외하고 가장 가까운 적 탐색 (수리검 튕김 등)</summary>
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
