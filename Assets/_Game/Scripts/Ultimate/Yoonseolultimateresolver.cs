// UTF-8
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 윤설 궁극기 "혹한의 집념" Resolver.
///
/// [v2 최적화]
/// - 화살 풀링: Instantiate/Destroy 48회 → Stack 기반 풀에서 꺼내기/반환
/// - WaitForSeconds 캐싱: new WaitForSeconds 48회 GC 할당 → 1개 캐싱
/// - GetComponent 캐싱: 매 틱 수십 회 호출 → UltimateResolverBase 캐시 사용
/// - sqrMagnitude 거리 비교: sqrt 제거
///
/// [동작 — LoL 애쉬 Q 참고]
/// 1. 0.5초 간격으로 ResolveHit 호출 (4초간 = 8번)
/// 2. 매 ResolveHit마다 6발을 짧은 간격(0.08초)으로 burst 발사
/// 3. 각 화살: 10 데미지 + 혹한 중첩 1
/// </summary>
public sealed class YoonseolUltimateResolver : UltimateResolverBase
{
    [Header("윤설 전용: Burst 발사")]
    [SerializeField] private int arrowsPerBurst = 6;
    [SerializeField] private float burstDelay = 0.08f;

    [Header("윤설 전용: 화살 비주얼")]
    [SerializeField] private GameObject arrowVfxPrefab;
    [SerializeField] private float arrowSpeed = 18f;
    [SerializeField] private float arrowLifetime = 0.8f;
    [SerializeField] private float spreadWidth = 1.5f;

    [Header("윤설 전용: 혹한 중첩")]
    [SerializeField] private int maxExtremeColdStacks = 100;

    [Header("윤설 전용: 궁극기 VFX")]
    [SerializeField] private GameObject ultimateVfxPrefab;

    // ── 런타임 ──
    private readonly Dictionary<int, int> _coldStacks = new Dictionary<int, int>();
    private GameObject _currentTarget;
    private GameObject _ultimateVfxInstance;

    // ── v2: WaitForSeconds 캐싱 ──
    private WaitForSeconds _cachedBurstDelay;

    // ── v2: 화살 풀링 ──
    private readonly Stack<UltimateArrowVisual2D> _arrowPool = new Stack<UltimateArrowVisual2D>(64);
    private Transform _arrowPoolRoot;
    private const int ARROW_PREWARM = 16; // 6발 × 2~3 burst 동시 비행 가능

    protected override void OnInit()
    {
        _cachedBurstDelay = burstDelay > 0f ? new WaitForSeconds(burstDelay) : null;
        PrepareArrowPool();
    }

    public override void OnCastBegin()
    {
        base.OnCastBegin(); // 컴포넌트 캐시 초기화
        _coldStacks.Clear();
        _currentTarget = null;

        if (ultimateVfxPrefab != null && casterTransform != null)
        {
            _ultimateVfxInstance = Object.Instantiate(
                ultimateVfxPrefab, casterTransform.position, Quaternion.identity
            );
        }

        GameLogger.Log("[윤설 궁극기] 시전 시작 — 혹한 중첩 초기화");
    }

    public override void OnCastEnd()
    {
        if (_ultimateVfxInstance != null)
        {
            Object.Destroy(_ultimateVfxInstance);
            _ultimateVfxInstance = null;
        }

        GameLogger.Log("[윤설 궁극기] 시전 종료");
        _coldStacks.Clear();
        _currentTarget = null;
        base.OnCastEnd(); // 컴포넌트 캐시 정리
    }

    public override void ResolveHit()
    {
        if (data == null || playerTransform == null) return;

        // ★ v2: 캐시된 IsTargetDead 사용
        if (_currentTarget == null || !_currentTarget.activeInHierarchy || IsTargetDeadCached(_currentTarget))
        {
            FindEnemiesInRadius(data.HitRadius);
            _currentTarget = FindPriorityTargetCached(); // ★ v2: 캐시된 버전
        }

        if (_currentTarget == null) return;

        StartCoroutine(BurstFire(_currentTarget));
    }

    private IEnumerator BurstFire(GameObject target)
    {
        if (target == null) yield break;

        int targetId = target.GetInstanceID();
        _coldStacks.TryGetValue(targetId, out int currentStacks);

        int hits = 0;

        for (int i = 0; i < arrowsPerBurst; i++)
        {
            // 타겟이 죽었으면 재탐색 (★ v2: 캐시된 버전)
            if (target == null || !target.activeInHierarchy || IsTargetDeadCached(target))
            {
                FindEnemiesInRadius(data.HitRadius);
                target = FindPriorityTargetCached();
                if (target == null) break;

                targetId = target.GetInstanceID();
                _coldStacks.TryGetValue(targetId, out currentStacks);
                currentStacks += hits;
            }

            float bonusPercent = Mathf.Clamp(currentStacks + hits, 0, maxExtremeColdStacks) * 0.01f;
            int finalDmg = Mathf.Max(1, Mathf.RoundToInt(
                data.BaseDamage * runtimeDamageMultiplier * (1f + bonusPercent)
            ));

            if (DamageUtil2D.TryApplyDamage(target, finalDmg, data.DamageElement))
                hits++;

            // ★ v2: 풀링된 화살 생성
            SpawnArrowFromPool(target.transform, i);

            // ★ v2: 캐싱된 WaitForSeconds
            if (i < arrowsPerBurst - 1 && _cachedBurstDelay != null)
                yield return _cachedBurstDelay;
        }

        if (hits > 0 && target != null)
        {
            int newStacks = Mathf.Clamp(currentStacks + hits, 0, maxExtremeColdStacks);
            _coldStacks[targetId] = newStacks;
        }

        if (target != null && IsTargetDeadCached(target))
            _currentTarget = null;
    }

    // ═══════════════════════════════════════════════════════
    //  v2: 화살 풀링
    // ═══════════════════════════════════════════════════════

    private void PrepareArrowPool()
    {
        if (arrowVfxPrefab == null) return;

        if (_arrowPoolRoot == null)
        {
            var rootGo = new GameObject("[YoonseolArrowPool]");
            rootGo.transform.SetParent(transform);
            _arrowPoolRoot = rootGo.transform;
        }

        for (int i = 0; i < ARROW_PREWARM; i++)
        {
            var arrow = CreateArrow();
            arrow.gameObject.SetActive(false);
            _arrowPool.Push(arrow);
        }
    }

    private UltimateArrowVisual2D CreateArrow()
    {
        GameObject go = Object.Instantiate(arrowVfxPrefab, _arrowPoolRoot);
        go.name = "PooledArrow";

        var visual = go.GetComponent<UltimateArrowVisual2D>();
        if (visual == null)
            visual = go.AddComponent<UltimateArrowVisual2D>();

        return visual;
    }

    private UltimateArrowVisual2D AcquireArrow()
    {
        if (_arrowPool.Count > 0)
            return _arrowPool.Pop();

        return CreateArrow();
    }

    private void ReturnArrow(UltimateArrowVisual2D arrow)
    {
        if (arrow == null) return;
        arrow.gameObject.SetActive(false);
        _arrowPool.Push(arrow);
    }

    private void SpawnArrowFromPool(Transform target, int arrowIndex)
    {
        if (arrowVfxPrefab == null || casterTransform == null || target == null) return;

        Vector3 origin = casterTransform.position;
        Vector2 forward = ((Vector2)target.position - (Vector2)origin).normalized;
        Vector2 lateral = new Vector2(-forward.y, forward.x);

        float halfSpread = spreadWidth * 0.5f;
        float step = arrowsPerBurst > 1
            ? spreadWidth / (arrowsPerBurst - 1)
            : 0f;
        float offset = -halfSpread + step * arrowIndex;

        Vector3 spawnPos = origin + (Vector3)(lateral * offset);
        Vector2 dir = ((Vector2)target.position - (Vector2)spawnPos).normalized;

        var arrow = AcquireArrow();
        arrow.transform.position = spawnPos;
        arrow.gameObject.SetActive(true);
        arrow.Init(target, dir, arrowSpeed, arrowLifetime, 0.5f, ReturnArrow);
    }
}