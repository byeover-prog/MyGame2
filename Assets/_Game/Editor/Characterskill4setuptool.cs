#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using _Game.Skills;

public static class CharacterSkill4SetupTool
{
    // ─── 경로 상수 ───────────────────────────────────────────────────
    private const string SO_FOLDER       = "Assets/_Game/Data/Skills/Character";
    private const string JSON_PATH       = "Assets/_Game/Balance/skill_balance.json";
    private const string PREFAB_SEARCH_LABEL = "t:Prefab";
    
    private sealed class SkillData
    {
        public string id;                 // 예: "weapon_yeolcham"
        public string displayName;        // "열참"
        public string tagKr;              // "범위 · 음"
        public int    maxLevel;           // 8 (전용 스킬도 공통과 동일)
        public string descLv1;            // 카드 Lv1 설명 (시각적 + 메커니즘 설명, 레벨 무관)
        public string levelUpEffectKr;    // "레벨당 피해 +10%" — Add Info 자동 생성용

        // JSON 밸런스 (1레벨 기준)
        public int    damage;
        public float  cooldown;
        public int    castCount;          // 시전 횟수 (-1 = 의미 없음)

        // 레벨당 증가량 (% → 절대값으로 환산해서 저장)
        public int    damageAddPerLevel;
        public float  cooldownAddPerLevel;

        // 범위 (-1 = 미사용)
        public float  explosionRadius;
        public float  explosionRadiusAddPerLevel;

        // 프리팹 / 컴포넌트 매칭
        public string weaponPrefabName;            // 검색 이름 (예: "Weapon_Yeolcham")
        public string weaponComponentTypeName;     // 무기 스크립트 타입 (예: "YeolchamWeapon2D")
    }

    private static readonly SkillData[] Skills = new SkillData[]
    {
        // ─── 열참 (하린, 음, 데미지 15 / 쿨 3 / 시전1 / 레벨당 피해+15%) ───
        new SkillData
        {
            id = "weapon_yeolcham",
            displayName = "열참",
            tagKr = "범위 · 음",
            maxLevel = 8,
            descLv1 = "다리우스 Q처럼 외곽에 맞으면 추가 피해를 입히고 생명력을 흡수한다.",
            levelUpEffectKr = "피해량 +15%",
            damage = 15,
            cooldown = 3f,
            castCount = -1,
            damageAddPerLevel = 2,            // 15 × 15% = 2.25 → 2 (반올림)
            cooldownAddPerLevel = 0f,
            explosionRadius = 3.0f,
            explosionRadiusAddPerLevel = 0f,
            weaponPrefabName = "Weapon_Yeolcham",
            weaponComponentTypeName = "YeolchamWeapon2D",
        },

        // ─── 월참(검기 발사) (하린, 음, 데미지 15 / 쿨 3 / 시전1 / 레벨당 피해+10%) ───
        new SkillData
        {
            id = "weapon_wolcham",
            displayName = "월참",
            tagKr = "관통 · 음",
            maxLevel = 8,
            descLv1 = "마우스 방향으로 직선으로 날아가며 관통하는 초승달형 검기를 발사한다.",
            levelUpEffectKr = "피해량 +10%",
            damage = 15,
            cooldown = 3f,
            castCount = -1,
            damageAddPerLevel = 2,            // 15 × 10% = 1.5 → 2 (반올림)
            cooldownAddPerLevel = 0f,
            explosionRadius = -1f,
            explosionRadiusAddPerLevel = 0f,
            weaponPrefabName = "Weapon_Wolcham",
            weaponComponentTypeName = "WolchamWeapon2D",
        },

        // ─── 설빙탄(폭발 부착형 화살) (윤설, 빙결, 데미지 15 / 쿨 3 / 시전2 / 레벨당 쿨-10%·피해+10%) ───
        new SkillData
        {
            id = "weapon_seolbingtan",
            displayName = "설빙탄",
            tagKr = "범위 · 빙결",
            maxLevel = 8,
            descLv1 = "한 명의 대상에게 부착되는 화살을 발사한다. 부착 후 1.5초 뒤에 폭발한다.",
            levelUpEffectKr = "재사용 -10%, 피해량 +10%",
            damage = 15,
            cooldown = 3f,
            castCount = 2,
            damageAddPerLevel = 2,            // 15 × 10% = 1.5 → 2
            cooldownAddPerLevel = -0.3f,      // 3 × -10% = -0.3
            explosionRadius = 2.0f,
            explosionRadiusAddPerLevel = 0f,
            weaponPrefabName = "Weapon_Seolbing",
            weaponComponentTypeName = "SeolbingtanWeapon2D",
        },

        // ─── 뇌운 (하율, 전기, 데미지 5 / 쿨 4 / 시전1 / 피해간격 0.5초 / 레벨당 피해+10%·범위+10%) ───
        new SkillData
        {
            id = "weapon_noeun",
            displayName = "뇌운",
            tagKr = "소환 · 전기",
            maxLevel = 8,
            descLv1 = "지속적으로 전기 공격을 가하는 구름을 소환한다. 정화구처럼 적을 추적하며 0.5초마다 번개를 떨어뜨린다.",
            levelUpEffectKr = "피해량 +10%, 스킬 범위 +10%",
            damage = 5,
            cooldown = 4f,
            castCount = -1,
            damageAddPerLevel = 1,            // 5 × 10% = 0.5 → 1 (올림, 정수 보장)
            cooldownAddPerLevel = 0f,
            explosionRadius = 2.5f,
            explosionRadiusAddPerLevel = 0.25f,  // 2.5 × 10% = 0.25
            weaponPrefabName = "Weapon_Noeun",
            weaponComponentTypeName = "NoeunWeapon2D",
        },
    };

    // ════════════════════════════════════════════════════════════════
    //  메뉴 — 메인 진입점
    // ════════════════════════════════════════════════════════════════

    [MenuItem("Tools/혼령검/캐릭터 전용 스킬 4종 일괄 셋업", false, 1000)]
    public static void Run()
    {
        var report = new StringBuilder();
        report.AppendLine("═══ 캐릭터 전용 스킬 4종 셋업 ═══\n");

        try
        {
            // 1) 폴더 보장
            EnsureFolder(SO_FOLDER);
            report.AppendLine("[1/4] 폴더 확인 완료");

            // 2) SO 생성/갱신
            int soCreated = 0, soUpdated = 0;
            var soByid = new Dictionary<string, SkillDefinitionSO>();

            foreach (var sk in Skills)
            {
                bool created = CreateOrUpdateSkillDefSO(sk, out var soAsset);
                if (created) soCreated++; else soUpdated++;
                soByid[sk.id] = soAsset;
                report.AppendLine($"   • {sk.displayName} ({sk.id}) → {(created ? "생성" : "갱신")}");
            }
            report.AppendLine($"[2/4] SkillDefinitionSO {soCreated}개 생성, {soUpdated}개 갱신\n");

            // 3) JSON 갱신
            int jsonAdded = 0, jsonUpdated = 0;
            UpsertJsonRows(Skills, out jsonAdded, out jsonUpdated);
            report.AppendLine($"[3/4] skill_balance.json — {jsonAdded}개 추가, {jsonUpdated}개 갱신\n");

            // 4) 프리팹 자동 연결
            int prefabLinked = 0, prefabSkipped = 0;
            foreach (var sk in Skills)
            {
                bool linked = TryLinkPrefab(sk, soByid[sk.id], out string detail);
                if (linked) prefabLinked++; else prefabSkipped++;
                report.AppendLine($"   • {sk.weaponPrefabName} → {detail}");
            }
            report.AppendLine($"[4/4] 프리팹 {prefabLinked}개 연결, {prefabSkipped}개 스킵\n");

            // 저장
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.AppendLine("═══ 완료 ═══");
            report.AppendLine("\n[남은 수동 작업]");
            report.AppendLine(" 1. Weapon_Noeun 의 Cloud Pool / Bolt Pool 슬롯에 자식 ProjectilePool2D 드래그");
            report.AppendLine(" 2. 각 ProjectilePool2D 의 Prefab 슬롯에 투사체/구름/번개 프리팹 드래그");
            report.AppendLine(" 3. 각 SkillDef_*.asset 의 Icon 필드에 아이콘 스프라이트 연결");
            report.AppendLine(" 4. LevelUpCardGenerator 의 Character Skill Sets 배열에 (Definition, Prefab) 등록");

            string finalReport = report.ToString();
            Debug.Log(finalReport);
            EditorUtility.DisplayDialog("캐릭터 전용 스킬 4종 셋업", finalReport, "확인");

            // 첫 번째 SO 선택 → Inspector에 표시
            if (soByid.Count > 0)
            {
                foreach (var so in soByid.Values)
                {
                    Selection.activeObject = so;
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CharacterSkill4SetupTool] 셋업 실패:\n{e}");
            EditorUtility.DisplayDialog("셋업 실패", e.Message, "확인");
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  1. 폴더 보장
    // ════════════════════════════════════════════════════════════════

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string name   = Path.GetFileName(folderPath);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }

    // ════════════════════════════════════════════════════════════════
    //  2. SkillDefinitionSO 생성/갱신
    // ════════════════════════════════════════════════════════════════

    /// <summary>SO 가 없으면 생성, 있으면 값만 갱신. 반환값: true = 신규 생성</summary>
    private static bool CreateOrUpdateSkillDefSO(SkillData sk, out SkillDefinitionSO so)
    {
        // 파일명: SkillDef_Yeolcham.asset (snake → Pascal)
        string assetName = "SkillDef_" + ToPascalCase(sk.id.Replace("weapon_", ""));
        string assetPath = $"{SO_FOLDER}/{assetName}.asset";

        bool isNew = false;
        so = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(assetPath);

        if (so == null)
        {
            so = ScriptableObject.CreateInstance<SkillDefinitionSO>();
            AssetDatabase.CreateAsset(so, assetPath);
            isNew = true;
        }

        // SerializedObject 로 private 필드 직접 설정
        var sObj = new SerializedObject(so);

        SetStringIfExists(sObj, "skillId", sk.id);
        SetStringIfExists(sObj, "displayName", sk.displayName);
        SetStringIfExists(sObj, "tagKr", sk.tagKr);
        SetIntIfExists(sObj, "maxLevel", sk.maxLevel);

        // skillType — Active = 0 (LevelUpCardGenerator 는 Active 만 카드로 띄움)
        var typeProp = sObj.FindProperty("skillType");
        if (typeProp != null && typeProp.propertyType == SerializedPropertyType.Enum)
            typeProp.enumValueIndex = 0; // Active

        // 카드 설명 (Desc Lv1 ~ Lv{maxLevel}) — maxLevel 이상은 비워둠
        // ※ Lv1: 메커니즘 설명. Lv2 이상: 레벨업 효과를 자동 생성해서 채움.
        for (int lv = 1; lv <= 8; lv++)
        {
            string desc;
            if (lv == 1)
            {
                desc = sk.descLv1;
            }
            else if (lv <= sk.maxLevel)
            {
                // Lv2 이상: 레벨업 효과 누적 표시 (예: "Lv2 — 피해량 +15%")
                desc = $"Lv{lv} — {sk.levelUpEffectKr}";
            }
            else
            {
                desc = ""; // maxLevel 초과 영역은 비워둠
            }
            SetStringIfExists(sObj, $"descLv{lv}", desc);
        }

        // Add Info — Lv1 ~ Lv{maxLevel} 까지 실제 수치 자동 계산. 이상은 비워둠.
        for (int lv = 1; lv <= 8; lv++)
        {
            string addInfo = lv <= sk.maxLevel ? BuildAddInfo(sk, lv) : "";
            SetStringIfExists(sObj, $"addInfoLv{lv}", addInfo);
        }

        sObj.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(so);

        return isNew;
    }

    /// <summary>레벨별 추가 정보 문자열 자동 생성 (예: "피해 17 · 쿨 2.7s · 시전 2회")</summary>
    private static string BuildAddInfo(SkillData sk, int level)
    {
        int    dmg = sk.damage   + sk.damageAddPerLevel   * (level - 1);
        float  cd  = sk.cooldown + sk.cooldownAddPerLevel * (level - 1);
        if (cd < 0.1f) cd = 0.1f;  // 최소값 가드

        var parts = new List<string>(4);
        parts.Add($"피해 {dmg}");
        parts.Add($"쿨 {cd:F1}s");
        if (sk.castCount > 0) parts.Add($"시전 {sk.castCount}회");

        if (sk.explosionRadius > 0f)
        {
            float r = sk.explosionRadius + sk.explosionRadiusAddPerLevel * (level - 1);
            parts.Add($"범위 {r:F1}");
        }

        // Lv1 에는 레벨업 효과 안내 추가
        if (level == 1 && !string.IsNullOrWhiteSpace(sk.levelUpEffectKr))
        {
            return string.Join(" · ", parts) + $"\n레벨업: {sk.levelUpEffectKr}";
        }

        return string.Join(" · ", parts);
    }

    private static void SetStringIfExists(SerializedObject obj, string fieldName, string value)
    {
        var prop = obj.FindProperty(fieldName);
        if (prop != null && prop.propertyType == SerializedPropertyType.String)
            prop.stringValue = value;
    }

    private static void SetIntIfExists(SerializedObject obj, string fieldName, int value)
    {
        var prop = obj.FindProperty(fieldName);
        if (prop != null && prop.propertyType == SerializedPropertyType.Integer)
            prop.intValue = value;
    }

    private static string ToPascalCase(string snake)
    {
        if (string.IsNullOrEmpty(snake)) return snake;
        var parts = snake.Split('_');
        var sb = new StringBuilder();
        foreach (var p in parts)
        {
            if (string.IsNullOrEmpty(p)) continue;
            sb.Append(char.ToUpperInvariant(p[0]));
            if (p.Length > 1) sb.Append(p.Substring(1));
        }
        return sb.ToString();
    }

    // ════════════════════════════════════════════════════════════════
    //  3. skill_balance.json 갱신
    // ════════════════════════════════════════════════════════════════

    private static void UpsertJsonRows(SkillData[] skills, out int added, out int updated)
    {
        added = 0;
        updated = 0;

        if (!File.Exists(JSON_PATH))
        {
            Debug.LogError($"[CharacterSkill4SetupTool] JSON 파일이 없습니다: {JSON_PATH}");
            throw new FileNotFoundException(JSON_PATH);
        }

        // JsonUtility 로 파싱
        string originalText = File.ReadAllText(JSON_PATH);
        SkillBalanceDB2D db = null;
        try
        {
            db = JsonUtility.FromJson<SkillBalanceDB2D>(originalText);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CharacterSkill4SetupTool] JSON 파싱 실패: {e.Message}");
            throw;
        }

        if (db == null) db = new SkillBalanceDB2D { version = 1 };
        if (db.skills == null) db.skills = new SkillBalanceDB2D.SkillRow2D[0];

        var list = new List<SkillBalanceDB2D.SkillRow2D>(db.skills);

        foreach (var sk in skills)
        {
            int idx = list.FindIndex(r => r != null && r.id == sk.id);

            var row = BuildRow(sk);

            if (idx >= 0)
            {
                list[idx] = row;
                updated++;
            }
            else
            {
                list.Add(row);
                added++;
            }
        }

        db.skills = list.ToArray();

        // 다시 직렬화 (들여쓰기 적용)
        string newText = JsonUtility.ToJson(db, true);
        File.WriteAllText(JSON_PATH, newText);

        AssetDatabase.ImportAsset(JSON_PATH, ImportAssetOptions.ForceUpdate);
    }

    private static SkillBalanceDB2D.SkillRow2D BuildRow(SkillData sk)
    {
        // SkillRow2D 의 거의 모든 필드는 -1(미사용) 기본값.
        // 사용하는 필드만 명시적으로 할당.
        return new SkillBalanceDB2D.SkillRow2D
        {
            id = sk.id,

            damage = sk.damage,
            damageAddPerLevel = sk.damageAddPerLevel,

            cooldown = sk.cooldown,
            cooldownAddPerLevel = sk.cooldownAddPerLevel,

            explosionRadius = sk.explosionRadius,
            explosionRadiusAddPerLevel = sk.explosionRadiusAddPerLevel,

            // 나머지는 SkillRow2D 의 -1 기본값 사용
            speed = -1f,
            life = -1f,
            count = -1,
            hitInterval = -1f,
            orbitRadius = -1f,
            orbitSpeed = -1f,
            active = -1f,
            burstInterval = -1f,
            spinDps = -1f,
            bounceCount = -1,
            chainCount = -1,
            splitCount = -1,
            explodeDistance = -1f,
            childSpeed = -1f,
            slowRate = -1f,
            slowSeconds = -1f,
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  4. 무기 프리팹 자동 연결
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 무기 프리팹의 skillDefinition / balanceId 필드를 자동 연결.
    /// 반환: true = 성공
    /// </summary>
    private static bool TryLinkPrefab(SkillData sk, SkillDefinitionSO so, out string detail)
    {
        // 프리팹 검색
        string prefabPath = FindPrefabPath(sk.weaponPrefabName);
        if (string.IsNullOrEmpty(prefabPath))
        {
            detail = $"프리팹 '{sk.weaponPrefabName}' 을 찾을 수 없음 (스킵)";
            return false;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            detail = $"프리팹 로드 실패 ({prefabPath})";
            return false;
        }

        // 프리팹 직접 편집 모드로 열기
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        var contents = PrefabUtility.LoadPrefabContents(assetPath);

        try
        {
            // 무기 스크립트 컴포넌트 찾기
            MonoBehaviour weaponComp = null;
            var allComps = contents.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var c in allComps)
            {
                if (c == null) continue;
                if (c.GetType().Name == sk.weaponComponentTypeName)
                {
                    weaponComp = c;
                    break;
                }
            }

            if (weaponComp == null)
            {
                detail = $"컴포넌트 '{sk.weaponComponentTypeName}' 을 찾을 수 없음";
                return false;
            }

            var sObj = new SerializedObject(weaponComp);

            bool changed = false;

            // skillDefinition 필드
            var defProp = sObj.FindProperty("skillDefinition");
            if (defProp != null && defProp.propertyType == SerializedPropertyType.ObjectReference)
            {
                // 타입 호환성을 Reflection 으로 확인
                var fieldInfo = weaponComp.GetType().GetField("skillDefinition",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (fieldInfo != null && fieldInfo.FieldType.IsAssignableFrom(typeof(SkillDefinitionSO)))
                {
                    defProp.objectReferenceValue = so;
                    changed = true;
                }
                else if (fieldInfo != null)
                {
                    detail = $"skillDefinition 필드 타입이 SkillDefinitionSO 가 아님 (실제 타입: {fieldInfo.FieldType.Name}) — 수동 연결 필요";
                    return false;
                }
                else
                {
                    // FieldInfo 못 찾았으면 그냥 시도
                    defProp.objectReferenceValue = so;
                    changed = true;
                }
            }

            // balanceId 필드
            var balanceProp = sObj.FindProperty("balanceId");
            if (balanceProp != null && balanceProp.propertyType == SerializedPropertyType.String)
            {
                balanceProp.stringValue = sk.id;
                changed = true;
            }

            if (changed)
            {
                sObj.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
                detail = $"skillDefinition + balanceId 연결 완료";
                return true;
            }
            else
            {
                detail = "변경할 필드 없음";
                return false;
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static string FindPrefabPath(string prefabName)
    {
        // 정확한 이름 매칭 우선
        var guids = AssetDatabase.FindAssets($"{prefabName} {PREFAB_SEARCH_LABEL}");
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName == prefabName) return path;
        }
        return null;
    }
}
#endif