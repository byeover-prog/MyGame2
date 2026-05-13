using System;
using UnityEngine;

/// <summary>
/// 캐릭터 전용 스킬의 레벨별 수치입니다.
/// 구현 원리:
///  공통 필드는 고정으로 두고, 스킬마다 다른 값은 customValues로 보관합니다.
///  -1 값은 "미사용"으로 처리하여 기존 코드 fallback을 유지합니다.
/// </summary>
[Serializable]
public sealed class SkillLevelBalanceData2D
{
    [Header("레벨")]
    [Tooltip("이 수치가 적용될 스킬 레벨입니다.")]
    [SerializeField, Min(1)] private int level = 1;

    [Header("공통 수치 (-1이면 기존 기본값 사용)")]
    [Tooltip("피해량입니다. -1이면 기존 기본값을 사용합니다.")]
    [SerializeField] private int damage = -1;

    [Tooltip("쿨다운입니다. -1이면 기존 기본값을 사용합니다.")]
    [SerializeField] private float cooldown = -1f;

    [Tooltip("시전 횟수 또는 발사 개수입니다. -1이면 기존 기본값을 사용합니다.")]
    [SerializeField] private int count = -1;

    [Tooltip("투사체 속도입니다. -1이면 기존 기본값을 사용합니다.")]
    [SerializeField] private float speed = -1f;

    [Tooltip("투사체 또는 장판 수명입니다. -1이면 기존 기본값을 사용합니다.")]
    [SerializeField] private float lifetime = -1f;

    [Tooltip("범위 또는 반경입니다. -1이면 기존 기본값을 사용합니다.")]
    [SerializeField] private float radius = -1f;

    [Tooltip("지속 시간입니다. -1이면 기존 기본값을 사용합니다.")]
    [SerializeField] private float duration = -1f;

    [Tooltip("틱 간격 또는 발동 간격입니다. -1이면 기존 기본값을 사용합니다.")]
    [SerializeField] private float interval = -1f;

    [Tooltip("지연 시간입니다. -1이면 기존 기본값을 사용합니다.")]
    [SerializeField] private float delay = -1f;

    [Tooltip("다중 발사 각도입니다. -1이면 기존 기본값을 사용합니다.")]
    [SerializeField] private float spreadAngle = -1f;

    [Tooltip("추가 개수입니다. -1이면 기존 기본값을 사용합니다.")]
    [SerializeField] private int extraCount = -1;

    [Header("스킬별 특수 수치")]
    [Tooltip("공통 필드로 표현하기 어려운 수치를 key-value로 추가합니다.")]
    [SerializeField] private SkillCustomValue2D[] customValues;

    public int Level => level;
    public int Damage => damage;
    public float Cooldown => cooldown;
    public int Count => count;
    public float Speed => speed;
    public float Lifetime => lifetime;
    public float Radius => radius;
    public float Duration => duration;
    public float Interval => interval;
    public float Delay => delay;
    public float SpreadAngle => spreadAngle;
    public int ExtraCount => extraCount;
    public SkillCustomValue2D[] CustomValues => customValues;

    public void SetLevelForEditor(int newLevel)
    {
        level = Mathf.Max(1, newLevel);
    }

    public bool TryGetFloat(string key, out float value)
    {
        value = 0f;

        if (TryGetCustomFloat(key, out value))
            return true;

        string normalized = Normalize(key);

        switch (normalized)
        {
            case "cooldown":
                return TryUseFloat(cooldown, out value);

            case "speed":
            case "projectilespeed":
            case "arrowspeed":
            case "followspeed":
                return TryUseFloat(speed, out value);

            case "life":
            case "lifetime":
            case "projectilelifetime":
            case "arrowmaxflighttime":
                return TryUseFloat(lifetime, out value);

            case "radius":
            case "hitradius":
            case "explosionradius":
            case "boltradius":
            case "autoaimradius":
            case "detectrange":
                return TryUseFloat(radius, out value);

            case "duration":
            case "frostduration":
            case "slowduration":
                return TryUseFloat(duration, out value);

            case "interval":
            case "hitinterval":
            case "boltinterval":
                return TryUseFloat(interval, out value);

            case "delay":
            case "armdelay":
            case "attachdelay":
                return TryUseFloat(delay, out value);

            case "spread":
            case "spreadangle":
            case "multishotspread":
            case "multicastanglespread":
                return TryUseFloat(spreadAngle, out value);
        }

        return false;
    }

    public bool TryGetInt(string key, out int value)
    {
        value = 0;

        if (TryGetCustomInt(key, out value))
            return true;

        string normalized = Normalize(key);

        switch (normalized)
        {
            case "damage":
                return TryUseInt(damage, out value);

            case "count":
            case "castcount":
            case "shotcount":
            case "projectilecount":
                return TryUseInt(count, out value);

            case "extracount":
            case "awakeningextracount":
            case "awakeningextracasts":
            case "awakeningextrashots":
                return TryUseInt(extraCount, out value);
        }

        return false;
    }

    public bool TryGetBool(string key, out bool value)
    {
        value = false;

        if (customValues == null)
            return false;

        for (int i = 0; i < customValues.Length; i++)
        {
            SkillCustomValue2D custom = customValues[i];
            if (custom == null) continue;

            if (custom.TryGetBool(key, out value))
                return true;
        }

        return false;
    }

    public bool TryGetString(string key, out string value)
    {
        value = string.Empty;

        if (customValues == null)
            return false;

        for (int i = 0; i < customValues.Length; i++)
        {
            SkillCustomValue2D custom = customValues[i];
            if (custom == null) continue;

            if (custom.TryGetString(key, out value))
                return true;
        }

        return false;
    }

    private bool TryGetCustomFloat(string key, out float value)
    {
        value = 0f;

        if (customValues == null)
            return false;

        for (int i = 0; i < customValues.Length; i++)
        {
            SkillCustomValue2D custom = customValues[i];
            if (custom == null) continue;

            if (custom.TryGetFloat(key, out value))
                return true;
        }

        return false;
    }

    private bool TryGetCustomInt(string key, out int value)
    {
        value = 0;

        if (customValues == null)
            return false;

        for (int i = 0; i < customValues.Length; i++)
        {
            SkillCustomValue2D custom = customValues[i];
            if (custom == null) continue;

            if (custom.TryGetInt(key, out value))
                return true;
        }

        return false;
    }

    private static bool TryUseFloat(float source, out float value)
    {
        value = source;
        return source >= 0f;
    }

    private static bool TryUseInt(int source, out int value)
    {
        value = source;
        return source >= 0;
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .Trim()
            .Replace("_", "")
            .Replace("-", "")
            .Replace(" ", "")
            .ToLowerInvariant();
    }
}