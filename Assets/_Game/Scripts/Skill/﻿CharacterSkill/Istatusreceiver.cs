using UnityEngine;

/// <summary>상태이상 종류 구분용 열거형입니다.</summary>
public enum StatusEffectKind
{
    /// <summary>출혈. 일정 시간 동안 주기적으로 피해를 받습니다. 하린 전용 스킬.</summary>
    Bleed,

    /// <summary>동상(Frost). 이동속도 감소. 윤설 빙주 스킬 등에서 사용합니다.</summary>
    Frost,

    /// <summary>혹한(ExtremeCold). 중첩당 1% 추가 피해. 윤설 패시브.</summary>
    ExtremeCold,

    /// <summary>마비(Shock). 전기 연쇄 훅. 하율 관련.</summary>
    Shock,

    /// <summary>화상(Burn). 일정 시간 도트 피해. 진 루이 관련.</summary>
    Burn
}

/// <summary>
/// 스킬에서 적에게 전달하는 상태이상 정보입니다.
/// 필요한 필드만 채우고 나머지는 0으로 두어도 됩니다.
/// </summary>
public readonly struct StatusEffectInfo
{
    /// <summary>상태이상 종류입니다.</summary>
    public readonly StatusEffectKind Kind;

    /// <summary>지속 시간(초)입니다.</summary>
    public readonly float Duration;

    /// <summary>틱당 피해량(출혈/화상 등)입니다.</summary>
    public readonly int TickDamage;

    /// <summary>효과 강도(감속 배율 등). 0.5 = 50% 감속.</summary>
    public readonly float Magnitude;

    /// <summary>중첩 수(혹한 등)입니다.</summary>
    public readonly int Stacks;

    public StatusEffectInfo(StatusEffectKind kind,
        float duration = 0f, int tickDamage = 0, float magnitude = 0f, int stacks = 0)
    {
        Kind = kind;
        Duration = duration;
        TickDamage = tickDamage;
        Magnitude = magnitude;
        Stacks = stacks;
    }

    public static StatusEffectInfo Bleed(float duration, int tickDamage)
        => new StatusEffectInfo(StatusEffectKind.Bleed, duration, tickDamage, 0f, 0);

    public static StatusEffectInfo Frost(float duration, float slowMultiplier)
        => new StatusEffectInfo(StatusEffectKind.Frost, duration, 0, slowMultiplier, 0);

    public static StatusEffectInfo ExtremeCold(int stacks)
        => new StatusEffectInfo(StatusEffectKind.ExtremeCold, 0f, 0, 0f, stacks);
}

/// <summary>
/// 적 프리팹의 상태이상 컴포넌트가 구현하는 인터페이스입니다.
/// 해당 상태이상을 처리하지 않는 경우 false를 반환하면 됩니다.
/// </summary>
public interface IStatusReceiver
{
    /// <summary>상태이상 적용을 시도합니다. 처리했으면 true, 아니면 false 반환.</summary>
    bool TryApplyStatus(StatusEffectInfo info);
}