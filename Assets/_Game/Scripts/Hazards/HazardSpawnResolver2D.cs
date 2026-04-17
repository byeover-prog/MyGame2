// UTF-8
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 Bounds와 플레이어 위치를 바탕으로, 방해 패턴이 스폰되기 가장 안전한 면과 좌표를 계산하는 전담 클래스입니다.
///
/// 구현 원리:
/// 1. MapBoundsProvider에게 현재 맵의 플레이 가능 영역 Bounds를 받아옵니다.
/// 2. 플레이어가 어떤 경계면에 너무 가까이 있으면, 그 면은 스폰 후보에서 제외합니다.
/// 3. 단일 몬스터가 아니라 "무리 전체 크기"가 맵 안에 들어와야 하므로,
///    패턴의 전체 폭/높이(footprintSize)를 고려해 최종 좌표를 계산합니다.
/// 4. 최종 스폰 좌표는 "맵 밖"이 아니라 "맵 안쪽 경계선"에서 edgeInset만큼 안쪽으로 잡습니다.
/// 5. 후보가 여러 개면, 플레이어와 더 먼 면에 가중치를 주어 조금 더 안전하고 자연스러운 결과를 선택합니다.
/// 6. 만약 플레이어가 코너에 붙어 있어 모든 면이 막히면,
///    마지막 안전장치로 "가까운 면 제외" 규칙을 잠시 완화해서라도 스폰 가능한 면을 찾습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class HazardSpawnResolver2D : MonoBehaviour
{
    public enum SpawnSide
    {
        Left,
        Right,
        Top,
        Bottom
    }

    [Serializable]
    public struct SpawnResolveResult
    {
        [Tooltip("스폰 좌표 계산 성공 여부입니다.")]
        public bool isSuccess;

        [Tooltip("최종 선택된 스폰 면입니다.")]
        public SpawnSide side;

        [Tooltip("최종 스폰 월드 좌표입니다.")]
        public Vector3 worldPosition;

        [Tooltip("스폰 계산에 사용한 플레이어 위치 스냅샷입니다.")]
        public Vector3 playerSnapshotPosition;

        [Tooltip("스폰 계산에 사용한 맵 Bounds입니다.")]
        public Bounds mapBounds;

        [Tooltip("실패했을 때 이유를 기록합니다.")]
        public string message;
    }

    [Header("필수 참조")]
    [Tooltip("현재 맵의 플레이 가능 영역 Bounds를 제공하는 MapBoundsProvider입니다.")]
    [SerializeField] private MapBoundsProvider mapBoundsProvider;

    [Tooltip("플레이어 Transform입니다. 비워두면 Player 태그로 자동 탐색합니다.")]
    [SerializeField] private Transform target;

    [Tooltip("플레이어 자동 탐색 시 사용할 태그입니다.")]
    [SerializeField] private string playerTag = "Player";

    [Header("스폰 안전 거리")]
    [Tooltip("맵 경계선에서 얼마나 안쪽으로 들어와서 스폰할지 정하는 거리입니다. 벽에 끼지 않도록 하는 기본 여백입니다.")]
    [SerializeField][Min(0f)] private float edgeInset = 1.5f;

    [Tooltip("선택된 면을 따라 랜덤 위치를 뽑을 때, 코너와 너무 가까워지지 않도록 추가로 남길 여백입니다.")]
    [SerializeField][Min(0f)] private float alongSidePadding = 1.0f;

    [Tooltip("플레이어가 특정 경계면에 이 거리보다 가까우면, 해당 면은 스폰 후보에서 제외합니다.")]
    [SerializeField][Min(0f)] private float playerSideBlockThreshold = 6f;

    [Tooltip("모든 면이 막혔을 때, 마지막 안전장치로 '가까운 면 제외' 규칙을 무시하고 다시 한 번 후보를 찾을지 결정합니다.")]
    [SerializeField] private bool allowFallbackIgnoringPlayerSideBlock = true;

    [Header("선택 규칙")]
    [Tooltip("체크하면 플레이어와 더 먼 면일수록 더 높은 가중치를 부여해 선택합니다. 체크를 끄면 단순 랜덤으로 고릅니다.")]
    [SerializeField] private bool preferFartherSides = true;

    [Tooltip("가중치 계산 시 플레이어와의 거리값에 추가되는 기본 가중치입니다. 1 이상이면 너무 약한 차이도 안정적으로 랜덤에 반영됩니다.")]
    [SerializeField][Min(0.01f)] private float baseSideWeight = 1f;

    [Header("디버그")]
    [Tooltip("체크하면 후보 면 선정과 최종 결과를 콘솔에 출력합니다.")]
    [SerializeField] private bool debugLog = false;

    public MapBoundsProvider MapBoundsProvider => mapBoundsProvider;

    private readonly List<SpawnSide> _candidateSides = new List<SpawnSide>(4);
    private readonly List<float> _candidateWeights = new List<float>(4);

    private void Reset()
    {
        if (mapBoundsProvider == null)
            mapBoundsProvider = GetComponent<MapBoundsProvider>();
    }

    private void Awake()
    {
        if (mapBoundsProvider == null)
            mapBoundsProvider = GetComponent<MapBoundsProvider>();
    }

    private void OnValidate()
    {
        if (edgeInset < 0f)
            edgeInset = 0f;

        if (alongSidePadding < 0f)
            alongSidePadding = 0f;

        if (playerSideBlockThreshold < 0f)
            playerSideBlockThreshold = 0f;

        if (baseSideWeight < 0.01f)
            baseSideWeight = 0.01f;
    }

    /// <summary>
    /// 플레이어 위치와 패턴의 전체 footprint를 기준으로, 스폰 가능한 좌표를 계산합니다.
    /// </summary>
    /// <param name="targetOverride">현재 계산에 사용할 플레이어 Transform입니다. null이면 내부 target 또는 Player 태그 탐색을 사용합니다.</param>
    /// <param name="footprintSize">패턴 전체가 차지하는 폭/높이입니다. 무리 전체가 맵 안에 들어가게 하기 위한 값입니다.</param>
    /// <param name="spawnZ">최종 스폰 Z 좌표입니다.</param>
    public bool TryResolveSpawn(Transform targetOverride, Vector2 footprintSize, float spawnZ, out SpawnResolveResult result)
    {
        result = default;
        result.isSuccess = false;

        if (mapBoundsProvider == null)
        {
            result.message = "MapBoundsProvider가 비어 있습니다.";
            if (debugLog)
                Debug.LogWarning("[HazardSpawnResolver2D] " + result.message, this);
            return false;
        }

        if (!mapBoundsProvider.TryGetWorldBounds(out Bounds mapBounds))
        {
            result.message = "플레이 영역 Bounds를 읽지 못했습니다.";
            if (debugLog)
                Debug.LogWarning("[HazardSpawnResolver2D] " + result.message, this);
            return false;
        }

        Transform resolvedTarget = ResolveTarget(targetOverride);
        if (resolvedTarget == null)
        {
            result.message = "플레이어 Transform을 찾지 못했습니다.";
            if (debugLog)
                Debug.LogWarning("[HazardSpawnResolver2D] " + result.message, this);
            return false;
        }

        Vector2 safeFootprintSize = new Vector2(
            Mathf.Max(0f, footprintSize.x),
            Mathf.Max(0f, footprintSize.y));

        Vector3 playerSnapshot = resolvedTarget.position;

        if (!TryCollectCandidateSides(mapBounds, playerSnapshot, safeFootprintSize, true, out bool usedFallback))
        {
            result.message = "현재 맵 크기와 패턴 크기 기준으로 스폰 가능한 면을 찾지 못했습니다.";
            result.playerSnapshotPosition = playerSnapshot;
            result.mapBounds = mapBounds;

            if (debugLog)
                Debug.LogWarning("[HazardSpawnResolver2D] " + result.message, this);

            return false;
        }

        SpawnSide selectedSide = SelectSide(mapBounds, playerSnapshot);
        Vector3 spawnPosition = CalculateSpawnPosition(mapBounds, selectedSide, safeFootprintSize, spawnZ);

        result.isSuccess = true;
        result.side = selectedSide;
        result.worldPosition = spawnPosition;
        result.playerSnapshotPosition = playerSnapshot;
        result.mapBounds = mapBounds;
        result.message = usedFallback
            ? "플레이어 근접 면 제외 규칙을 완화하여 스폰 위치를 계산했습니다."
            : "정상적으로 스폰 위치를 계산했습니다.";

        if (debugLog)
        {
            Debug.Log(
                $"[HazardSpawnResolver2D] Spawn Resolve 성공 | Side: {selectedSide} | Position: {spawnPosition} | Footprint: {safeFootprintSize} | FallbackUsed: {usedFallback}",
                this);
        }

        return true;
    }

    private Transform ResolveTarget(Transform targetOverride)
    {
        if (targetOverride != null && targetOverride.gameObject.activeInHierarchy)
            return targetOverride;

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
            // Player 태그가 프로젝트에 아직 없을 수 있으므로 조용히 실패 처리합니다.
        }

        return null;
    }

    private bool TryCollectCandidateSides(
        Bounds mapBounds,
        Vector3 playerSnapshot,
        Vector2 footprintSize,
        bool blockNearPlayerSide,
        out bool usedFallback)
    {
        usedFallback = false;

        _candidateSides.Clear();
        _candidateWeights.Clear();

        CollectCandidatesInternal(mapBounds, playerSnapshot, footprintSize, blockNearPlayerSide);

        if (_candidateSides.Count > 0)
            return true;

        if (!allowFallbackIgnoringPlayerSideBlock || !blockNearPlayerSide)
            return false;

        CollectCandidatesInternal(mapBounds, playerSnapshot, footprintSize, false);
        usedFallback = _candidateSides.Count > 0;
        return _candidateSides.Count > 0;
    }

    private void CollectCandidatesInternal(Bounds mapBounds, Vector3 playerSnapshot, Vector2 footprintSize, bool blockNearPlayerSide)
    {
        _candidateSides.Clear();
        _candidateWeights.Clear();

        TryAddCandidate(SpawnSide.Left, mapBounds, playerSnapshot, footprintSize, blockNearPlayerSide);
        TryAddCandidate(SpawnSide.Right, mapBounds, playerSnapshot, footprintSize, blockNearPlayerSide);
        TryAddCandidate(SpawnSide.Top, mapBounds, playerSnapshot, footprintSize, blockNearPlayerSide);
        TryAddCandidate(SpawnSide.Bottom, mapBounds, playerSnapshot, footprintSize, blockNearPlayerSide);
    }

    private void TryAddCandidate(
        SpawnSide side,
        Bounds mapBounds,
        Vector3 playerSnapshot,
        Vector2 footprintSize,
        bool blockNearPlayerSide)
    {
        if (!CanFitFootprintOnSide(mapBounds, side, footprintSize))
            return;

        float playerDistanceToSide = GetPlayerDistanceToSide(mapBounds, playerSnapshot, side);

        if (blockNearPlayerSide && playerDistanceToSide < playerSideBlockThreshold)
            return;

        _candidateSides.Add(side);
        _candidateWeights.Add(preferFartherSides ? baseSideWeight + Mathf.Max(0f, playerDistanceToSide) : 1f);
    }

    private bool CanFitFootprintOnSide(Bounds mapBounds, SpawnSide side, Vector2 footprintSize)
    {
        float halfWidth = footprintSize.x * 0.5f;
        float halfHeight = footprintSize.y * 0.5f;

        switch (side)
        {
            case SpawnSide.Left:
            case SpawnSide.Right:
            {
                float xLine = mapBounds.min.x + edgeInset + halfWidth;
                float oppositeLine = mapBounds.max.x - edgeInset - halfWidth;
                if (xLine > oppositeLine)
                    return false;

                float minY = mapBounds.min.y + alongSidePadding + halfHeight;
                float maxY = mapBounds.max.y - alongSidePadding - halfHeight;
                return minY <= maxY;
            }

            case SpawnSide.Top:
            case SpawnSide.Bottom:
            {
                float yLine = mapBounds.min.y + edgeInset + halfHeight;
                float oppositeLine = mapBounds.max.y - edgeInset - halfHeight;
                if (yLine > oppositeLine)
                    return false;

                float minX = mapBounds.min.x + alongSidePadding + halfWidth;
                float maxX = mapBounds.max.x - alongSidePadding - halfWidth;
                return minX <= maxX;
            }
        }

        return false;
    }

    private float GetPlayerDistanceToSide(Bounds mapBounds, Vector3 playerSnapshot, SpawnSide side)
    {
        switch (side)
        {
            case SpawnSide.Left:
                return playerSnapshot.x - mapBounds.min.x;

            case SpawnSide.Right:
                return mapBounds.max.x - playerSnapshot.x;

            case SpawnSide.Top:
                return mapBounds.max.y - playerSnapshot.y;

            case SpawnSide.Bottom:
                return playerSnapshot.y - mapBounds.min.y;
        }

        return 0f;
    }

    private SpawnSide SelectSide(Bounds mapBounds, Vector3 playerSnapshot)
    {
        if (_candidateSides.Count == 1)
            return _candidateSides[0];

        float totalWeight = 0f;
        for (int i = 0; i < _candidateWeights.Count; i++)
            totalWeight += Mathf.Max(0.0001f, _candidateWeights[i]);

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < _candidateSides.Count; i++)
        {
            cumulative += Mathf.Max(0.0001f, _candidateWeights[i]);
            if (randomValue <= cumulative)
                return _candidateSides[i];
        }

        return _candidateSides[_candidateSides.Count - 1];
    }

    private Vector3 CalculateSpawnPosition(Bounds mapBounds, SpawnSide side, Vector2 footprintSize, float spawnZ)
    {
        float halfWidth = footprintSize.x * 0.5f;
        float halfHeight = footprintSize.y * 0.5f;

        switch (side)
        {
            case SpawnSide.Left:
            {
                float x = mapBounds.min.x + edgeInset + halfWidth;
                float minY = mapBounds.min.y + alongSidePadding + halfHeight;
                float maxY = mapBounds.max.y - alongSidePadding - halfHeight;
                float y = PickValueOnRange(minY, maxY, mapBounds.center.y);
                return new Vector3(x, y, spawnZ);
            }

            case SpawnSide.Right:
            {
                float x = mapBounds.max.x - edgeInset - halfWidth;
                float minY = mapBounds.min.y + alongSidePadding + halfHeight;
                float maxY = mapBounds.max.y - alongSidePadding - halfHeight;
                float y = PickValueOnRange(minY, maxY, mapBounds.center.y);
                return new Vector3(x, y, spawnZ);
            }

            case SpawnSide.Top:
            {
                float y = mapBounds.max.y - edgeInset - halfHeight;
                float minX = mapBounds.min.x + alongSidePadding + halfWidth;
                float maxX = mapBounds.max.x - alongSidePadding - halfWidth;
                float x = PickValueOnRange(minX, maxX, mapBounds.center.x);
                return new Vector3(x, y, spawnZ);
            }

            case SpawnSide.Bottom:
            {
                float y = mapBounds.min.y + edgeInset + halfHeight;
                float minX = mapBounds.min.x + alongSidePadding + halfWidth;
                float maxX = mapBounds.max.x - alongSidePadding - halfWidth;
                float x = PickValueOnRange(minX, maxX, mapBounds.center.x);
                return new Vector3(x, y, spawnZ);
            }
        }

        return new Vector3(mapBounds.center.x, mapBounds.center.y, spawnZ);
    }

    private float PickValueOnRange(float min, float max, float fallback)
    {
        if (max < min)
            return fallback;

        if (Mathf.Approximately(min, max))
            return min;

        return UnityEngine.Random.Range(min, max);
    }
}
