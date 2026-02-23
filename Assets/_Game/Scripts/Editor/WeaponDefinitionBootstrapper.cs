#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WeaponDefinitionBootstrapper
{
    private const string DefOutFolder = "Assets/_Game/Scripts/So/SO_Weapon/WeaponDefinitions";
    private static readonly string[] PrefabSearchFolders = { "Assets/_Game" };

    private static readonly (string assetName, string weaponId, string displayNameKr, string tagKr, string[] prefabNameCandidates,
        int baseDamage, float baseInterval, float baseRange)[] Specs =
    {
        ("WeaponDef_Boomerang",  "weapon_boomerang",   "부메랑",  "공통", new[] { "BoomerangProjectile", "Boomerang" },                    10, 1.2f, 12f),
        ("WeaponDef_Musket",     "weapon_musket",      "총알",    "공통", new[] { "Bullet", "PiercingBullet", "MusketBullet" },             12, 0.6f, 18f),
        ("WeaponDef_Homing",     "weapon_homing",      "호밍",    "공통", new[] { "HomingOrb", "HomingMissile", "Homing" },                9,  1.0f, 16f),
        ("WeaponDef_DarkOrb",    "weapon_darkorb",     "다크오브","공통", new[] { "DarkOrb" },                                               14, 1.6f, 10f),
        ("WeaponDef_Shuriken",   "weapon_shuriken",    "수리검",  "공통", new[] { "Shuriken" },                                              8,  0.8f, 14f),
        ("WeaponDef_Arrow",      "weapon_arrow",       "화살",    "공통", new[] { "ArrowProjectile", "Arrow" },                             9,  0.7f, 18f),
        ("WeaponDef_RotateSword","weapon_rotatesword", "회전검",  "공통", new[] { "OrbitSword", "RotateSword", "OrbitingBlade", "Sword" },  7,  0.25f, 3f),
    };

    [MenuItem("Tools/그날이후/레벨업/WeaponDefinitionSO 생성+Deck/DB 채우기(프로토타입)")]
    public static void CreateDefsAndSeed()
    {
        EnsureFolder(DefOutFolder);

        var createdDefs = new List<WeaponDefinitionSO>(Specs.Length);
        var missing = new List<string>(8);

        for (int i = 0; i < Specs.Length; i++)
        {
            var s = Specs[i];

            string assetPath = $"{DefOutFolder}/{s.assetName}.asset";
            var def = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(assetPath);
            bool isNewAsset = false;

            if (def == null)
            {
                def = ScriptableObject.CreateInstance<WeaponDefinitionSO>();
                AssetDatabase.CreateAsset(def, assetPath);
                isNewAsset = true;
            }

            var projectile = LoadPrefabSmart(s.prefabNameCandidates);
            if (projectile == null)
            {
                missing.Add($"{s.assetName}  후보=[{string.Join(", ", s.prefabNameCandidates)}]");
                continue;
            }

            Undo.RecordObject(def, "Bootstrap WeaponDefinitionSO");

            def.weaponId = s.weaponId;
            def.displayNameKr = s.displayNameKr;
            def.tagsKr = s.tagKr;
            def.projectilePrefab = projectile;

            def.baseDamage = s.baseDamage;
            def.baseFireInterval = s.baseInterval;
            def.baseRange = s.baseRange;

            // 중요: includeInPrototype=true는 "시작 무기 3개만" 수동으로 켜야 함.
            // 여기서 전부 true로 박아버리면 무기 풀(3개)이 깨지고, 공통스킬(8개)과 중복 카드가 뜸.
            if (isNewAsset) def.includeInPrototype = false;

            // weight는 0이면 후보에서 걸러질 수 있으니 안전 기본값만 보장
            if (def.weight <= 0) def.weight = 100;

            EditorUtility.SetDirty(def);
            createdDefs.Add(def);
        }

        AssetDatabase.SaveAssets();

        if (createdDefs.Count == 0)
        {
            Debug.LogError("[WeaponDef] 생성 실패: 어떤 프리팹도 찾지 못했습니다. 프리팹 이름 후보를 실제 이름에 맞게 수정하세요.");
            DumpMissing(missing);
            return;
        }

        var sys = UnityEngine.Object.FindFirstObjectByType<PlayerSkillUpgradeSystem>();
        if (sys == null)
        {
            Debug.LogWarning("[WeaponDef] 씬에서 PlayerSkillUpgradeSystem을 못 찾음. Scene_Game 열고 다시 실행.");
            DumpMissing(missing);
            return;
        }

        var soSys = new SerializedObject((UnityEngine.Object)sys);
        var deckProp = soSys.FindProperty("deck");
        var dbProp = soSys.FindProperty("weaponDatabase");

        if (deckProp == null || dbProp == null || deckProp.objectReferenceValue == null || dbProp.objectReferenceValue == null)
        {
            Debug.LogError("[WeaponDef] PlayerSkillUpgradeSystem의 deck/weaponDatabase 참조가 비었음. 인스펙터 연결 후 다시 실행.");
            DumpMissing(missing);
            return;
        }

        var deck = deckProp.objectReferenceValue as WeaponSkillDeckSO;
        var db = dbProp.objectReferenceValue as WeaponDatabaseSO;

        if (deck == null || db == null)
        {
            Debug.LogError("[WeaponDef] deck/db 타입 캐스팅 실패. (참조 슬롯에 다른 SO가 들어갔거나 타입이 다름)");
            DumpMissing(missing);
            return;
        }

        // DB는 전체 defs
        FillWeaponList(db, "weapons", createdDefs);
        // Deck은 includeInPrototype=true인 것만 (무기 3개 규칙 보장)
        FillWeaponList(deck, "weapons", createdDefs.FindAll(d => d != null && d.includeInPrototype));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WeaponDef] 완료. WeaponDefinitionSO {createdDefs.Count}개 확보 + Deck/DB 채움.");
        DumpMissing(missing);
    }

    private static GameObject LoadPrefabSmart(string[] nameCandidates)
    {
        if (nameCandidates == null || nameCandidates.Length == 0) return null;

        for (int i = 0; i < nameCandidates.Length; i++)
        {
            string n = nameCandidates[i];
            if (string.IsNullOrWhiteSpace(n)) continue;

            string[] guids = AssetDatabase.FindAssets($"t:Prefab {n}", PrefabSearchFolders);
            GameObject best = null;

            for (int g = 0; g < guids.Length; g++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;

                if (string.Equals(go.name, n, StringComparison.OrdinalIgnoreCase))
                    return go;

                if (best == null) best = go;
            }

            if (best != null) return best;
        }

        return null;
    }

    private static void FillWeaponList(ScriptableObject target, string listFieldName, List<WeaponDefinitionSO> defs)
    {
        var so = new SerializedObject(target);
        var list = so.FindProperty(listFieldName);
        if (list == null || !list.isArray)
        {
            Debug.LogError($"[WeaponDef] {target.name}에 배열 필드 '{listFieldName}'를 못 찾음");
            return;
        }

        list.arraySize = defs.Count;
        for (int i = 0; i < defs.Count; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            e.objectReferenceValue = defs[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void DumpMissing(List<string> missing)
    {
        if (missing == null || missing.Count == 0) return;

        Debug.LogWarning("[WeaponDef] 일부 프리팹을 못 찾아서 해당 무기는 스킵되었습니다.\n- " + string.Join("\n- ", missing));
        Debug.LogWarning("[WeaponDef] 해결법: Project 검색창에서 프리팹 실제 이름을 확인하고 Specs의 prefabNameCandidates에 그 이름을 추가하세요.");
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string name = Path.GetFileName(folderPath);

        if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        if (!string.IsNullOrWhiteSpace(parent))
            AssetDatabase.CreateFolder(parent, name);
    }
}
#endif