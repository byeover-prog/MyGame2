// UTF-8
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 방해 패턴 프리팹을 관리하고, 일정 쿨타임마다 안전한 스폰 좌표를 계산하여 실제 패턴을 즉시 생성하는 매니저입니다.
///
/// 변경된 흐름 요약:
/// 1. 기존의 "스폰 전 경고(warningLeadTime)" 로직은 완전히 제거했습니다.
/// 2. 이제 매니저는 쿨타임이 차면 HazardSpawnResolver2D로 안전한 좌표를 계산하고, 패턴을 바로 스폰합니다.
/// 3. 경고 UI의 타이밍 판단은 각 패턴이 직접 담당합니다.
/// 4. 대신 매니저는 공용 경고 UI 생성 기능인 ShowWarning(worldPosition, duration)만 외부에 제공합니다.
/// 5. 이렇게 하면 스폰 시점 책임은 매니저가, 경고 시점 책임은 패턴이 맡게 되어 역할이 더 명확해집니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class HazardEventManager2D : MonoBehaviour
{
    [Serializable]
    private sealed class HazardPatternEntry
    {
        [Tooltip("실제로 생성할 방해 패턴 프리팹입니다.")]
        public GameObject patternPrefab;

        [Tooltip("이 패턴 전체가 차지하는 대략적인 폭/높이입니다. 예: 스웜 진형 전체 크기.")]
        public Vector2 footprintSize = new Vector2(6f, 4f);
    }

    [Serializable]
    private struct PendingHazardSpawn
    {
        public GameObject patternPrefab;
        public Vector2 footprintSize;
        public HazardSpawnResolver2D.SpawnResolveResult resolveResult;
    }

    [Header("패턴 프리팹 리스트")]
    [Tooltip("랜덤으로 선택될 방해 패턴 프리팹과 footprint 정보를 등록합니다.")]
    [SerializeField] private List<HazardPatternEntry> hazardPatterns = new List<HazardPatternEntry>();

    [Header("필수 참조")]
    [Tooltip("안전한 스폰 좌표를 계산해 주는 HazardSpawnResolver2D입니다.")]
    [SerializeField] private HazardSpawnResolver2D spawnResolver;

    [Tooltip("실제로 생성된 방해 패턴들이 들어갈 부모 오브젝트입니다. 비워두면 부모 없이 생성합니다.")]
    [SerializeField] private Transform activeHazardsRoot;

    [Tooltip("월드 좌표를 화면 가장자리 방향으로 계산할 때 사용할 게임 카메라입니다. 비워두면 Camera.main을 사용합니다.")]
    [SerializeField] private Camera gameplayCamera;

    [Tooltip("플레이어 Transform입니다. 비워두면 Player 태그로 자동 탐색합니다.")]
    [SerializeField] private Transform target;

    [Tooltip("플레이어 자동 탐색 시 사용할 태그입니다.")]
    [SerializeField] private string playerTag = "Player";

    [Header("경고 UI 참조")]
    [Tooltip("느낌표 경고 UI 프리팹입니다. HazardWarningUI2D가 붙어 있어야 합니다.")]
    [SerializeField] private HazardWarningUI2D warningUIPrefab;

    [Tooltip("경고 UI가 생성될 부모 RectTransform입니다. 보통 Canvas 아래의 HazardWarningUIRoot를 연결합니다.")]
    [SerializeField] private RectTransform warningUIRoot;

    [Header("경고 UI 위치 설정")]
    [Tooltip("화면 가장자리에서 느낌표 UI를 얼마나 안쪽으로 띄울지 결정하는 픽셀 오프셋입니다.")]
    [SerializeField] private Vector2 warningEdgePadding = new Vector2(80f, 80f);

    [Header("스폰 타이밍 설정")]
    [Tooltip("게임 시작 후 첫 방해 패턴이 나오기 전까지 기다리는 시간입니다.")]
    [SerializeField][Min(0f)] private float firstSpawnDelay = 6f;

    [Tooltip("초기 스폰 쿨타임입니다.")]
    [SerializeField][Min(0.1f)] private float initialSpawnCooldown = 12f;

    [Tooltip("난이도가 올라가도 이 값 이하로는 쿨타임이 내려가지 않도록 막는 최소값입니다.")]
    [SerializeField][Min(0.1f)] private float minimumSpawnCooldown = 6f;

    [Tooltip("1분이 지날 때마다 스폰 쿨타임을 얼마나 줄일지 결정합니다.")]
    [SerializeField][Min(0f)] private float cooldownReductionPerMinute = 0.5f;

    [Header("패턴 스폰 위치 설정")]
    [Tooltip("실제 패턴 프리팹을 생성할 Z 좌표입니다. 비워 둔 2D 씬이라면 0을 권장합니다.")]
    [SerializeField] private float spawnZPosition = 0f;

    [Header("동작 옵션")]
    [Tooltip("체크하면 이 오브젝트가 활성화될 때 자동으로 스폰 루프를 시작합니다.")]
    [SerializeField] private bool startOnEnable = true;

    [Tooltip("체크하면 각 쿨타임에 약간의 랜덤 오차를 주어 너무 기계적으로 보이지 않게 합니다.")]
    [SerializeField] private bool useCooldownJitter = true;

    [Tooltip("쿨타임 랜덤 오차 비율입니다. 0.1이면 현재 쿨타임의 ±10% 범위로 흔들립니다.")]
    [SerializeField][Range(0f, 0.5f)] private float cooldownJitterRatio = 0.1f;

    [Header("디버그")]
    [Tooltip("체크하면 주요 상태 전환과 스폰 로그를 콘솔에 출력합니다.")]
    [SerializeField] private bool debugLog = false;

    private Coroutine _spawnLoopRoutine;
    private float _sessionStartTime;

    private void Reset()
    {
        if (spawnResolver == null)
            spawnResolver = GetComponent<HazardSpawnResolver2D>();

        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
    }

    private void Awake()
    {
        if (spawnResolver == null)
            spawnResolver = GetComponent<HazardSpawnResolver2D>();

        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (startOnEnable)
            StartHazardLoop();
    }

    private void OnDisable()
    {
        StopHazardLoop();
    }

    private void OnValidate()
    {
        if (initialSpawnCooldown < 0.1f)
            initialSpawnCooldown = 0.1f;

        if (minimumSpawnCooldown < 0.1f)
            minimumSpawnCooldown = 0.1f;

        if (minimumSpawnCooldown > initialSpawnCooldown)
            minimumSpawnCooldown = initialSpawnCooldown;

        if (firstSpawnDelay < 0f)
            firstSpawnDelay = 0f;

        if (cooldownReductionPerMinute < 0f)
            cooldownReductionPerMinute = 0f;
    }

    [ContextMenu("Start Hazard Loop")]
    public void StartHazardLoop()
    {
        if (_spawnLoopRoutine != null)
            return;

        if (!ValidateSpawnRequirements(logWarning: true))
            return;

        _sessionStartTime = Time.time;
        _spawnLoopRoutine = StartCoroutine(CoHazardLoop());

        if (debugLog)
            Debug.Log("[HazardEventManager2D] 방해 요소 루프 시작", this);
    }

    [ContextMenu("Stop Hazard Loop")]
    public void StopHazardLoop()
    {
        if (_spawnLoopRoutine == null)
            return;

        StopCoroutine(_spawnLoopRoutine);
        _spawnLoopRoutine = null;

        if (debugLog)
            Debug.Log("[HazardEventManager2D] 방해 요소 루프 중지", this);
    }

    [ContextMenu("Spawn Random Hazard Now")]
    public void SpawnRandomHazardNow()
    {
        if (!ValidateSpawnRequirements(logWarning: true))
            return;

        if (!TryBuildPendingSpawn(out PendingHazardSpawn pendingSpawn))
            return;

        SpawnHazard(pendingSpawn);
    }

    /// <summary>
    /// 패턴이 필요할 때 호출하는 공용 경고 UI 생성 메서드입니다.
    /// </summary>
    public void ShowWarning(Vector3 worldPosition, float duration)
    {
        if (duration <= 0f)
            return;

        if (warningUIPrefab == null)
        {
            if (debugLog)
                Debug.LogWarning("[HazardEventManager2D] Warning UI Prefab이 없어 경고를 생략합니다.", this);
            return;
        }

        if (warningUIRoot == null)
        {
            if (debugLog)
                Debug.LogWarning("[HazardEventManager2D] Warning UI Root가 없어 경고를 생략합니다.", this);
            return;
        }

        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        if (gameplayCamera == null)
        {
            if (debugLog)
                Debug.LogWarning("[HazardEventManager2D] Gameplay Camera를 찾지 못해 경고를 생략합니다.", this);
            return;
        }

        HazardWarningUI2D spawnedWarning = Instantiate(warningUIPrefab, warningUIRoot);
        spawnedWarning.name = $"{warningUIPrefab.name}_Runtime";

        // 실제 프로젝트의 HazardWarningUI2D 시그니처에 맞춘 호출
        spawnedWarning.Play(gameplayCamera, worldPosition, duration, warningEdgePadding);

        if (debugLog)
            Debug.Log($"[HazardEventManager2D] 경고 UI 표시 | Position: {worldPosition} | Duration: {duration:0.00}", this);
    }

    private IEnumerator CoHazardLoop()
    {
        if (firstSpawnDelay > 0f)
            yield return new WaitForSeconds(firstSpawnDelay);

        while (enabled && gameObject.activeInHierarchy)
        {
            if (!ValidateSpawnRequirements(logWarning: true))
            {
                yield return null;
                continue;
            }

            float currentCooldown = EvaluateCurrentSpawnCooldown();
            if (currentCooldown > 0f)
                yield return new WaitForSeconds(currentCooldown);

            if (!TryBuildPendingSpawn(out PendingHazardSpawn pendingSpawn))
            {
                yield return null;
                continue;
            }

            SpawnHazard(pendingSpawn);
        }

        _spawnLoopRoutine = null;
    }

    private bool ValidateSpawnRequirements(bool logWarning)
    {
        bool isValid = true;

        if (hazardPatterns == null || hazardPatterns.Count == 0)
        {
            if (logWarning)
                Debug.LogWarning("[HazardEventManager2D] Hazard Patterns 리스트가 비어 있습니다.", this);
            isValid = false;
        }

        if (spawnResolver == null)
        {
            if (logWarning)
                Debug.LogWarning("[HazardEventManager2D] Hazard Spawn Resolver가 비어 있습니다.", this);
            isValid = false;
        }

        return isValid;
    }

    private bool TryBuildPendingSpawn(out PendingHazardSpawn pendingSpawn)
    {
        pendingSpawn = default;

        if (!TryPickRandomPattern(out HazardPatternEntry selectedPattern))
            return false;

        return TryResolvePendingSpawn(selectedPattern, out pendingSpawn);
    }

    private bool TryPickRandomPattern(out HazardPatternEntry selectedPattern)
    {
        selectedPattern = null;

        if (hazardPatterns == null || hazardPatterns.Count == 0)
            return false;

        List<HazardPatternEntry> validPatterns = new List<HazardPatternEntry>(hazardPatterns.Count);

        for (int i = 0; i < hazardPatterns.Count; i++)
        {
            HazardPatternEntry entry = hazardPatterns[i];
            if (entry != null && entry.patternPrefab != null)
                validPatterns.Add(entry);
        }

        if (validPatterns.Count == 0)
        {
            if (debugLog)
                Debug.LogWarning("[HazardEventManager2D] 유효한 패턴 프리팹이 없습니다.", this);
            return false;
        }

        int randomIndex = UnityEngine.Random.Range(0, validPatterns.Count);
        selectedPattern = validPatterns[randomIndex];
        return selectedPattern != null;
    }

    private bool TryResolvePendingSpawn(HazardPatternEntry selectedPattern, out PendingHazardSpawn pendingSpawn)
    {
        pendingSpawn = default;

        if (selectedPattern == null || selectedPattern.patternPrefab == null)
            return false;

        Transform resolvedTarget = ResolveTarget();
        Vector2 safeFootprintSize = new Vector2(
            Mathf.Max(0f, selectedPattern.footprintSize.x),
            Mathf.Max(0f, selectedPattern.footprintSize.y));

        // 실제 프로젝트의 HazardSpawnResolver2D 시그니처에 맞춘 호출
        if (!spawnResolver.TryResolveSpawn(
                resolvedTarget,
                safeFootprintSize,
                spawnZPosition,
                out HazardSpawnResolver2D.SpawnResolveResult resolveResult))
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    $"[HazardEventManager2D] 스폰 좌표 계산 실패 | Pattern: {selectedPattern.patternPrefab.name} | Reason: {resolveResult.message}",
                    this);
            }
            return false;
        }

        pendingSpawn.patternPrefab = selectedPattern.patternPrefab;
        pendingSpawn.footprintSize = safeFootprintSize;
        pendingSpawn.resolveResult = resolveResult;
        return true;
    }

    private Transform ResolveTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy)
            return target;

        if (string.IsNullOrWhiteSpace(playerTag))
            return null;

        try
        {
            GameObject found = GameObject.FindWithTag(playerTag);
            if (found != null)
            {
                target = found.transform;
                return target;
            }
        }
        catch (UnityException)
        {
            // Player 태그가 아직 없을 수 있으므로 조용히 실패 처리
        }

        return null;
    }

    private void SpawnHazard(PendingHazardSpawn pendingSpawn)
    {
        if (pendingSpawn.patternPrefab == null)
            return;

        Transform parent = activeHazardsRoot != null ? activeHazardsRoot : null;

        GameObject spawned = Instantiate(
            pendingSpawn.patternPrefab,
            pendingSpawn.resolveResult.worldPosition,
            Quaternion.identity,
            parent);

        spawned.name = $"{pendingSpawn.patternPrefab.name}_Runtime";

        if (debugLog)
        {
            Debug.Log(
                $"[HazardEventManager2D] 패턴 스폰 완료 | Pattern: {pendingSpawn.patternPrefab.name} | Side: {pendingSpawn.resolveResult.side} | Position: {pendingSpawn.resolveResult.worldPosition}",
                this);
        }
    }

    private float EvaluateCurrentSpawnCooldown()
    {
        float elapsedMinutes = (Time.time - _sessionStartTime) / 60f;
        float cooldown = initialSpawnCooldown - (cooldownReductionPerMinute * elapsedMinutes);
        cooldown = Mathf.Max(minimumSpawnCooldown, cooldown);

        if (useCooldownJitter && cooldownJitterRatio > 0f)
        {
            float jitterAmount = cooldown * cooldownJitterRatio;
            cooldown += UnityEngine.Random.Range(-jitterAmount, jitterAmount);
            cooldown = Mathf.Max(minimumSpawnCooldown, cooldown);
        }

        return cooldown;
    }
}