public enum WeaponUpgradeType
{
    DamageAdd,
    CooldownMul,
    RangeAdd,
    ToggleEnabled,

    // ── 확장 업그레이드 ──
    UpgradeStateAddDamage,
    UpgradeStateAddRange,
    UpgradeStateAddProjectileSpeed,
    UpgradeStateAddLifetime,
    UpgradeStateMulFireRateAdd,
    UpgradeStateMulAreaAdd,
    UpgradeStateMulKnockbackAdd,
    UpgradeStateAddPierce,
    UpgradeStateAddSplit,
    UpgradeStateAddShotCount,
    UpgradeStateEnableHoming,
    UpgradeStateEnableBoomerang,
    UpgradeStateEnableRotate
}