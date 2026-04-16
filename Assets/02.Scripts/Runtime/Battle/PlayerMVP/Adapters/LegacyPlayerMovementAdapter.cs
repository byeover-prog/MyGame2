using UnityEngine;

namespace Battle.PlayerMVP.Adapters
{
    [DisallowMultipleComponent]
    public sealed class LegacyPlayerMovementAdapter : MonoBehaviour
    {
        [Header("레거시 참조")]
        [SerializeField, Tooltip("기존 이동 컴포넌트입니다.")]
        private PlayerMover2D legacyMover;

        [SerializeField, Tooltip("기존 이동에서 사용하는 리지드바디입니다.")]
        private Rigidbody2D rb;

        public Vector2 CurrentVelocity => rb != null ? rb.linearVelocity : Vector2.zero;
        public Vector2 CurrentFacing => legacyMover != null ? legacyMover.FacingDir : Vector2.right;
        public bool IsMoving => CurrentVelocity.sqrMagnitude > 0.0004f;

        private void Reset()
        {
            legacyMover = GetComponent<PlayerMover2D>();
            rb = GetComponent<Rigidbody2D>();
        }
    }
}
