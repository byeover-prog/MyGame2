using UnityEngine;

[CreateAssetMenu(menuName = "혼령검/메타/상점 아이템", fileName = "ShopItem_")]
public sealed class ShopItemSO : ScriptableObject
{
    [Header("아이템 식별")]
    [Tooltip("아이템 고유 ID입니다.")]
    [SerializeField] private string itemId;

    [Tooltip("상점에 표시되는 아이템 이름입니다.")]
    [SerializeField] private string displayName;

    [Tooltip("아이템 효과 설명입니다.")]
    [TextArea(1, 3)]
    [SerializeField] private string description;

    [Tooltip("상점/인벤토리에 표시되는 아이콘입니다.")]
    [SerializeField] private Sprite icon;

    [Header("효과")]
    [Tooltip("적용할 아웃게임 강화 종류입니다.")]
    [SerializeField] private OutgameModifierKind2D modifierKind;

    [Tooltip("효과 수치입니다. (예: +20이면 20 입력)")]
    [SerializeField] private float modifierValue;

    [Header("구매")]
    [Tooltip("구매 비용 (냥)입니다.")]
    [SerializeField] private int cost;

    [Tooltip("캐릭터당 최대 보유 개수입니다.")]
    [SerializeField] private int maxPerCharacter = 1;

    // ─── 프로퍼티 ───
    public string ItemId => itemId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public OutgameModifierKind2D ModifierKind => modifierKind;
    public float ModifierValue => modifierValue;
    public int Cost => cost;
    public int MaxPerCharacter => maxPerCharacter;

    /// <summary>효과 수치를 사람이 읽을 수 있는 형태로 포맷합니다.</summary>
    public string FormatEffect()
    {
        switch (modifierKind)
        {
            case OutgameModifierKind2D.CastCountFlat:
            case OutgameModifierKind2D.DefenseFlat:
            case OutgameModifierKind2D.HpRegenFlat:
            case OutgameModifierKind2D.SkillAccelerationFlat:
            case OutgameModifierKind2D.MaxHpFlat:
                return $"+{modifierValue:0}";

            case OutgameModifierKind2D.CooldownReductionPercent:
                return $"-{modifierValue:0}%";

            default:
                return $"+{modifierValue:0}%";
        }
    }
}