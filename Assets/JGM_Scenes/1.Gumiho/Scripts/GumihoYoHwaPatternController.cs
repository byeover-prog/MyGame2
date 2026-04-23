// UTF-8
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 구현 원리 요약:
// 요화 패턴 전체 진행을 담당한다.
// 여우불은 생성될 때마다 최종 슬롯 기준으로 하나씩 채워 넣는다.
// 그래서 새 여우불이 생겨도 기존 여우불이 역방향으로 재정렬되지 않는다.
// 생성 직후에는 코루틴으로 자기 슬롯까지 자연스럽게 합류하고,
// 합류가 끝나면 전체 공전에 그대로 편입된다.

[DisallowMultipleComponent]
public sealed class GumihoYoHwaPatternController : MonoBehaviour
{
    [Header("패턴 설정 SO")]

    [Tooltip("구미호 요화 패턴 설정 SO입니다.")]
    [SerializeField] private GumihoYoHwaPatternConfigSO patternConfig;


    [Header("참조")]

    [Tooltip("보스 공용 타겟 제공 컴포넌트입니다.")]
    [SerializeField] private BossTargetProvider targetProvider;

    [Tooltip("공전 중심 Transform입니다. 비어 있으면 자기 자신을 사용합니다.")]
    [SerializeField] private Transform orbitAnchor;

    [Tooltip("비활성 여우불을 넣어둘 풀 루트입니다. 비어 있으면 자동 생성합니다.")]
    [SerializeField] private Transform poolRoot;


    [Header("공전 보정")]

    [Tooltip("여우불이 공전 슬롯을 따라가는 이동 속도입니다.")]
    [Min(0.1f)]
    [SerializeField] private float orbitFollowSpeed = 10f;

    [Tooltip("생성된 여우불이 자기 슬롯에 자연스럽게 합류하는 시간입니다.")]
    [Min(0.01f)]
    [SerializeField] private float spawnSettleDuration = 0.22f;


    [Header("디버그")]

    [Tooltip("디버그 로그를 출력할지 여부입니다.")]
    [SerializeField] private bool debugLog = false;


    private readonly List<GumihoFoxFireOrb> orbitingFoxFires = new List<GumihoFoxFireOrb>(16);
    private readonly Queue<GumihoFoxFireOrb> pooledFoxFires = new Queue<GumihoFoxFireOrb>(16);
    private readonly Dictionary<GumihoFoxFireOrb, int> foxFireSlotIndexMap = new Dictionary<GumihoFoxFireOrb, int>(16);
    private readonly List<Coroutine> spawnCoroutines = new List<Coroutine>(16);

    private float cooldownTimer;
    private float spawnTimer;
    private float launchDelayTimer;
    private float orbitAngle;

    private bool isPatternRunning;
    private bool isWaitingLaunch;


    private void Reset()
    {
        targetProvider = GetComponent<BossTargetProvider>();
        orbitAnchor = transform;
    }

    private void Awake()
    {
        if (targetProvider == null)
        {
            targetProvider = GetComponent<BossTargetProvider>();
        }

        if (orbitAnchor == null)
        {
            orbitAnchor = transform;
        }

        EnsurePoolRoot();
        WarmPool();
    }

    private void OnEnable()
    {
        ClearAllFoxFires();

        if (patternConfig == null)
        {
            return;
        }

        cooldownTimer = patternConfig.PlayOnEnable ? 0f : patternConfig.Cooldown;
    }

    private void OnDisable()
    {
        ClearAllFoxFires();
    }

    private void Update()
    {
        if (patternConfig == null)
        {
            return;
        }

        Transform target = GetCurrentTarget();

        if (isPatternRunning)
        {
            UpdateOrbit();

            if (!isWaitingLaunch)
            {
                spawnTimer += Time.deltaTime;

                if (orbitingFoxFires.Count < patternConfig.FoxFireCount &&
                    spawnTimer >= patternConfig.SpawnInterval)
                {
                    spawnTimer = 0f;
                    SpawnFoxFire();
                }

                if (orbitingFoxFires.Count >= patternConfig.FoxFireCount)
                {
                    isWaitingLaunch = true;
                    launchDelayTimer = patternConfig.LaunchDelay;
                }
            }
            else
            {
                launchDelayTimer -= Time.deltaTime;

                if (launchDelayTimer <= 0f)
                {
                    LaunchAllFoxFires(target);
                    FinishPattern();
                }
            }

            return;
        }

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f && target != null)
        {
            StartPattern();
        }
    }

    private Transform GetCurrentTarget()
    {
        if (targetProvider == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("[GumihoYoHwaPatternController] BossTargetProvider가 연결되지 않았습니다.", this);
            }

            return null;
        }

        if (!targetProvider.HasTarget())
        {
            return null;
        }

        return targetProvider.GetTarget();
    }

    private void EnsurePoolRoot()
    {
        if (poolRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("[GumihoFoxFirePool]");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        poolRoot = root.transform;
    }

    private void WarmPool()
    {
        if (patternConfig == null || patternConfig.FoxFirePrefab == null)
        {
            return;
        }

        int needCount = Mathf.Max(patternConfig.PrewarmCount, patternConfig.FoxFireCount);

        for (int i = pooledFoxFires.Count; i < needCount; i++)
        {
            GumihoFoxFireOrb foxFire = CreateNewFoxFire();
            ReturnFoxFireToPool(foxFire);
        }
    }

    private GumihoFoxFireOrb CreateNewFoxFire()
    {
        GumihoFoxFireOrb foxFire = Instantiate(patternConfig.FoxFirePrefab, poolRoot);
        foxFire.gameObject.SetActive(false);
        foxFire.BindReturn(ReturnFoxFireToPool);
        return foxFire;
    }

    private GumihoFoxFireOrb GetFoxFire()
    {
        if (pooledFoxFires.Count == 0)
        {
            pooledFoxFires.Enqueue(CreateNewFoxFire());
        }

        GumihoFoxFireOrb foxFire = pooledFoxFires.Dequeue();
        foxFire.transform.SetParent(null);
        foxFire.gameObject.SetActive(true);
        return foxFire;
    }

    private void ReturnFoxFireToPool(GumihoFoxFireOrb foxFire)
    {
        if (foxFire == null)
        {
            return;
        }

        orbitingFoxFires.Remove(foxFire);
        foxFireSlotIndexMap.Remove(foxFire);

        foxFire.transform.SetParent(poolRoot);

        if (!pooledFoxFires.Contains(foxFire))
        {
            pooledFoxFires.Enqueue(foxFire);
        }
    }

    private void StartPattern()
    {
        isPatternRunning = true;
        isWaitingLaunch = false;
        spawnTimer = 0f;
        launchDelayTimer = 0f;
        orbitAngle = 0f;

        if (debugLog)
        {
            Debug.Log("[GumihoYoHwaPatternController] 요화 패턴 시작", this);
        }
    }

    private void FinishPattern()
    {
        isPatternRunning = false;
        isWaitingLaunch = false;
        cooldownTimer = patternConfig.Cooldown;

        if (debugLog)
        {
            Debug.Log("[GumihoYoHwaPatternController] 요화 패턴 종료", this);
        }
    }

    private void SpawnFoxFire()
    {
        Vector3 anchorPosition = GetAnchorWorldPosition();
        Vector2 randomOffset = Random.insideUnitCircle * patternConfig.SpawnJitterRadius;
        Vector3 spawnPosition = anchorPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);

        GumihoFoxFireOrb foxFire = GetFoxFire();
        foxFire.BeginOrbit(
            spawnPosition,
            orbitFollowSpeed,
            patternConfig.SpawnScaleDuration);

        int slotIndex = orbitingFoxFires.Count;
        orbitingFoxFires.Add(foxFire);
        foxFireSlotIndexMap[foxFire] = slotIndex;

        Coroutine spawnRoutine = StartCoroutine(Co_SettleFoxFireToOrbit(foxFire, slotIndex, spawnPosition));
        spawnCoroutines.Add(spawnRoutine);
    }

    private IEnumerator Co_SettleFoxFireToOrbit(GumihoFoxFireOrb foxFire, int slotIndex, Vector3 spawnPosition)
    {
        float elapsed = 0f;

        while (elapsed < spawnSettleDuration)
        {
            if (foxFire == null || !foxFire.gameObject.activeSelf)
            {
                yield break;
            }

            elapsed += Time.deltaTime;

            Vector3 anchorPosition = GetAnchorWorldPosition();
            Vector3 slotPosition = GetSlotWorldPosition(anchorPosition, slotIndex);

            float t = Mathf.Clamp01(elapsed / spawnSettleDuration);
            float easedT = EaseOutCubic(t);

            Vector3 settledPosition = Vector3.Lerp(spawnPosition, slotPosition, easedT);
            foxFire.SetOrbitTarget(settledPosition);

            yield return null;
        }

        if (foxFire != null && foxFire.gameObject.activeSelf)
        {
            Vector3 anchorPosition = GetAnchorWorldPosition();
            Vector3 slotPosition = GetSlotWorldPosition(anchorPosition, slotIndex);
            foxFire.SetOrbitTarget(slotPosition);
        }
    }

    private void UpdateOrbit()
    {
        int count = orbitingFoxFires.Count;
        if (count == 0)
        {
            return;
        }

        orbitAngle += patternConfig.OrbitAngularSpeed * Time.deltaTime;
        Vector3 anchorPosition = GetAnchorWorldPosition();

        for (int i = 0; i < count; i++)
        {
            GumihoFoxFireOrb foxFire = orbitingFoxFires[i];
            if (foxFire == null)
            {
                continue;
            }

            if (!foxFireSlotIndexMap.TryGetValue(foxFire, out int slotIndex))
            {
                slotIndex = i;
                foxFireSlotIndexMap[foxFire] = slotIndex;
            }

            Vector3 slotPosition = GetSlotWorldPosition(anchorPosition, slotIndex);
            foxFire.SetOrbitTarget(slotPosition);
        }
    }

    private Vector3 GetSlotWorldPosition(Vector3 anchorPosition, int slotIndex)
    {
        int maxCount = Mathf.Max(1, patternConfig.FoxFireCount);
        float fixedAngleStep = 360f / maxCount;

        float angle = orbitAngle + (fixedAngleStep * slotIndex);
        float rad = angle * Mathf.Deg2Rad;

        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(rad) * patternConfig.OrbitRadius,
            Mathf.Sin(rad) * patternConfig.OrbitRadius,
            0f);

        float bob = Mathf.Sin((Time.time * patternConfig.OrbitBobFrequency) + slotIndex) * patternConfig.OrbitBobAmplitude;

        return anchorPosition + orbitOffset + new Vector3(0f, bob, 0f);
    }

    private void LaunchAllFoxFires(Transform target)
    {
        for (int i = orbitingFoxFires.Count - 1; i >= 0; i--)
        {
            GumihoFoxFireOrb foxFire = orbitingFoxFires[i];
            if (foxFire == null)
            {
                continue;
            }

            Vector2 direction;

            if (target != null)
            {
                direction = ((Vector2)target.position - (Vector2)foxFire.transform.position).normalized;
            }
            else
            {
                direction = Vector2.right;
            }

            foxFire.Launch(
                direction,
                patternConfig.LaunchSpeed,
                patternConfig.LaunchLifetime,
                patternConfig.Damage,
                patternConfig.TargetLayerMask);
        }

        orbitingFoxFires.Clear();
        foxFireSlotIndexMap.Clear();
    }

    private Vector3 GetAnchorWorldPosition()
    {
        Transform anchor = orbitAnchor != null ? orbitAnchor : transform;
        return anchor.position + (Vector3)patternConfig.OrbitAnchorLocalOffset;
    }

    private void ClearAllFoxFires()
    {
        isPatternRunning = false;
        isWaitingLaunch = false;
        spawnTimer = 0f;
        launchDelayTimer = 0f;
        orbitAngle = 0f;

        for (int i = 0; i < spawnCoroutines.Count; i++)
        {
            if (spawnCoroutines[i] != null)
            {
                StopCoroutine(spawnCoroutines[i]);
            }
        }

        spawnCoroutines.Clear();
        foxFireSlotIndexMap.Clear();

        for (int i = orbitingFoxFires.Count - 1; i >= 0; i--)
        {
            if (orbitingFoxFires[i] != null)
            {
                orbitingFoxFires[i].ReturnToPool();
            }
        }

        orbitingFoxFires.Clear();
    }

    private float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}