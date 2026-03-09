#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CommonSkillBalanceAutoSetter
{
    private const int TargetMaxLevel = 8;
    private static readonly string[] TargetFolders = { "Assets/_Game/Data/Defs/CommonSkills" };

    [MenuItem("Tools/그날이후/밸런스/공통스킬(CommonSkills) 레벨 1~8 자동 세팅")]
    public static void ApplyBalance()
    {
        var guids = AssetDatabase.FindAssets("t:CommonSkillConfigSO", TargetFolders);

        // 폴더가 다르면 fallback: 전체 검색(느리지만 안전)
        if (guids == null || guids.Length == 0)
            guids = AssetDatabase.FindAssets("t:CommonSkillConfigSO");

        int changed = 0;

        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var cfg = AssetDatabase.LoadAssetAtPath<CommonSkillConfigSO>(path);
            if (cfg == null) continue;

            ApplyOne(cfg);
            EditorUtility.SetDirty(cfg);
            changed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CommonSkillBalanceAutoSetter] 완료. CommonSkillConfigSO {changed}개 갱신.");
    }

    private static void ApplyOne(CommonSkillConfigSO cfg)
    {
        cfg.maxLevel = TargetMaxLevel;

        if (cfg.levels == null || cfg.levels.Length != TargetMaxLevel)
            cfg.levels = new CommonSkillLevelParams[TargetMaxLevel];

        for (int i = 0; i < TargetMaxLevel; i++)
        {
            int lv = i + 1;
            float t = (TargetMaxLevel == 1) ? 1f : (i / (float)(TargetMaxLevel - 1));

            CommonSkillLevelParams p = default;

            switch (cfg.kind)
            {
                case CommonSkillKind.OrbitingBlade: // 회전검(월륜검)
                    p.damage = Mathf.RoundToInt(Mathf.Lerp(10f, 18f, t));
                    p.projectileCount = lv; // 레벨당 +1
                    p.hitInterval = Mathf.Lerp(0.35f, 0.22f, t);
                    p.orbitRadius = Mathf.Lerp(1.35f, 1.75f, t);
                    p.orbitAngularSpeed = Mathf.Lerp(220f, 340f, t);
                    break;

                case CommonSkillKind.Boomerang: // 부메랑
                    p.cooldown = Mathf.Lerp(2.40f, 1.60f, t);
                    p.damage = 20; // 스펙: 기본 20
                    p.projectileCount = lv; // 레벨당 +1
                    p.spreadAngleDeg = 6f; // 너무 V자 방지(과확산 금지)
                    p.projectileSpeed = 8.0f;
                    p.returnSpeed = 10.0f;
                    p.maxDistance = 10.0f;
                    p.lifeSeconds = 3.0f;
                    break;

                case CommonSkillKind.PiercingBullet: // 총알(관통)
                    p.cooldown = Mathf.Lerp(0.55f, 0.35f, t);
                    p.damage = Mathf.RoundToInt(20f * (1f + 0.10f * (lv - 1))); // 레벨당 +10% 느낌
                    p.projectileCount = 1;
                    p.spreadAngleDeg = 0f;
                    p.projectileSpeed = 18.0f;
                    p.lifeSeconds = 1.10f;
                    break;

                case CommonSkillKind.HomingMissile: // 호밍(정화된 영혼)
                    p.cooldown = Mathf.Lerp(2.60f, 1.70f, t);
                    p.damage = 20;
                    p.projectileCount = lv;       // 레벨당 +1 (요구사항)
                    p.projectileSpeed = 9.0f;
                    p.lifeSeconds = 2.20f;
                    p.turnSpeedDeg = 720f;
                    p.chainCount = 0;             // 이번 단계에선 체인 미사용(각성/확장 때 사용)
                    break;

                case CommonSkillKind.DarkOrb: // 다크오브(요괴영혼)
                    p.cooldown = Mathf.Lerp(3.60f, 2.60f, t);
                    p.damage = Mathf.RoundToInt(Mathf.Lerp(5f, 14f, t));
                    p.projectileSpeed = 6.0f;
                    p.lifeSeconds = 1.40f;
                    p.explosionRadius = Mathf.Lerp(0.80f, 1.10f, t); // 넓으면 사기라 상한 제한
                    p.splitCount = 2 + (lv - 1);  // 2..9
                    p.childSpeed = 7.8f;
                    break;

                case CommonSkillKind.Shuriken: // 수리검
                    p.cooldown = Mathf.Lerp(1.50f, 0.95f, t);
                    p.damage = Mathf.RoundToInt(Mathf.Lerp(5f, 14f, t));
                    p.projectileCount = lv;             // 레벨당 +1 (요구사항)
                    p.bounceCount = 2 + (lv - 1) / 2;   // 2,2,3,3,4,4,5,5
                    p.projectileSpeed = 10.0f;
                    p.lifeSeconds = 2.60f;
                    break;

                case CommonSkillKind.ArrowShot: // 기본화살(각궁)
                    p.cooldown = Mathf.Lerp(1.00f, 0.85f, t);
                    p.damage = 20;
                    p.projectileCount = lv;       // 레벨당 +1
                    p.spreadAngleDeg = 4.0f;      // 디아2 멀티샷 느낌(너무 V자 방지)
                    p.projectileSpeed = 14.0f;
                    p.lifeSeconds = 1.20f;
                    break;

                case CommonSkillKind.ArrowRain: // 화살비(화차)
                    p.cooldown = Mathf.Lerp(5.60f, 3.90f, t);
                    p.damage = Mathf.RoundToInt(10f * (1f + 0.14f * (lv - 1))); // 레벨당 +14% 정도
                    p.hitInterval = Mathf.Lerp(0.75f, 0.42f, t);                // 레벨 오를수록 더 촘촘
                    p.lifeSeconds = Mathf.Lerp(3.40f, 4.10f, t);
                    p.explosionRadius = Mathf.Lerp(2.10f, 3.00f, t);
                    break;

                case CommonSkillKind.Balsi: // 발시를 CommonSkill 쪽으로 쓰는 경우 대비(선택)
                    p.cooldown = Mathf.Lerp(0.90f, 0.60f, t);
                    p.damage = Mathf.RoundToInt(20f * (1f + 0.10f * (lv - 1)));
                    p.projectileSpeed = 16.0f;
                    p.lifeSeconds = 1.10f;
                    break;
            }

            cfg.levels[i] = p;
        }
    }
}
#endif