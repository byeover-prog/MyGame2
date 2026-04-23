// UTF-8
using System.Collections.Generic;
using UnityEngine;

// 구현 원리 요약:
// 요호의 여우구슬 패턴 전체 진행을 담당한다.
// 쿨타임이 되면 패턴 시작 연출을 먼저 재생하고,
// 그 다음 여우구슬을 생성한다.
// 시작 연출은 보스 자식으로 붙여서 보스 이동을 따라가게 한다.

[DisallowMultipleComponent]
public sealed class GumihoYohoFoxBeadPatternController : MonoBehaviour
{
    [Header("패턴 카탈로그")]

    [Tooltip("구미호 패턴 카탈로그 SO입니다.")]
    [SerializeField] private GumihoPatternCatalogSO patternCatalog;


    [Header("참조")]

    [Tooltip("보스 공용 타겟 제공 컴포넌트입니다.")]
    [SerializeField] private BossTargetProvider targetProvider;

    [Tooltip("기본 공격 강화 런타임 상태 컴포넌트입니다.")]
    [SerializeField] private GumihoAttackEnhancementRuntime attackEnhancementRuntime;

    [Tooltip("비활성 여우구슬을 넣어둘 풀 루트입니다. 비어 있으면 자동 생성합니다.")]
    [SerializeField] private Transform poolRoot;

    [Tooltip("여우구슬 패턴 시작 연출을 붙일 기준 위치입니다. 비어 있으면 보스 자신을 사용합니다.")]
    [SerializeField] private Transform patternStartEffectAnchor;


    [Header("디버그")]

    [Tooltip("디버그 로그를 출력할지 여부입니다.")]
    [SerializeField] private bool debugLog = false;


    private readonly Queue<GumihoYohoFoxBeadObject> pooledBeads = new Queue<GumihoYohoFoxBeadObject>(8);
    private readonly List<GumihoYohoFoxBeadObject> activeBeads = new List<GumihoYohoFoxBeadObject>(4);

    private GumihoYohoFoxBeadPatternConfigSO config;

    private float cooldownTimer;
    private bool isPatternRunning;


    private void Reset()
    {
        targetProvider = GetComponent<BossTargetProvider>();
        attackEnhancementRuntime = GetComponent<GumihoAttackEnhancementRuntime>();
        patternStartEffectAnchor = transform;
    }

    private void Awake()
    {
        if (targetProvider == null)
        {
            targetProvider = GetComponent<BossTargetProvider>();
        }

        if (attackEnhancementRuntime == null)
        {
            attackEnhancementRuntime = GetComponent<GumihoAttackEnhancementRuntime>();
        }

        if (patternStartEffectAnchor == null)
        {
            patternStartEffectAnchor = transform;
        }

        if (patternCatalog != null)
        {
            config = patternCatalog.YohoFoxBeadPatternConfig;
        }

        EnsurePoolRoot();
        WarmPool();
    }

    private void OnEnable()
    {
        if (patternCatalog != null)
        {
            config = patternCatalog.YohoFoxBeadPatternConfig;
        }

        ClearAllBeads();

        if (config == null)
        {
            return;
        }

        cooldownTimer = config.PlayOnEnable ? 0f : config.Cooldown;
    }

    private void OnDisable()
    {
        ClearAllBeads();

        if (attackEnhancementRuntime != null)
        {
            attackEnhancementRuntime.ClearEnhancement();
        }
    }

    private void Update()
    {
        if (config == null)
        {
            return;
        }

        if (!isPatternRunning)
        {
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer <= 0f && HasValidTarget())
            {
                StartPattern();
            }

            return;
        }

        CleanupReleasedBeads();

        if (activeBeads.Count == 0)
        {
            FinishPattern();
        }
    }

    private bool HasValidTarget()
    {
        if (targetProvider == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("[GumihoYohoFoxBeadPatternController] BossTargetProvider가 연결되지 않았습니다.", this);
            }

            return false;
        }

        return targetProvider.HasTarget() && targetProvider.GetTarget() != null;
    }

    private void EnsurePoolRoot()
    {
        if (poolRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("[GumihoYohoFoxBeadPool]");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        poolRoot = root.transform;
    }

    private void WarmPool()
    {
        if (config == null || config.FoxBeadPrefab == null)
        {
            return;
        }

        int needCount = Mathf.Max(1, config.PrewarmCount);

        for (int i = pooledBeads.Count; i < needCount; i++)
        {
            GumihoYohoFoxBeadObject bead = CreateNewBead();
            ReturnBeadToPool(bead);
        }
    }

    private GumihoYohoFoxBeadObject CreateNewBead()
    {
        GumihoYohoFoxBeadObject bead = Instantiate(config.FoxBeadPrefab, poolRoot);
        bead.gameObject.SetActive(false);
        bead.BindReturn(ReturnBeadToPool);
        return bead;
    }

    private GumihoYohoFoxBeadObject GetBead()
    {
        if (pooledBeads.Count == 0)
        {
            pooledBeads.Enqueue(CreateNewBead());
        }

        GumihoYohoFoxBeadObject bead = pooledBeads.Dequeue();
        bead.transform.SetParent(null);
        bead.gameObject.SetActive(true);
        return bead;
    }

    private void ReturnBeadToPool(GumihoYohoFoxBeadObject bead)
    {
        if (bead == null)
        {
            return;
        }

        activeBeads.Remove(bead);
        bead.transform.SetParent(poolRoot);

        if (!pooledBeads.Contains(bead))
        {
            pooledBeads.Enqueue(bead);
        }
    }

    private void StartPattern()
    {
        isPatternRunning = true;

        SpawnPatternStartEffect();
        ApplyAttackEnhancement();
        SpawnBead();

        if (debugLog)
        {
            Debug.Log("[GumihoYohoFoxBeadPatternController] 여우구슬 패턴 시작", this);
        }
    }

    private void FinishPattern()
    {
        isPatternRunning = false;
        cooldownTimer = config.Cooldown;

        if (attackEnhancementRuntime != null)
        {
            attackEnhancementRuntime.ClearEnhancement();
        }

        if (debugLog)
        {
            Debug.Log("[GumihoYohoFoxBeadPatternController] 여우구슬 패턴 종료", this);
        }
    }

    private void SpawnPatternStartEffect()
    {
        if (config == null || config.PatternStartEffectPrefab == null)
        {
            return;
        }

        Transform anchor = patternStartEffectAnchor != null ? patternStartEffectAnchor : transform;

        GameObject effectInstance = Instantiate(config.PatternStartEffectPrefab, anchor);

        if (effectInstance == null)
        {
            return;
        }

        effectInstance.transform.localPosition = config.PatternStartEffectOffset;
        effectInstance.transform.localRotation = Quaternion.identity;
        effectInstance.transform.localScale = config.PatternStartEffectScale;

        Destroy(effectInstance, Mathf.Max(0.1f, config.PatternStartEffectLifetime));
    }

    private void ApplyAttackEnhancement()
    {
        if (attackEnhancementRuntime == null)
        {
            return;
        }

        attackEnhancementRuntime.ApplyEnhancement(
            config.EnhancedProjectileCount,
            config.EnhancedDamageMultiplier,
            config.EnhancedSpreadAngle);
    }

    private void SpawnBead()
    {
        Vector3 spawnPosition = transform.position + (Vector3)config.SpawnLocalOffset;
        Vector2 jitter = Random.insideUnitCircle * config.SpawnJitterRadius;
        spawnPosition += new Vector3(jitter.x, jitter.y, 0f);

        float finalExplosionRadius = CalculateExplosionRadius();
        Vector3 finalWarningScale = CalculateWarningScale(finalExplosionRadius);

        GumihoYohoFoxBeadObject bead = GetBead();
        bead.Begin(
            spawnPosition,
            targetProvider,
            config.FollowSpeed,
            config.MinDistanceToTarget,
            config.BeadLifetime,
            finalExplosionRadius,
            config.ExplosionInterval,
            config.ExplosionDamage,
            config.TargetLayerMask,
            config.PreExplosionDelay,
            config.PostExplosionPause,
            config.ExplosionEffectPrefab,
            config.ExplosionEffectScale,
            config.ExplosionEffectLifetime,
            config.ExplosionWarningSprite,
            config.ExplosionWarningColor,
            config.ExplosionWarningSortingOrder,
            finalWarningScale,
            config.FoxBeadScale);

        activeBeads.Add(bead);

        if (debugLog)
        {
            Debug.Log($"[GumihoYohoFoxBeadPatternController] 여우구슬 생성 | explosionRadius={finalExplosionRadius}", this);
        }
    }

    private float CalculateExplosionRadius()
    {
        if (config == null)
        {
            return 1f;
        }

        if (!config.UseExplosionEffectVisualSize)
        {
            return Mathf.Max(0.05f, config.ManualExplosionRadius);
        }

        if (config.ExplosionEffectPrefab == null)
        {
            return Mathf.Max(0.05f, config.ManualExplosionRadius);
        }

        float visualRadius = GetPrefabVisualRadius(config.ExplosionEffectPrefab, config.ExplosionEffectScale);
        if (visualRadius <= 0f)
        {
            return Mathf.Max(0.05f, config.ManualExplosionRadius);
        }

        return Mathf.Max(0.05f, visualRadius * config.ExplosionRadiusMultiplier);
    }

    private Vector3 CalculateWarningScale(float explosionRadius)
    {
        if (config == null)
        {
            return Vector3.one;
        }

        if (!config.AutoFitWarningToExplosionRadius)
        {
            return config.ManualWarningScale;
        }

        if (config.ExplosionWarningSprite == null)
        {
            return Vector3.one;
        }

        Bounds spriteBounds = config.ExplosionWarningSprite.bounds;
        float spriteDiameter = Mathf.Max(spriteBounds.size.x, spriteBounds.size.y);

        if (spriteDiameter <= 0.0001f)
        {
            return Vector3.one;
        }

        float targetDiameter = explosionRadius * 2f;
        float uniformScale = (targetDiameter / spriteDiameter) * config.WarningScaleMultiplier;

        return new Vector3(uniformScale, uniformScale, 1f);
    }

    private float GetPrefabVisualRadius(GameObject prefab, Vector3 scale)
    {
        SpriteRenderer[] spriteRenderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            return 0f;
        }

        bool hasBounds = false;
        Bounds mergedBounds = new Bounds();

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer renderer = spriteRenderers[i];
            if (renderer == null || renderer.sprite == null)
            {
                continue;
            }

            Bounds localBounds = renderer.localBounds;
            Vector3 scaledSize = Vector3.Scale(localBounds.size, scale);
            Bounds scaledBounds = new Bounds(Vector3.Scale(localBounds.center, scale), scaledSize);

            if (!hasBounds)
            {
                mergedBounds = scaledBounds;
                hasBounds = true;
            }
            else
            {
                mergedBounds.Encapsulate(scaledBounds.min);
                mergedBounds.Encapsulate(scaledBounds.max);
            }
        }

        if (!hasBounds)
        {
            return 0f;
        }

        return Mathf.Max(mergedBounds.extents.x, mergedBounds.extents.y);
    }

    private void CleanupReleasedBeads()
    {
        for (int i = activeBeads.Count - 1; i >= 0; i--)
        {
            if (activeBeads[i] == null || !activeBeads[i].gameObject.activeSelf)
            {
                activeBeads.RemoveAt(i);
            }
        }
    }

    private void ClearAllBeads()
    {
        for (int i = activeBeads.Count - 1; i >= 0; i--)
        {
            if (activeBeads[i] != null)
            {
                activeBeads[i].ReturnToPool();
            }
        }

        activeBeads.Clear();
        isPatternRunning = false;
    }
}