using UnityEngine;
using UnityEngine.UI;
using _Game.Player;

public class SkillSlotHUDController : MonoBehaviour
{
    [Header("액티브 슬롯 (slot_1 ~ slot_6)")]
    [SerializeField] private Image[] activeSlots; // slot_1~6

    [Header("패시브 슬롯 (slot_1 ~ slot_5)")]
    [SerializeField] private Image[] passiveSlots; // slot_1~5

    [Header("연결")]
    [SerializeField] private PlayerSkillLoadout loadout;
    
    [Header("액티브 스킬 아이콘 (SkillIcon)")]
    [SerializeField] private Image[] activeSkillIcons; // 각 slot의 SkillIcon 자식

    [Header("패시브 스킬 아이콘 (SkillIcon)")]
    [SerializeField] private Image[] passiveSkillIcons;

    private void Update()
    {
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        var active = loadout.GetActiveSlots();
        for (int i = 0; i < activeSkillIcons.Length; i++)
        {
            if (activeSkillIcons[i] == null) continue;
            bool hasSkill = i < active.Length && active[i] != null;
            if (!hasSkill) continue;

            var icon = active[i].Definition.Icon;
            if (icon != null)
            {
                activeSkillIcons[i].sprite = icon;
                activeSkillIcons[i].color = Color.white;
            }
        }

        var passive = loadout.GetPassiveSlots();
        for (int i = 0; i < passiveSkillIcons.Length; i++)
        {
            if (passiveSkillIcons[i] == null) continue;
            bool hasSkill = i < passive.Length && passive[i] != null;
            if (!hasSkill) continue;

            var icon = passive[i].Definition.Icon;
            if (icon != null)
            {
                passiveSkillIcons[i].sprite = icon;
                passiveSkillIcons[i].color = Color.white;
            }
        }
    }
}