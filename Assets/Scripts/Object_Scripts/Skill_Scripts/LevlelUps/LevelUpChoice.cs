using UnityEngine;

public sealed class LevelUpChoice
{
    public WeaponSkillSO Skill { get; }
    public int NextLevel { get; }
    public string Title { get; }
    public string Description { get; }
    public string Tag { get; }
    public Sprite Icon { get; }

    public LevelUpChoice(WeaponSkillSO skill, int nextLevel, string title, string description, string tag, Sprite icon)
    {
        Skill = skill;
        NextLevel = nextLevel;
        Title = title;
        Description = description;
        Tag = tag;
        Icon = icon;
    }
}