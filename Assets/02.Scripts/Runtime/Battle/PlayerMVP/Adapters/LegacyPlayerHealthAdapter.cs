using UnityEngine;

namespace Battle.PlayerMVP.Adapters
{
    [DisallowMultipleComponent]
    public sealed class LegacyPlayerHealthAdapter : MonoBehaviour
    {
        [Header("레거시 참조")]
        [SerializeField, Tooltip("기존 체력 컴포넌트입니다.")]
        private PlayerHealth legacyHealth;

        public int MaxHp => legacyHealth != null ? legacyHealth.MaxHp : 1;
        public int CurrentHp => legacyHealth != null ? legacyHealth.CurrentHp : 0;
        public bool IsDead => legacyHealth != null && legacyHealth.IsDead;

        private void Reset()
        {
            legacyHealth = GetComponent<PlayerHealth>();
        }

        public bool TryHeal(int amount)
        {
            if (legacyHealth == null)
                return false;

            legacyHealth.Heal(amount);
            return true;
        }

        public bool TryDamage(int amount)
        {
            if (legacyHealth == null)
                return false;

            legacyHealth.TakeDamage(amount);
            return true;
        }
    }
}
