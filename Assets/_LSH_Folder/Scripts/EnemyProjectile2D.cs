using UnityEngine;

/// <summary>
/// 적 투사체의 이동, 수명, 화면 밖 제거, 플레이어 충돌 데미지를 담당합니다.
/// 구현 원리:
/// 1. Launch로 발사 방향을 한 번 설정한 뒤 Rigidbody2D.linearVelocity로 직선 이동합니다.
/// 2. 일정 시간이 지나거나 화면 밖으로 나가면 DestroySelf로 정리합니다.
/// 3. 플레이어 Trigger 충돌 시 지정한 메서드명으로 피해를 전달하고 사라집니다.
/// </summary>
[DisallowMultipleComponent]
public class EnemyProjectile2D : MonoBehaviour
{
    [Header("참조 설정")]
    [Tooltip("투사체의 Rigidbody2D입니다. 비워두면 자동으로 찾아서 연결합니다.")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("투사체의 충돌 판정용 Collider2D입니다. Is Trigger를 체크해야 합니다. 비워두면 자동으로 찾아서 연결합니다.")]
    [SerializeField] private Collider2D triggerCollider;

    [Header("이동 설정")]
    [Tooltip("투사체가 날아가는 속도입니다. 단위는 world units per second입니다.")]
    [SerializeField][Min(0.01f)] private float moveSpeed = 8f;

    [Tooltip("체크하면 발사 방향에 맞춰 투사체가 회전합니다. 스프라이트가 오른쪽(→)을 기본 정면으로 보고 있을 때 켜두면 편합니다.")]
    [SerializeField] private bool rotateToMoveDirection = true;

    [Header("데미지 설정")]
    [Tooltip("플레이어에게 전달할 피해량입니다.")]
    [SerializeField][Min(1)] private int damage = 10;

    [Tooltip("피해를 받을 플레이어의 태그입니다. 보통 Player를 사용합니다.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("플레이어에게 피해를 전달할 메서드 이름입니다. 기본값은 TakeDamage입니다.")]
    [SerializeField] private string damageMethodName = "TakeDamage";

    [Tooltip("이 태그를 가진 오브젝트와는 충돌 시 무시합니다. 기본값은 Enemy이며, 자기 자신이나 다른 몬스터를 맞고 사라지는 일을 줄이기 위한 값입니다.")]
    [SerializeField] private string ignoreTag = "Enemy";

    [Header("수명 및 화면 밖 제거 설정")]
    [Tooltip("이 시간이 지나면 투사체를 자동으로 제거합니다.")]
    [SerializeField][Min(0.01f)] private float lifeTime = 5f;

    [Tooltip("체크하면 화면 밖으로 충분히 벗어났을 때 투사체를 제거합니다.")]
    [SerializeField] private bool destroyWhenOffscreen = true;

    [Tooltip("생성 직후 바로 제거되는 것을 막기 위해 화면 밖 판정을 시작하기 전 잠깐 대기하는 시간입니다.")]
    [SerializeField][Min(0f)] private float offscreenCheckDelay = 0.15f;

    [Tooltip("화면 밖 제거 판정 여유값입니다. 0이면 화면 경계 바깥으로 나가는 즉시 제거되고, 값이 클수록 조금 더 멀리 나간 뒤 제거됩니다.")]
    [SerializeField][Min(0f)] private float offscreenPadding = 0.1f;

    private Vector2 _moveDirection = Vector2.right;
    private float _lifeTimer;
    private float _offscreenTimer;
    private bool _isLaunched;
    private Camera _mainCamera;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        triggerCollider = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider2D>();

        if (rb == null || triggerCollider == null)
        {
            Debug.LogWarning("[EnemyProjectile2D] Rigidbody2D 또는 Collider2D를 찾지 못해 비활성화됩니다.", this);
            enabled = false;
            return;
        }

        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (!enabled)
            return;

        _lifeTimer = 0f;
        _offscreenTimer = 0f;
        _isLaunched = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnValidate()
    {
        if (lifeTime < 0.01f)
            lifeTime = 0.01f;

        if (moveSpeed < 0.01f)
            moveSpeed = 0.01f;

        if (offscreenCheckDelay < 0f)
            offscreenCheckDelay = 0f;

        if (offscreenPadding < 0f)
            offscreenPadding = 0f;
    }

    /// <summary>
    /// 외부에서 발사 방향을 받아 투사체를 시작합니다.
    /// </summary>
    public void Launch(Vector2 direction)
    {
        if (!enabled)
            return;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.right;

        _moveDirection = direction.normalized;
        _lifeTimer = 0f;
        _offscreenTimer = 0f;
        _isLaunched = true;

        if (rotateToMoveDirection)
            transform.right = _moveDirection;

        rb.linearVelocity = _moveDirection * moveSpeed;
    }

    private void Update()
    {
        if (!_isLaunched)
            return;

        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= lifeTime)
        {
            DestroySelf();
            return;
        }

        if (!destroyWhenOffscreen)
            return;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null)
            return;

        _offscreenTimer += Time.deltaTime;
        if (_offscreenTimer < offscreenCheckDelay)
            return;

        Vector3 viewportPoint = _mainCamera.WorldToViewportPoint(transform.position);

        bool isOutside =
            viewportPoint.z < 0f ||
            viewportPoint.x < -offscreenPadding ||
            viewportPoint.x > 1f + offscreenPadding ||
            viewportPoint.y < -offscreenPadding ||
            viewportPoint.y > 1f + offscreenPadding;

        if (isOutside)
            DestroySelf();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isLaunched)
            return;

        GameObject hitObject = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (!string.IsNullOrEmpty(ignoreTag) && hitObject.CompareTag(ignoreTag))
            return;

        if (!string.IsNullOrEmpty(playerTag) && hitObject.CompareTag(playerTag))
        {
            hitObject.SendMessage(damageMethodName, damage, SendMessageOptions.DontRequireReceiver);
            DestroySelf();
        }
    }

    private void DestroySelf()
    {
        if (!_isLaunched)
            return;

        _isLaunched = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Destroy(gameObject);
    }
}