#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 패시브 8종 SO 자동 생성 도구.
///
/// [Issue 5 수정] struct 값 복사 버그 해결.
/// 원인 : Action&lt;PassiveLevelParams&gt; 람다에서 struct를 값 전달 → 내부 수정이 원본 p에 미반영.
///        결과로 so.levels[i]에 default(0)가 저장되어 패시브 수치가 0%로 표시됨.
/// 수정 : Func&lt;PassiveLevelParams&gt; 팩토리 패턴으로 변경.
///        매번 완성된 struct를 반환값으로 받으므로 복사 손실 없음.
/// </summary>
public static class PassiveAutoBuilder
{
    private const string Folder = "Assets/_Game/Data/Defs/Passives";

    [MenuItem("Tools/그날이후/밸런스/패시브 8종 SO 생성+레벨수치 자동세팅")]
    public static void Build()
    {
        EnsureFolder(Folder);

        var catalogPath = $"{Folder}/PassiveCatalog_Prototype.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<PassiveCatalogSO>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<PassiveCatalogSO>();
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }

        catalog.passives.Clear();

        // [수정] Func<PassiveLevelParams> 팩토리 패턴: 완성된 struct를 반환값으로 받음
        CreateOrUpdate(catalog, PassiveKind.AttackDamage,      "공격력",      () => new PassiveLevelParams { addPercent = 0.06f, addInt = 0  });
        CreateOrUpdate(catalog, PassiveKind.Defense,            "방어력",      () => new PassiveLevelParams { addPercent = 0.04f, addInt = 0  });
        CreateOrUpdate(catalog, PassiveKind.CooldownReduction,  "쿨타임 감소", () => new PassiveLevelParams { addPercent = 0.05f, addInt = 0  });
        CreateOrUpdate(catalog, PassiveKind.MoveSpeed,          "이동속도",    () => new PassiveLevelParams { addPercent = 0.05f, addInt = 0  });
        CreateOrUpdate(catalog, PassiveKind.PickupRange,        "픽업 범위",   () => new PassiveLevelParams { addPercent = 0.10f, addInt = 0  });
        CreateOrUpdate(catalog, PassiveKind.MaxHp,              "최대 체력",   () => new PassiveLevelParams { addPercent = 0f,    addInt = 10 });
        CreateOrUpdate(catalog, PassiveKind.ElementDamage,      "속성 피해",   () => new PassiveLevelParams { addPercent = 0.06f, addInt = 0  });
        CreateOrUpdate(catalog, PassiveKind.SkillArea,          "스킬 범위",   () => new PassiveLevelParams { addPercent = 0.06f, addInt = 0  });

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PassiveAutoBuilder] 완료. PassiveCatalog_Prototype 갱신. 경로: " + Folder);
    }

    /// <summary>
    /// 패시브 SO를 생성하거나 기존 것을 갱신한다.
    /// factory: 레벨 1개의 수치를 담은 PassiveLevelParams를 반환하는 팩토리.
    ///          모든 레벨에 동일한 증가량을 적용(레벨별 다른 수치가 필요하면 SO 에디터에서 직접 수정).
    /// </summary>
    private static void CreateOrUpdate(
        PassiveCatalogSO catalog,
        PassiveKind kind,
        string displayName,
        System.Func<PassiveLevelParams> factory)
    {
        string path = $"{Folder}/Passive_{kind}.asset";
        var so = AssetDatabase.LoadAssetAtPath<PassiveConfigSO>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<PassiveConfigSO>();
            AssetDatabase.CreateAsset(so, path);
        }

        so.kind = kind;
        so.displayName = displayName;
        so.maxLevel = 8;

        if (so.levels == null || so.levels.Length != 8)
            so.levels = new PassiveLevelParams[8];

        // [수정] factory()가 완성된 struct를 반환 → 값 복사 손실 없음
        for (int i = 0; i < 8; i++)
            so.levels[i] = factory();

        catalog.passives.Add(so);
        EditorUtility.SetDirty(so);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        var parent = Path.GetDirectoryName(folder);
        var leafName = Path.GetFileName(folder);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leafName);
    }
}
#endif