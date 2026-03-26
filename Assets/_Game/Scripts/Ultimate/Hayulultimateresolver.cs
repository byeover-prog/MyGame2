// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하율 궁극기 "천강뇌전부" Resolver.
///
/// [v2 최적화]
/// - 전파 탐색: OverlapCircle N회 → 1회로 통합
///   기존: 피격 적 30마리 × 전파 탐색 1회 = 31회 물리 쿼리
///   변경: 확장 반경 1회 탐색 + 메모리 내 거리 비교 = 1회 물리 쿼리
/// - GetComponent 캐싱: UltimateResolverBase.GetCachedHealth 사용
/// - sqrMagnitude 거리 비교: sqrt 제거
/// </summary>
public sealed class HayulUltimateResolver : UltimateResolverBase
{
    [Header("하율 전용: 전파 설정")]
    [Tooltip("전파 데미지 비율 (0.15 = 본체의 15%)")]
    [SerializeField] private float chainDamageRate = 0.15f;

    [Tooltip("한 대상당 최대 전파 횟수")]
    [SerializeField] private int maxChainCount = 5;

    // ── GC-free 버퍼 ──
    private readonly HashSet<int> _alreadyHit = new HashSet<int>();
    private readonly Dictionary<int, int> _chainCountMap = new Dictionary<int, int>();

    // ★ v2: 확장 반경 탐색용 버퍼 (한 번만 물리 쿼리)
    private readonly List<Collider2D> _extendedBuffer = new List<Collider2D>(128);

    // ★ v2: 루트 GameObject 캐시 (Collider → Root 매핑)
    private struct EnemyEntry
    {
        public GameObject Root;
        public Vector2 Position;
        public int RootId;
    }
    private readonly List<EnemyEntry> _mainHitEntries = new List<EnemyEntry>(64);
    private readonly List<EnemyEntry> _allEntries = new List<EnemyEntry>(128);

    public override void OnCastBegin()
    {
        base.OnCastBegin();
        _alreadyHit.Clear();
        _chainCountMap.Clear();
    }

    public override void ResolveHit()
    {
        if (data == null || playerTransform == null) return;

        _alreadyHit.Clear();
        _chainCountMap.Clear();
        _mainHitEntries.Clear();
        _allEntries.Clear();

        // ═══════════════════════════════════════════════════
        //  ★ v2: 확장 반경으로 1회만 물리 쿼리
        //  기존: hitRadius 1회 + secondaryRadius × N회 = N+1회
        //  변경: (hitRadius + secondaryRadius) 1회 = 1회
        // ═══════════════════════════════════════════════════

        float extendedRadius = data.HitRadius + data.SecondaryRadius;
        _extendedBuffer.Clear();
        int totalCount = Physics2D.OverlapCircle(
            playerTransform.position, extendedRadius, enemyFilter, _extendedBuffer
        );

        // ── 루트 해석 + 엔트리 구축 ──
        Vector2 playerPos = playerTransform.position;
        float hitRadiusSqr = data.HitRadius * data.HitRadius;

        for (int i = 0; i < totalCount; i++)
        {
            Collider2D col = _extendedBuffer[i];
            if (col == null) continue;

            GameObject root = col.attachedRigidbody != null
                ? col.attachedRigidbody.gameObject
                : col.transform.root.gameObject;

            if (root == null || !root.activeInHierarchy) continue;

            int rootId = root.GetInstanceID();
            Vector2 pos = (Vector2)root.transform.position;

            var entry = new EnemyEntry { Root = root, Position = pos, RootId = rootId };
            _allEntries.Add(entry);

            // hitRadius 내 = 본체 데미지 대상
            float dx = pos.x - playerPos.x;
            float dy = pos.y - playerPos.y;
            if (dx * dx + dy * dy <= hitRadiusSqr)
            {
                _mainHitEntries.Add(entry);
            }
        }

        // ── 1단계: 본체 데미지 ──
        int baseDmg = CalcFinalDamage(data.BaseDamage);
        int damagedCount = 0;

        for (int i = 0; i < _mainHitEntries.Count; i++)
        {
            var entry = _mainHitEntries[i];
            if (_alreadyHit.Contains(entry.RootId)) continue;

            if (DamageUtil2D.TryApplyDamage(entry.Root, baseDmg, data.DamageElement))
            {
                _alreadyHit.Add(entry.RootId);
                damagedCount++;
            }
        }

        // ── 2단계: 전파 데미지 (★ v2: 메모리 내 거리 비교) ──
        int chainDmg = Mathf.Max(1, Mathf.RoundToInt(baseDmg * chainDamageRate));
        int totalChains = 0;
        float secRadiusSqr = data.SecondaryRadius * data.SecondaryRadius;

        // ★ 하율 고유 패시브 보너스
        int finalMaxChain = maxChainCount + HayulPassive_Dosa.ChainBonus;
        if (finalMaxChain <= 0) finalMaxChain = maxChainCount;

        for (int s = 0; s < _mainHitEntries.Count; s++)
        {
            var source = _mainHitEntries[s];
            if (!_alreadyHit.Contains(source.RootId)) continue;

            // _allEntries 순회하며 거리 비교 (물리 쿼리 없음)
            for (int t = 0; t < _allEntries.Count; t++)
            {
                var target = _allEntries[t];
                if (target.RootId == source.RootId) continue;

                // 거리 체크 (sqrMagnitude)
                float dx = target.Position.x - source.Position.x;
                float dy = target.Position.y - source.Position.y;
                if (dx * dx + dy * dy > secRadiusSqr) continue;

                // 전파 횟수 체크
                _chainCountMap.TryGetValue(target.RootId, out int currentCount);
                if (currentCount >= finalMaxChain) continue;

                if (DamageUtil2D.TryApplyDamage(target.Root, chainDmg, data.DamageElement))
                {
                    _chainCountMap[target.RootId] = currentCount + 1;
                    totalChains++;
                }
            }
        }

        GameLogger.Log($"[하율 궁극기] 본체={damagedCount} 전파={totalChains} (쿼리1회)");
    }
}