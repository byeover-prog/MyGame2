using _Game.Player;

/// <summary>
/// 한 캐릭터의 아웃게임 보정 스냅샷입니다.
/// 스킬 트리 + 상점 장착 아이템의 합산 결과를 담습니다.
/// </summary>
public struct MetaCharacterBonusSnapshot2D
{
    // ─── 기존 필드 ───
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

    // ─── 상점 아이템용 추가 필드 ───
    /// <summary>시전 횟수 추가 (정수)</summary>
    public int castCountFlat;

    /// <summary>스킬 가속 추가 (정수, LoL 스킬 가속)</summary>
    public float skillAccelerationFlat;

    /// <summary>방어력 고정 추가 (정수)</summary>
    public int defenseFlat;

    /// <summary>체력 재생 추가 (초당, 정수)</summary>
    public int hpRegenFlat;

    /// <summary>치명타 확률 % 증가</summary>
    public float critChancePercent;

    /// <summary>치명타 피해량 % 증가</summary>
    public float critDamagePercent;

    /// <summary>재사용 대기시간 % 감소</summary>
    public float cooldownReductionPercent;

    public static MetaCharacterBonusSnapshot2D Empty => default;
}