// AwakeningRecipeSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Awakening/Awakening Recipe")]
public sealed class AwakeningRecipeSO : ScriptableObject
{
    [Header("기본 무기")]
    [SerializeField] private string baseWeaponId;
    [SerializeField] private int baseRequiredLevel = 8;

    [Header("각성 결과")]
    [SerializeField] private string awakenedWeaponId;
    [SerializeField] private WeaponDefinitionSO awakenedWeaponDefinition;

    [Header("추가 조건(무기/레벨)")]
    [SerializeField] private AwakeningRequirement[] requirements;

    [Header("표시용(카드 UI)")]
    [SerializeField] private string displayName;
    [TextArea(2, 4)]
    [SerializeField] private string description;

    public string BaseWeaponId => baseWeaponId;
    public int BaseRequiredLevel => baseRequiredLevel;
    public string AwakenedWeaponId => awakenedWeaponId;
    public WeaponDefinitionSO AwakenedWeaponDefinition => awakenedWeaponDefinition;
    public AwakeningRequirement[] Requirements => requirements;
    public string DisplayName => displayName;
    public string Description => description;
}