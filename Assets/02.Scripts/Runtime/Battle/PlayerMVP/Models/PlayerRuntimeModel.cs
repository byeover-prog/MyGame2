using System;
using UnityEngine;

namespace Battle.PlayerMVP.Models
{
    [DisallowMultipleComponent]
    public sealed class PlayerRuntimeModel : MonoBehaviour
    {
        [Header("모델")]
        [SerializeField, Tooltip("플레이어 스탯 모델입니다.")]
        private PlayerStatModel stat = new PlayerStatModel();

        [SerializeField, Tooltip("플레이어 체력 모델입니다.")]
        private PlayerHealthModel health = new PlayerHealthModel();

        [Header("전투 상태")]
        [SerializeField, Tooltip("전투 중 여부입니다.")]
        private bool inCombat;

        [SerializeField, Tooltip("피격 가능 여부입니다.")]
        private bool canTakeDamage = true;

        public event Action<bool> OnCombatStateChanged;
        public event Action<bool> OnDamageGateChanged;

        public PlayerStatModel Stat => stat;
        public PlayerHealthModel Health => health;
        public bool InCombat => inCombat;
        public bool CanTakeDamage => canTakeDamage;

        public void SetInCombat(bool value)
        {
            if (inCombat == value)
                return;

            inCombat = value;
            OnCombatStateChanged?.Invoke(inCombat);
        }

        public void SetCanTakeDamage(bool value)
        {
            if (canTakeDamage == value)
                return;

            canTakeDamage = value;
            OnDamageGateChanged?.Invoke(canTakeDamage);
        }
    }
}
