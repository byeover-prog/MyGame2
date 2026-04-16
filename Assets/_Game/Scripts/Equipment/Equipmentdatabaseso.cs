using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EquipmentDatabase",
    menuName = "혼령검/장비/장비 데이터베이스 SO",
    order = 201)]
public class EquipmentDatabaseSO : ScriptableObject
{
    [Header("전체 장비 목록 (44종)")]
    [Tooltip("모든 장비 SO를 여기에 드래그 등록. Editor 툴로 자동 채울 수 있음")]
    public List<EquipmentDefinitionSO> allEquipments = new List<EquipmentDefinitionSO>();
    
    private List<EquipmentDefinitionSO> commonCache;
    private List<EquipmentDefinitionSO> uncommonCache;
    private List<EquipmentDefinitionSO> rareCache;
    private List<EquipmentDefinitionSO> epicCache;
    private bool cacheBuilt;

    /// <summary>등급별 캐시 강제 재빌드. OnEnable에서 호출해도 되고 첫 접근 시 자동 빌드.</summary>
    public void BuildCache()
    {
        commonCache = new List<EquipmentDefinitionSO>();
        uncommonCache = new List<EquipmentDefinitionSO>();
        rareCache = new List<EquipmentDefinitionSO>();
        epicCache = new List<EquipmentDefinitionSO>();

        for (int i = 0; i < allEquipments.Count; i++)
        {
            var eq = allEquipments[i];
            if (eq == null) continue;

            switch (eq.rarity)
            {
                case EquipmentRarity.Common: commonCache.Add(eq); break;
                case EquipmentRarity.Uncommon: uncommonCache.Add(eq); break;
                case EquipmentRarity.Rare: rareCache.Add(eq); break;
                case EquipmentRarity.Epic: epicCache.Add(eq); break;
            }
        }
        cacheBuilt = true;
    }

    /// <summary>지정한 등급의 장비 풀을 반환. 가챠 2단계(등급 확정 후 등급 내 랜덤 선택)에서 사용.</summary>
    public List<EquipmentDefinitionSO> GetPool(EquipmentRarity rarity)
    {
        if (!cacheBuilt) BuildCache();

        switch (rarity)
        {
            case EquipmentRarity.Common: return commonCache;
            case EquipmentRarity.Uncommon: return uncommonCache;
            case EquipmentRarity.Rare: return rareCache;
            case EquipmentRarity.Epic: return epicCache;
            default: return null;
        }
    }

    /// <summary>ID로 장비 SO 검색. 저장 데이터 로드 시 사용.</summary>
    public EquipmentDefinitionSO FindById(string id)
    {
        for (int i = 0; i < allEquipments.Count; i++)
        {
            if (allEquipments[i] != null && allEquipments[i].equipmentId == id)
                return allEquipments[i];
        }
        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        cacheBuilt = false; // Inspector 수정 시 캐시 무효화
    }
#endif
}