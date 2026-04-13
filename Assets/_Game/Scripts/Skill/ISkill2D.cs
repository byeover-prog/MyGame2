// UTF-8
public interface ISkill2D
{
    bool IsOwned { get; }
    bool IsEquipped { get; }
    int Level { get; }

    void Grant(int startLevel, bool equip);
    void Upgrade(int delta);
    void SetLevel(int newLevel);
    void SetEquipped(bool equip);
}