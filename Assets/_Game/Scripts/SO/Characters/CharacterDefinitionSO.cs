using System;
using UnityEngine;

[CreateAssetMenu(menuName = "혼령검/메타/캐릭터 정의", fileName = "CharacterDefinition_")]
public sealed class CharacterDefinitionSO : ScriptableObject
{
    // ─── 식별 ───────────────────────────────────────────

    [Header("식별")]
    [Tooltip("저장·코드에서 사용하는 고유 ID입니다. 예: yunseol, hayul, harin")]
    [SerializeField] private string characterId;

    [Tooltip("UI에 표시할 한글 이름입니다.")]
    [SerializeField] private string displayName;

    // ─── 속성 ───────────────────────────────────────────

    [Header("속성")]
    [Tooltip("이 캐릭터의 기본 속성입니다. (기존 CharacterAttributeKind 사용)")]
    [SerializeField] private CharacterAttributeKind attribute = CharacterAttributeKind.None;

    // ─── 비주얼 ─────────────────────────────────────────

    [Header("비주얼")]
    [Tooltip("편성 화면 등에 표시할 초상화 스프라이트입니다.")]
    [SerializeField] private Sprite portrait;

    [Tooltip("클리어 UI 등에 표시할 썸네일 스프라이트입니다.")]
    [SerializeField] private Sprite thumbnail;

    [Tooltip("Player의 Idle 상태 스프라이트입니다.")]
    [SerializeField] private Sprite playerIdleSprite;

    [Tooltip("Player의 Animator Controller입니다.")]
    [SerializeField] private RuntimeAnimatorController animatorController;

    // ─── 기본 스킬 ──────────────────────────────────────

    [Header("기본 스킬")]
    [Tooltip("기본/대표 아이콘입니다. 모양별 슬롯이 비어있을 때 fallback으로 사용됩니다.")]
    [SerializeField] private Sprite basicSkillIcon;

    [Tooltip("[다이아몬드 슬롯 전용] 레벨업 카드 등에서 사용. 비워두면 BasicSkillIcon으로 자동 fallback.")]
    [SerializeField] private Sprite basicSkillIconDiamond;

    [Tooltip("[정사각형 슬롯 전용] 인게임 HUD 8칸 슬롯에서 사용. 비워두면 BasicSkillIcon으로 자동 fallback.")]
    [SerializeField] private Sprite basicSkillIconSquare;

    [Tooltip("기본 스킬 ID입니다. 예: weapon_balsi, weapon_nakroebu, weapon_jwagyekyose")]
    [SerializeField] private string basicSkillId;

    [Tooltip("이 캐릭터의 시작 스킬(무기) Config SO입니다.")]
    [SerializeField] private CommonSkillConfigSO startingSkill;

    // ─── 궁극기 ─────────────────────────────────────────

    [Header("궁극기")]
    [Tooltip("기본/대표 아이콘입니다. 모양별 슬롯이 비어있을 때 fallback으로 사용됩니다.")]
    [SerializeField] private Sprite ultimateSkillIcon;

    [Tooltip("[다이아몬드 슬롯 전용] R/T HUD 슬롯, 레벨업 카드, 편성 화면 등에서 사용. 비워두면 UltimateSkillIcon으로 자동 fallback.")]
    [SerializeField] private Sprite ultimateSkillIconDiamond;

    [Tooltip("[정사각형 슬롯 전용] 향후 정사각형 슬롯에서 표시할 경우 사용. 비워두면 UltimateSkillIcon으로 자동 fallback.")]
    [SerializeField] private Sprite ultimateSkillIconSquare;

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

    [Header("지원 버프 (T키 궁극기 시 메인 캐릭터에게 부여)")]
    [Tooltip("이 캐릭터가 지원일 때 메인에게 주는 버프입니다.")]
    [SerializeField] private SupportBuffData2D supportBuff = new SupportBuffData2D
    {
        kind = SupportBuffKind2D.None,
        value = 0f,
        duration = 10f
    };

    [Header("지원 등장 연출")]
    [Tooltip("이 캐릭터의 등장/퇴장 연출 설정입니다. 비워두면 기본값을 사용합니다.")]
    [SerializeField] private SupportLandingConfigSO supportLandingConfig;

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

    // ═══════════════════════════════════════════════════
    //  프로퍼티
    // ═══════════════════════════════════════════════════

    public string CharacterId => characterId;
    public string DisplayName => displayName;
    public CharacterAttributeKind Attribute => attribute;
    public Sprite Portrait => portrait;
    public Sprite Thumbnail => thumbnail;
    public Sprite PlayerIdleSprite => playerIdleSprite;
    public RuntimeAnimatorController AnimatorController => animatorController;

    /// <summary>기본/대표 아이콘. 어떤 슬롯 모양에도 fallback으로 쓰입니다.</summary>
    public Sprite BasicSkillIcon => basicSkillIcon;

    /// <summary>다이아몬드 슬롯에 표시할 기본 스킬 아이콘. 없으면 BasicSkillIcon으로 fallback.</summary>
    public Sprite BasicSkillIconDiamond => basicSkillIconDiamond != null ? basicSkillIconDiamond : basicSkillIcon;

    /// <summary>정사각형 슬롯에 표시할 기본 스킬 아이콘. 없으면 BasicSkillIcon으로 fallback.</summary>
    public Sprite BasicSkillIconSquare => basicSkillIconSquare != null ? basicSkillIconSquare : basicSkillIcon;

    public string BasicSkillId => basicSkillId;
    public CommonSkillConfigSO StartingSkill => startingSkill;

    /// <summary>기본/대표 궁극기 아이콘. 어떤 슬롯 모양에도 fallback으로 쓰입니다.</summary>
    public Sprite UltimateSkillIcon => ultimateSkillIcon;

    /// <summary>다이아몬드 슬롯에 표시할 궁극기 아이콘. 없으면 UltimateSkillIcon으로 fallback.</summary>
    public Sprite UltimateSkillIconDiamond => ultimateSkillIconDiamond != null ? ultimateSkillIconDiamond : ultimateSkillIcon;

    /// <summary>정사각형 슬롯에 표시할 궁극기 아이콘. 없으면 UltimateSkillIcon으로 fallback.</summary>
    public Sprite UltimateSkillIconSquare => ultimateSkillIconSquare != null ? ultimateSkillIconSquare : ultimateSkillIcon;

    public string UltimateSkillId => ultimateSkillId;
    public UltimateDataSO UltimateData => ultimateData;
    public GameObject UltimateResolverPrefab => ultimateResolverPrefab;
    public float SupportDamageMultiplier => supportDamageMultiplier;
    public GameObject SupportVisualPrefab => supportVisualPrefab;
    public SupportBuffData2D SupportBuff => supportBuff;

    /// <summary>이 캐릭터의 등장/퇴장 연출 설정입니다. null이면 기본값을 사용합니다.</summary>
    public SupportLandingConfigSO SupportLandingConfig => supportLandingConfig;

    public PlayerBaseStatProfileSO BaseStatProfile => baseStatProfile;
    public CharacterUpgradeTreeSO UpgradeTree => upgradeTree;
    public bool UnlockedByDefault => unlockedByDefault;
}

/// <summary>
/// 지원 궁극기 발동 시 메인 캐릭터에게 부여하는 버프 데이터입니다.
/// </summary>
[Serializable]
public struct SupportBuffData2D
{
    [Tooltip("버프 종류입니다.")]
    public SupportBuffKind2D kind;

    [Tooltip("버프 수치입니다. (%, 고정값 등 종류에 따라 해석)")]
    public float value;

    [Tooltip("버프 지속시간(초)입니다.")]
    public float duration;

    /// <summary>유효한 버프 데이터인지 확인합니다.</summary>
    public bool IsValid => kind != SupportBuffKind2D.None && value > 0f && duration > 0f;
}