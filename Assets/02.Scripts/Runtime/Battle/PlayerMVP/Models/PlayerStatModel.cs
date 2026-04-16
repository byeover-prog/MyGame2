using System;
using UnityEngine;

namespace Battle.PlayerMVP.Models
{
    [Serializable]
    public sealed class PlayerStatModel
    {
        [SerializeField, Tooltip("기본 이동 속도입니다.")]
        private float baseMoveSpeed = 4f;

        [SerializeField, Tooltip("이동 속도 배율입니다.")]
        private float moveSpeedMultiplier = 1f;

        [SerializeField, Tooltip("기본 공격력입니다.")]
        private int baseAttackPower = 10;

        [SerializeField, Tooltip("공격력 배율입니다.")]
        private float attackMultiplier = 1f;

        [SerializeField, Tooltip("현재 이동 방향입니다.")]
        private Vector2 facingDirection = Vector2.right;

        [SerializeField, Tooltip("현재 이동 속도(읽기용)입니다.")]
        private float currentMoveSpeed;

        public event Action OnStatChanged;

        public float BaseMoveSpeed => Mathf.Max(0f, baseMoveSpeed);
        public float MoveSpeedMultiplier => Mathf.Max(0.01f, moveSpeedMultiplier);
        public float FinalMoveSpeed => BaseMoveSpeed * MoveSpeedMultiplier;
        public int BaseAttackPower => Mathf.Max(1, baseAttackPower);
        public float AttackMultiplier => Mathf.Max(0.01f, attackMultiplier);
        public int FinalAttackPower => Mathf.Max(1, Mathf.RoundToInt(BaseAttackPower * AttackMultiplier));
        public Vector2 FacingDirection => facingDirection.sqrMagnitude > 0.0001f ? facingDirection.normalized : Vector2.right;
        public float CurrentMoveSpeed => Mathf.Max(0f, currentMoveSpeed);

        public void SetBaseMoveSpeed(float value)
        {
            baseMoveSpeed = Mathf.Max(0f, value);
            OnStatChanged?.Invoke();
        }

        public void SetMoveSpeedMultiplier(float value)
        {
            moveSpeedMultiplier = Mathf.Max(0.01f, value);
            OnStatChanged?.Invoke();
        }

        public void SetBaseAttackPower(int value)
        {
            baseAttackPower = Mathf.Max(1, value);
            OnStatChanged?.Invoke();
        }

        public void SetAttackMultiplier(float value)
        {
            attackMultiplier = Mathf.Max(0.01f, value);
            OnStatChanged?.Invoke();
        }

        public void SetFacingDirection(Vector2 value)
        {
            if (value.sqrMagnitude <= 0.0001f)
                return;

            facingDirection = value.normalized;
            OnStatChanged?.Invoke();
        }

        public void SetCurrentMoveSpeed(float value)
        {
            currentMoveSpeed = Mathf.Max(0f, value);
            OnStatChanged?.Invoke();
        }
    }
}
