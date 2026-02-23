#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

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

        CreateOrUpdate(catalog, PassiveKind.AttackDamage, "공격력",  p => { p.addPercent = 0.06f; p.addInt = 0; });
        CreateOrUpdate(catalog, PassiveKind.Defense,      "방어력",  p => { p.addPercent = 0.04f; p.addInt = 0; });
        CreateOrUpdate(catalog, PassiveKind.CooldownReduction, "쿨타임 감소", p => { p.addPercent = 0.05f; p.addInt = 0; });
        CreateOrUpdate(catalog, PassiveKind.MoveSpeed,    "이동속도", p => { p.addPercent = 0.05f; p.addInt = 0; });
        CreateOrUpdate(catalog, PassiveKind.PickupRange,  "픽업 범위", p => { p.addPercent = 0.10f; p.addInt = 0; });
        CreateOrUpdate(catalog, PassiveKind.MaxHp,        "최대 체력", p => { p.addPercent = 0f; p.addInt = 10; });
        CreateOrUpdate(catalog, PassiveKind.ElementDamage,"속성 피해", p => { p.addPercent = 0.06f; p.addInt = 0; });
        CreateOrUpdate(catalog, PassiveKind.SkillArea,    "스킬 범위", p => { p.addPercent = 0.06f; p.addInt = 0; });

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PassiveAutoBuilder] 완료. PassiveCatalog_Prototype 갱신.");
    }

    private static void CreateOrUpdate(PassiveCatalogSO catalog, PassiveKind kind, string name, System.Action<PassiveLevelParams> perLevel)
    {
        string path = $"{Folder}/Passive_{kind}.asset";
        var so = AssetDatabase.LoadAssetAtPath<PassiveConfigSO>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<PassiveConfigSO>();
            AssetDatabase.CreateAsset(so, path);
        }

        so.kind = kind;
        so.displayName = name;
        so.maxLevel = 8;

        if (so.levels == null || so.levels.Length != 8)
            so.levels = new PassiveLevelParams[8];

        for (int i = 0; i < 8; i++)
        {
            PassiveLevelParams p = default;
            perLevel(p); // 기본값(퍼레벨 동일 증가량) 넣기
            so.levels[i] = p;
        }

        catalog.passives.Add(so);
        EditorUtility.SetDirty(so);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        var parent = Path.GetDirectoryName(folder);
        var name = Path.GetFileName(folder);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif