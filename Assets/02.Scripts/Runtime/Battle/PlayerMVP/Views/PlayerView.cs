using UnityEngine;

namespace Battle.PlayerMVP.Views
{
    [DisallowMultipleComponent]
    public sealed class PlayerView : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField, Tooltip("플레이어 루트 트랜스폼입니다.")]
        private Transform root;

        [SerializeField, Tooltip("플레이어 물리 바디입니다.")]
        private Rigidbody2D rb;

        [SerializeField, Tooltip("플레이어 애니메이터입니다.")]
        private Animator animator;

        [SerializeField, Tooltip("플레이어 스프라이트 렌더러입니다.")]
        private SpriteRenderer spriteRenderer;

        public Transform Root => root != null ? root : transform;
        public Rigidbody2D Rigidbody => rb;
        public Animator Animator => animator;
        public SpriteRenderer SpriteRenderer => spriteRenderer;

        private void Reset()
        {
            root = transform;
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponentInChildren<Animator>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            if (root == null)
                root = transform;
        }

        public void SetVelocity(Vector2 velocity)
        {
            if (rb == null)
                return;

            rb.linearVelocity = velocity;
        }

        public void SetWalkAnimation(bool isWalking)
        {
            if (animator == null)
                return;

            animator.SetBool("isWalking", isWalking);
        }

        public void SetFacing(Vector2 direction)
        {
            if (spriteRenderer == null)
                return;

            if (direction.x < -0.0001f)
                spriteRenderer.flipX = true;
            else if (direction.x > 0.0001f)
                spriteRenderer.flipX = false;
        }
    }
}
