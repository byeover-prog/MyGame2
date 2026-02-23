using System.Collections.Generic;
using UnityEngine;

public sealed class SkillRuntimeState : MonoBehaviour
{
    // skillId -> level
    private readonly Dictionary<string, int> _levels = new Dictionary<string, int>(32);

    public bool HasSkill(string skillId) => _levels.ContainsKey(skillId);

    public int GetLevel(string skillId)
    {
        return _levels.TryGetValue(skillId, out int lv) ? lv : 0;
    }

    public int GrantOrLevelUp(string skillId)
    {
        if (_levels.TryGetValue(skillId, out int lv))
        {
            lv++;
            _levels[skillId] = lv;
            return lv;
        }
        else
        {
            _levels.Add(skillId, 1);
            return 1;
        }
    }
}