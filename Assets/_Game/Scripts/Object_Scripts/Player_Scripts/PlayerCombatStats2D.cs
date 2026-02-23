using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerCombatStats2D : MonoBehaviour
{
    public float DamageMul { get; private set; } = 1f;         // 공격력 배율
    public float CooldownMul { get; private set; } = 1f;       // 쿨타임 배율(작을수록 빠름)
    public float AreaMul { get; private set; } = 1f;           // 범위/크기 배율
    public float MoveSpeedMul { get; private set; } = 1f;      // 이동속도 배율
    public float PickupRangeMul { get; private set; } = 1f;    // 픽업 범위 배율
    public float IncomingDamageMul { get; private set; } = 1f; // 받는 피해 배율(작을수록 단단)
    public float ElementDamageMul { get; private set; } = 1f;  // 속성 피해(지금은 훅만)

    public void SetDamageMul(float v) => DamageMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetCooldownMul(float v) => CooldownMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetAreaMul(float v) => AreaMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetMoveSpeedMul(float v) => MoveSpeedMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetPickupRangeMul(float v) => PickupRangeMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetIncomingDamageMul(float v) => IncomingDamageMul = Mathf.Clamp(v, 0.1f, 10f);
    public void SetElementDamageMul(float v) => ElementDamageMul = Mathf.Clamp(v, 0.1f, 10f);
}