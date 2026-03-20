using UnityEngine;

/// <summary>
/// 지원 캐릭터 비주얼이 메인 캐릭터를 따라다니게 합니다.
/// SupportUltimateController2D가 등장 시 자동으로 부착합니다.
///
/// [동작]
/// - 메인 캐릭터 위치 + 오프셋을 목표로 부드럽게 이동
/// - 이동 중: isWalking = true (Run 모션)
/// - 정지 시: isWalking = false (Idle 모션)
/// - 메인 캐릭터의 flipX를 따라감 (같은 방향을 바라봄)
/// </summary>
[DisallowMultipleComponent]
public sealed class SupportFollower2D : MonoBehaviour
{
    /// <summary>따라갈 대상 (메인 캐릭터 Transform)입니다.</summary>
    public Transform Target { get; set; }

    /// <summary>대상으로부터의 오프셋 (좌: 음수, 우: 양수)입니다.</summary>
    public Vector3 Offset { get; set; }

    /// <summary>따라가기 속도입니다.</summary>
    public float FollowSpeed { get; set; } = 12f;

    /// <summary>이 거리 이내면 정지 판정합니다.</summary>
    public float StopThreshold { get; set; } = 0.05f;

    /// <summary>활성화 여부입니다. false면 이동하지 않습니다.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>메인 캐릭터의 SpriteRenderer입니다. flipX를 따라갑니다.</summary>
    public SpriteRenderer MainSpriteRenderer { get; set; }

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private static readonly int IsWalking = Animator.StringToHash("isWalking");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (!IsActive || Target == null) return;

        // ── 위치 추적 ──
        Vector3 targetPos = Target.position + Offset;
        Vector3 currentPos = transform.position;
        float distance = Vector2.Distance(currentPos, targetPos);

        if (distance > StopThreshold)
        {
            transform.position = Vector3.Lerp(currentPos, targetPos, FollowSpeed * Time.deltaTime);

            if (_animator != null)
                _animator.SetBool(IsWalking, true);
        }
        else
        {
            transform.position = targetPos;

            if (_animator != null)
                _animator.SetBool(IsWalking, false);
        }

        // ── 방향: 메인 캐릭터의 flipX를 그대로 따라감 ──
        // 메인이 왼쪽 보면 지원도 왼쪽, 메인이 오른쪽 보면 지원도 오른쪽
        if (_spriteRenderer != null && MainSpriteRenderer != null)
        {
            _spriteRenderer.flipX = MainSpriteRenderer.flipX;
        }
    }
}