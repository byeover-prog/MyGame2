#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 패시브 8종 SO 자동 생성 도구.
/// 설계 문서 기준 수치 적용.
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

        // 설계 문서 기준 수치
        CreateOrUpdate(catalog, PassiveKind.AttackDamage,      "공격력 증가",       8, () => new PassiveLevelParams { addPercent = 0.10f, addInt = 0  });
        CreateOrUpdate(catalog, PassiveKind.Defense,            "방어력 증가",       8, () => new PassiveLevelParams { addPercent = 0.10f, addInt = 0  });
        CreateOrUpdate(catalog, PassiveKind.CooldownReduction,  "스킬 가속 증가",   8, () => new PassiveLevelParams { addPercent = 0.10f, addInt = 0  });
        CreateOrUpdate(catalog, PassiveKind.MoveSpeed,          "이동속도 증가",     8, () => new PassiveLevelParams { addPercent = 0.05f, addInt = 0  });
        CreateOrUpdate(catalog, PassiveKind.PickupRange,        "픽업 범위 증가",    8, () => new PassiveLevelParams { addPercent = 0.20f, addInt = 0  });
        CreateOrUpdate(catalog, PassiveKind.MaxHp,              "최대 체력 증가",    8, () => new PassiveLevelParams { addPercent = 0f,    addInt = 20 });
        CreateOrUpdate(catalog, PassiveKind.ExpGain,            "경험치 획득량 증가", 8, () => new PassiveLevelParams { addPercent = 0.10f, addInt = 0  });
        CreateOrUpdate(catalog, PassiveKind.SkillArea,          "스킬 범위 증가",    5, () => new PassiveLevelParams { addPercent = 0.05f, addInt = 0  });

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PassiveAutoBuilder] 완료. PassiveCatalog_Prototype 갱신. 경로: " + Folder);
    }

    private static void CreateOrUpdate(
        PassiveCatalogSO catalog,
        PassiveKind kind,
        string displayName,
        int maxLevel,
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
        so.maxLevel = maxLevel;

        if (so.levels == null || so.levels.Length != maxLevel)
            so.levels = new PassiveLevelParams[maxLevel];

        for (int i = 0; i < maxLevel; i++)
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