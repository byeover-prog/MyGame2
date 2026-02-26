// UTF-8
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSpriteFlip2D : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("PlayerMover2D를 넣으세요. (FacingDir 기준으로 좌/우를 판단합니다)")]
    [SerializeField] private PlayerMover2D mover;

    [Tooltip("뒤집을 SpriteRenderer를 넣으세요. (보통 플레이어 본체)")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("기본 방향")]
    [Tooltip("스프라이트 원본이 '오른쪽'을 보고 그려졌으면 true.\n원본이 '왼쪽'을 보고 그려졌으면 false.")]
    [SerializeField] private bool spriteDefaultFacesRight = true;

    [Header("동작")]
    [Tooltip("입력이 없을 때도 마지막 방향을 유지할지")]
    [SerializeField] private bool keepLastFacing = true;

    private void Reset()
    {
        mover = GetComponent<PlayerMover2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (mover == null) mover = GetComponent<PlayerMover2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (mover == null || spriteRenderer == null) return;

        if (!keepLastFacing && mover.MoveInput.sqrMagnitude < 0.0001f)
            return;

        Vector2 dir = mover.FacingDir;
        bool facingLeft = dir.x < -0.001f;
        bool facingRight = dir.x > 0.001f;

        // x==0이면 방향 유지(스프라이트는 그대로)
        if (!facingLeft && !facingRight) return;

        // 원본이 오른쪽이면: 왼쪽 볼 때 flipX=true
        // 원본이 왼쪽이면: 오른쪽 볼 때 flipX=true
        if (spriteDefaultFacesRight)
            spriteRenderer.flipX = facingLeft;
        else
            spriteRenderer.flipX = facingRight;
    }
}