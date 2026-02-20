using System;
using System.Collections.Generic;

public static class WeightedOfferPicker
{
    public static List<T> Pick<T>(
        List<T> source,
        int count,
        Func<T, int> weightSelector,
        Func<T, string> uniqueKeySelector)
    {
        var result = new List<T>(count);
        var usedKeys = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            T selected = default;
            int totalWeight = 0;

            foreach (var item in source)
            {
                var key = uniqueKeySelector(item);
                if (usedKeys.Contains(key)) continue;
                totalWeight += weightSelector(item);
            }

            if (totalWeight == 0) break;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int current = 0;

            foreach (var item in source)
            {
                var key = uniqueKeySelector(item);
                if (usedKeys.Contains(key)) continue;

                current += weightSelector(item);
                if (roll < current)
                {
                    selected = item;
                    usedKeys.Add(key);
                    break;
                }
            }

            result.Add(selected);
        }

        return result;
    }
}