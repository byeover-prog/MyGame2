using UnityEngine;

public enum WeaponUpgradeStat2D
{
    Damage,
    FireRateMul,
    FireIntervalMul,   // "발사 간격"에 곱하는 프로젝트면 이쪽 사용(0.9면 더 빨라짐)
    Range,
    ProjectileSpeed,
    Lifetime,
    Pierce,
    Split,
    ShotCount,
    HomingToggle,
    BoomerangToggle,
    RotateToggle
}

public static class WeaponUpgradeApplier2D
{
    public static void Apply(ref WeaponSlotUpgradeState state, WeaponUpgradeStat2D stat, UpgradeValue value)
    {
        // UpgradeValue의 실제 멤버명이 프로젝트마다 다를 수 있어서,
        // 호환 레이어(UpgradeValueCompat)로 안전하게 값을 읽는다.
        int addInt = UpgradeValueCompat.GetAddInt(value);
        float addFloat = UpgradeValueCompat.GetAddFloat(value);
        float mulFloat = UpgradeValueCompat.GetMulFloat(value);
        bool toggle = UpgradeValueCompat.GetBool(value);

        switch (stat)
        {
            case WeaponUpgradeStat2D.Damage:
                state.addDamage += addInt;
                break;

            case WeaponUpgradeStat2D.Range:
                state.addRange += addFloat;
                break;

            case WeaponUpgradeStat2D.ProjectileSpeed:
                state.addProjectileSpeed += addFloat;
                break;

            case WeaponUpgradeStat2D.Lifetime:
                state.addLifetime += addFloat;
                break;

            case WeaponUpgradeStat2D.Pierce:
                state.addPierce += addInt;
                break;

            case WeaponUpgradeStat2D.Split:
                state.addSplit += addInt;
                break;

            case WeaponUpgradeStat2D.ShotCount:
                state.addShotCount += addInt;
                break;

            case WeaponUpgradeStat2D.FireRateMul:
                state.mulFireRate *= Mathf.Max(0.01f, mulFloat);
                break;

            case WeaponUpgradeStat2D.FireIntervalMul:
                state.mulFireRate *= Mathf.Max(0.01f, mulFloat);
                break;

            case WeaponUpgradeStat2D.HomingToggle:
                if (toggle) state.enableHoming = true;
                break;

            case WeaponUpgradeStat2D.BoomerangToggle:
                if (toggle) state.enableBoomerang = true;
                break;

            case WeaponUpgradeStat2D.RotateToggle:
                if (toggle) state.enableRotate = true;
                break;
        }
    }
}
