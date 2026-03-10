using UnityEngine;

public static class UpgradeTextFormatter
{
    public static string Format(WeaponUpgradeType type, UpgradeValue v)
    {
        switch (type)
        {
            case WeaponUpgradeType.DamageAdd:
                return $"+{v.addInt} 피해";

            case WeaponUpgradeType.CooldownMul:
                {
                    float pctDown = (1f - v.mulFloat) * 100f;
                    if (pctDown >= 0f) return $"쿨타임 {pctDown:0}% 감소";
                    return $"쿨타임 {(-pctDown):0}% 증가";
                }

            case WeaponUpgradeType.RangeAdd:
                return $"+{v.addFloat:0.##} 사거리";

            case WeaponUpgradeType.ToggleEnabled:
                return v.toggleBool ? "스킬 활성화" : "스킬 비활성화";

            case WeaponUpgradeType.UpgradeStateAddDamage:
                return $"+{v.addInt} 추가 피해";

            case WeaponUpgradeType.UpgradeStateAddRange:
                return $"+{v.addFloat:0.##} 추가 사거리";

            case WeaponUpgradeType.UpgradeStateAddProjectileSpeed:
                return $"+{v.addFloat:0.##} 투사체 속도";

            case WeaponUpgradeType.UpgradeStateAddLifetime:
                return $"+{v.addFloat:0.##} 지속시간";

            case WeaponUpgradeType.UpgradeStateMulFireRateAdd:
                return $"+{v.addFloat * 100f:0}% 연사 배율";

            case WeaponUpgradeType.UpgradeStateMulAreaAdd:
                return $"+{v.addFloat * 100f:0}% 범위 배율";

            case WeaponUpgradeType.UpgradeStateMulKnockbackAdd:
                return $"+{v.addFloat * 100f:0}% 넉백 배율";

            case WeaponUpgradeType.UpgradeStateAddPierce:
                return $"+{v.addInt} 관통";

            case WeaponUpgradeType.UpgradeStateAddSplit:
                return $"+{v.addInt} 분열";

            case WeaponUpgradeType.UpgradeStateAddShotCount:
                return $"+{v.addInt} 발사 수";

            case WeaponUpgradeType.UpgradeStateEnableHoming:
                return v.toggleBool ? "호밍 활성화" : "호밍 비활성화";

            case WeaponUpgradeType.UpgradeStateEnableBoomerang:
                return v.toggleBool ? "부메랑 활성화" : "부메랑 비활성화";

            case WeaponUpgradeType.UpgradeStateEnableRotate:
                return v.toggleBool ? "회전 활성화" : "회전 비활성화";

            default:
                return "업그레이드";
        }
    }
}
