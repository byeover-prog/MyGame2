using UnityEngine;

/// <summary>
/// 개별 캐릭터의 기본 정의입니다.
/// 윤설, 하율, 하린 각각 하나의 에셋으로 만들어 CharacterCatalogSO에 등록합니다.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/메타/캐릭터 정의", fileName = "CharacterDefinition_")]
public sealed class CharacterDefinitionSO : ScriptableObject
{
    // ─── 식별 ──────────────────────────────────────────

    [Header("식별")]
    [Tooltip("저장·코드에서 사용하는 고유 ID입니다. 예: yunseol, hayul, harin")]
    [SerializeField] private string characterId;

    [Tooltip("UI에 표시할 한글 이름입니다.")]
    [SerializeField] private string displayName;

    // ─── 속성 ──────────────────────────────────────────

    [Header("속성")]
    [Tooltip("이 캐릭터의 기본 속성입니다. (기존 CharacterAttributeKind 사용)")]
    [SerializeField] private CharacterAttributeKind attribute = CharacterAttributeKind.None;

    // ─── 비주얼 ────────────────────────────────────────

    [Header("비주얼")]
    [Tooltip("편성 화면 등에 표시할 초상화 스프라이트입니다.")]
    [SerializeField] private Sprite portrait;

    [Tooltip("클리어 UI 등에 표시할 썸네일 스프라이트입니다.")]
    [SerializeField] private Sprite thumbnail;

    [Tooltip("Player의 Idle 상태 스프라이트입니다.")]
    [SerializeField] private Sprite playerIdleSprite;

    [Tooltip("Player의 Animator Controller입니다.")]
    [SerializeField] private RuntimeAnimatorController animatorController;

    // ─── 스킬/궁극기 ──────────────────────────────────

    [Header("기본 스킬")]
    [Tooltip("기본 스킬 아이콘입니다.")]
    [SerializeField] private Sprite basicSkillIcon;

    [Tooltip("기본 스킬 ID입니다. 예: weapon_balsi, weapon_nakroebu, weapon_jwagyekyose")]
    [SerializeField] private string basicSkillId;

    [Tooltip("이 캐릭터의 시작 스킬(무기) Config SO입니다.")]
    [SerializeField] private CommonSkillConfigSO startingSkill;

    [Header("궁극기")]
    [Tooltip("궁극기 아이콘입니다.")]
    [SerializeField] private Sprite ultimateSkillIcon;

    [Tooltip("궁극기 스킬 ID입니다. 예: ult_hokhan, ult_cheongang, ult_wolgwang")]
    [SerializeField] private string ultimateSkillId;

    [Tooltip("궁극기 데이터 SO입니다.")]
    [SerializeField] private UltimateDataSO ultimateData;

    [Tooltip("궁극기 실행 로직 프리팹입니다.")]
    [SerializeField] private GameObject ultimateResolverPrefab;

    // ─── 지원 캐릭터 전용 ──────────────────────────────

    [Header("지원 캐릭터 전용")]
    [Tooltip("지원 궁극기 사용 시 데미지 배율입니다. (예: 0.6 = 60%)")]
    [SerializeField] private float supportDamageMultiplier = 0.6f;

    [Tooltip("지원 캐릭터가 T키로 등장할 때 사용하는 비주얼 프리팹입니다.")]
    [SerializeField] private GameObject supportVisualPrefab;

    // ─── 능력치 ────────────────────────────────────────

    [Header("능력치 프로파일")]
    [Tooltip("이 캐릭터의 기본 능력치 SO입니다. (기존 PlayerBaseStatProfileSO 연결)")]
    [SerializeField] private PlayerBaseStatProfileSO baseStatProfile;

    // ─── 아웃게임 강화 ────────────────────────────────

    [Header("아웃게임 강화")]
    [Tooltip("캐릭터별 강화 트리 SO입니다. 비워두면 런타임 기본 트리가 생성됩니다.")]
    [SerializeField] private CharacterUpgradeTreeSO upgradeTree;

    // ─── 해금 ──────────────────────────────────────────

    [Header("해금 조건")]
    [Tooltip("true면 게임 시작 시 기본 해금 상태입니다.")]
    [SerializeField] private bool unlockedByDefault = false;

    // ─── 프로퍼티 ──────────────────────────────────────

    public string CharacterId => characterId;
    public string DisplayName => displayName;

    /// <summary>캐릭터 속성입니다. 기존 CharacterAttributeKind 열거형을 사용합니다.</summary>
    public CharacterAttributeKind Attribute => attribute;

    public Sprite Portrait => portrait;
    public Sprite Thumbnail => thumbnail;
    public Sprite PlayerIdleSprite => playerIdleSprite;
    public RuntimeAnimatorController AnimatorController => animatorController;

    public Sprite BasicSkillIcon => basicSkillIcon;
    public string BasicSkillId => basicSkillId;
    public CommonSkillConfigSO StartingSkill => startingSkill;

    public Sprite UltimateSkillIcon => ultimateSkillIcon;
    public string UltimateSkillId => ultimateSkillId;
    public UltimateDataSO UltimateData => ultimateData;
    public GameObject UltimateResolverPrefab => ultimateResolverPrefab;

    public float SupportDamageMultiplier => supportDamageMultiplier;
    public GameObject SupportVisualPrefab => supportVisualPrefab;

    public PlayerBaseStatProfileSO BaseStatProfile => baseStatProfile;
    public CharacterUpgradeTreeSO UpgradeTree => upgradeTree;
    public bool UnlockedByDefault => unlockedByDefault;
}