public interface IDamageable2D
{
    bool IsDead { get; }
    void TakeDamage(int damage);
}