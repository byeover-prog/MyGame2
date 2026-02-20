using System.Collections.Generic;
using UnityEngine;

public sealed class SkillLevelRuntimeState2D : MonoBehaviour
{
    [Tooltip("slotIndex별 현재 스킬 레벨(0=미습득, 1~8)")]
    private readonly Dictionary<int, int> _levelBySlot = new Dictionary<int, int>(32);

    public int GetLevel(int slotIndex)
    {
        if (_levelBySlot.TryGetValue(slotIndex, out int lv))
            return lv;
        return 0;
    }

    public void SetLevel(int slotIndex, int level)
    {
        _levelBySlot[slotIndex] = Mathf.Clamp(level, 0, 8);
    }

    public void IncreaseLevel(int slotIndex, int delta = 1)
    {
        int cur = GetLevel(slotIndex);
        SetLevel(slotIndex, cur + delta);
    }
}