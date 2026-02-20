using System.Collections.Generic;
using UnityEngine;

public interface ILevelUpCardData
{
    string TitleKorean { get; }
    string DescriptionKorean { get; }
    Sprite Icon { get; }
    IReadOnlyList<SkillTag> Tags { get; }

    bool CanPick();
    void Apply();
}