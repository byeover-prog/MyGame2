using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Equipment_",
    menuName = "혼령검/장비/장비 정의 SO",
    order = 100)]
public class EquipmentDefinitionSO : ScriptableObject
{
    // 식별자

    [Header("식별자")]
    [Tooltip("고유 ID (예: E1, R6, U3, C12). 저장/로드 및 중복 판정 키")]
    public string equipmentId;

    [Tooltip("표시 이름 (예: 시전 주문의 격서)")]
    public string equipmentName;

    [Tooltip("장비 등급 — 가챠 확률과 환산 냥 계산의 기준")]
    public EquipmentRarity rarity;
    
    // 표시용

    [Header("표시용")]
    [Tooltip("아이템 아이콘 (상점 / 인벤토리 / 장착 슬롯에 표시)")]
    public Sprite icon;

    [TextArea(2, 4)]
    [Tooltip("아이템 설명문 (툴팁에 표시). 예: '기본 스킬의 시전 횟수를 2회 증가시킨다.'")]
    public string description;

    [TextArea(1, 3)]
    [Tooltip("플레이버 텍스트 (이탤릭 서브문구, 선택 사항)")]
    public string flavorText;
    
    // 효과

    [Header("효과 목록")]
    [Tooltip("이 장비가 장착 시 부여하는 효과들. 단일 효과 아이템은 1개, 디메리트 아이템은 2개 이상")]
    public List<EquipmentEffect> effects = new List<EquipmentEffect>();
    
    // 검증 헬퍼 (Editor 전용 경고)

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(equipmentId))
            Debug.LogWarning($"[EquipmentDefinitionSO] equipmentId가 비어있습니다: {name}", this);

        if (effects == null || effects.Count == 0)
            Debug.LogWarning($"[EquipmentDefinitionSO] 효과가 하나도 없습니다: {equipmentName}", this);
    }
#endif
}