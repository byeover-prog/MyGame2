using UnityEngine;

/// <summary>
/// 적 투사체의 차징 미리보기, 발사, 이동, 수명, 화면 밖 제거, 플레이어 충돌 데미지를 담당합니다.
///
/// 구현 원리:
/// 1. 투사체가 처음 생성될 때 프리팹의 원본 localScale / worldScale을 저장합니다.
/// 2. 차징 미리보기 상태에서는 SpawnPoint의 자식으로 붙이되, 부모가 바뀌어도 저장한 원본 worldScale을 다시 적용하여 크기가 변하지 않게 합니다.
/// 3. 발사 시에는 부모에서 분리한 뒤 다시 한 번 원본 worldScale을 적용하여, 차징을 거쳤든 안 거쳤든 항상 같은 크기로 날아가게 합니다.
/// 4. 차징 중에는 외부에서 UpdateChargePreviewAim을 호출하여 현재 타겟 방향으로 계속 조준 회전을 갱신할 수 있고, 발사 후에는 저장된 방향으로 직선 이동합니다.
/// </summary>
[DisallowMultipleComponent]
public class EnemyProjectile2D : MonoBehaviour
{
    [Header("1. 참조 설정")]
    [SerializeField, Tooltip("투사체의 Rigidbody2D입니다.\n"
                             + "비워두면 자동으로 찾아서 연결합니다.")]
    private Rigidbody2D rb;

    [SerializeField, Tooltip("투사체의 충돌 판정용 Collider2D입니다.\n"
                             + "Is Trigger를 체크해야 합니다.\n"
                             + "비워두면 자동으로 찾아서 연결합니다.")]
    private Collider2D triggerCollider;

    [Header("2. 이동 설정")]
    [SerializeField, Min(0.01f), Tooltip("투사체가 날아가는 속도입니다.\n"
                                         + "단위는 world unit / second입니다.")]
    private float moveSpeed = 8f;

    [SerializeField, Tooltip("체크하면 발사 방향에 맞춰 투사체가 회전합니다.\n"
                             + "스프라이트가 오른쪽(→)을 기본 정면으로 보고 있을 때 켜두면 편합니다.")]
    private bool rotateToMoveDirection = true;

    [Header("3. 데미지 설정")]
    [SerializeField, Min(0), Tooltip("플레이어에게 전달할 피해량입니다.")]
    private int damage = 10;

    [SerializeField, Tooltip("피해를 받을 플레이어의 태그입니다.\n"
                             + "보통 Player를 사용합니다.")]
    private string playerTag = "Player";

    [SerializeField, Tooltip("플레이어에게 피해를 전달할 메서드 이름입니다.\n"
                             + "기본값은 TakeDamage입니다.")]
    private string damageMethodName = "TakeDamage";

    [SerializeField, Tooltip("이 태그를 가진 오브젝트와는 충돌 시 무시합니다.\n"
                             + "기본값은 Enemy이며,\n"
                             + "자기 자신이나 다른 몬스터를 맞고 사라지는 일을 줄이기 위한 값입니다.")]
    private string ignoreTag = "Enemy";

    [Header("4. 수명 및 화면 밖 제거 설정")]
    [SerializeField, Min(0.01f), Tooltip("이 시간이 지나면 투사체를 자동으로 제거합니다.")]
    private float lifeTime = 5f;

    [SerializeField, Tooltip("체크하면 화면 밖으로 충분히 벗어났을 때 투사체를 제거합니다.")]
    private bool destroyWhenOffscreen = true;

    [SerializeField, Min(0f), Tooltip("생성 직후 바로 제거되는 것을 막기 위해\n"
                                      + "화면 밖 판정을 시작하기 전 잠깐 대기하는 시간입니다.")]
    private float offscreenCheckDelay = 0.15f;

    [SerializeField, Min(0f), Tooltip("화면 밖 제거 판정 여유값입니다.\n"
                                      + "0이면 화면 경계 바깥으로 나가는 즉시 제거되고,\n"
                                      + "값이 클수록 조금 더 멀리 나간 뒤 제거됩니다.")]
    private float offscreenPadding = 0.1f;

    private Vector2 _moveDirection = Vector2.right;
    private float _lifeTimer;
    private float _offscreenTimer;
    private bool _isLaunched;
    private bool _isChargePreview;
    private Camera _mainCamera;

    private Vector3 _authoredLocalScale = Vector3.one;
    private Vector3 _authoredWorldScale = Vector3.one;

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

        CacheAuthoredScale();
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (!enabled)
            return;

        if (transform.parent == null)
            CacheAuthoredScale();

        _lifeTimer = 0f;
        _offscreenTimer = 0f;
        _isLaunched = false;
        _isChargePreview = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (triggerCollider != null)
            triggerCollider.enabled = true;
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
    /// 런타임에 투사체 속도와 피해량을 주입합니다.
    /// 원거리 몬스터 SO의 설계값을 실제 투사체에 반영할 때 사용합니다.
    /// </summary>
    public void ConfigureRuntime(float runtimeMoveSpeed, int runtimeDamage)
    {
        moveSpeed = Mathf.Max(0.01f, runtimeMoveSpeed);
        damage = Mathf.Max(0, runtimeDamage);
    }

    /// <summary>
    /// 차징 중 손에 들고 있는 것처럼 보이도록 미리보기 상태로 전환합니다.
    /// 부모가 바뀌어도 프리팹의 원래 world scale이 유지되도록 보정합니다.
    /// </summary>
    public void PrepareForChargePreview(Transform previewAnchor, Vector2 direction)
    {
        if (!enabled)
            return;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.right;

        _moveDirection = direction.normalized;
        _lifeTimer = 0f;
        _offscreenTimer = 0f;
        _isLaunched = false;
        _isChargePreview = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (triggerCollider != null)
            triggerCollider.enabled = false;

        if (previewAnchor != null)
        {
            transform.SetParent(previewAnchor, true);
            transform.localPosition = Vector3.zero;
            ApplyAuthoredWorldScale();
        }

        UpdateChargePreviewAim(_moveDirection);
    }

    /// <summary>
    /// 차징 미리보기 상태에서 현재 타겟 방향으로 조준을 계속 갱신합니다.
    /// </summary>
    public void UpdateChargePreviewAim(Vector2 direction)
    {
        if (!enabled)
            return;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        _moveDirection = direction.normalized;

        if (rotateToMoveDirection)
            transform.right = _moveDirection;

        if (_isChargePreview)
            ApplyAuthoredWorldScale();
    }

    /// <summary>
    /// 차징 미리보기 상태를 끝내고 실제 투사체로 발사합니다.
    /// 부모에서 분리한 뒤에도 프리팹 원래 world scale을 유지합니다.
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
        _isChargePreview = false;
        _isLaunched = true;

        transform.SetParent(null, true);

        if (rotateToMoveDirection)
            transform.right = _moveDirection;

        ApplyAuthoredWorldScale();

        if (triggerCollider != null)
            triggerCollider.enabled = true;

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
        _isLaunched = false;
        _isChargePreview = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Destroy(gameObject);
    }

    private void CacheAuthoredScale()
    {
        _authoredLocalScale = transform.localScale;
        _authoredWorldScale = transform.lossyScale;
    }

    private void ApplyAuthoredWorldScale()
    {
        SetWorldScale(_authoredWorldScale);
    }

    private void SetWorldScale(Vector3 targetWorldScale)
    {
        Transform parent = transform.parent;

        if (parent == null)
        {
            transform.localScale = targetWorldScale;
            return;
        }

        Vector3 parentLossyScale = parent.lossyScale;

        transform.localScale = new Vector3(
            SafeDivideScale(targetWorldScale.x, parentLossyScale.x, _authoredLocalScale.x),
            SafeDivideScale(targetWorldScale.y, parentLossyScale.y, _authoredLocalScale.y),
            SafeDivideScale(targetWorldScale.z, parentLossyScale.z, _authoredLocalScale.z)
        );
    }

    private float SafeDivideScale(float targetWorld, float parentWorld, float fallbackLocal)
    {
        if (Mathf.Abs(parentWorld) <= 0.0001f)
            return fallbackLocal;

        return targetWorld / parentWorld;
    }
}