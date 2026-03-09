public enum WeaponUpgradeType
{
    // 슬롯 기본 강화
    ToggleEnabled = 0,
    DamageAdd = 1,
    CooldownMul = 2,
    RangeAdd = 3,

    // 확장 업그레이드 누적(upgradeState.*)
    UpgradeStateAddDamage = 10,
    UpgradeStateAddRange = 11,
    UpgradeStateAddProjectileSpeed = 12,
    UpgradeStateAddLifetime = 13,

    UpgradeStateMulFireRateAdd = 20,
    UpgradeStateMulAreaAdd = 21,
    UpgradeStateMulKnockbackAdd = 22,

    UpgradeStateAddPierce = 30,
    UpgradeStateAddSplit = 31,
    UpgradeStateAddShotCount = 32,

    UpgradeStateEnableHoming = 40,
    UpgradeStateEnableBoomerang = 41,
    UpgradeStateEnableRotate = 42
}