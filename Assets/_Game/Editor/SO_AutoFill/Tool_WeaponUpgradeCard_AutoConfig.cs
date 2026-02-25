// UTF-8
#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Tool_WeaponUpgradeCard_AutoConfig
{
    private const string DIR  = "Assets/_Game/Data/LevelUp/WeaponUpgradeAutoConfig";
    private const string PATH = "Assets/_Game/Data/LevelUp/WeaponUpgradeAutoConfig/WeaponUpgradeCardAutoConfig.asset";

    [MenuItem("Tool/그날이후/SO/무기 업그레이드/카드 정답지 생성(선택 카드 캡처)")]
    public static void CaptureSelected()
    {
        EnsureFolder(DIR);
        var config = LoadOrCreateConfig();

        var cards = Selection.objects
            .Select(o => AssetDatabase.GetAssetPath(o))
            .Where(p => p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            .Select(p => AssetDatabase.LoadAssetAtPath<WeaponUpgradeCardSO>(p))
            .Where(c => c != null)
            .ToArray();

        if (cards.Length == 0)
        {
            Debug.LogWarning("[정답지 캡처] 선택된 WeaponUpgradeCardSO가 없습니다. 카드 에셋을 여러 개 선택하고 실행하세요.");
            return;
        }

        int add = 0, upd = 0;

        foreach (var c in cards)
        {
            var snap = config.Find(c.name);
            if (snap == null)
            {
                snap = new WeaponUpgradeCardAutoConfigSO.CardSnapshot { assetName = c.name };
                config.cards.Add(snap);
                add++;
            }
            else upd++;

            // 네 프로젝트 인스펙터 필드명(소문자) 기준으로 저장
            snap.slotIndex = c.slotIndex;
            snap.weaponNameKr = c.weaponNameKr;
            snap.weaponId = c.weaponId;
            snap.type = c.type;
            snap.value = c.value;

            snap.icon = c.icon;
            snap.titleKr = c.titleKr;
            snap.descKr = c.descKr;
            snap.tagsKr = c.tagsKr;
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        Debug.Log($"[정답지 캡처] 완료 add={add}, upd={upd}  =>  {PATH}");
        EditorGUIUtility.PingObject(config);
        Selection.activeObject = config;
    }

    [MenuItem("Tool/그날이후/SO/무기 업그레이드/카드 정답지 생성(선택 폴더 전체 캡처)")]
    public static void CaptureFolder()
    {
        EnsureFolder(DIR);
        var config = LoadOrCreateConfig();

        string basePath = "Assets";
        var sel = Selection.activeObject;
        var selPath = sel ? AssetDatabase.GetAssetPath(sel) : "";
        if (!string.IsNullOrEmpty(selPath) && AssetDatabase.IsValidFolder(selPath))
            basePath = selPath;

        var guids = AssetDatabase.FindAssets("t:WeaponUpgradeCardSO", new[] { basePath });
        if (guids.Length == 0)
        {
            Debug.LogWarning($"[정답지 캡처] 폴더({basePath})에 WeaponUpgradeCardSO가 없습니다.");
            return;
        }

        int add = 0, upd = 0;

        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var c = AssetDatabase.LoadAssetAtPath<WeaponUpgradeCardSO>(p);
            if (c == null) continue;

            var snap = config.Find(c.name);
            if (snap == null)
            {
                snap = new WeaponUpgradeCardAutoConfigSO.CardSnapshot { assetName = c.name };
                config.cards.Add(snap);
                add++;
            }
            else upd++;

            snap.slotIndex = c.slotIndex;
            snap.weaponNameKr = c.weaponNameKr;
            snap.weaponId = c.weaponId;
            snap.type = c.type;
            snap.value = c.value;

            snap.icon = c.icon;
            snap.titleKr = c.titleKr;
            snap.descKr = c.descKr;
            snap.tagsKr = c.tagsKr;
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        Debug.Log($"[정답지 캡처] 폴더({basePath}) 완료 add={add}, upd={upd}");
        EditorGUIUtility.PingObject(config);
        Selection.activeObject = config;
    }

    [MenuItem("Tool/그날이후/SO/무기 업그레이드/카드 내용 복원(정답지 기준)")]
    public static void ApplyToCards()
    {
        var config = AssetDatabase.LoadAssetAtPath<WeaponUpgradeCardAutoConfigSO>(PATH);
        if (config == null)
        {
            Debug.LogWarning("[복원] 정답지 SO가 없습니다. 먼저 '카드 정답지 생성(캡처)'를 실행하세요.");
            return;
        }

        // 1) 카드 선택되어 있으면 그 카드만
        var selectedCards = Selection.objects
            .Select(o => AssetDatabase.GetAssetPath(o))
            .Where(p => p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            .Select(p => AssetDatabase.LoadAssetAtPath<WeaponUpgradeCardSO>(p))
            .Where(c => c != null)
            .ToArray();

        WeaponUpgradeCardSO[] targets;

        if (selectedCards.Length > 0) targets = selectedCards;
        else
        {
            // 2) 아니면 선택 폴더 전체
            string basePath = "Assets";
            var sel = Selection.activeObject;
            var selPath = sel ? AssetDatabase.GetAssetPath(sel) : "";
            if (!string.IsNullOrEmpty(selPath) && AssetDatabase.IsValidFolder(selPath))
                basePath = selPath;

            var guids = AssetDatabase.FindAssets("t:WeaponUpgradeCardSO", new[] { basePath });
            targets = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<WeaponUpgradeCardSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null)
                .ToArray();
        }

        if (targets.Length == 0)
        {
            Debug.LogWarning("[복원] 적용할 WeaponUpgradeCardSO가 없습니다. 카드 또는 카드 폴더를 선택하고 실행하세요.");
            return;
        }

        int ok = 0, miss = 0;

        foreach (var c in targets)
        {
            var snap = config.Find(c.name);
            if (snap == null) { miss++; continue; }

            c.slotIndex = snap.slotIndex;
            c.weaponNameKr = snap.weaponNameKr;
            c.weaponId = snap.weaponId;
            c.type = snap.type;
            c.value = snap.value;

            c.icon = snap.icon;
            c.titleKr = snap.titleKr;
            c.descKr = snap.descKr;
            c.tagsKr = snap.tagsKr;

            EditorUtility.SetDirty(c);
            ok++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[복원] 완료 ok={ok}, 정답지에 없는 카드={miss}");
    }

    private static WeaponUpgradeCardAutoConfigSO LoadOrCreateConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<WeaponUpgradeCardAutoConfigSO>(PATH);
        if (config != null) return config;

        config = ScriptableObject.CreateInstance<WeaponUpgradeCardAutoConfigSO>();
        AssetDatabase.CreateAsset(config, PATH);
        return config;
    }

    private static void EnsureFolder(string fullAssetPath)
    {
        if (AssetDatabase.IsValidFolder(fullAssetPath)) return;

        var parts = fullAssetPath.Split('/');
        string cur = parts[0]; // Assets
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
#endif