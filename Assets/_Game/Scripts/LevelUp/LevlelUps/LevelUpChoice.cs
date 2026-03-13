using UnityEngine;
using _Game.Skills;

public enum LevelUpChoiceType
{
    WeaponSkill = 0,
    CommonSkill = 1,
    Passive = 2
}

public sealed class LevelUpChoice
{
    public LevelUpChoiceType Type { get; }

    public WeaponDefinitionSO Weapon { get; }
    public CommonSkillConfigSO CommonSkill { get; }
    public PassiveConfigSO Passive { get; }
    public SkillDefinitionSO PassiveDefinition { get; }

    public int NextLevel { get; }
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public string Tag { get; }
    public Sprite Icon { get; }

    public LevelUpChoice(WeaponDefinitionSO weapon, int nextLevel, string title, string description, string tagKr, Sprite icon)
    {
        Type = LevelUpChoiceType.WeaponSkill;
        Weapon = weapon;
        NextLevel = nextLevel;
        Id = weapon != null ? weapon.Id : "";
        Title = title;
        Description = description;
        Tag = tagKr;
        Icon = icon;
    }

    public LevelUpChoice(CommonSkillConfigSO common, int nextLevel, string title, string description, string tagKr, Sprite icon)
    {
        Type = LevelUpChoiceType.CommonSkill;
        CommonSkill = common;
        NextLevel = nextLevel;
        Id = common != null ? common.kind.ToString() : "";
        Title = title;
        Description = description;
        Tag = tagKr;
        Icon = icon;
    }

    public LevelUpChoice(PassiveConfigSO passive, int nextLevel, string title, string description, string tagKr, Sprite icon)
    {
        Type = LevelUpChoiceType.Passive;
        Passive = passive;
        NextLevel = nextLevel;
        Id = passive != null ? passive.kind.ToString() : "";
        Title = title;
        Description = description;
        Tag = tagKr;
        Icon = icon;
    }

    // ★ SkillDefinitionSO 기반 패시브 생성자
    public LevelUpChoice(SkillDefinitionSO passiveDef, int nextLevel, string title, string description, string tagKr, Sprite icon)
    {
        Type = LevelUpChoiceType.Passive;
        PassiveDefinition = passiveDef;
        NextLevel = nextLevel;
        Id = passiveDef != null ? passiveDef.SkillId : "";
        Title = title;
        Description = description;
        Tag = tagKr;
        Icon = icon;
    }
}