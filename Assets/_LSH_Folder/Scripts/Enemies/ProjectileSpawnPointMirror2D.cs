using UnityEngine;

/// <summary>
/// 몬스터의 SpriteRenderer.flipX 상태에 맞춰
/// ProjectileSpawnPoint의 localPosition.x를 좌우 반전시키는 컴포넌트입니다.
///
/// 구현 원리:
/// 1. 현재 배치된 localPosition을 기준 위치로 저장합니다.
/// 2. 기준 위치를 저장할 당시의 flipX 상태도 함께 저장합니다.
/// 3. 현재 flipX가 저장 당시와 같으면 기준 위치를 그대로 사용합니다.
/// 4. 현재 flipX가 저장 당시와 다르면 localPosition.x만 반전하여 반대쪽 위치로 이동합니다.
/// </summary>
[DisallowMultipleComponent]
public class ProjectileSpawnPointMirror2D : MonoBehaviour
{
    [Header("참조 설정")]
    [Tooltip("좌우 반전 상태를 읽어올 몬스터 본체 SpriteRenderer입니다. EnemySpriteFlip2D가 flipX를 바꾸는 바로 그 SpriteRenderer를 연결하세요.")]
    [SerializeField] private SpriteRenderer bodySpriteRenderer;

    [Header("기준 위치 설정")]
    [Tooltip("체크하면 현재 ProjectileSpawnPoint의 localPosition을 기준 위치로 자동 저장합니다.")]
    [SerializeField] private bool useCurrentLocalPositionAsAuthoredPosition = true;

    [Tooltip("ProjectileSpawnPoint의 기준 localPosition입니다. 오른쪽이든 왼쪽이든 현재 네가 원하는 입 앞 위치를 넣으면 됩니다.")]
    [SerializeField] private Vector3 authoredLocalPosition = new Vector3(0.3f, 0f, 0f);

    [Tooltip("기준 위치를 저장할 당시 bodySpriteRenderer.flipX 값입니다. 현재 배치 위치가 어떤 방향에서의 입 앞인지 판단하는 기준입니다.")]
    [SerializeField] private bool authoredFlipXState = false;

    [Header("디버그")]
    [Tooltip("체크하면 플레이 중에도 현재 방향에 맞춰 localPosition이 자동 보정됩니다.")]
    [SerializeField] private bool updateEveryFrame = true;

    private void Reset()
    {
        if (bodySpriteRenderer == null)
            bodySpriteRenderer = GetComponentInParent<SpriteRenderer>();

        CaptureCurrentAsAuthoredReference();
    }

    private void Awake()
    {
        if (bodySpriteRenderer == null)
            bodySpriteRenderer = GetComponentInParent<SpriteRenderer>();

        if (useCurrentLocalPositionAsAuthoredPosition)
            CaptureCurrentAsAuthoredReference();

        ApplyMirroredPosition();
    }

    private void OnEnable()
    {
        ApplyMirroredPosition();
    }

    private void OnValidate()
    {
        if (bodySpriteRenderer == null)
            bodySpriteRenderer = GetComponentInParent<SpriteRenderer>();

        if (!Application.isPlaying && useCurrentLocalPositionAsAuthoredPosition)
            CaptureCurrentAsAuthoredReference();

        if (!Application.isPlaying)
            ApplyMirroredPosition();
    }

    private void LateUpdate()
    {
        if (!updateEveryFrame)
            return;

        ApplyMirroredPosition();
    }

    /// <summary>
    /// 현재 localPosition과 현재 flipX 상태를 기준값으로 저장합니다.
    /// </summary>
    [ContextMenu("현재 위치를 기준값으로 저장")]
    private void CaptureCurrentAsAuthoredReference()
    {
        authoredLocalPosition = transform.localPosition;

        if (bodySpriteRenderer != null)
            authoredFlipXState = bodySpriteRenderer.flipX;
    }

    private void ApplyMirroredPosition()
    {
        if (bodySpriteRenderer == null)
            return;

        Vector3 targetLocalPosition = authoredLocalPosition;

        bool shouldMirrorX = bodySpriteRenderer.flipX != authoredFlipXState;

        if (shouldMirrorX)
            targetLocalPosition.x = -authoredLocalPosition.x;

        transform.localPosition = targetLocalPosition;
    }
}