using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가중치 기반 랜덤 선택 유틸.
/// 
/// 요구사항(프로젝트 성격상 중요):
/// - "최소 구현"이라도 컴파일/런타임 안정성을 우선한다.
/// - 후보가 비었거나, 가중치가 0/음수거나, key가 null이더라도 예외 없이 동작한다.
/// - key가 null/빈 문자열인 항목은 "중복 방지 대상 아님"으로 취급한다(= 중복 허용).
/// 
/// 복잡도:
/// - O(count * n). 레벨업 후보가 수십 개 수준이므로 충분.
/// </summary>
public static class WeightedOfferPicker
{
    /// <summary>
    /// source에서 count개를 가중치 랜덤으로 뽑되, uniqueKeySelector로 중복을 방지합니다.
    /// </summary>
    public static List<T> Pick<T>(
        IReadOnlyList<T> source,
        int count,
        Func<T, int> weightSelector,
        Func<T, string> uniqueKeySelector)
    {
        var result = new List<T>(Mathf.Max(0, count));

        if (source == null || source.Count == 0) return result;
        if (count <= 0) return result;
        if (weightSelector == null) throw new ArgumentNullException(nameof(weightSelector));
        if (uniqueKeySelector == null) throw new ArgumentNullException(nameof(uniqueKeySelector));

        var usedKeys = new HashSet<string>();

        for (int pickIndex = 0; pickIndex < count; pickIndex++)
        {
            // 1) 총 가중치
            int totalWeight = 0;
            for (int i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (item is null) continue;

                string key = uniqueKeySelector(item);
                if (!string.IsNullOrWhiteSpace(key) && usedKeys.Contains(key))
                    continue;

                int w = weightSelector(item);
                if (w <= 0) continue;

                // overflow 방지(실제로는 거의 발생하지 않음)
                if (totalWeight > int.MaxValue - w)
                    totalWeight = int.MaxValue;
                else
                    totalWeight += w;
            }

            if (totalWeight <= 0)
                break;

            // 2) 롤
            int roll = UnityEngine.Random.Range(0, totalWeight);
            int acc = 0;

            bool found = false;
            T selected = default;
            string selectedKey = null;

            for (int i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (item is null) continue;

                string key = uniqueKeySelector(item);
                if (!string.IsNullOrWhiteSpace(key) && usedKeys.Contains(key))
                    continue;

                int w = weightSelector(item);
                if (w <= 0) continue;

                acc += w;
                if (roll < acc)
                {
                    selected = item;
                    selectedKey = key;
                    found = true;
                    break;
                }
            }

            if (!found)
                break;

            result.Add(selected);

            // null/빈 key는 "중복 방지"를 하지 않는다.
            if (!string.IsNullOrWhiteSpace(selectedKey))
                usedKeys.Add(selectedKey);
        }

        return result;
    }
}
