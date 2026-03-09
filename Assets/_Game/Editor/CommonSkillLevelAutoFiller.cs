#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CommonSkillLevelAutoFiller
{
    [MenuItem("Tools/그날이후/공통스킬/1) 선택된 스킬 설정(Levels) 자동 채우기(안전)")]
    public static void FillSelected()
    {
        var selection = Selection.objects;
        int count = 0;

        foreach (var obj in selection)
        {
            if (obj == null) continue;
            if (obj is not ScriptableObject) continue;

            var so = new SerializedObject(obj);

            // CommonSkillConfigSO 판별(필드명: kind)
            var kindProp = so.FindProperty("kind");
            if (kindProp == null) continue;

            // maxLevel
            int maxLevel = 10;
            var maxLevelProp = so.FindProperty("maxLevel");
            if (maxLevelProp != null && maxLevelProp.propertyType == SerializedPropertyType.Integer)
                maxLevel = Mathf.Max(1, maxLevelProp.intValue);

            // levels 배열/리스트
            var levelsProp = so.FindProperty("levels") ?? so.FindProperty("Levels");
            if (levelsProp == null || !levelsProp.isArray) continue;

            levelsProp.arraySize = maxLevel;

            var kind = (CommonSkillKind)kindProp.enumValueIndex;

            for (int lv = 1; lv <= maxLevel; lv++)
            {
                var e = levelsProp.GetArrayElementAtIndex(lv - 1);

                // 기본값(0 방지)
                float dmg = Round1(8f * Mathf.Pow(1.12f, lv - 1));
                float cd  = Round2(0.8f * Mathf.Pow(0.97f, lv - 1));
                int proj  = 1;
                float spd = 18f;
                float life = 3.0f;

                switch (kind)
                {
                    case CommonSkillKind.OrbitingBlade:
                        proj = (lv == 1) ? 1 : lv;
                        dmg = Round1(4f * Mathf.Pow(1.10f, lv - 1));
                        cd = 0f; // 의미 없을 수 있음
                        TrySetNumber(e, "hitInterval", 0.25f);
                        life = 999f; // 회전형은 상시
                        break;

                    case CommonSkillKind.Boomerang:
                        proj = (lv == 1) ? 1 : lv;
                        cd = Round2(1.2f * Mathf.Pow(0.97f, lv - 1));
                        dmg = Round1(9f * Mathf.Pow(1.12f, lv - 1));
                        spd = 16f;
                        life = 4f;
                        TrySetNumber(e, "returnSpeed", 14f + (lv - 1) * 1.5f);
                        break;

                    case CommonSkillKind.PiercingBullet:
                        proj = 1;
                        cd = Round2(0.65f * Mathf.Pow(0.97f, lv - 1));
                        dmg = (lv == 1) ? 10f : Round1(10f * Mathf.Pow(1.35f, lv - 1));
                        spd = 26f;
                        life = 2.5f;
                        break;

                    case CommonSkillKind.HomingMissile:
                        proj = 1;
                        cd = Round2(1.0f * Mathf.Pow(0.97f, lv - 1));
                        dmg = Round1(8f * Mathf.Pow(1.18f, lv - 1));
                        spd = 18f;
                        life = 4.5f;
                        TrySetNumber(e, "chainCount", Mathf.Max(0, lv - 1));
                        TrySetNumber(e, "turnSpeedDeg", 360f + (lv - 1) * 45f);
                        break;

                    case CommonSkillKind.DarkOrb:
                        proj = 1;
                        cd = Round2(2.0f * Mathf.Pow(0.98f, lv - 1));
                        dmg = Round1(12f * Mathf.Pow(1.15f, lv - 1));
                        spd = 14f;
                        life = 3.5f;
                        TrySetNumber(e, "splitCount", (lv == 1) ? 2 : 2 + (lv - 2) * 2);
                        TrySetNumber(e, "explosionRadius", 1.2f + (lv - 1) * 0.1f);
                        break;

                    case CommonSkillKind.Shuriken:
                        proj = 1;
                        cd = Round2(0.9f * Mathf.Pow(0.97f, lv - 1));
                        dmg = Round1(7f * Mathf.Pow(1.14f, lv - 1));
                        spd = 22f;
                        life = 3.0f;
                        TrySetNumber(e, "bounceCount", (lv == 1) ? 1 : 1 + (lv - 1) * 2);
                        break;

                    case CommonSkillKind.ArrowShot:
                        proj = (lv == 1) ? 1 : lv;
                        cd = Round2(0.75f * Mathf.Pow(0.97f, lv - 1));
                        dmg = Round1(8f * Mathf.Pow(1.12f, lv - 1));
                        spd = 24f;
                        life = 2.5f;
                        TrySetNumber(e, "spreadAngleDeg", (proj <= 1) ? 0f : 18f);
                        break;

                    case CommonSkillKind.ArrowRain:
                        proj = 1;
                        cd = Round2(2.5f * Mathf.Pow(0.98f, lv - 1));
                        dmg = Round1(6f * Mathf.Pow(1.18f, lv - 1));
                        spd = 0f;   // 낙하형이면 투사체 속도 의미 없을 수 있음
                        life = 3.0f;
                        TrySetNumber(e, "tickInterval", Round2(0.55f * Mathf.Pow(0.93f, lv - 1)));
                        break;
                }

                // 존재하는 필드만 채움(타입 int/float 자동)
                TrySetNumber(e, "damage", dmg);
                TrySetNumber(e, "cooldown", cd);
                TrySetNumber(e, "projectileCount", proj);

                TrySetNumber(e, "projectileSpeed", spd);
                TrySetNumber(e, "lifeSeconds", life);

                // 있으면 기본
                TrySetNumber(e, "maxDistance", 25f);
                TrySetNumber(e, "pierceCount", 0);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(obj);
            count++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[CommonSkillLevelAutoFiller] 완료: {count}개 Levels 자동 채움(0 방지)");
    }

    private static void TrySetNumber(SerializedProperty element, string name, float v)
    {
        var p = element.FindPropertyRelative(name);
        if (p == null) return;

        if (p.propertyType == SerializedPropertyType.Float) p.floatValue = v;
        else if (p.propertyType == SerializedPropertyType.Integer) p.intValue = Mathf.RoundToInt(v);
    }

    private static void TrySetNumber(SerializedProperty element, string name, int v)
    {
        var p = element.FindPropertyRelative(name);
        if (p == null) return;

        if (p.propertyType == SerializedPropertyType.Integer) p.intValue = v;
        else if (p.propertyType == SerializedPropertyType.Float) p.floatValue = v;
    }

    private static float Round1(float v) => Mathf.Round(v * 10f) / 10f;
    private static float Round2(float v) => Mathf.Round(v * 100f) / 100f;
}
#endif