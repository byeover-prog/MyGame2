using UnityEngine;

/// <summary>
/// 캐릭터 1명의 전체 정의 데이터.
/// UI 표시 + 전투 데이터(기본 스킬, 궁극기, 패시브, 비주얼)를 모두 담는다.
///
/// [기존 필드] — 이름/순서 변경 금지 (Inspector 참조 유지)
/// characterId, displayName, portrait, basicSkillIcon, thumbnail,
/// ultimateSkillIcon, attribute
///
/// [신규 필드] — 기존 뒤에 추가
/// 기본 스킬, 궁극기 SO, 패시브, 캐릭터 프리팹 등
/// </summary>
[CreateAssetMenu(menuName = "그날이후/캐릭터/캐릭터 정의", fileName = "SO_Character_")]
public sealed class CharacterDefinitionSO : ScriptableObject
{
    // ═══════════════════════════════════════════════════════
    //  기존 필드 (순서/이름 변경 금지 — Inspector 참조 유지)
    // ═══════════════════════════════════════════════════════

    [Header("식별자(변경 금지)")]
    [Tooltip("저장/런타임에서 캐릭터를 구분하는 고유 ID입니다. 출시 후 변경하면 세이브가 깨질 수 있습니다.")]
    [SerializeField] private string characterId;

    [Header("표시")]
    [SerializeField] private string displayName;
    [SerializeField] private Sprite portrait;

    [Header("아이콘")]
    [Tooltip("기본 스킬 아이콘(상단 슬롯 왼쪽)")]
    [SerializeField] private Sprite basicSkillIcon;
    [Tooltip("ClearUI 스쿼드 패널용 상체 썸네일")]
    [SerializeField] private Sprite thumbnail;
    public Sprite Thumbnail => thumbnail;

    [Tooltip("궁극기 아이콘(상단 슬롯 오른쪽)")]
    [SerializeField] private Sprite ultimateSkillIcon;

    [Header("속성")]
    [SerializeField] private CharacterAttributeKind attribute = CharacterAttributeKind.None;

    // ═══════════════════════════════════════════════════════
    //  기존 프로퍼티 (변경 없음)
    // ═══════════════════════════════════════════════════════

    public string CharacterId => characterId;
    public string DisplayName => displayName;
    public Sprite Portrait => portrait;
    public Sprite BasicSkillIcon => basicSkillIcon;
    public Sprite UltimateSkillIcon => ultimateSkillIcon;
    public CharacterAttributeKind Attribute => attribute;

    // ═══════════════════════════════════════════════════════
    //  신규: 기본 스킬
    // ═══════════════════════════════════════════════════════

    [Header("기본 스킬")]
    [Tooltip("이 캐릭터가 메인일 때 시작하는 기본 스킬 SO (CommonSkillConfigSO)")]
    [SerializeField] private CommonSkillConfigSO startingSkill;

    public CommonSkillConfigSO StartingSkill => startingSkill;

    // ═══════════════════════════════════════════════════════
    //  신규: 궁극기
    // ═══════════════════════════════════════════════════════

    [Header("궁극기")]
    [Tooltip("이 캐릭터의 궁극기 데이터 SO")]
    [SerializeField] private UltimateDataSO ultimateData;

    [Tooltip("이 캐릭터 궁극기 Resolver 프리팹.\n" +
             "UltimateResolverBase를 상속한 컴포넌트가 붙은 프리팹을 연결.\n" +
             "런타임에 Instantiate 후 사용.")]
    [SerializeField] private GameObject ultimateResolverPrefab;

    public UltimateDataSO UltimateData => ultimateData;
    public GameObject UltimateResolverPrefab => ultimateResolverPrefab;

    // ═══════════════════════════════════════════════════════
    //  신규: 패시브
    // ═══════════════════════════════════════════════════════

    [Header("패시브")]
    [Tooltip("캐릭터 고유 패시브 이름 (한글)")]
    [SerializeField] private string passiveName;

    [Tooltip("캐릭터 고유 패시브 설명 (한글)")]
    [TextArea(2, 4)]
    [SerializeField] private string passiveDescription;

    public string PassiveName => passiveName;
    public string PassiveDescription => passiveDescription;

    // ═══════════════════════════════════════════════════════
    //  신규: 지원 궁극기 보정
    // ═══════════════════════════════════════════════════════

    [Header("지원 궁극기 보정")]
    [Tooltip("지원 캐릭터로 궁극기 사용 시 데미지 배율 (0.55 = 55%)")]
    [SerializeField] private float supportDamageMultiplier = 0.55f;

    [Tooltip("지원 궁극기 사용 시 메인 캐릭터에게 주는 버프 타입")]
    [SerializeField] private SupportBuffType supportBuffType = SupportBuffType.None;

    [Tooltip("지원 버프 수치 (공격력%/흡혈비율/스킬가속 등)")]
    [SerializeField] private float supportBuffValue;

    [Tooltip("지원 버프 지속시간 (초)")]
    [SerializeField] private float supportBuffDuration = 10f;

    public float SupportDamageMultiplier => supportDamageMultiplier;
    public SupportBuffType SupportBuff => supportBuffType;
    public float SupportBuffValue => supportBuffValue;
    public float SupportBuffDuration => supportBuffDuration;

    // ═══════════════════════════════════════════════════════
    //  신규: 캐릭터 비주얼
    // ═══════════════════════════════════════════════════════

    [Header("캐릭터 비주얼")]
    [Tooltip("메인 캐릭터일 때 사용할 기본 Idle 스프라이트.\n" +
             "Animator Controller가 없거나 교체 전 기본 표시용.")]
    [SerializeField] private Sprite playerIdleSprite;

    [Tooltip("지원 궁극기 연출 시 생성할 캐릭터 비주얼 프리팹")]
    [SerializeField] private GameObject supportVisualPrefab;

    [Tooltip("캐릭터 Animator Controller (메인/지원 공용)")]
    [SerializeField] private RuntimeAnimatorController animatorController;

    public Sprite PlayerIdleSprite => playerIdleSprite;
    public GameObject SupportVisualPrefab => supportVisualPrefab;
    public RuntimeAnimatorController AnimatorController => animatorController;

    // ═══════════════════════════════════════════════════════
    //  편의 메서드
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// CharacterAttributeKind → DamageElement2D 변환.
    /// 데미지 시스템에서 속성을 전달할 때 사용.
    /// </summary>
    public DamageElement2D GetDamageElement()
    {
        return attribute.ToDamageElement();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(characterId)) return;
    }
#endif
}

/// <summary>
/// 지원 궁극기 사용 시 메인 캐릭터에게 주는 버프 타입.
/// </summary>
public enum SupportBuffType
{
    None = 0,
    /// <summary>공격력 증가 (%) — 윤설 지원</summary>
    AttackPower = 1,
    /// <summary>모든 피해 흡혈 (비율) — 하린 지원</summary>
    Omnivamp = 2,
    /// <summary>스킬 가속 증가 — 하율 지원</summary>
    SkillHaste = 3,
}