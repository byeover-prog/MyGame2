using System;
using UnityEngine;

namespace Battle.PlayerMVP.Models
{
    [Serializable]
    public sealed class PlayerHealthModel
    {
        [SerializeField, Tooltip("최대 체력입니다.")]
        private int maxHp = 100;

        [SerializeField, Tooltip("현재 체력입니다.")]
        private int currentHp = 100;

        [SerializeField, Tooltip("사망 여부입니다.")]
        private bool isDead;

        public event Action<int, int> OnHealthChanged;
        public event Action OnDead;

        public int MaxHp => Mathf.Max(1, maxHp);
        public int CurrentHp => Mathf.Clamp(currentHp, 0, MaxHp);
        public bool IsDead => isDead;

        public void SetAbsolute(int max, int current, bool dead)
        {
            maxHp = Mathf.Max(1, max);
            currentHp = Mathf.Clamp(current, 0, maxHp);
            bool wasDead = isDead;
            isDead = dead;

            OnHealthChanged?.Invoke(currentHp, maxHp);

            if (!wasDead && isDead)
                OnDead?.Invoke();
        }

        public void Heal(int amount)
        {
            if (isDead || amount <= 0)
                return;

            currentHp = Mathf.Clamp(currentHp + amount, 0, maxHp);
            OnHealthChanged?.Invoke(currentHp, maxHp);
        }

        public void ApplyDamage(int amount)
        {
            if (isDead || amount <= 0)
                return;

            currentHp = Mathf.Clamp(currentHp - amount, 0, maxHp);
            if (currentHp <= 0)
                isDead = true;

            OnHealthChanged?.Invoke(currentHp, maxHp);

            if (isDead)
                OnDead?.Invoke();
        }
    }
}
