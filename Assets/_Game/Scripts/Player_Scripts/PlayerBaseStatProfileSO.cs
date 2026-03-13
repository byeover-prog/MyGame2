using UnityEngine;
using _Game.Player;

/// <summary>
/// 캐릭터의 기본 능력치 보정을 SO로 관리합니다.
/// PlayerStatRuntimeApplier2D에서 패시브 스냅샷과 합산됩니다.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/플레이어/기본 능력치 프로필", fileName = "PlayerBaseStatProfile")]
public sealed class PlayerBaseStatProfileSO : ScriptableObject
{
    [Header("=== 공격/방어 ===")]
    [Tooltip("캐릭터 기본 공격력 보너스(%)")]
    [SerializeField] private float attackPowerPercent;

    [Tooltip("캐릭터 기본 방어력 보너스(%)")]
    [SerializeField] private float defensePercent;

    [Header("=== 이동/흡수 ===")]
    [Tooltip("캐릭터 기본 이동속도 보너스(%)")]
    [SerializeField] private float moveSpeedPercent;

    [Tooltip("캐릭터 기본 픽업 범위 보너스(%)")]
    [SerializeField] private float pickupRangePercent;

    [Header("=== 체력 ===")]
    [Tooltip("기본 최대 HP에 더할 추가값")]
    [SerializeField] private int maxHpFlat;

    [Header("=== 속성/보상 ===")]
    [Tooltip("속성 피해 보너스(%)")]
    [SerializeField] private float elementDamagePercent;

    [Tooltip("재화 획득량 보너스(%)")]
    [SerializeField] private float goldGainPercent;

    [Tooltip("경험치 획득량 보너스(%)")]
    [SerializeField] private float expGainPercent;

    public PlayerStatSnapshot BuildSnapshot()
    {
        return new PlayerStatSnapshot
        {
            AttackPowerPercent   = attackPowerPercent,
            DefensePercent       = defensePercent,
            MoveSpeedPercent     = moveSpeedPercent,
            PickupRangePercent   = pickupRangePercent,
            MaxHpFlat            = maxHpFlat,
            ElementDamagePercent = elementDamagePercent,
            GoldGainPercent      = goldGainPercent,
            ExpGainPercent       = expGainPercent,
        };
    }
}