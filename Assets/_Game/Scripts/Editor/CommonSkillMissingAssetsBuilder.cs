#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CommonSkillMissingAssetsBuilder
{
    // 생성될 Config SO 저장 위치(원하는 곳으로 바꿔도 됨)
    private const string ConfigOutFolder = "Assets/_Game/Scripts/So/SO_Weapon/CommonSkills";

    // Weapon 프리팹 찾을 때 후보 폴더(너 프로젝트 기준)
    private static readonly string[] WeaponPrefabSearchFolders =
    {
        "Assets/_Game/Scripts/So/SO_Weapon/WP_Shooter",
        "Assets/_Game/Scripts/So/SO_Weapon",
        "Assets/_Game",
        "Assets",
    };

    [MenuItem("Tools/그날이후/공통스킬/누락 3종 생성(발시Config + 다크오브풀 + 회전검템플릿)")]
    public static void BuildMissingThree()
    {
        EnsureFolder(ConfigOutFolder);

        int ok = 0, fail = 0;

        if (Build_BalsiConfig()) ok++; else fail++;
        if (Build_DarkOrbPools()) ok++; else fail++;
        if (Build_RotateSwordBladeTemplate()) ok++; else fail++;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[누락 생성] 완료. 성공={ok}, 실패={fail}");
    }

    // ------------------------------------------------------------
    // 1) 발시 Config SO 생성 + Weapon_Balsi.config 연결
    // ------------------------------------------------------------
    private static bool Build_BalsiConfig()
    {
        var weaponGo = FindPrefab("Weapon_Balsi");
        if (weaponGo == null)
        {
            Debug.LogError("[발시] Weapon_Balsi 프리팹을 못 찾음");
            return false;
        }

        // BalsiWeapon2D 찾기(이름 고정 가정)
        var weaponComp = weaponGo.GetComponent("BalsiWeapon2D");
        if (weaponComp == null)
        {
            Debug.LogError("[발시] Weapon_Balsi에 BalsiWeapon2D 컴포넌트가 없음");
            return false;
        }

        // Config 에셋 생성(없으면 생성, 있으면 재사용)
        string assetPath = $"{ConfigOutFolder}/CSkill_Balsi.asset";
        var cfg = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) as CommonSkillConfigSO;
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<CommonSkillConfigSO>();
            cfg.kind = CommonSkillKind.Balsi;
            cfg.displayName = "발시";
            cfg.maxLevel = 10;
            cfg.levels = BuildDefaultLevels(10);
            cfg.weaponPrefab = weaponGo;

            AssetDatabase.CreateAsset(cfg, assetPath);
            EditorUtility.SetDirty(cfg);

            Debug.Log($"[발시] Config 생성: {assetPath}");
        }
        else
        {
            // weaponPrefab만 안전하게 보정
            if (cfg.weaponPrefab != weaponGo)
            {
                Undo.RecordObject(cfg, "Set Balsi weaponPrefab");
                cfg.weaponPrefab = weaponGo;
                EditorUtility.SetDirty(cfg);
            }

            Debug.Log($"[발시] Config 이미 존재: {assetPath}");
        }

        // Weapon_Balsi의 Config 슬롯에 연결
        AssignSerializedObjectRefIfExists(weaponComp, "config", cfg);

        // 프리팹에 변경 반영
        SavePrefab(weaponGo);

        Debug.Log("[발시] Weapon_Balsi.config 연결 완료");
        return true;
    }

    // ------------------------------------------------------------
    // 2) 다크오브 OrbPool / SplitPool 생성 + 슬롯 연결
    // ------------------------------------------------------------
    private static bool Build_DarkOrbPools()
    {
        var weaponGo = FindPrefab("Weapon_DarkOrb");
        if (weaponGo == null)
        {
            Debug.LogError("[다크오브] Weapon_DarkOrb 프리팹을 못 찾음");
            return false;
        }

        var weaponComp = weaponGo.GetComponent("DarkOrbWeapon2D");
        if (weaponComp == null)
        {
            Debug.LogError("[다크오브] Weapon_DarkOrb에 DarkOrbWeapon2D 컴포넌트가 없음");
            return false;
        }

        // OrbPool / SplitPool 자식 생성(이미 있으면 재사용)
        var orbPoolTr = FindOrCreateChild(weaponGo.transform, "OrbPool");
        var splitPoolTr = FindOrCreateChild(weaponGo.transform, "SplitPool");

        var orbPool = EnsureComponent(orbPoolTr.gameObject, "ProjectilePool2D");
        var splitPool = EnsureComponent(splitPoolTr.gameObject, "ProjectilePool2D");

        if (orbPool == null || splitPool == null)
        {
            Debug.LogError("[다크오브] ProjectilePool2D 컴포넌트를 붙이지 못함(스크립트 이름 확인 필요)");
            return false;
        }

        // 투사체 프리팹 자동 탐색 (타입명이 확실치 않아서 '이름/타입명' 기반으로 최대한 안전하게 찾음)
        // - orb: DarkOrbProjectile2D가 붙은 프리팹
        // - split: DarkOrbSplitProjectile2D가 붙은 프리팹 (없으면 orb와 동일하게 넣고 나중에 교체)
        var orbProjectilePrefab = FindPrefabContainingComponentTypeName("DarkOrbProjectile2D");
        var splitProjectilePrefab = FindPrefabContainingComponentTypeName("DarkOrbSplitProjectile2D") ?? orbProjectilePrefab;

        if (orbProjectilePrefab == null)
        {
            Debug.LogError("[다크오브] DarkOrbProjectile2D가 붙은 투사체 프리팹을 못 찾음 (투사체 프리팹은 있다고 했으니, 컴포넌트 이름이 다른지 확인 필요)");
            return false;
        }

        // ProjectilePool2D.prefab 슬롯에 PooledObject2D 파생을 넣어야 함
        // => 프리팹 루트에서 PooledObject2D(또는 파생)를 찾아서 연결 시도
        var orbPooled = orbProjectilePrefab.GetComponent("PooledObject2D");
        if (orbPooled == null)
        {
            Debug.LogError("[다크오브] orb 투사체 프리팹에 PooledObject2D가 없음");
            return false;
        }

        var splitPooled = splitProjectilePrefab != null ? splitProjectilePrefab.GetComponent("PooledObject2D") : null;
        if (splitPooled == null) splitPooled = orbPooled;

        // 풀 내부의 prefab 필드명은 보통 "prefab"
        AssignSerializedObjectRefIfExists(orbPool, "prefab", orbPooled as UnityEngine.Object);
        AssignSerializedObjectRefIfExists(splitPool, "prefab", splitPooled as UnityEngine.Object);

        // DarkOrbWeapon2D 슬롯 연결: orbPool / splitPool (필드명은 스샷 기준)
        AssignSerializedObjectRefIfExists(weaponComp, "orbPool", orbPool as UnityEngine.Object);
        AssignSerializedObjectRefIfExists(weaponComp, "splitPool", splitPool as UnityEngine.Object);

        // SpawnPoint 없으면 생성(스샷에 이미 있음)
        var sp = FindOrCreateChild(weaponGo.transform, "SpawnPoint");
        AssignSerializedObjectRefIfExists(weaponComp, "spawnPoint", sp);

        SavePrefab(weaponGo);

        Debug.Log("[다크오브] OrbPool/SplitPool 생성 및 연결 완료 (SplitProjectile이 따로 없으면 orb로 대체됨)");
        return true;
    }

    // ------------------------------------------------------------
    // 3) 회전검 Blade Template 생성 + 슬롯 연결
    // ------------------------------------------------------------
    private static bool Build_RotateSwordBladeTemplate()
    {
        var weaponGo = FindPrefab("Weapon_RotateSword");
        if (weaponGo == null)
        {
            Debug.LogError("[회전검] Weapon_RotateSword 프리팹을 못 찾음");
            return false;
        }

        var weaponComp = weaponGo.GetComponent("OrbitingBladeWeapon2D");
        if (weaponComp == null)
        {
            Debug.LogError("[회전검] Weapon_RotateSword에 OrbitingBladeWeapon2D 컴포넌트가 없음");
            return false;
        }

        var templateTr = FindOrCreateChild(weaponGo.transform, "BladeTemplate");

        // 템플릿 구성: OrbitingBladeHitbox2D + 트리거 콜라이더
        var hitbox = EnsureComponent(templateTr.gameObject, "OrbitingBladeHitbox2D");
        if (hitbox == null)
        {
            Debug.LogError("[회전검] OrbitingBladeHitbox2D를 붙이지 못함(스크립트 이름 확인 필요)");
            return false;
        }

        var col = templateTr.GetComponent<CircleCollider2D>();
        if (col == null) col = templateTr.gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.25f;

        // 템플릿은 보통 비활성(원본이 떠있으면 중복 타격/연산 가능)
        templateTr.gameObject.SetActive(false);

        // OrbitingBladeWeapon2D.bladeTemplate 연결(스샷 필드명 기준)
        AssignSerializedObjectRefIfExists(weaponComp, "bladeTemplate", hitbox as UnityEngine.Object);

        SavePrefab(weaponGo);

        Debug.Log("[회전검] BladeTemplate 생성 및 연결 완료");
        return true;
    }

    // ------------------------------------------------------------
    // Utilities
    // ------------------------------------------------------------
    private static CommonSkillLevelParams[] BuildDefaultLevels(int maxLevel)
    {
        var arr = new CommonSkillLevelParams[Mathf.Max(1, maxLevel)];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = new CommonSkillLevelParams
            {
                cooldown = 1.5f,
                damage = 10,
                projectileCount = 1,

                projectileSpeed = 8f,
                lifeSeconds = 2.5f,
                maxDistance = 20f,

                spreadAngleDeg = 10f,
                bounceCount = 0,
                chainCount = 0,
                splitCount = 0,
                explosionRadius = 1.5f,
                childSpeed = 6f,

                hitInterval = 0.25f,
                orbitRadius = 1.5f,
                orbitAngularSpeed = 180f,

                returnSpeed = 10f,
                turnSpeedDeg = 360f,
            };
        }
        return arr;
    }

    private static GameObject FindPrefab(string prefabName)
    {
        string filter = $"t:prefab {prefabName}";
        var guids = AssetDatabase.FindAssets(filter, WeaponPrefabSearchFolders);
        if (guids != null && guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        guids = AssetDatabase.FindAssets(filter);
        if (guids != null && guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        return null;
    }

    private static GameObject FindPrefabContainingComponentTypeName(string typeName)
    {
        // 프리팹 전체 훑어서(한 번 메뉴 실행이니 OK) 특정 컴포넌트 타입명을 가진 프리팹 찾기
        var guids = AssetDatabase.FindAssets("t:prefab", WeaponPrefabSearchFolders);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            var c = go.GetComponent(typeName);
            if (c != null) return go;
        }

        // 폴더 밖에 있을 수도 있으니 전체에서도 한 번 더
        guids = AssetDatabase.FindAssets("t:prefab");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            var c = go.GetComponent(typeName);
            if (c != null) return go;
        }

        return null;
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return t;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    private static UnityEngine.Object EnsureComponent(GameObject go, string componentTypeName)
    {
        var c = go.GetComponent(componentTypeName);
        if (c != null) return c;

        var type = FindTypeByName(componentTypeName);
        if (type == null) return null;

        return go.AddComponent(type);
    }

    private static Type FindTypeByName(string typeName)
    {
        // Unity에서 컴포넌트 타입을 문자열로 찾기(어셈블리 구분 없이)
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return t;
        }

        // 네임스페이스가 있을 수 있으니 EndsWith도 한 번 더
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in asm.GetTypes())
            {
                if (t.Name == typeName) return t;
            }
        }

        return null;
    }

    private static void AssignSerializedObjectRefIfExists(object target, string fieldName, UnityEngine.Object value)
    {
        if (target is not UnityEngine.Object uo) return;

        var so = new SerializedObject(uo);
        var sp = so.FindProperty(fieldName);
        if (sp == null) return;
        if (sp.propertyType != SerializedPropertyType.ObjectReference) return;

        sp.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(uo);
    }

    private static void SavePrefab(GameObject prefabAssetRoot)
    {
        // prefabAssetRoot는 프리팹 에셋(GameObject)이라 저장 가능
        var path = AssetDatabase.GetAssetPath(prefabAssetRoot);
        if (string.IsNullOrEmpty(path)) return;

        PrefabUtility.SavePrefabAsset(prefabAssetRoot);
        EditorUtility.SetDirty(prefabAssetRoot);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string name = Path.GetFileName(folderPath);

        if (string.IsNullOrWhiteSpace(parent)) return;

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif