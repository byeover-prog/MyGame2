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
///
/// ■ 각성(Awakening) 규칙
///   - 일반전: 체력 높은 적 최대 4명을 각각 타겟으로 4개 장판 동시 낙하
///   - 보스전: 보스 1명에게 4개 장판 겹쳐서 동시 낙하
///   - 각성은 외부에서 SetAwakened(true) 호출로 활성화
///
/// ■ CommonSkillLevelParams 매핑
///   - cooldown        → 장판 생성 주기
///   - damage          → 틱 피해
///   - hitInterval     → 틱 간격(작을수록 자주)
///   - lifeSeconds     → 장판 지속시간
///   - explosionRadius → 장판 반경
///   - projectileCount → 동시 장판 수(각성 전 1, 각성 후 4)
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

    [Tooltip("보스 태그 이름(이 태그를 가진 적은 보스로 판정)")]
    [SerializeField] private string bossTag = "Boss";

    [Tooltip("같은 타겟에 겹칠 때 장판 간 랜덤 오프셋(유닛). 0이면 정확히 겹침")]
    [Min(0f)]
    [SerializeField] private float overlapOffset = 0.35f;

    [Header("화살비 — 풀/성능")]
    [Tooltip("동시에 존재할 수 있는 장판 최대 수")]
    [Min(1)]
    [SerializeField] private int maxPoolSize = 8;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    // ── 장판 풀 ──
    private readonly List<ArrowRainArea2D> _pool = new List<ArrowRainArea2D>(8);

    // ── 타겟 탐색 재사용 버퍼 (GC 방지) ──
    private readonly List<Collider2D> _hitBuffer = new List<Collider2D>(128);

    // ── 타겟 정렬용 임시 리스트 ──
    private readonly List<TargetCandidate> _candidates = new List<TargetCandidate>(64);

    /// <summary>
    /// 각성 상태 외부 설정용.
    /// 레벨업/각성 시스템에서 호출한다.
    /// </summary>
    public void SetAwakened(bool awakened)
    {
        isAwakened = awakened;
        if (debugLog) Debug.Log($"[ArrowRainWeapon2D] 각성 상태 변경 → {isAwakened}", this);
    }

    public bool IsAwakened => isAwakened;

    // ════════════════════════════════════════════
    //  Update 루프
    // ════════════════════════════════════════════

    private void Update()
    {
        if (config == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        int areaCount = isAwakened ? awakenedAreaCount : 1;

        // 타겟 탐색: 체력 높은 적 N명
        int targetCount = FindHighestHpTargets(areaCount);
        if (targetCount <= 0) return;

        // 발사
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
            Debug.LogWarning("[ArrowRainWeapon2D] areaPrefab이 비어있습니다.", this);
            return;
        }

        var p = P;
        float areaRadius = Mathf.Max(0.5f, p.explosionRadius);
        float duration   = Mathf.Max(0.25f, p.lifeSeconds);
        float tickIvl    = Mathf.Max(0.05f, p.hitInterval);
        int   tickDmg    = Mathf.Max(0, p.damage);

        // ── 보스전 분기: 타겟이 1명인데 각성이면 같은 위치에 areaCount개 ──
        bool bossMode = isAwakened && targetCount == 1 && IsBoss(_candidates[0].transform);

        for (int i = 0; i < areaCount; i++)
        {
            // 타겟 인덱스 결정
            int targetIdx;
            if (bossMode)
            {
                targetIdx = 0; // 보스에게 전부
            }
            else
            {
                targetIdx = (i < targetCount) ? i : 0; // 남으면 1번에 겹침
            }

            Transform target = _candidates[targetIdx].transform;
            if (target == null) continue;

            Vector2 pos = (Vector2)target.position;

            // 같은 타겟에 겹칠 때 약간 오프셋
            if (i > 0 && (bossMode || targetIdx == 0) && overlapOffset > 0f)
            {
                pos += Random.insideUnitCircle * overlapOffset;
            }

            ArrowRainArea2D area = GetAreaFromPool();
            area.transform.position = pos;

            area.Setup(
                newRadius:            areaRadius,
                newDurationSeconds:   duration,
                newDamageTickInterval: tickIvl,
                newDamagePerTick:     tickDmg,
                newEnemyMask:         enemyMask
            );

            if (!area.gameObject.activeSelf)
                area.gameObject.SetActive(true);
        }

        if (debugLog)
            Debug.Log($"[ArrowRainWeapon2D] Fire → {areaCount}개 장판, 보스모드={bossMode}, dmg={tickDmg}, radius={areaRadius:F1}");
    }

    // ════════════════════════════════════════════
    //  타겟 탐색 (체력 높은 적 N명)
    // ════════════════════════════════════════════

    private struct TargetCandidate
    {
        public Transform transform;
        public int hpScore;
    }

    /// <summary>
    /// 체력이 높은 적 최대 maxCount명을 _candidates에 저장한다.
    /// 반환값: 실제 찾은 타겟 수.
    /// </summary>
    private int FindHighestHpTargets(int maxCount)
    {
        _candidates.Clear();

        Vector2 origin = (owner != null) ? (Vector2)owner.position : (Vector2)transform.position;

        var filter = new ContactFilter2D();
        filter.SetLayerMask(enemyMask);
        filter.useTriggers = true;
        int hitCount = Physics2D.OverlapCircle(origin, targetSearchRadius, filter, _hitBuffer);

        if (hitCount <= 0) return 0;

        // 후보 수집
        for (int i = 0; i < hitCount; i++)
        {
            var col = _hitBuffer[i];
            if (col == null) continue;

            var hp = col.GetComponentInParent<EnemyHealth2D>();
            int score;
            if (hp != null)
            {
                // 기준: 현재 HP 또는 최대 HP
                score = useCurrentHp ? hp.CurrentHp : hp.MaxHp;
                if (hp.IsDead) continue; // 죽은 적 제외
            }
            else
            {
                score = 0; // 체력 컴포넌트 없으면 최저 우선순위
            }

            _candidates.Add(new TargetCandidate
            {
                transform = col.transform,
                hpScore = score
            });
        }

        if (_candidates.Count == 0) return 0;

        // 체력 높은 순으로 정렬 (내림차순)
        _candidates.Sort((a, b) => b.hpScore.CompareTo(a.hpScore));

        // maxCount 초과분 제거
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

        // 자기 자신 또는 루트에 보스 태그가 있으면 보스
        if (target.CompareTag(bossTag)) return true;
        if (target.root.CompareTag(bossTag)) return true;

        return false;
    }

    // ════════════════════════════════════════════
    //  장판 풀 관리
    // ════════════════════════════════════════════

    private ArrowRainArea2D GetAreaFromPool()
    {
        // 비활성 장판 찾기
        for (int i = 0; i < _pool.Count; i++)
        {
            if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                return _pool[i];
        }

        // 풀 한도 초과 시 가장 오래된 것 재활용
        if (_pool.Count >= maxPoolSize)
        {
            var oldest = _pool[0];
            if (oldest != null)
            {
                oldest.gameObject.SetActive(false); // 강제 종료
                return oldest;
            }
        }

        // 새로 생성
        var go = Instantiate(areaPrefab.gameObject);
        go.name = $"{areaPrefab.gameObject.name}_{_pool.Count}";
        go.SetActive(false);

        var area = go.GetComponent<ArrowRainArea2D>();
        _pool.Add(area);

        return area;
    }

    // ════════════════════════════════════════════
    //  레벨 변경 콜백
    // ════════════════════════════════════════════

    protected override void OnLevelChanged()
    {
        // Setup()이 매번 최신 P를 적용하므로 별도 처리 불필요
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var o = (owner != null) ? owner.position : transform.position;
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.3f);
        Gizmos.DrawWireSphere(o, targetSearchRadius);
    }
#endif
}