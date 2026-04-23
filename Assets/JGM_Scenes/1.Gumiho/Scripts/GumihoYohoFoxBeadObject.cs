// UTF-8
using System;
using UnityEngine;

// 구현 원리 요약:
// 여우구슬 1개의 독립 동작만 담당한다.
// 활성 중에는 플레이어를 따라가고,
// 폭발 직전에는 경고 원을 띄우고 멈춘다.
// 폭발 후에는 경고 원을 지우고 잠깐 멈춘 다음 다시 추적한다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class GumihoYohoFoxBeadObject : MonoBehaviour
{
    [Header("참조")]

    [Tooltip("여우구슬 충돌 범위 시각화나 바디 충돌에 사용하는 콜라이더입니다.")]
    [SerializeField] private Collider2D bodyCollider;

    [Tooltip("여우구슬에 Rigidbody2D가 있다면 캐시만 합니다.\n이동은 transform.position으로 처리합니다.")]
    [SerializeField] private Rigidbody2D rb;


    [Header("디버그")]

    [Tooltip("디버그 로그를 출력할지 여부입니다.")]
    [SerializeField] private bool debugLog = false;


    private Action<GumihoYohoFoxBeadObject> returnAction;
    private BossTargetProvider targetProvider;

    private float followSpeed;
    private float minDistanceToTarget;

    private float explosionInterval;
    private float explosionRadius;
    private int explosionDamage;
    private LayerMask targetLayerMask;
    private float preExplosionDelay;
    private float postExplosionPause;

    private GameObject explosionEffectPrefab;
    private Vector3 explosionEffectScale;
    private float explosionEffectLifetime;

    private Sprite explosionWarningSprite;
    private Color explosionWarningColor;
    private int explosionWarningSortingOrder;
    private Vector3 explosionWarningScale = Vector3.one;

    private float lifeTimer;
    private float explosionTimer;
    private float preExplosionTimer;
    private float postExplosionPauseTimer;

    private bool isRunning;
    private bool isPreparingExplosion;
    private bool isPostExplosionPause;

    private GameObject warningVisualObject;
    private SpriteRenderer warningSpriteRenderer;


    private void Reset()
    {
        bodyCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = true;
        }

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }

    private void OnEnable()
    {
        isRunning = false;
        isPreparingExplosion = false;
        isPostExplosionPause = false;

        lifeTimer = 0f;
        explosionTimer = 0f;
        preExplosionTimer = 0f;
        postExplosionPauseTimer = 0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        HideWarningVisual();
    }

    private void OnDisable()
    {
        HideWarningVisual();
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            ReturnToPool();
            return;
        }

        UpdateWarningVisualPosition();

        if (isPreparingExplosion)
        {
            preExplosionTimer -= Time.deltaTime;

            if (preExplosionTimer <= 0f)
            {
                ExecuteExplosion();
                HideWarningVisual();

                isPreparingExplosion = false;
                isPostExplosionPause = postExplosionPause > 0f;
                postExplosionPauseTimer = postExplosionPause;
                explosionTimer = explosionInterval;
            }

            return;
        }

        if (isPostExplosionPause)
        {
            postExplosionPauseTimer -= Time.deltaTime;

            if (postExplosionPauseTimer <= 0f)
            {
                isPostExplosionPause = false;
            }

            return;
        }

        UpdateFollow();

        explosionTimer -= Time.deltaTime;
        if (explosionTimer <= 0f)
        {
            isPreparingExplosion = true;
            preExplosionTimer = Mathf.Max(0f, preExplosionDelay);
            ShowWarningVisual();
        }
    }

    public void BindReturn(Action<GumihoYohoFoxBeadObject> onReturn)
    {
        returnAction = onReturn;
    }

    public void Begin(
        Vector3 spawnPosition,
        BossTargetProvider provider,
        float followSpeedValue,
        float minDistanceValue,
        float lifeTimeValue,
        float explosionRadiusValue,
        float explosionIntervalValue,
        int explosionDamageValue,
        LayerMask targetMask,
        float preExplosionDelayValue,
        float postExplosionPauseValue,
        GameObject explosionEffectPrefabValue,
        Vector3 explosionEffectScaleValue,
        float explosionEffectLifetimeValue,
        Sprite explosionWarningSpriteValue,
        Color explosionWarningColorValue,
        int explosionWarningSortingOrderValue,
        Vector3 explosionWarningScaleValue,
        Vector3 foxBeadScaleValue)
    {
        transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        transform.rotation = Quaternion.identity;
        transform.localScale = foxBeadScaleValue;

        targetProvider = provider;

        followSpeed = Mathf.Max(0.1f, followSpeedValue);
        minDistanceToTarget = Mathf.Max(0f, minDistanceValue);

        lifeTimer = Mathf.Max(0.1f, lifeTimeValue);
        explosionRadius = Mathf.Max(0.05f, explosionRadiusValue);
        explosionInterval = Mathf.Max(0.05f, explosionIntervalValue);
        explosionDamage = Mathf.Max(1, explosionDamageValue);
        targetLayerMask = targetMask;

        preExplosionDelay = Mathf.Max(0f, preExplosionDelayValue);
        postExplosionPause = Mathf.Max(0f, postExplosionPauseValue);

        explosionEffectPrefab = explosionEffectPrefabValue;
        explosionEffectScale = explosionEffectScaleValue;
        explosionEffectLifetime = Mathf.Max(0.1f, explosionEffectLifetimeValue);

        explosionWarningSprite = explosionWarningSpriteValue;
        explosionWarningColor = explosionWarningColorValue;
        explosionWarningSortingOrder = explosionWarningSortingOrderValue;
        explosionWarningScale = explosionWarningScaleValue;

        explosionTimer = explosionInterval;
        preExplosionTimer = 0f;
        postExplosionPauseTimer = 0f;

        isRunning = true;
        isPreparingExplosion = false;
        isPostExplosionPause = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        HideWarningVisual();

        if (debugLog)
        {
            Debug.Log($"[GumihoYohoFoxBeadObject] 여우구슬 시작 | explosionRadius={explosionRadius}", this);
        }
    }

    private void UpdateFollow()
    {
        Transform target = GetCurrentTarget();
        if (target == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("[GumihoYohoFoxBeadObject] 현재 타겟을 찾지 못했습니다.", this);
            }

            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = target.position;
        targetPosition.z = currentPosition.z;

        float distance = Vector3.Distance(currentPosition, targetPosition);
        if (distance <= minDistanceToTarget)
        {
            return;
        }

        float moveStep = followSpeed * Time.deltaTime;
        Vector3 nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveStep);
        transform.position = nextPosition;
    }

    private Transform GetCurrentTarget()
    {
        if (targetProvider == null)
        {
            return null;
        }

        if (!targetProvider.HasTarget())
        {
            return null;
        }

        return targetProvider.GetTarget();
    }

    private void ExecuteExplosion()
    {
        SpawnExplosionEffect();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayerMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            GameObject targetObject = hit.attachedRigidbody != null
                ? hit.attachedRigidbody.gameObject
                : hit.gameObject;

            PlayerHealth playerHealth = targetObject.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = targetObject.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(explosionDamage, transform.position);
            }
            else
            {
                targetObject.SendMessage("TakeDamage", explosionDamage, SendMessageOptions.DontRequireReceiver);
            }
        }

        if (debugLog)
        {
            Debug.Log($"[GumihoYohoFoxBeadObject] 폭발 실행 | radius={explosionRadius} damage={explosionDamage}", this);
        }
    }

    private void SpawnExplosionEffect()
    {
        if (explosionEffectPrefab == null)
        {
            return;
        }

        GameObject spawnedEffect = Instantiate(
            explosionEffectPrefab,
            transform.position,
            Quaternion.identity);

        if (spawnedEffect != null)
        {
            spawnedEffect.transform.localScale = explosionEffectScale;
            Destroy(spawnedEffect, explosionEffectLifetime);
        }
    }

    private void ShowWarningVisual()
    {
        if (explosionWarningSprite == null)
        {
            return;
        }

        if (warningVisualObject == null)
        {
            warningVisualObject = new GameObject("[FoxBeadExplosionWarning]");
            warningVisualObject.transform.SetParent(null);

            warningSpriteRenderer = warningVisualObject.AddComponent<SpriteRenderer>();
        }

        warningVisualObject.transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        warningVisualObject.transform.rotation = Quaternion.identity;
        warningVisualObject.transform.localScale = explosionWarningScale;

        warningSpriteRenderer.sprite = explosionWarningSprite;
        warningSpriteRenderer.color = explosionWarningColor;
        warningSpriteRenderer.sortingOrder = explosionWarningSortingOrder;

        warningVisualObject.SetActive(true);
    }

    private void HideWarningVisual()
    {
        if (warningVisualObject != null)
        {
            warningVisualObject.SetActive(false);
        }
    }

    private void UpdateWarningVisualPosition()
    {
        if (warningVisualObject == null || !warningVisualObject.activeSelf)
        {
            return;
        }

        warningVisualObject.transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
    }

    public void ReturnToPool()
    {
        isRunning = false;
        isPreparingExplosion = false;
        isPostExplosionPause = false;

        HideWarningVisual();

        targetProvider = null;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        gameObject.SetActive(false);
        returnAction?.Invoke(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.08f);
    }
#endif
}