using _Game.Player;

public struct MetaCharacterBonusSnapshot2D
{
    public PlayerStatSnapshot coreStats;
    public float basicSkillDamagePercent;
    public float basicSkillCooldownPercent;
    public float ultimateDamagePercent;
    public float ultimateCooldownPercent;
    public float passivePowerPercent;
    public float storyExpGainPercent;
    public float casualExpGainPercent;
    public float nyangGainPercent;
    public int characterLevel;
    public int purchasedNodeCount;

    public static MetaCharacterBonusSnapshot2D Empty => default;
}
