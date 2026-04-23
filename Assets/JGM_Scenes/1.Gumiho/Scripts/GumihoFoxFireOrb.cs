// UTF-8
using System;
using UnityEngine;

// 구현 원리 요약:
// 여우불 1개의 동작만 담당한다.
// 공전 중에는 목표 슬롯을 부드럽게 따라가고,
// 발사 후에는 Rigidbody2D 속도로 날아간다.
// 발사 순간에만 플레이어 방향으로 비주얼 각도를 한 번 맞춘다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class GumihoFoxFireOrb : MonoBehaviour
{
    [Header("참조")]

    [Tooltip("여우불 Rigidbody2D입니다.")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("여우불 충돌 콜라이더입니다.")]
    [SerializeField] private Collider2D hitCollider;

    [Header("발사 방향 설정")]

    [Tooltip("원본 이펙트 방향 보정값입니다. 위쪽을 보고 있는 이펙트면 보통 -90을 사용합니다.")]
    [SerializeField] private float launchAngleOffset = -90f;


    private Action<GumihoFoxFireOrb> returnAction;

    private bool isOrbiting;
    private bool isLaunching;
    private bool hasHit;

    private Vector3 orbitTargetPosition;
    private float orbitMoveSpeed;

    private float spawnScaleDuration;
    private float spawnElapsed;

    private float launchLifetimeTimer;
    private int damage;
    private LayerMask targetLayerMask;


    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        hitCollider = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (hitCollider == null)
        {
            hitCollider = GetComponent<Collider2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;

        if (hitCollider != null)
        {
            hitCollider.isTrigger = true;
            hitCollider.enabled = false;
        }
    }

    private void OnEnable()
    {
        hasHit = false;
        launchLifetimeTimer = 0f;
    }

    private void Update()
    {
        if (isOrbiting)
        {
            Vector3 nextPosition = Vector3.MoveTowards(
                transform.position,
                orbitTargetPosition,
                orbitMoveSpeed * Time.deltaTime);

            transform.position = nextPosition;
            UpdateSpawnScale();
            return;
        }

        if (isLaunching)
        {
            launchLifetimeTimer -= Time.deltaTime;

            if (launchLifetimeTimer <= 0f)
            {
                ReturnToPool();
            }
        }
    }

    public void BindReturn(Action<GumihoFoxFireOrb> onReturn)
    {
        returnAction = onReturn;
    }

    public void BeginOrbit(
        Vector3 spawnPosition,
        float orbitMoveSpeedValue,
        float scaleDuration)
    {
        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.zero;

        orbitTargetPosition = spawnPosition;
        orbitMoveSpeed = Mathf.Max(0.1f, orbitMoveSpeedValue);

        spawnScaleDuration = Mathf.Max(0.01f, scaleDuration);
        spawnElapsed = 0f;

        isOrbiting = true;
        isLaunching = false;
        hasHit = false;

        rb.linearVelocity = Vector2.zero;

        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }
    }

    public void SetOrbitTarget(Vector3 targetPosition)
    {
        if (!isOrbiting)
        {
            return;
        }

        orbitTargetPosition = targetPosition;
    }

    public void Launch(
        Vector2 direction,
        float launchSpeed,
        float launchLifetime,
        int damageValue,
        LayerMask layerMask)
    {
        isOrbiting = false;
        isLaunching = true;
        hasHit = false;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }

        direction = direction.normalized;

        damage = Mathf.Max(1, damageValue);
        targetLayerMask = layerMask;
        launchLifetimeTimer = Mathf.Max(0.1f, launchLifetime);

        rb.linearVelocity = direction * Mathf.Max(0.1f, launchSpeed);

        if (hitCollider != null)
        {
            hitCollider.enabled = true;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + launchAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = Vector3.one;
    }

    private void UpdateSpawnScale()
    {
        spawnElapsed += Time.deltaTime;

        float t = Mathf.Clamp01(spawnElapsed / spawnScaleDuration);
        float scale = Mathf.LerpUnclamped(0f, 1f, EaseOutBack(t));
        transform.localScale = Vector3.one * scale;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isLaunching)
        {
            return;
        }

        if (hasHit)
        {
            return;
        }

        int otherMask = 1 << other.gameObject.layer;
        if ((targetLayerMask.value & otherMask) == 0)
        {
            return;
        }

        hasHit = true;

        GameObject targetObject = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        PlayerHealth playerHealth = targetObject.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = targetObject.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, transform.position);
        }
        else
        {
            targetObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        ReturnToPool();
    }

    public void ReturnToPool()
    {
        isOrbiting = false;
        isLaunching = false;
        hasHit = false;

        rb.linearVelocity = Vector2.zero;

        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }

        gameObject.SetActive(false);
        returnAction?.Invoke(this);
    }
}