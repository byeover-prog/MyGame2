#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class CommonSkillAutoBuilderEditor2
{
    private const string RootFolder = "Assets/_GameData/CommonSkills";
    private const int DefaultMaxLevel = 8;

    [MenuItem("Tools/그날이후/공통스킬/Auto Build Skill SOs (8 levels)")]
    public static void Build()
    {
        EnsureFolder(RootFolder);

        // 1) Config 생성/갱신
        var configs = new List<CommonSkillConfigSO>(16);

        // 공통 스킬 7종(화살비 제외)
        configs.Add(UpsertConfig(CommonSkillKind.OrbitingBlade, "회전검", "Weapon_OrbitingBlade", "Icon_OrbitingBlade", ApplyOrbitingBladeDefaults));
        configs.Add(UpsertConfig(CommonSkillKind.Boomerang, "부메랑", "Weapon_Boomerang", "Icon_Boomerang", ApplyBoomerangDefaults));
        configs.Add(UpsertConfig(CommonSkillKind.PiercingBullet, "총알", "Weapon_PiercingBullet", "Icon_PiercingBullet", ApplyPiercingBulletDefaults));
        configs.Add(UpsertConfig(CommonSkillKind.HomingMissile, "호밍 미사일", "Weapon_HomingMissile", "Icon_HomingMissile", ApplyHomingMissileDefaults));
        configs.Add(UpsertConfig(CommonSkillKind.DarkOrb, "다크오브", "Weapon_DarkOrb", "Icon_DarkOrb", ApplyDarkOrbDefaults));
        configs.Add(UpsertConfig(CommonSkillKind.Shuriken, "수리검", "Weapon_Shuriken", "Icon_Shuriken", ApplyShurikenDefaults));
        configs.Add(UpsertConfig(CommonSkillKind.ArrowShot, "기본 화살", "Weapon_ArrowShot", "Icon_ArrowShot", ApplyArrowShotDefaults));
        configs.Add(UpsertConfig(CommonSkillKind.ArrowRain, "화살비", "Weapon_ArrowRain", "Icon_ArrowRain", ApplyArrowRainDefaults));

        // 기본 스킬 3종(네가 enum 확장하면 자동으로 생성됨)
        // enum에 아래 이름이 없다면 스킵된다.
        TryAddCharacterBasic(configs, "Balsi", "발시", "Weapon_Balsi", "Icon_Balsi", ApplyBalsiDefaults);
        TryAddCharacterBasic(configs, "JwagyeokYose", "좌격요세", "Weapon_JwagyeokYose", "Icon_JwagyeokYose", ApplyJwagyeokDefaults);
        TryAddCharacterBasic(configs, "NakroeBu", "낙뢰부", "Weapon_NakroeBu", "Icon_NakroeBu", ApplyNakroeDefaults);

        // null 제거
        for (int i = configs.Count - 1; i >= 0; i--)
            if (configs[i] == null) configs.RemoveAt(i);

        // 2) CardPool 생성/갱신
        var pool = UpsertCardPool("CommonSkillCardPool");
        pool.cards.Clear();

        for (int i = 0; i < configs.Count; i++)
        {
            var cfg = configs[i];
            if (cfg == null) continue;

            var card = UpsertCard(cfg);
            pool.cards.Add(card);
        }
        EditorUtility.SetDirty(pool);

        // 3) Catalog 생성/갱신
        var catalog = UpsertCatalog("CommonSkillCatalog");
        catalog.skills.Clear();
        for (int i = 0; i < configs.Count; i++)
            catalog.skills.Add(configs[i]);

        catalog.cardPool = pool;
        EditorUtility.SetDirty(catalog);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CommonSkillAutoBuilderEditor2] Done. skills={configs.Count}");
    }

    // -----------------------------
    // Upsert helpers
    // -----------------------------

    private static CommonSkillConfigSO UpsertConfig(
        CommonSkillKind kind,
        string displayName,
        string weaponPrefabToken,
        string iconToken,
        Action<CommonSkillConfigSO> applyDefaults)
    {
        string path = $"{RootFolder}/CSkill_{kind}.asset";
        var cfg = AssetDatabase.LoadAssetAtPath<CommonSkillConfigSO>(path);

        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<CommonSkillConfigSO>();
            AssetDatabase.CreateAsset(cfg, path);
        }

        cfg.kind = kind;
        cfg.displayName = displayName;
        cfg.maxLevel = DefaultMaxLevel;

        cfg.weaponPrefab = FindPrefabByToken(weaponPrefabToken);
        cfg.icon = FindSpriteByToken(iconToken);

        applyDefaults?.Invoke(cfg);

        EditorUtility.SetDirty(cfg);
        return cfg;
    }

    private static void TryAddCharacterBasic(
        List<CommonSkillConfigSO> list,
        string enumName,
        string displayName,
        string weaponPrefabToken,
        string iconToken,
        Action<CommonSkillConfigSO> applyDefaults)
    {
        if (!Enum.TryParse(enumName, out CommonSkillKind kind))
        {
            Debug.LogWarning($"[CommonSkillAutoBuilderEditor2] CommonSkillKind에 '{enumName}' 없음. 기본 스킬 '{displayName}' 생성 스킵.");
            return;
        }

        var cfg = UpsertConfig(kind, displayName, weaponPrefabToken, iconToken, applyDefaults);
        list.Add(cfg);
    }

    private static CommonSkillCardSO UpsertCard(CommonSkillConfigSO cfg)
    {
        string path = $"{RootFolder}/CCard_{cfg.kind}.asset";
        var card = AssetDatabase.LoadAssetAtPath<CommonSkillCardSO>(path);

        if (card == null)
        {
            card = ScriptableObject.CreateInstance<CommonSkillCardSO>();
            AssetDatabase.CreateAsset(card, path);
        }

        card.skill = cfg;
        card.weight = 10; // 기본값. 메인 기본 스킬 +20%는 런타임에서 가중치 보정하는 게 정석.
        EditorUtility.SetDirty(card);
        return card;
    }

    private static CommonSkillCardPoolSO UpsertCardPool(string name)
    {
        string path = $"{RootFolder}/{name}.asset";
        var pool = AssetDatabase.LoadAssetAtPath<CommonSkillCardPoolSO>(path);

        if (pool == null)
        {
            pool = ScriptableObject.CreateInstance<CommonSkillCardPoolSO>();
            AssetDatabase.CreateAsset(pool, path);
        }
        return pool;
    }

    private static CommonSkillCatalogSO UpsertCatalog(string name)
    {
        string path = $"{RootFolder}/{name}.asset";
        var cat = AssetDatabase.LoadAssetAtPath<CommonSkillCatalogSO>(path);

        if (cat == null)
        {
            cat = ScriptableObject.CreateInstance<CommonSkillCatalogSO>();
            AssetDatabase.CreateAsset(cat, path);
        }
        return cat;
    }

    private static GameObject FindPrefabByToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        string[] guids = AssetDatabase.FindAssets($"{token} t:Prefab");
        if (guids == null || guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static Sprite FindSpriteByToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        string[] guids = AssetDatabase.FindAssets($"{token} t:Sprite");
        if (guids == null || guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string[] parts = folder.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{cur}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    // -----------------------------
    // Defaults (8 levels)
    // - 프로토타입 기준: 체감 위주, 과밸런스 신경 X
    // -----------------------------

    private static void EnsureLevels(CommonSkillConfigSO cfg)
    {
        if (cfg.levels == null || cfg.levels.Length != cfg.maxLevel)
            cfg.levels = new SkillEffectConfig[cfg.maxLevel];
    }

    private static void ApplyOrbitingBladeDefaults(CommonSkillConfigSO cfg)
    {
        EnsureLevels(cfg);
        for (int lv = 1; lv <= cfg.maxLevel; lv++)
        {
            SkillEffectConfig p = new SkillEffectConfig();
            p.cooldown = 0f;
            p.damage = 4 + lv * 2;
            p.projectileCount = Mathf.Clamp(lv, 1, 8);
            p.hitInterval = 0.25f;
            p.orbitRadius = 1.3f;
            p.orbitAngularSpeed = 260f;
            cfg.levels[lv - 1] = p;
        }
    }

    private static void ApplyBoomerangDefaults(CommonSkillConfigSO cfg)
    {
        EnsureLevels(cfg);
        for (int lv = 1; lv <= cfg.maxLevel; lv++)
        {
            SkillEffectConfig p = new SkillEffectConfig();
            p.cooldown = Mathf.Max(0.35f, 1.10f - 0.06f * (lv - 1));
            p.damage = 10 + lv * 3;
            p.projectileCount = 1 + Mathf.Max(0, lv / 2); // 1,1,2,2,3,3...
            p.projectileSpeed = 9.5f;
            p.returnSpeed = 11.0f;
            p.maxDistance = 5.2f;
            p.lifeSeconds = 1.8f;
            p.spreadAngleDeg = 10f;
            cfg.levels[lv - 1] = p;
        }
    }

    private static void ApplyPiercingBulletDefaults(CommonSkillConfigSO cfg)
    {
        EnsureLevels(cfg);
        for (int lv = 1; lv <= cfg.maxLevel; lv++)
        {
            SkillEffectConfig p = new SkillEffectConfig();
            p.cooldown = 0.45f;
            p.damage = 8 + (lv - 1) * 7;
            p.projectileCount = 1;
            p.projectileSpeed = 14f;
            p.lifeSeconds = 1.2f;
            p.spreadAngleDeg = 0f;
            cfg.levels[lv - 1] = p;
        }
    }

    private static void ApplyHomingMissileDefaults(CommonSkillConfigSO cfg)
    {
        EnsureLevels(cfg);
        for (int lv = 1; lv <= cfg.maxLevel; lv++)
        {
            SkillEffectConfig p = new SkillEffectConfig();
            p.cooldown = 1.35f;
            p.damage = 14 + lv * 5;
            p.projectileCount = 1;
            p.projectileSpeed = 7.5f;
            p.turnSpeedDeg = 360f;
            p.chainCount = Mathf.Max(0, lv - 1);
            p.lifeSeconds = 3.5f;
            cfg.levels[lv - 1] = p;
        }
    }

    private static void ApplyDarkOrbDefaults(CommonSkillConfigSO cfg)
    {
        EnsureLevels(cfg);
        for (int lv = 1; lv <= cfg.maxLevel; lv++)
        {
            SkillEffectConfig p = new SkillEffectConfig();
            p.cooldown = 2.1f;
            p.damage = 18 + lv * 6;
            p.projectileCount = 1;
            p.projectileSpeed = 6.5f;
            p.explosionRadius = 1.2f;
            p.childSpeed = 8.5f;
            p.splitCount = 2 + (lv - 1) * 2;
            p.lifeSeconds = 2.2f;
            cfg.levels[lv - 1] = p;
        }
    }

    private static void ApplyShurikenDefaults(CommonSkillConfigSO cfg)
    {
        EnsureLevels(cfg);
        for (int lv = 1; lv <= cfg.maxLevel; lv++)
        {
            SkillEffectConfig p = new SkillEffectConfig();
            p.cooldown = 0.90f;
            p.damage = 9 + lv * 4;
            p.projectileCount = 1;
            p.projectileSpeed = 10.5f;
            p.bounceCount = 2 + (lv - 1) * 2;
            p.lifeSeconds = 4.0f;
            cfg.levels[lv - 1] = p;
        }
    }

    private static void ApplyArrowShotDefaults(CommonSkillConfigSO cfg)
    {
        EnsureLevels(cfg);
        for (int lv = 1; lv <= cfg.maxLevel; lv++)
        {
            SkillEffectConfig p = new SkillEffectConfig();
            p.cooldown = 0.55f;
            p.damage = 7 + lv * 3;
            p.projectileCount = (lv == 1) ? 1 : (1 + (lv - 1) * 2);
            p.projectileSpeed = 12f;
            p.lifeSeconds = 1.2f;
            p.spreadAngleDeg = 8f;
            cfg.levels[lv - 1] = p;
        }
    }

    // 기본 스킬 3종(임시 기본값)
    private static void ApplyBalsiDefaults(CommonSkillConfigSO cfg)
    {
        EnsureLevels(cfg);
        for (int lv = 1; lv <= cfg.maxLevel; lv++)
        {
            SkillEffectConfig p = new SkillEffectConfig();
            p.cooldown = Mathf.Max(0.45f, 0.90f - 0.05f * (lv - 1));
            p.damage = 10 + (lv - 1) * 3;
            p.projectileCount = 1;
            p.projectileSpeed = 12.5f;
            p.lifeSeconds = 1.2f;
            p.spreadAngleDeg = 0f;
            cfg.levels[lv - 1] = p;
        }
    }

    private static void ApplyJwagyeokDefaults(CommonSkillConfigSO cfg)
    {
        EnsureLevels(cfg);
        for (int lv = 1; lv <= cfg.maxLevel; lv++)
        {
            SkillEffectConfig p = new SkillEffectConfig();
            p.cooldown = Mathf.Max(0.35f, 0.75f - 0.04f * (lv - 1));
            p.damage = 12 + (lv - 1) * 4;
            p.projectileCount = 1 + Mathf.Max(0, lv - 1); // 베는 횟수 용도로 사용(무기에서 해석)
            p.projectileSpeed = 0f;
            p.lifeSeconds = 0.25f;
            cfg.levels[lv - 1] = p;
        }
    }

    private static void ApplyNakroeDefaults(CommonSkillConfigSO cfg)
    {
        EnsureLevels(cfg);
        for (int lv = 1; lv <= cfg.maxLevel; lv++)
        {
            SkillEffectConfig p = new SkillEffectConfig();
            p.cooldown = Mathf.Max(0.55f, 1.10f - 0.06f * (lv - 1));
            p.damage = 14 + (lv - 1) * 5;
            p.projectileCount = 1;
            p.projectileSpeed = 9.0f;
            p.lifeSeconds = 2.0f;
            p.explosionRadius = 1.1f;
            cfg.levels[lv - 1] = p;
        }
    }

    private static void ApplyArrowRainDefaults(CommonSkillConfigSO cfg)
    {
        EnsureLevels(cfg);
        for (int lv = 1; lv <= cfg.maxLevel; lv++)
        {
            SkillEffectConfig p = new SkillEffectConfig();
            p.cooldown = Mathf.Max(2.5f, 5.0f - 0.25f * (lv - 1));
            p.damage = 2 + (lv - 1);
            p.areaRadius = 2.0f + 0.1f * (lv - 1);
            p.lifeSeconds = 4.0f;
            p.areaDamageTickInterval = 0.25f;
            cfg.levels[lv - 1] = p;
        }
    }
}
#endif