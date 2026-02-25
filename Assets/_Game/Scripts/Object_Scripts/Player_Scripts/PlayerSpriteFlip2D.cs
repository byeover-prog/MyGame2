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

    [Header("동작")]
    [Tooltip("왼쪽을 볼 때 flipX=true로 뒤집습니다.")]
    [SerializeField] private bool flipWhenFacingLeft = true;

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

        // keepLastFacing=true면 FacingDir를 그대로 쓰고,
        // false면 입력이 없을 때는 방향 변경을 하지 않음
        Vector2 dir = mover.FacingDir;
        if (!keepLastFacing && mover.MoveInput.sqrMagnitude < 0.0001f)
            return;

        // x가 음수면 왼쪽을 본다
        bool facingLeft = dir.x < -0.001f;

        // 왼쪽을 볼 때 뒤집기
        if (flipWhenFacingLeft)
            spriteRenderer.flipX = facingLeft;
        else
            spriteRenderer.flipX = !facingLeft;
    }
}