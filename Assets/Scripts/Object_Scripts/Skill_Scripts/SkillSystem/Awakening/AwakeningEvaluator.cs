// AwakeningEvaluator.cs
using System.Collections.Generic;
using UnityEngine;

public static class AwakeningEvaluator
{
    public static int CollectEligible(
        AwakeningDatabaseSO database,
        IWeaponProgressProvider progress,
        int currentLevelUpIndex,
        List<AwakeningRecipeSO> results)
    {
        if (results == null)
            return 0;

        results.Clear();

        if (database == null || progress == null)
            return 0;

        var recipes = database.Recipes;
        if (recipes == null || recipes.Length == 0)
            return 0;

        for (int i = 0; i < recipes.Length; i++)
        {
            var recipe = recipes[i];
            if (recipe == null)
                continue;

            if (IsEligible(recipe, progress, currentLevelUpIndex))
                results.Add(recipe);
        }

        return results.Count;
    }

    public static bool IsEligible(AwakeningRecipeSO recipe, IWeaponProgressProvider progress, int currentLevelUpIndex)
    {
        if (recipe == null || progress == null)
            return false;

        string baseId = recipe.BaseWeaponId;
        if (string.IsNullOrEmpty(baseId))
            return false;

        if (progress.IsWeaponAwakened(baseId))
            return false;

        if (!progress.TryGetWeaponLevel(baseId, out int baseLevel))
            return false;

        if (baseLevel < recipe.BaseRequiredLevel)
            return false;

        // "다음 레벨업에서" 규칙:
        // 8을 찍은 레벨업 인덱스(maxedAt)보다 현재 레벨업 인덱스가 '큰' 경우에만 각성 후보
        if (!progress.TryGetWeaponMaxedAtLevelUpIndex(baseId, out int maxedAt))
            return false;

        if (currentLevelUpIndex <= maxedAt)
            return false;

        var reqs = recipe.Requirements;
        if (reqs != null)
        {
            for (int r = 0; r < reqs.Length; r++)
            {
                var req = reqs[r];
                if (string.IsNullOrEmpty(req.WeaponId))
                    continue;

                if (!progress.TryGetWeaponLevel(req.WeaponId, out int reqLevel))
                    return false;

                if (reqLevel < req.MinLevel)
                    return false;
            }
        }

        return true;
    }

    public static AwakeningRecipeSO PickOne(IList<AwakeningRecipeSO> eligible)
    {
        if (eligible == null || eligible.Count == 0)
            return null;

        int idx = Random.Range(0, eligible.Count);
        return eligible[idx];
    }
}