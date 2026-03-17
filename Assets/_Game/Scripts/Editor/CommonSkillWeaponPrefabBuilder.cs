#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CommonSkillWeaponPrefabBuilder
{
    // 생성될 Weapon_ 프리팹 저장 위치(원하는 곳으로 바꿔도 됨)
    private const string WeaponPrefabOutFolder = "Assets/_Game/Prefabs/Weapons";

    // 투사체(풀에 넣을 프리팹) 검색 범위 후보 폴더
    // (네 프로젝트 폴더 구조에 맞게 필요하면 추가)
    private static readonly string[] ProjectileSearchFolders =
    {
        "Assets/_Game",
        "Assets",
    };

    // Weapon 프리팹 이름 + 붙일 무기 컴포넌트 + (있다면) 투사체 컴포넌트
    // - 풀 기반 무기: ProjectilePool2D를 함께 붙이고, pool.prefab에 해당 투사체 프리팹을 자동 연결
    // - 회전검: 블레이드 템플릿 자식을 자동 생성하고 bladeTemplate에 연결
    private static readonly WeaponBuildSpec[] Specs =
    {
        new WeaponBuildSpec(
            prefabName: "Weapon_Homing",
            weaponType: typeof(HomingMissileWeapon2D),
            projectileType: typeof(HomingMissileProjectile2D),
            extraSetup: null
        ),
        new WeaponBuildSpec(
            prefabName: "Weapon_Musket",
            weaponType: typeof(PiercingBulletWeapon2D),
            projectileType: typeof(PiercingBulletProjectile2D),
            extraSetup: null
        ),
        new WeaponBuildSpec(
            prefabName: "Weapon_Shuriken",
            weaponType: typeof(RicochetShurikenWeapon2D),
            projectileType: typeof(RicochetShurikenProjectile2D),
            extraSetup: null
        ),
        new WeaponBuildSpec(
            prefabName: "Weapon_DarkOrb",
            weaponType: typeof(DarkOrbWeapon2D),
            projectileType: typeof(DarkOrbProjectile2D),
            extraSetup: null
        ),
        new WeaponBuildSpec(
            prefabName: "Weapon_Boomerang",
            weaponType: typeof(BoomerangWeapon2D),
            projectileType: typeof(BoomerangProjectile2D),
            extraSetup: null
        ),
        new WeaponBuildSpec(
            prefabName: "Weapon_Arrow",
            weaponType: typeof(ArrowShotWeapon2D),
            projectileType: typeof(ArrowProjectile2D),
            extraSetup: null
        ),
        new WeaponBuildSpec(
            prefabName: "Weapon_Balsi",
            weaponType: typeof(BalsiWeapon2D),
            projectileType: typeof(_Game.Scripts.Object_Scripts.WeaponSystem.WeaponPrefab_Scripts.BalsiProjectile2D),
            extraSetup: null
        ),
        new WeaponBuildSpec(
            prefabName: "Weapon_RotateSword",
            weaponType: typeof(OrbitingBladeWeapon2D),
            projectileType: null,
            extraSetup: SetupOrbitingBladeTemplate
        ),
    };

    private sealed class WeaponBuildSpec
    {
        public readonly string prefabName;
        public readonly Type weaponType;
        public readonly Type projectileType; // PooledObject2D 파생이어야 함(풀 기반 무기)
        public readonly Action<GameObject> extraSetup;

        public WeaponBuildSpec(string prefabName, Type weaponType, Type projectileType, Action<GameObject> extraSetup)
        {
            this.prefabName = prefabName;
            this.weaponType = weaponType;
            this.projectileType = projectileType;
            this.extraSetup = extraSetup;
        }
    }

    [MenuItem("Tools/그날이후/공통스킬/발사대 프리팹 생성(Weapon_*)")]
    public static void BuildAllWeaponPrefabs()
    {
        EnsureFolder(WeaponPrefabOutFolder);

        int created = 0;
        int updated = 0;
        int failed = 0;

        foreach (var spec in Specs)
        {
            string outPath = $"{WeaponPrefabOutFolder}/{spec.prefabName}.prefab";

            bool exists = File.Exists(outPath);
            bool ok = BuildOne(spec, outPath, out string log);

            if (!ok)
            {
                failed++;
                Debug.LogError($"[발사대 생성 실패] {spec.prefabName}\n- {log}");
                continue;
            }

            if (exists) updated++;
            else created++;

            Debug.Log($"[발사대 생성 완료] {spec.prefabName}\n- {log}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[발사대 생성] 완료. 생성={created}, 갱신={updated}, 실패={failed}\n경로: {WeaponPrefabOutFolder}");
    }

    private static bool BuildOne(WeaponBuildSpec spec, string outPath, out string log)
    {
        log = "";

        if (spec.weaponType == null)
        {
            log = "weaponType null";
            return false;
        }

        // 루트 오브젝트 생성
        var root = new GameObject(spec.prefabName);

        try
        {
            // 무기 컴포넌트 추가(CommonSkillWeapon2D 파생)
            var weapon = root.AddComponent(spec.weaponType) as MonoBehaviour;
            if (weapon == null)
            {
                log = $"무기 컴포넌트 AddComponent 실패: {spec.weaponType.Name}";
                UnityEngine.Object.DestroyImmediate(root);
                return false;
            }

            // 대부분 무기: spawnPoint 필요(스크립트에 private SerializeField Transform spawnPoint)
            // => 자식 SpawnPoint 만들어서 자동 할당 시도
            var spawnPoint = new GameObject("SpawnPoint").transform;
            spawnPoint.SetParent(root.transform, false);
            spawnPoint.localPosition = Vector3.zero;

            AssignSerializedFieldIfExists(weapon, "spawnPoint", spawnPoint);

            // 풀 기반 무기면 ProjectilePool2D도 같이 붙이고 pool/prefab 연결
            if (spec.projectileType != null)
            {
                var pool = root.AddComponent<ProjectilePool2D>();

                // weapon.pool <- pool
                AssignSerializedFieldIfExists(weapon, "pool", pool);

                // pool.prefab <- (투사체 프리팹에서 PooledObject2D 컴포넌트)
                var projectilePrefabGo = FindPrefabHavingComponent(spec.projectileType);
                if (projectilePrefabGo == null)
                {
                    log = $"투사체 프리팹을 못 찾음: (컴포넌트) {spec.projectileType.Name}\n" +
                          $" - 해결: 해당 투사체가 붙은 프리팹을 프로젝트에 만들고 이름/위치 상관없이 저장하면 자동으로 잡힘";
                    UnityEngine.Object.DestroyImmediate(root);
                    return false;
                }

                var pooled = projectilePrefabGo.GetComponent(spec.projectileType) as PooledObject2D;
                if (pooled == null)
                {
                    log = $"찾은 프리팹에 PooledObject2D 파생이 아님: {AssetDatabase.GetAssetPath(projectilePrefabGo)}";
                    UnityEngine.Object.DestroyImmediate(root);
                    return false;
                }

                AssignSerializedFieldIfExists(pool, "prefab", pooled);

                // 기본 풀 파라미터는 ProjectilePool2D 기본값 사용(원하면 여기서 조절 가능)
            }

            // 무기별 추가 세팅(회전검 템플릿 등)
            spec.extraSetup?.Invoke(root);

            // 프리팹 저장/갱신
            var saved = PrefabUtility.SaveAsPrefabAsset(root, outPath);
            if (saved == null)
            {
                log = $"Prefab 저장 실패: {outPath}";
                UnityEngine.Object.DestroyImmediate(root);
                return false;
            }

            log = $"저장 경로: {outPath}";
            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void SetupOrbitingBladeTemplate(GameObject root)
    {
        // OrbitingBladeWeapon2D의 bladeTemplate은 GameObject 참조.
        // 타격 판정은 무기 스크립트가 Physics2D.OverlapCircle로 직접 처리하므로
        // 템플릿에는 비주얼(SpriteRenderer)만 있으면 됨.
        var weapon = root.GetComponent<OrbitingBladeWeapon2D>();
        if (weapon == null) return;

        var templateGo = new GameObject("BladeTemplate");
        templateGo.transform.SetParent(root.transform, false);
        templateGo.transform.localPosition = Vector3.right * 1.0f;

        // 비주얼용 SpriteRenderer (스프라이트는 Inspector에서 직접 할당)
        templateGo.AddComponent<SpriteRenderer>();

        // bladeTemplate <- GameObject 직접 연결
        AssignSerializedFieldIfExists(weapon, "bladeTemplate", templateGo);

        // 템플릿 원본은 비활성 (런타임에 복제본만 활성화됨)
        templateGo.SetActive(false);
    }

    private static GameObject FindPrefabHavingComponent(Type componentType)
    {
        // 1) 빠른 후보 검색: 컴포넌트 타입 이름으로 프리팹들만 추려서 검사
        //    (ex: "HomingMissileProjectile2D")
        string filter = $"t:prefab {componentType.Name}";
        var guids = AssetDatabase.FindAssets(filter, ProjectileSearchFolders);

        // 후보가 없으면 이름 없이 전체 프리팹에서(조금 느리지만 1회 메뉴 실행이니 OK)
        if (guids == null || guids.Length == 0)
            guids = AssetDatabase.FindAssets("t:prefab", ProjectileSearchFolders);

        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            if (go.GetComponent(componentType) != null)
                return go;
        }

        return null;
    }

    private static void AssignSerializedFieldIfExists(UnityEngine.Object obj, string fieldName, UnityEngine.Object value)
    {
        if (obj == null) return;

        var so = new SerializedObject(obj);
        var sp = so.FindProperty(fieldName);
        if (sp == null) return;

        if (sp.propertyType != SerializedPropertyType.ObjectReference) return;

        sp.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
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