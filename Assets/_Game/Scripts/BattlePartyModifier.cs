using UnityEngine;

/// <summary>
/// 전투 시작 시 편성 결과로 계산된 보정값만 보관한다.
/// 플레이 도중 수시 계산하지 않고, 시작 시 1회 계산 후 전달하는 용도.
/// </summary>
public sealed class BattlePartyModifier
{
    /// <summary>
    /// 바람 속성 편성 여부
    /// </summary>
    public bool HasWindAttribute { get; private set; }

    /// <summary>
    /// 대쉬 최대 충전 수
    /// </summary>
    public int Dash_Max_Count { get; private set; }

    /// <summary>
    /// 대쉬 쿨타임 감소 비율 (0.20 = 20%)
    /// </summary>
    public float Dash_Cooldown_Reduction_Rate { get; private set; }

    /// <summary>
    /// 편성 결과 반영
    /// </summary>
    public void Build(bool hasWindAttribute, int baseDashCount, float windCooldownReductionRate)
    {
        HasWindAttribute = hasWindAttribute;
        Dash_Max_Count = hasWindAttribute ? baseDashCount + 1 : baseDashCount;
        Dash_Cooldown_Reduction_Rate = hasWindAttribute ? windCooldownReductionRate : 0f;
    }
}