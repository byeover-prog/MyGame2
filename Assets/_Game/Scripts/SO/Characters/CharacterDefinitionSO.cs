using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/캐릭터/캐릭터 정의", fileName = "SO_Character_")]
public sealed class CharacterDefinitionSO : ScriptableObject
{
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

    public string CharacterId => characterId;
    public string DisplayName => displayName;
    public Sprite Portrait => portrait;
    public Sprite BasicSkillIcon => basicSkillIcon;
    public Sprite UltimateSkillIcon => ultimateSkillIcon;
    public CharacterAttributeKind Attribute => attribute;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 실수 방지용 최소 안전장치
        if (!string.IsNullOrWhiteSpace(characterId)) return;
        // 파일명에서 ID를 자동으로 잡아주고 싶으면 여기서 확장 가능.
    }
#endif
}
