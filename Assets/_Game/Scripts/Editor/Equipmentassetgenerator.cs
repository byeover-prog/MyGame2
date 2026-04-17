#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EquipmentAssetGenerator
{
    private const string TargetFolder = "Assets/GameData/Equipments";

    [MenuItem("Tools/혼령검/장비 44종 자동 생성")]
    public static void GenerateAllEquipments()
    {
        if (!AssetDatabase.IsValidFolder("Assets/GameData"))
            AssetDatabase.CreateFolder("Assets", "GameData");
        if (!AssetDatabase.IsValidFolder(TargetFolder))
            AssetDatabase.CreateFolder("Assets/GameData", "Equipments");

        var dataList = BuildEquipmentData();
        int created = 0, updated = 0;

        foreach (var data in dataList)
        {
            string assetPath = $"{TargetFolder}/Equipment_{data.id}_{data.safeName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EquipmentDefinitionSO>(assetPath);

            if (existing == null)
            {
                var so = ScriptableObject.CreateInstance<EquipmentDefinitionSO>();
                ApplyData(so, data);
                AssetDatabase.CreateAsset(so, assetPath);
                created++;
            }
            else
            {
                ApplyData(existing, data);
                EditorUtility.SetDirty(existing);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EquipmentAssetGenerator] 생성 {created}개 / 갱신 {updated}개 완료. 경로: {TargetFolder}");
    }

    [MenuItem("Tools/혼령검/장비 데이터베이스 SO 자동 연결")]
    public static void AutoFillDatabase()
    {
        string dbPath = "Assets/GameData/EquipmentDatabase.asset";
        var db = AssetDatabase.LoadAssetAtPath<EquipmentDatabaseSO>(dbPath);

        if (db == null)
        {
            db = ScriptableObject.CreateInstance<EquipmentDatabaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        db.allEquipments.Clear();
        string[] guids = AssetDatabase.FindAssets("t:EquipmentDefinitionSO", new[] { TargetFolder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<EquipmentDefinitionSO>(path);
            if (so != null) db.allEquipments.Add(so);
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"[EquipmentAssetGenerator] 데이터베이스 자동 연결: {db.allEquipments.Count}개");
    }
    
    // 데이터 정의
    
    private struct Data
    {
        public string id;
        public string name;
        public string safeName;
        public EquipmentRarity rarity;
        public string description;
        public EquipmentEffect[] effects;
    }

    private static void ApplyData(EquipmentDefinitionSO so, Data d)
    {
        so.equipmentId = d.id;
        so.equipmentName = d.name;
        so.rarity = d.rarity;
        so.description = d.description;
        if (so.effects == null) so.effects = new List<EquipmentEffect>();
        so.effects.Clear();
        so.effects.AddRange(d.effects);
    }

    private static EquipmentEffect EF(EquipmentEffectType t, float v)
        => new EquipmentEffect { type = t, value = v };

    private static List<Data> BuildEquipmentData()
    {
        var list = new List<Data>();
        
        // 에픽 4종
        
        list.Add(new Data {
            id = "E1", name = "시전 주문의 격서", safeName = "CastScripture",
            rarity = EquipmentRarity.Epic,
            description = "기본 스킬의 시전 횟수를 2회 증가시킨다.",
            effects = new[] { EF(EquipmentEffectType.ProjectileCountFlat, 2) }
        });
        list.Add(new Data {
            id = "E2", name = "시조의 인장", safeName = "AncestorSeal",
            rarity = EquipmentRarity.Epic,
            description = "시작 스킬의 피해량을 50% 증가시킨다.",
            effects = new[] { EF(EquipmentEffectType.StartingSkillDamagePercent, 50) }
        });
        list.Add(new Data {
            id = "E3", name = "주박해제의 부적", safeName = "BindingRelease",
            rarity = EquipmentRarity.Epic,
            description = "기본 스킬 가속을 60 증가시킨다.",
            effects = new[] { EF(EquipmentEffectType.SkillHasteFlat, 60) }
        });
        list.Add(new Data {
            id = "E4", name = "혈맥각성의 단", safeName = "BloodlineAwakening",
            rarity = EquipmentRarity.Epic,
            description = "기본 피해량을 20% 증가시킨다.",
            effects = new[] { EF(EquipmentEffectType.AttackDamagePercent, 20) }
        });
        
        // 레어 8종

        list.Add(new Data {
            id = "R1", name = "범수의 눈", safeName = "HunterEye",
            rarity = EquipmentRarity.Rare,
            description = "치명타 확률을 15% 증가시킨다.",
            effects = new[] { EF(EquipmentEffectType.CritChancePercent, 15) }
        });
        list.Add(new Data {
            id = "R2", name = "혈섬의 송곳니", safeName = "BloodFang",
            rarity = EquipmentRarity.Rare,
            description = "치명타 피해량을 40% 증가시킨다.",
            effects = new[] { EF(EquipmentEffectType.CritDamagePercent, 40) }
        });
        list.Add(new Data {
            id = "R3", name = "전장군주의 문장", safeName = "WarlordCrest",
            rarity = EquipmentRarity.Rare,
            description = "최대 체력을 30% 증가시킨다.",
            effects = new[] { EF(EquipmentEffectType.MaxHpPercent, 30) }
        });
        list.Add(new Data {
            id = "R4", name = "철갑의 수호부", safeName = "IronGuard",
            rarity = EquipmentRarity.Rare,
            description = "방어력을 60 증가시킨다.",
            effects = new[] { EF(EquipmentEffectType.DefenseFlat, 60) }
        });
        list.Add(new Data {
            id = "R5", name = "귀왕의 이빨", safeName = "GhostKingTooth",
            rarity = EquipmentRarity.Rare,
            description = "기본 피해량을 15% 증가시킨다.",
            effects = new[] { EF(EquipmentEffectType.AttackDamagePercent, 15) }
        });
        list.Add(new Data {
            id = "R6", name = "산악의 토룡갑", safeName = "MountainEarthArmor",
            rarity = EquipmentRarity.Rare,
            description = "받는 피해량을 15% 감소시킨다.",
            effects = new[] { EF(EquipmentEffectType.DamageTakenReducePercent, 15) }
        });
        list.Add(new Data {
            id = "R7", name = "영겁의 시계", safeName = "EternalClock",
            rarity = EquipmentRarity.Rare,
            description = "기본 스킬 가속을 40 증가시킨다.",
            effects = new[] { EF(EquipmentEffectType.SkillHasteFlat, 40) }
        });
        list.Add(new Data {
            id = "R8", name = "바람낭자의 신발", safeName = "WindLadyShoes",
            rarity = EquipmentRarity.Rare,
            description = "이동속도를 15% 증가시킨다.",
            effects = new[] { EF(EquipmentEffectType.MoveSpeedPercent, 15) }
        });
        
        // 희귀 16종

        list.Add(new Data { id = "U1", name = "연검의 문양", safeName = "RefinedCrest",
            rarity = EquipmentRarity.Uncommon, description = "기본 피해량 +10%",
            effects = new[] { EF(EquipmentEffectType.AttackDamagePercent, 10) } });
        list.Add(new Data { id = "U2", name = "주술사의 구슬", safeName = "ShamanOrb",
            rarity = EquipmentRarity.Uncommon, description = "스킬 가속 +30",
            effects = new[] { EF(EquipmentEffectType.SkillHasteFlat, 30) } });
        list.Add(new Data { id = "U3", name = "쌍발의 부적", safeName = "TwinShot",
            rarity = EquipmentRarity.Uncommon, description = "시전 횟수 +1",
            effects = new[] { EF(EquipmentEffectType.ProjectileCountFlat, 1) } });
        list.Add(new Data { id = "U4", name = "매의 눈", safeName = "FalconEye",
            rarity = EquipmentRarity.Uncommon, description = "치명타 확률 +10%",
            effects = new[] { EF(EquipmentEffectType.CritChancePercent, 10) } });
        list.Add(new Data { id = "U5", name = "피의 인장", safeName = "BloodSeal",
            rarity = EquipmentRarity.Uncommon, description = "치명타 피해량 +30%",
            effects = new[] { EF(EquipmentEffectType.CritDamagePercent, 30) } });
        list.Add(new Data { id = "U6", name = "거령의 갑주", safeName = "GuardianArmor",
            rarity = EquipmentRarity.Uncommon, description = "방어력 +40",
            effects = new[] { EF(EquipmentEffectType.DefenseFlat, 40) } });
        list.Add(new Data { id = "U7", name = "옥련의 심장", safeName = "JadeHeart",
            rarity = EquipmentRarity.Uncommon, description = "최대 체력 +15%",
            effects = new[] { EF(EquipmentEffectType.MaxHpPercent, 15) } });
        list.Add(new Data { id = "U8", name = "질풍의 짚신", safeName = "GaleSandals",
            rarity = EquipmentRarity.Uncommon, description = "이동속도 +10%",
            effects = new[] { EF(EquipmentEffectType.MoveSpeedPercent, 10) } });

        list.Add(new Data { id = "U9", name = "혹한의 부서", safeName = "FrostTome",
            rarity = EquipmentRarity.Uncommon, description = "빙결 속성 피해량 +15%",
            effects = new[] { EF(EquipmentEffectType.IceDamageBonusPercent, 15) } });
        list.Add(new Data { id = "U10", name = "천둥의 벼락부", safeName = "ThunderTalisman",
            rarity = EquipmentRarity.Uncommon, description = "전기 속성 피해량 +15%",
            effects = new[] { EF(EquipmentEffectType.ElectricDamageBonusPercent, 15) } });
        list.Add(new Data { id = "U11", name = "화염의 봉인", safeName = "FlameSeal",
            rarity = EquipmentRarity.Uncommon, description = "화염 속성 피해량 +15%",
            effects = new[] { EF(EquipmentEffectType.FireDamageBonusPercent, 15) } });
        list.Add(new Data { id = "U12", name = "음양의 옥", safeName = "YinYangGem",
            rarity = EquipmentRarity.Uncommon, description = "음/양 속성 효과 +30% (흡혈/재생)",
            effects = new[] { EF(EquipmentEffectType.YinYangEffectBonusPercent, 30) } });

        list.Add(new Data { id = "U13", name = "재기의 단약", safeName = "ReviveElixir",
            rarity = EquipmentRarity.Uncommon, description = "체력 재생 +3",
            effects = new[] { EF(EquipmentEffectType.HpRegenFlat, 3) } });
        list.Add(new Data { id = "U14", name = "백동의 경전", safeName = "WhiteBronzeScripture",
            rarity = EquipmentRarity.Uncommon, description = "경험치 획득량 +15%",
            effects = new[] { EF(EquipmentEffectType.ExpGainPercent, 15) } });
        list.Add(new Data { id = "U15", name = "재물의 부적", safeName = "WealthTalisman",
            rarity = EquipmentRarity.Uncommon, description = "재화 획득량 +20%",
            effects = new[] { EF(EquipmentEffectType.GoldGainPercent, 20) } });
        list.Add(new Data { id = "U16", name = "영압의 장신구", safeName = "SpiritPressure",
            rarity = EquipmentRarity.Uncommon, description = "스킬 범위 +15%",
            effects = new[] { EF(EquipmentEffectType.SkillAreaPercent, 15) } });

        // 일반 16종
        
        list.Add(new Data { id = "C1", name = "흐린 문양", safeName = "FadedCrest",
            rarity = EquipmentRarity.Common, description = "기본 피해량 +5%",
            effects = new[] { EF(EquipmentEffectType.AttackDamagePercent, 5) } });
        list.Add(new Data { id = "C2", name = "낡은 구슬", safeName = "OldOrb",
            rarity = EquipmentRarity.Common, description = "스킬 가속 +15",
            effects = new[] { EF(EquipmentEffectType.SkillHasteFlat, 15) } });
        list.Add(new Data { id = "C3", name = "녹슨 표창", safeName = "RustyShuriken",
            rarity = EquipmentRarity.Common, description = "투사체 속도 +15%",
            effects = new[] { EF(EquipmentEffectType.ProjectileSpeedPercent, 15) } });
        list.Add(new Data { id = "C4", name = "삭은 인장", safeName = "WornSeal",
            rarity = EquipmentRarity.Common, description = "치명타 확률 +5%",
            effects = new[] { EF(EquipmentEffectType.CritChancePercent, 5) } });
        list.Add(new Data { id = "C5", name = "금간 구슬", safeName = "CrackedOrb",
            rarity = EquipmentRarity.Common, description = "치명타 피해량 +15%",
            effects = new[] { EF(EquipmentEffectType.CritDamagePercent, 15) } });
        list.Add(new Data { id = "C6", name = "허름한 문서", safeName = "ShabbyDocument",
            rarity = EquipmentRarity.Common, description = "스킬 범위 +10%",
            effects = new[] { EF(EquipmentEffectType.SkillAreaPercent, 10) } });
        list.Add(new Data { id = "C7", name = "낡은 호신부", safeName = "OldGuardAmulet",
            rarity = EquipmentRarity.Common, description = "최대 체력 +8%",
            effects = new[] { EF(EquipmentEffectType.MaxHpPercent, 8) } });
        list.Add(new Data {
            id = "C8", name = "조악한 부적", safeName = "CrudeTalisman",
            rarity = EquipmentRarity.Common,
            description = "이동속도 +10% / 받는 피해량 +15% (디메리트)",
            effects = new[] {
                EF(EquipmentEffectType.MoveSpeedPercent, 10),
                EF(EquipmentEffectType.DamageTakenIncreasePercent, 15)
            }
        });

        list.Add(new Data { id = "C9", name = "단련의 가죽띠", safeName = "TrainedBelt",
            rarity = EquipmentRarity.Common, description = "방어력 +20",
            effects = new[] { EF(EquipmentEffectType.DefenseFlat, 20) } });
        list.Add(new Data { id = "C10", name = "질박한 신발", safeName = "SimpleShoes",
            rarity = EquipmentRarity.Common, description = "이동속도 +5%",
            effects = new[] { EF(EquipmentEffectType.MoveSpeedPercent, 5) } });
        list.Add(new Data { id = "C11", name = "얕은 혈맥의 단약", safeName = "ShallowElixir",
            rarity = EquipmentRarity.Common, description = "체력 재생 +2",
            effects = new[] { EF(EquipmentEffectType.HpRegenFlat, 2) } });
        list.Add(new Data {
            id = "C12", name = "풍화된 부적", safeName = "WeatheredTalisman",
            rarity = EquipmentRarity.Common,
            description = "기본 피해량 +10% / 받는 피해량 +15% (디메리트)",
            effects = new[] {
                EF(EquipmentEffectType.AttackDamagePercent, 10),
                EF(EquipmentEffectType.DamageTakenIncreasePercent, 15)
            }
        });
        list.Add(new Data { id = "C13", name = "가벼운 짚신", safeName = "LightSandals",
            rarity = EquipmentRarity.Common, description = "대쉬 쿨타임 -15%",
            effects = new[] { EF(EquipmentEffectType.DashCooldownReducePercent, 15) } });
        list.Add(new Data { id = "C14", name = "흔한 주머니", safeName = "PlainPouch",
            rarity = EquipmentRarity.Common, description = "픽업 범위 +15%",
            effects = new[] { EF(EquipmentEffectType.PickupRangePercent, 15) } });
        list.Add(new Data { id = "C15", name = "동전 자루", safeName = "CoinBag",
            rarity = EquipmentRarity.Common, description = "재화 획득량 +8%",
            effects = new[] { EF(EquipmentEffectType.GoldGainPercent, 8) } });
        list.Add(new Data { id = "C16", name = "경험의 조각", safeName = "ExpShard",
            rarity = EquipmentRarity.Common, description = "경험치 획득량 +8%",
            effects = new[] { EF(EquipmentEffectType.ExpGainPercent, 8) } });

        return list;
    }
}
#endif