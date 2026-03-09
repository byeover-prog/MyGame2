using System;
using UnityEngine;

public enum SkillDefinitionType
{
    CommonSkill,     // 공통 스킬(예: CommonSkillConfigSO)
    Weapon,          // 무기(예: WeaponDefinitionSO)
    Passive,         // 패시브(예: PassiveConfigSO 등)
    CharacterSkill,  // 캐릭터 전용 스킬(예: CharacterSkillSO 등)
    Other
}

/// <summary>
/// 스킬/무기/패시브 등을 "카드/카탈로그/덱" 시스템에서 공통으로 다루기 위한 "정의 SO".
///
/// 의도
/// - UI(아이콘/표시명/설명)와 실제 로직 데이터(연결된 SO)를 분리해, 카드 시스템이 통일된 형태로 접근하게 한다.
/// - 연결된 실제 데이터는 linkedAsset에 꽂는다. (CommonSkillConfigSO, WeaponDefinitionSO 등)
///
/// 주의
/// - 이 SO는 "정의(Definition)" 역할만 담당. 실제 적용 로직은 linkedAsset 쪽 시스템이 수행.
/// - GUID 복구는 스크립트로 못 한다. (삭제된 .asset/.meta가 없으면 기존 참조는 Missing 상태로 남음)
///
/// 복잡도
/// - Getter/Validate만 제공: O(1)
/// </summary>
[CreateAssetMenu(menuName = "그날이후/SO/스킬 정의(SkillDefinition)", fileName = "SkillDef_")]
public sealed class SkillDefinitionSO : ScriptableObject
{
    [Header("분류")]
    [Tooltip("카드/덱/카탈로그에서의 분류용")]
    public SkillDefinitionType definitionType = SkillDefinitionType.Other;

    [Header("식별자")]
    [Tooltip("프로젝트 내 유니크 키(비워도 동작 가능). 예: 'CS_ARROW_RAIN', 'WP_BULLET'")]
    public string id;

    [Header("표시 정보")]
    [Tooltip("UI 표기용 이름(한글)")]
    public string titleKr = "스킬";

    [Tooltip("UI 표기용 간단 설명(레벨 표기 금지). 예: '가장 가까운 적을 향해 관통 탄을 발사합니다.'")]
    [TextArea]
    public string descriptionKr;

    public Sprite icon;

    [Header("연결된 실제 데이터(SO)")]
    [Tooltip("실제 스킬/무기/패시브 설정 SO를 연결.\n예: CommonSkillConfigSO, WeaponDefinitionSO 등")]
    public ScriptableObject linkedAsset;

    [Header("카드/덱 태그(선택)")]
    [Tooltip("덱 필터링/시너지용 태그. 예: '투사체', '범위', '냉기'")]
    public string[] tags;

    [Header("레벨 상한(선택)")]
    [Tooltip("이 정의가 카드에서 보여줄 최대 레벨. 0이면 linkedAsset(또는 외부 시스템) 기준을 따르게 권장.")]
    [Min(0)]
    public int maxLevelOverride = 0;

    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag) || tags == null) return false;
        for (int i = 0; i < tags.Length; i++)
        {
            if (string.Equals(tags[i], tag, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(titleKr))
            titleKr = name;

        // id가 비어있으면 강제하진 않지만, 흔한 실수를 줄이기 위해 자동 생성
        if (string.IsNullOrWhiteSpace(id))
            id = name;

        if (maxLevelOverride < 0)
            maxLevelOverride = 0;
    }
#endif
}