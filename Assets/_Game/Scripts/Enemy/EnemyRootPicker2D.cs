// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - EnemyRootSO에 등록된 목록을 "가중치 랜덤"으로 뽑는다.
// - 스폰 시스템은 이 함수를 호출해서 프리팹을 얻어 Instantiate/Pool 한다.
public static class EnemyRootPicker2D
{
    public static EnemyRootSO.EnemyEntry Pick(EnemyRootSO root)
    {
        if (root == null || root.Enemies == null || root.Enemies.Count == 0)
            return null;

        float total = 0f;
        for (int i = 0; i < root.Enemies.Count; i++)
        {
            var e = root.Enemies[i];
            if (e == null || e.Prefab == null) continue;
            if (e.Weight <= 0f) continue;
            total += e.Weight;
        }

        if (total <= 0f) return null;

        float r = Random.value * total;
        float acc = 0f;

        for (int i = 0; i < root.Enemies.Count; i++)
        {
            var e = root.Enemies[i];
            if (e == null || e.Prefab == null) continue;
            if (e.Weight <= 0f) continue;

            acc += e.Weight;
            if (r <= acc)
                return e;
        }

        // 부동소수 오차 안전망
        for (int i = root.Enemies.Count - 1; i >= 0; i--)
        {
            var e = root.Enemies[i];
            if (e != null && e.Prefab != null && e.Weight > 0f)
                return e;
        }

        return null;
    }
}