using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/패시브/패시브 설정", fileName = "Passive_")]
public sealed class PassiveConfigSO : ScriptableObject
{
    public PassiveKind kind;
    public string displayName = "패시브";

    [TextArea]
    public string descriptionKr;

    public Sprite icon;

    [Min(1)]
    public int maxLevel = 8;

    public PassiveLevelParams[] levels;

    public PassiveLevelParams GetLevelParams(int level)
    {
        if (levels == null || levels.Length == 0) return default;

        int lv = Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
        int idx = Mathf.Clamp(lv - 1, 0, levels.Length - 1);
        return levels[idx];
    }
}