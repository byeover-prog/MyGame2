// UTF-8
using UnityEngine;

/// <summary>
/// 돌진형 무리 패턴에 소속된 개별 멤버 1마리를 제어하는 보조 컴포넌트입니다.
///
/// 구현 원리:
/// 1. 기존 몬스터 프리팹의 체력, 피격, 충돌, 접촉 데미지 같은 개체 기능은 그대로 유지합니다.
/// 2. 대신 EnemyChaser2D, EnemyAutoDespawn2D, EnemySpriteFlip2D처럼
///    스웜 패턴 제어와 충돌하는 기본 행동만 런타임에 비활성화합니다.
/// 3. 부모 패턴 컨트롤러가 모든 멤버에게 같은 속도와 방향을 내려주면,
///    무리 전체가 하나의 패턴처럼 움직입니다.
/// 4. 패턴 종료 시 남은 개체는 조용히 Destroy하여
///    드랍/사망 연출 없이 정리할 수 있게 합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class SwarmRushMember2D : MonoBehaviour
{
    public enum OrientationMode
    {
        FlipX,
        RotateZ
    }

    public enum DefaultFacingDirection
    {
        Left,
        Right
    }

    [Header("참조 설정")]
    [Tooltip("이 멤버의 Rigidbody2D입니다. 비워두면 자동으로 찾아서 연결합니다.")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("방향 표시용 SpriteRenderer입니다. 비워두면 자기 자신 또는 자식에서 자동으로 찾아옵니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("체크하면 자기 자신에 SpriteRenderer가 없을 때 자식 오브젝트에서도 찾아봅니다.")]
    [SerializeField] private bool findSpriteRendererInChildren = true;

    [Header("방향 표시 설정")]
    [Tooltip("개별 멤버의 방향 표현 방식입니다. 일반 2D 스프라이트라면 FlipX를 권장합니다.")]
    [SerializeField] private OrientationMode orientationMode = OrientationMode.FlipX;

    [Tooltip("원본 스프라이트가 기본적으로 바라보는 방향입니다. 원본 그림이 왼쪽을 보면 Left, 오른쪽을 보면 Right입니다.")]
    [SerializeField] private DefaultFacingDirection defaultFacingDirection = DefaultFacingDirection.Left;

    [Header("기존 몬스터 컴포넌트 비활성화")]
    [Tooltip("체크하면 기존 EnemyChaser2D를 꺼서 스웜 패턴 이동과 충돌하지 않게 합니다.")]
    [SerializeField] private bool disableEnemyChaserOnInit = true;

    [Tooltip("체크하면 기존 EnemyAutoDespawn2D를 꺼서 패턴 이동 도중 개별 멤버가 따로 사라지지 않게 합니다.")]
    [SerializeField] private bool disableEnemyAutoDespawnOnInit = true;

    [Tooltip("체크하면 기존 EnemySpriteFlip2D를 꺼서 패턴이 방향을 직접 제어하게 합니다.")]
    [SerializeField] private bool disableEnemySpriteFlipOnInit = true;

    private SwarmRushPattern2D _owner;

    private EnemyChaser2D _enemyChaser;
    private EnemyAutoDespawn2D _enemyAutoDespawn;
    private EnemySpriteFlip2D _enemySpriteFlip;

    public Vector2 Position => rb != null ? rb.position : (Vector2)transform.position;
    public bool IsActiveMember => gameObject.activeInHierarchy;

    private void Reset()
    {
        CacheComponents();
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        StopMotion();
    }

    private void OnDisable()
    {
        StopMotion();

        if (_owner != null)
        {
            _owner.NotifyMemberDisabled(this);
            _owner = null;
        }
    }

    public void Initialize(
        SwarmRushPattern2D owner,
        OrientationMode newOrientationMode,
        DefaultFacingDirection newDefaultFacingDirection,
        bool disableChaser,
        bool disableAutoDespawn,
        bool disableSpriteFlip)
    {
        _owner = owner;
        orientationMode = newOrientationMode;
        defaultFacingDirection = newDefaultFacingDirection;

        disableEnemyChaserOnInit = disableChaser;
        disableEnemyAutoDespawnOnInit = disableAutoDespawn;
        disableEnemySpriteFlipOnInit = disableSpriteFlip;

        CacheComponents();
        DisableConflictingBehaviours();
        StopMotion();
    }

    public void ApplyMovement(Vector2 velocity, Vector2 facingDirection)
    {
        if (rb == null)
            return;

        rb.linearVelocity = velocity;
        rb.angularVelocity = 0f;

        Vector2 directionForVisual = facingDirection.sqrMagnitude > 0.0001f
            ? facingDirection.normalized
            : velocity.normalized;

        if (directionForVisual.sqrMagnitude > 0.0001f)
            ApplyOrientation(directionForVisual);
    }

    public void SilentDespawn()
    {
        StopMotion();
        Destroy(gameObject);
    }

    private void StopMotion()
    {
        if (rb == null)
            return;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void CacheComponents()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null && findSpriteRendererInChildren)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (_enemyChaser == null)
            _enemyChaser = GetComponent<EnemyChaser2D>();

        if (_enemyAutoDespawn == null)
            _enemyAutoDespawn = GetComponent<EnemyAutoDespawn2D>();

        if (_enemySpriteFlip == null)
            _enemySpriteFlip = GetComponent<EnemySpriteFlip2D>();
    }

    private void DisableConflictingBehaviours()
    {
        if (disableEnemyChaserOnInit && _enemyChaser != null)
            _enemyChaser.enabled = false;

        if (disableEnemyAutoDespawnOnInit && _enemyAutoDespawn != null)
            _enemyAutoDespawn.enabled = false;

        if (disableEnemySpriteFlipOnInit && _enemySpriteFlip != null)
            _enemySpriteFlip.enabled = false;
    }

    private void ApplyOrientation(Vector2 direction)
    {
        if (orientationMode == OrientationMode.RotateZ)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (rb != null)
                rb.MoveRotation(angle);
            else
                transform.rotation = Quaternion.Euler(0f, 0f, angle);

            return;
        }

        if (spriteRenderer == null)
            return;

        if (Mathf.Abs(direction.x) <= 0.0001f)
            return;

        bool shouldFaceRight = direction.x > 0f;

        if (defaultFacingDirection == DefaultFacingDirection.Right)
            spriteRenderer.flipX = !shouldFaceRight;
        else
            spriteRenderer.flipX = shouldFaceRight;
    }
}