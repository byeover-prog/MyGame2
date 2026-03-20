using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/캐릭터/캐릭터 정의", fileName = "SO_Character_")]
public sealed class CharacterDefinitionSO : ScriptableObject
{
    [Header("식별자(변경 금지)")]
    [Tooltip("저장/런타임에서 캐릭터를 구분하는 고유 ID입니다. 출시 후 변경하면 세이브가 깨질 수 있습니다.")]
    [SerializeField] private string characterId;

    [Header("표시")]
    [Tooltip("로비/편성/UI Toolkit 화면에 노출할 캐릭터 이름입니다.")]
    [SerializeField] private string displayName;

    [Tooltip("캐릭터 초상화입니다.")]
    [SerializeField] private Sprite portrait;

    [Header("아이콘")]
    [Tooltip("기본 스킬 아이콘(상단 슬롯 왼쪽)")]
    [SerializeField] private Sprite basicSkillIcon;

    [Tooltip("궁극기 아이콘(상단 슬롯 오른쪽)")]
    [SerializeField] private Sprite ultimateSkillIcon;

    [Header("속성")]
    [Tooltip("캐릭터 대표 속성입니다.")]
    [SerializeField] private CharacterAttributeKind attribute = CharacterAttributeKind.None;

    [Header("메타 진행")]
    [Tooltip("체크 시 저장 데이터가 없어도 처음부터 사용 가능한 캐릭터입니다.")]
    [SerializeField] private bool unlockedByDefault = true;

    [Tooltip("전투 시작 시 이 캐릭터의 기본 능력치를 적용할 프로필입니다.")]
    [SerializeField] private PlayerBaseStatProfileSO baseStatProfile;

    [Tooltip("캐릭터 영구 레벨 1~50 성장 규칙입니다. 비우면 기본 곡선을 사용합니다.")]
    [SerializeField] private CharacterLevelCurveSO levelCurve;

    [Tooltip("아웃게임 강화 트리입니다. 비우면 런타임 기본 트리를 사용합니다.")]
    [SerializeField] private CharacterUpgradeTreeSO upgradeTree;

    [Header("스킬 식별자")]
    [Tooltip("캐릭터 기본 스킬 ID입니다. 기본기 강화 분기에서 사용합니다.")]
    [SerializeField] private string basicSkillId;

    [Tooltip("캐릭터 궁극기 ID입니다. 궁극기 강화 분기에서 사용합니다.")]
    [SerializeField] private string ultimateSkillId;

    public string CharacterId => characterId;
    public string DisplayName => displayName;
    public Sprite Portrait => portrait;
    public Sprite BasicSkillIcon => basicSkillIcon;
    public Sprite UltimateSkillIcon => ultimateSkillIcon;
    public CharacterAttributeKind Attribute => attribute;
    public bool UnlockedByDefault => unlockedByDefault;
    public PlayerBaseStatProfileSO BaseStatProfile => baseStatProfile;
    public CharacterLevelCurveSO LevelCurve => levelCurve;
    public CharacterUpgradeTreeSO UpgradeTree => upgradeTree;
    public string BasicSkillId => basicSkillId;
    public string UltimateSkillId => ultimateSkillId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 실수 방지용 최소 안전장치
        if (!string.IsNullOrWhiteSpace(characterId)) return;
        // 파일명에서 ID를 자동으로 잡아주고 싶으면 여기서 확장 가능.
    }
#endif
}
