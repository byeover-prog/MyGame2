using UnityEngine;

public sealed class LevelUpChoice
{
    public WeaponDefinitionSO Weapon { get; }
    public int NextLevel { get; }
    public string Title { get; }
    public string Description { get; }
    public string TagKr { get; }
    public Sprite Icon { get; }

    public LevelUpChoice(WeaponDefinitionSO weapon, int nextLevel, string title, string description, string tagKr, Sprite icon)
    {
        Weapon = weapon;
        NextLevel = nextLevel;
        Title = title;
        Description = description;
        TagKr = tagKr;
        Icon = icon;
    }

    // --------------------
    // 하위 호환(예전 UI 코드 유지)
    // --------------------
    public WeaponDefinitionSO Skill => Weapon;
    public string Tag => TagKr;
}