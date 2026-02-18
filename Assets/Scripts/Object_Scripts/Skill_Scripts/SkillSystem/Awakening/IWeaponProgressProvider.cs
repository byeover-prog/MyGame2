// IWeaponProgressProvider.cs
public interface IWeaponProgressProvider
{
    bool TryGetWeaponLevel(string weaponId, out int level);
    bool IsWeaponAwakened(string weaponId);
    bool TryGetWeaponMaxedAtLevelUpIndex(string weaponId, out int levelUpIndex);
}