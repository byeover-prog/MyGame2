using UnityEngine;

/// <summary>
/// HUD 슬롯 1칸에 표시할 데이터 묶음.
/// UI는 이 데이터만 받아서 그림을 갱신한다.
/// </summary>
public sealed class HudSkillEntryViewData
{
    /// <summary>
    /// 스킬/패시브 고유 ID
    /// </summary>
    public string Id;

    /// <summary>
    /// 표시 이름
    /// </summary>
    public string DisplayName;

    /// <summary>
    /// 아이콘
    /// </summary>
    public Sprite Icon;

    /// <summary>
    /// 현재 레벨
    /// </summary>
    public int Level;

    /// <summary>
    /// 공통 스킬인지 여부
    /// </summary>
    public bool IsCommonSkill;

    /// <summary>
    /// 패시브인지 여부
    /// </summary>
    public bool IsPassive;
}