using UnityEngine;

/// <summary>
/// 적 캐릭터의 스프라이트 좌우 반전(Flip)만 담당하는 컴포넌트입니다.
/// 
/// [이 스크립트의 책임]
/// - 타겟의 X 위치를 기준으로 SpriteRenderer.flipX를 제어합니다.
/// - 이동, 공격, 상태 전환은 담당하지 않습니다.
/// 
/// [왜 별도 컴포넌트로 분리했는가]
/// - 기존 이동 스크립트(예: EnemyChaser2D)를 수정하지 않고 기능을 추가하기 위해서입니다.
/// - "이동"과 "시각적 방향 전환"은 책임이 다르므로 분리하는 편이 유지보수에 유리합니다.
/// - 나중에 방향 처리 방식이 바뀌더라도 이 스크립트만 수정하면 되도록 하기 위함입니다.
/// </summary>
[DisallowMultipleComponent]                 // 한 오브젝트에 이 스크립트가 여러 개 붙는 것을 방지합니다.
[RequireComponent(typeof(SpriteRenderer))]  // 이 스크립트를 넣으면 SpriteRenderer가 자동으로 필수 추가됩니다.
public sealed class EnemySpriteFlip2D : MonoBehaviour
{
    /// <summary>
    /// 원본 스프라이트가 기본적으로 바라보는 방향입니다.
    /// 
    /// 왜 enum으로 분리했는가?
    /// - "왼쪽을 바라보는 원본"이라는 가정을 코드에 하드코딩하면 다른 몬스터 재사용성이 떨어집니다.
    /// - Inspector에서 설정 가능하게 만들어서, 몬스터마다 원본 방향이 달라도 같은 스크립트를 재사용할 수 있게 합니다.
    /// </summary>
    private enum DefaultFacingDirection
    {
        Left,
        Right
    }

    [Header("참조")]
    [Tooltip("좌우 반전에 사용할 SpriteRenderer입니다. 비워두면 같은 오브젝트에서 자동으로 찾습니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("바라볼 대상입니다. 비워두면 자동 탐색 옵션에 따라 지정된 태그 대상을 찾습니다.")]
    [SerializeField] private Transform target;

    [Header("자동 타겟 탐색")]
    [Tooltip("target(바라볼 대상)이 비어 있을 때 targetTag(태그)를 기준으로 대상을 자동으로 찾을지 여부입니다.")]
    [SerializeField] private bool autoFindTarget = true;

    [Tooltip("자동 탐색에 사용할 대상의 태그입니다. 기본값은 Player입니다.")]
    [SerializeField] private string targetTag = "Player";

    [Tooltip("타겟을 찾지 못했을 때 다시 탐색을 시도하는 간격(초)입니다. 적이 많을 때 성능 저하를 막아줍니다.")]
    [SerializeField, Min(0.1f)] private float refindInterval = 0.5f;

    [Header("방향 설정")]
    [Tooltip("몬스터의 원본 이미지가 기본적으로 바라보고 있는 방향입니다.")]
    [SerializeField] private DefaultFacingDirection defaultFacingDirection = DefaultFacingDirection.Left;

    [Tooltip("대상과의 X축 거리 차이가 이 값보다 작으면 방향을 바꾸지 않습니다. (적이 제자리에서 좌우로 심하게 떨리는 현상 방지)")]
    [SerializeField, Min(0f)] private float flipThreshold = 0.05f;

    [Header("디버그")]
    [Tooltip("체크 시, 입력한 태그가 유니티에 존재하지 않는 태그일 경우 콘솔에 경고 로그를 띄워줍니다.")]
    [SerializeField] private bool logInvalidTagWarning = true;

    // 내부 계산을 위해 숨겨진 변수들
    private float _nextRefindTime;
    private bool _hasLoggedInvalidTagWarning;

    private void Reset()
    {
        CacheComponents();
    }

    private void Awake()
    {
        CacheComponents();
        TryFindTarget(force: true); // 게임 시작 시 즉시 플레이어를 한 번 찾습니다.
    }

    private void OnEnable()
    {
        if (target == null)
            TryFindTarget(force: true);
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null)
            return;

        if (!HasValidTarget())
            return;

        UpdateFlip(); // 유효한 플레이어가 있다면 방향을 업데이트합니다.
    }

    /// <summary> 필요한 컴포넌트를 미리 찾아 캐싱(저장)해둡니다. </summary>
    private void CacheComponents()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary> 유효한 타겟이 있는지 확인하고, 없으면 탐색을 시도합니다. </summary>
    private bool HasValidTarget()
    {
        if (target != null)
            return true;

        TryFindTarget(force: false);
        return target != null;
    }

    /// <summary> 태그를 이용해 타겟을 찾습니다. 매 프레임 찾지 않고 refindInterval 간격으로만 찾습니다. </summary>
    private void TryFindTarget(bool force)
    {
        if (!autoFindTarget)
            return;

        // 강제로 찾는 것이 아니고, 아직 다음 탐색 시간이 안 되었다면 그냥 넘어갑니다. (성능 최적화 핵심)
        if (!force && Time.time < _nextRefindTime)
            return;

        _nextRefindTime = Time.time + refindInterval;

        if (string.IsNullOrWhiteSpace(targetTag))
            return;

        try
        {
            GameObject targetObject = GameObject.FindWithTag(targetTag);
            if (targetObject != null)
                target = targetObject.transform;
        }
        catch (UnityException)
        {
            if (!logInvalidTagWarning || _hasLoggedInvalidTagWarning)
                return;

            Debug.LogWarning(
                $"[{nameof(EnemySpriteFlip2D)}] '{targetTag}' 태그를 찾을 수 없습니다. 태그 이름에 오타가 없는지 확인하세요.",
                this);

            _hasLoggedInvalidTagWarning = true; // 콘솔 도배 방지
        }
    }

    /// <summary> 타겟의 위치를 기반으로 이미지를 좌우로 뒤집습니다. </summary>
    private void UpdateFlip()
    {
        float deltaX = target.position.x - transform.position.x;

        // X축 차이가 임계값보다 작으면 무시 (제자리 떨림 방지)
        if (Mathf.Abs(deltaX) <= flipThreshold)
            return;

        bool isTargetOnRight = deltaX > 0f;

        // 원본 스프라이트가 바라보는 기본 방향을 기준으로, 타겟이 어느 쪽에 있는지에 따라 FlipX를 켜고 끕니다.
        spriteRenderer.flipX = defaultFacingDirection switch
        {
            DefaultFacingDirection.Left => isTargetOnRight,
            DefaultFacingDirection.Right => !isTargetOnRight,
            _ => spriteRenderer.flipX
        };
    }

    /// <summary> 외부 시스템(예: 다른 스크립트)에서 강제로 타겟을 지정할 때 사용합니다. </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary> 외부 시스템에서 타겟을 비울 때 사용합니다. </summary>
    public void ClearTarget()
    {
        target = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (refindInterval < 0.1f)
            refindInterval = 0.1f;

        if (flipThreshold < 0f)
            flipThreshold = 0f;
    }
#endif
}