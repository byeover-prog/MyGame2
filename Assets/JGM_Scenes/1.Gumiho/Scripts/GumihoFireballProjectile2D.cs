// UTF-8
using System;
using UnityEngine;

// 구현 원리 요약:
// 구미호 화염구 1개의 발사/이동/충돌/반환을 담당한다.
// 보스 기본 공격 전용 투사체지만, 구조는 다른 보스 투사체에도 재사용 가능하게 만든다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class GumihoFireballProjectile2D : MonoBehaviour
{
    [Header("참조")]

    [Tooltip("화염구 Rigidbody2D입니다.")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("화염구 충돌 콜라이더입니다.")]
    [SerializeField] private Collider2D hitCollider;


    private Action<GumihoFireballProjectile2D> returnAction;

    private bool isLaunched;
    private bool hasHit;

    private int damage;
    private float lifeTimer;
    private LayerMask targetLayerMask;

    private Collider2D[] ignoredOwnerColliders;


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

        hitCollider.isTrigger = true;
        hitCollider.enabled = false;
    }

    private void OnEnable()
    {
        isLaunched = false;
        hasHit = false;
        lifeTimer = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (!isLaunched)
        {
            return;
        }

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            ReturnToPool();
        }
    }

    private void OnDisable()
    {
        ClearOwnerIgnore();
    }

    public void BindReturn(Action<GumihoFireballProjectile2D> onReturn)
    {
        returnAction = onReturn;
    }

    public void Launch(
        Vector2 startPosition,
        Vector2 direction,
        float speed,
        float lifeSeconds,
        int damageValue,
        LayerMask mask,
        Transform owner)
    {
        transform.position = startPosition;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }

        direction = direction.normalized;

        damage = Mathf.Max(1, damageValue);
        lifeTimer = Mathf.Max(0.1f, lifeSeconds);
        targetLayerMask = mask;

        isLaunched = true;
        hasHit = false;

        hitCollider.enabled = true;

        rb.linearVelocity = direction * Mathf.Max(0.1f, speed);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        IgnoreOwnerCollision(owner);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isLaunched)
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

        GameObject hitTarget = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        PlayerHealth playerHealth = hitTarget.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = hitTarget.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, transform.position);
        }
        else
        {
            hitTarget.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        ReturnToPool();
    }

    public void ReturnToPool()
    {
        isLaunched = false;
        hasHit = false;

        rb.linearVelocity = Vector2.zero;
        hitCollider.enabled = false;

        gameObject.SetActive(false);
        returnAction?.Invoke(this);
    }

    private void IgnoreOwnerCollision(Transform owner)
    {
        ClearOwnerIgnore();

        if (owner == null)
        {
            return;
        }

        ignoredOwnerColliders = owner.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < ignoredOwnerColliders.Length; i++)
        {
            Collider2D ownerCollider = ignoredOwnerColliders[i];
            if (ownerCollider == null)
            {
                continue;
            }

            Physics2D.IgnoreCollision(hitCollider, ownerCollider, true);
        }
    }

    private void ClearOwnerIgnore()
    {
        if (ignoredOwnerColliders == null)
        {
            return;
        }

        for (int i = 0; i < ignoredOwnerColliders.Length; i++)
        {
            Collider2D ownerCollider = ignoredOwnerColliders[i];
            if (ownerCollider == null)
            {
                continue;
            }

            Physics2D.IgnoreCollision(hitCollider, ownerCollider, false);
        }

        ignoredOwnerColliders = null;
    }
}