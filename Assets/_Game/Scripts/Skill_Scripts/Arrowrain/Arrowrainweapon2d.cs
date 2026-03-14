// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공통스킬: 화살비(ArrowRain) — CommonSkillKind.ArrowRain (7)
///
/// ■ 동작 흐름
///   1. 쿨타임 도달
///   2. 체력이 가장 높은 적 N명 탐색 (기본 1명, 각성 시 최대 4명)
///   3. 각 타겟 위치에 ArrowRainArea2D(장판) 생성
///   4. 장판이 지속시간 동안 틱 데미지 수행 후 자동 비활성화
/// </summary>
[DisallowMultipleComponent]
public sealed class ArrowRainWeapon2D : CommonSkillWeapon2D
{
    [Header("화살비 — 장판")]
    [Tooltip("장판(ArrowRainArea2D) 프리팹")]
    [SerializeField] private ArrowRainArea2D areaPrefab;

    [Header("화살비 — 타겟")]
    [Tooltip("적 탐색 반경(월드 단위)")]
    [SerializeField] private float targetSearchRadius = 20f;

    [Tooltip("타겟 선정 기준: true = 현재 HP, false = 최대 HP")]
    [SerializeField] private bool useCurrentHp = true;

    [Header("화살비 — 각성")]
    [Tooltip("각성 상태 여부. true 시 동시 장판 4개 발동")]
    [SerializeField] private bool isAwakened = false;

    [Tooltip("각성 시 동시 낙하 장판 수")]
    [Min(1)]
    [SerializeField] private int awakenedAreaCount = 4;

    [Tooltip("보스 태그 이름")]
    [SerializeField] private string bossTag = "Boss";

    [Tooltip("같은 타겟에 겹칠 때 장판 간 랜덤 오프셋")]
    [Min(0f)]
    [SerializeField] private float overlapOffset = 0.35f;

    [Header("화살비 — 안전장치")]
    [Tooltip("explosionRadius가 0일 때 사용할 기본 반경")]
    [Min(1f)]
    [SerializeField] private float fallbackRadius = 4.0f;

    [Header("화살비 — 풀/성능")]
    [Min(1)]
    [SerializeField] private int maxPoolSize = 8;

    [Header("디버그 (문제 해결 후 끄세요)")]
    [SerializeField] private bool debugLog = true;

    // ── 장판 풀 ──
    private readonly List<ArrowRainArea2D> _pool = new List<ArrowRainArea2D>(8);

    // ── 타겟 탐색 버퍼 ──
    private readonly List<Collider2D> _hitBuffer = new List<Collider2D>(128);
    private readonly List<TargetCandidate> _candidates = new List<TargetCandidate>(64);

    public void SetAwakened(bool awakened) { isAwakened = awakened; }
    public bool IsAwakened => isAwakened;

    // ════════════════════════════════════════════
    //  Update
    // ════════════════════════════════════════════

    private void Update()
    {
        if (config == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        int areaCount = isAwakened ? awakenedAreaCount : 1;
        int targetCount = FindHighestHpTargets(areaCount);

        if (targetCount <= 0)
        {
            // 적이 없으면 쿨다운만 소비하지 않음 (다음 프레임에 다시 시도)
            return;
        }

        TryBeginFire(() => FireAreas(targetCount, areaCount));
    }

    // ════════════════════════════════════════════
    //  발사
    // ════════════════════════════════════════════

    private void FireAreas(int targetCount, int areaCount)
    {
        if (owner == null) return;
        if (areaPrefab == null)
        {
            Debug.LogError("[ArrowRainWeapon2D] areaPrefab이 비어있습니다!", this);
            return;
        }

        var p = P;

        // ★★★ explosionRadius가 0이면 fallbackRadius 사용 ★★★
        float areaRadius = p.explosionRadius;
        if (areaRadius < 1f)
        {
            areaRadius = fallbackRadius;
            if (debugLog)
                Debug.LogWarning($"[ArrowRainWeapon2D] explosionRadius가 {p.explosionRadius}! fallback={fallbackRadius} 사용", this);
        }

        float duration = Mathf.Max(0.5f, p.lifeSeconds);
        float tickIvl  = Mathf.Max(0.05f, p.hitInterval);
        int   tickDmg  = Mathf.Max(1, p.damage);

        if (debugLog)
            Debug.Log($"[ArrowRainWeapon2D] ★ Fire: {areaCount}개 장판 | dmg={tickDmg} | radius={areaRadius:F1} | " +
                      $"tick={tickIvl:F2}s | dur={duration:F1}s | mask={enemyMask.value}", this);

        bool bossMode = isAwakened && targetCount == 1 && IsBoss(_candidates[0].transform);

        for (int i = 0; i < areaCount; i++)
        {
            int targetIdx = bossMode ? 0 : ((i < targetCount) ? i : 0);

            Transform target = _candidates[targetIdx].transform;
            if (target == null) continue;

            Vector2 pos = (Vector2)target.position;

            if (i > 0 && (bossMode || targetIdx == 0) && overlapOffset > 0f)
                pos += Random.insideUnitCircle * overlapOffset;

            ArrowRainArea2D area = GetAreaFromPool();
            area.transform.position = pos;

            area.Setup(
                newRadius:             areaRadius,
                newDurationSeconds:    duration,
                newDamageTickInterval: tickIvl,
                newDamagePerTick:      tickDmg,
                newEnemyMask:          enemyMask
            );

            if (!area.gameObject.activeSelf)
                area.gameObject.SetActive(true);

            if (debugLog)
                Debug.Log($"[ArrowRainWeapon2D] 장판 #{i} 스폰 at {pos} → 타겟: {target.name}", this);
        }
    }

    // ════════════════════════════════════════════
    //  타겟 탐색
    // ════════════════════════════════════════════

    private struct TargetCandidate
    {
        public Transform transform;
        public int hpScore;
    }

    private int FindHighestHpTargets(int maxCount)
    {
        _candidates.Clear();

        Vector2 origin = (owner != null) ? (Vector2)owner.position : (Vector2)transform.position;

        var filter = new ContactFilter2D();
        filter.SetLayerMask(enemyMask);
        filter.useTriggers = true;
        int hitCount = Physics2D.OverlapCircle(origin, targetSearchRadius, filter, _hitBuffer);

        if (hitCount <= 0) return 0;

        for (int i = 0; i < hitCount; i++)
        {
            var col = _hitBuffer[i];
            if (col == null) continue;

            var hp = col.GetComponentInParent<EnemyHealth2D>();
            int score;
            if (hp != null)
            {
                score = useCurrentHp ? hp.CurrentHp : hp.MaxHp;
                if (hp.IsDead) continue;
            }
            else
            {
                score = 0;
            }

            _candidates.Add(new TargetCandidate
            {
                transform = col.transform,
                hpScore = score
            });
        }

        if (_candidates.Count == 0) return 0;

        _candidates.Sort((a, b) => b.hpScore.CompareTo(a.hpScore));

        if (_candidates.Count > maxCount)
            _candidates.RemoveRange(maxCount, _candidates.Count - maxCount);

        return _candidates.Count;
    }

    // ════════════════════════════════════════════
    //  보스 판정
    // ════════════════════════════════════════════

    private bool IsBoss(Transform target)
    {
        if (target == null) return false;
        if (string.IsNullOrEmpty(bossTag)) return false;
        if (target.CompareTag(bossTag)) return true;
        if (target.root.CompareTag(bossTag)) return true;
        return false;
    }

    // ════════════════════════════════════════════
    //  풀 관리
    // ════════════════════════════════════════════

    private ArrowRainArea2D GetAreaFromPool()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                return _pool[i];
        }

        if (_pool.Count >= maxPoolSize)
        {
            var oldest = _pool[0];
            if (oldest != null)
            {
                oldest.gameObject.SetActive(false);
                return oldest;
            }
        }

        var go = Instantiate(areaPrefab.gameObject);
        go.name = $"{areaPrefab.gameObject.name}_{_pool.Count}";
        go.SetActive(false);

        var area = go.GetComponent<ArrowRainArea2D>();
        _pool.Add(area);
        return area;
    }

    protected override void OnLevelChanged() { }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var o = (owner != null) ? owner.position : transform.position;
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.3f);
        Gizmos.DrawWireSphere(o, targetSearchRadius);
    }
#endif
}