using UnityEngine;

/// <summary>
/// 퀘스트 보상으로 획득하는 스킬 각성 효과의 정의입니다.
/// 각성은 특정 스킬에 추가 효과를 부여합니다.
///
/// [SO 에셋 생성]
/// Project 창 → 우클릭 → Create → 혼령검 → 각성 → 스킬 각성 효과
///
/// [Inspector 설정]
///   Awakening Id: "awk_arrow_frost" 등 고유 ID
///   Target Skill Id: "weapon_arrow" 등 대상 스킬 ID
///   나머지 필드는 각성 효과에 따라 설정
/// </summary>
[CreateAssetMenu(menuName = "혼령검/각성/스킬 각성 효과", fileName = "Awakening_")]
public sealed class SkillAwakeningSO : ScriptableObject
{
    [Header("각성 식별")]
    [Tooltip("각성 효과 고유 ID입니다.")]
    [SerializeField] private string awakeningId;

    [Tooltip("각성 이름 (한글)입니다.")]
    [SerializeField] private string displayName;

    [Tooltip("각성 설명입니다.")]
    [TextArea(2, 4)]
    [SerializeField] private string description;

    [Tooltip("각성 아이콘입니다.")]
    [SerializeField] private Sprite icon;

    [Header("대상 스킬")]
    [Tooltip("이 각성이 적용되는 스킬 ID입니다.")]
    [SerializeField] private string targetSkillId;

    [Tooltip("대상 캐릭터 ID입니다. (이 캐릭터가 메인일 때만 활성화)")]
    [SerializeField] private string targetCharacterId;

    [Header("각성 효과 — 수치")]
    [Tooltip("피해량 추가 증가 (%)입니다.")]
    [SerializeField] private float damageBoostPercent;

    [Tooltip("추가 투사체 수입니다.")]
    [SerializeField] private int extraProjectiles;

    [Tooltip("추가 범위 증가 (%)입니다.")]
    [SerializeField] private float areaBoostPercent;

    [Tooltip("추가 쿨타임 감소 (%)입니다.")]
    [SerializeField] private float cooldownReductionPercent;

    [Header("각성 효과 — 특수")]
    [Tooltip("특수 효과 키입니다. 코드에서 switch 분기로 처리합니다.")]
    [SerializeField] private string specialEffectKey;

    [Tooltip("특수 효과의 수치 파라미터입니다.")]
    [SerializeField] private float specialEffectValue;

    // ─── 프로퍼티 ───
    public string AwakeningId => awakeningId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public string TargetSkillId => targetSkillId;
    public string TargetCharacterId => targetCharacterId;
    public float DamageBoostPercent => damageBoostPercent;
    public int ExtraProjectiles => extraProjectiles;
    public float AreaBoostPercent => areaBoostPercent;
    public float CooldownReductionPercent => cooldownReductionPercent;
    public string SpecialEffectKey => specialEffectKey;
    public float SpecialEffectValue => specialEffectValue;

    /// <summary>수치 효과가 하나라도 있는지 확인합니다.</summary>
    public bool HasNumericEffect =>
        damageBoostPercent != 0f ||
        extraProjectiles != 0 ||
        areaBoostPercent != 0f ||
        cooldownReductionPercent != 0f;

    /// <summary>특수 효과가 정의되어 있는지 확인합니다.</summary>
    public bool HasSpecialEffect => !string.IsNullOrWhiteSpace(specialEffectKey);
}