#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 패시브 8종 PassiveConfigSO 자동 생성 도구 (하위 호환용).
/// ★ PassiveCatalog는 이제 SkillDefinitionSO를 받으므로 카탈로그 연결은 하지 않음.
///    카탈로그에는 기존 PS_Attack 등 SkillDefinitionSO를 Inspector에서 직접 드래그.
/// </summary>
public static class PassiveAutoBuilder
{
    private const string Folder = "Assets/_Game/Data/Defs/Passives";

    [MenuItem("Tools/그날이후/밸런스/패시브 8종 PassiveConfigSO 생성 (하위 호환용)")]
    public static void Build()
    {
        EnsureFolder(Folder);

        CreateOrUpdate(PassiveKind.AttackDamage,      "공격력 증가",        8, () => new PassiveLevelParams { addPercent = 0.10f, addInt = 0  });
        CreateOrUpdate(PassiveKind.Defense,            "방어력 증가",        8, () => new PassiveLevelParams { addPercent = 0.10f, addInt = 0  });
        CreateOrUpdate(PassiveKind.CooldownReduction,  "스킬 가속 증가",    8, () => new PassiveLevelParams { addPercent = 0.10f, addInt = 0  });
        CreateOrUpdate(PassiveKind.MoveSpeed,          "이동속도 증가",      8, () => new PassiveLevelParams { addPercent = 0.05f, addInt = 0  });
        CreateOrUpdate(PassiveKind.PickupRange,        "픽업 범위 증가",     8, () => new PassiveLevelParams { addPercent = 0.20f, addInt = 0  });
        CreateOrUpdate(PassiveKind.MaxHp,              "최대 체력 증가",     8, () => new PassiveLevelParams { addPercent = 0f,    addInt = 20 });
        CreateOrUpdate(PassiveKind.ExpGain,            "경험치 획득량 증가",  8, () => new PassiveLevelParams { addPercent = 0.10f, addInt = 0  });
        CreateOrUpdate(PassiveKind.SkillArea,          "스킬 범위 증가",     5, () => new PassiveLevelParams { addPercent = 0.05f, addInt = 0  });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PassiveAutoBuilder] 완료. PassiveConfigSO 8종 생성/갱신. 경로: {Folder}");
    }

    private static void CreateOrUpdate(
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