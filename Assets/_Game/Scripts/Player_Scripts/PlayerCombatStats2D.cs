using UnityEngine;

/// <summary>
/// 플레이어 전투/이동/획득 배율을 보관하는 런타임 컴포넌트.
/// PlayerStatRuntimeApplier2D가 값을 설정하고,
/// 각 시스템(무기/이동/경험치/재화)이 읽어간다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerCombatStats2D : MonoBehaviour
{
    public float DamageMul { get; private set; } = 1f;
    public float CooldownMul { get; private set; } = 1f;
    public float AreaMul { get; private set; } = 1f;
    public float MoveSpeedMul { get; private set; } = 1f;
    public float PickupRangeMul { get; private set; } = 1f;
    public float IncomingDamageMul { get; private set; } = 1f;
    public float ElementDamageMul { get; private set; } = 1f;
    public float GoldGainMul { get; private set; } = 1f;
    public float ExpGainMul { get; private set; } = 1f;

    public void SetDamageMul(float v) => DamageMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetCooldownMul(float v) => CooldownMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetAreaMul(float v) => AreaMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetMoveSpeedMul(float v) => MoveSpeedMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetPickupRangeMul(float v) => PickupRangeMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetIncomingDamageMul(float v) => IncomingDamageMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetElementDamageMul(float v) => ElementDamageMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetGoldGainMul(float v) => GoldGainMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetExpGainMul(float v) => ExpGainMul = Mathf.Clamp(v, 0.1f, 10f);
}