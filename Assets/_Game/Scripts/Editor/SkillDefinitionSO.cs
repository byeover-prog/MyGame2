using UnityEngine;

public enum SkillKind
{
    Weapon,
    CommonSkill,
    CharacterSkill
}

[CreateAssetMenu(menuName = "그날이후/SO/스킬 정의", fileName = "SO_Skill_")]
public sealed class SkillDefinitionSO : ScriptableObject
{
    [Header("식별")]
    [SerializeField] private string skillId = "skill_id";

    [Header("종류")]
    [SerializeField] private SkillKind kind = SkillKind.CommonSkill;

    [Header("표시")]
    [SerializeField] private string displayName = "스킬 이름";
    [SerializeField, TextArea] private string description = "설명";
    [SerializeField] private Sprite icon;

    [Header("게임플레이(기본값)")]
    [Min(1)] [SerializeField] private int maxLevel = 8;
    [Min(0f)] [SerializeField] private float baseCooldown = 1.0f;
    [Min(0)] [SerializeField] private int baseProjectileCount = 1;

    [Header("프리팹(무기/투사체)")]
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private GameObject projectilePrefab;

    public string SkillId => skillId;
    public SkillKind Kind => kind;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;

    public int MaxLevel => maxLevel;
    public float BaseCooldown => baseCooldown;
    public int BaseProjectileCount => baseProjectileCount;

    public GameObject WeaponPrefab => weaponPrefab;
    public GameObject ProjectilePrefab => projectilePrefab;
}