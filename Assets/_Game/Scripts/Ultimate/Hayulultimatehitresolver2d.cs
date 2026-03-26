using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하율 궁극기 본체 피해 + 전파 처리 (독립 컴포넌트 버전).
///
/// [v2 최적화]
/// - 전파 탐색: OverlapCircle N회 → 1회 확장 반경 + 메모리 거리 비교
/// - GetComponent 캐싱: Dictionary로 EnemyHealth2D 반복 호출 제거
/// - sqrMagnitude 거리 비교: sqrt 제거
/// </summary>
public class HayulUltimateHitResolver2D : MonoBehaviour
{
    [Header("데미지")]
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float chainDamageRate = 0.15f;

    [Header("범위 설정")]
    [SerializeField] private float hitRadius = 18f;
    [SerializeField] private float chainSearchRadius = 5f;
    [SerializeField] private int maxChainCount = 5;

    [Header("탐색")]
    [SerializeField] private LayerMask enemyMask;

    [Header("전기 부착 VFX")]
    [SerializeField] private GameObject attachedSparkVfxPrefab;
    [SerializeField] private float attachedSparkHoldTime = 0.25f;
    [SerializeField] private Vector3 attachedSparkOffset = Vector3.zero;
    [SerializeField] private Vector3 attachedSparkScale = Vector3.one;
    [SerializeField] private string attachedSparkSortingLayer = "";
    [SerializeField] private int attachedSparkOrderInLayer = 0;

    [Header("참조")]
    [SerializeField] private Transform playerTransform;

    // ── GC-free 버퍼 ──
    private readonly List<Collider2D> _extendedBuffer = new List<Collider2D>(128);
    private readonly HashSet<int> _alreadyHit = new HashSet<int>();
    private readonly Dictionary<int, int> _chainCountMap = new Dictionary<int, int>();

    // ★ v2: 루트/위치 캐시 (Collider → Root 매핑을 1회만)
    private struct EnemyEntry
    {
        public GameObject Root;
        public Vector2 Position;
        public int RootId;
    }
    private readonly List<EnemyEntry> _mainHitEntries = new List<EnemyEntry>(64);
    private readonly List<EnemyEntry> _allEntries = new List<EnemyEntry>(128);

    // ★ v2: GetComponent 캐싱
    private readonly Dictionary<int, AttachedElectricEffect2D> _effectCache
        = new Dictionary<int, AttachedElectricEffect2D>(64);

    private ContactFilter2D _enemyFilter;

    public void ResolveHit()
    {
        EnsurePlayerRef();
        if (playerTransform == null) return;

        _alreadyHit.Clear();
        _chainCountMap.Clear();
        _mainHitEntries.Clear();
        _allEntries.Clear();

        // ═══════════════════════════════════════════════════
        //  ★ v2: 확장 반경 1회 물리 쿼리
        // ═══════════════════════════════════════════════════

        Vector2 center = playerTransform.position;
        float extendedRadius = hitRadius + chainSearchRadius;

        _extendedBuffer.Clear();
        int totalCount = Physics2D.OverlapCircle(center, extendedRadius, _enemyFilter, _extendedBuffer);

        float hitRadiusSqr = hitRadius * hitRadius;

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

            float dx = pos.x - center.x;
            float dy = pos.y - center.y;
            if (dx * dx + dy * dy <= hitRadiusSqr)
            {
                _mainHitEntries.Add(entry);
            }
        }

        // ── 1단계: 본체 데미지 ──
        int damagedCount = 0;

        for (int i = 0; i < _mainHitEntries.Count; i++)
        {
            var entry = _mainHitEntries[i];
            if (_alreadyHit.Contains(entry.RootId)) continue;

            if (TryDealDamage(entry.Root, baseDamage))
            {
                _alreadyHit.Add(entry.RootId);
                damagedCount++;
                RefreshAttachedSpark(entry.Root);
            }
        }

        // ── 2단계: 전파 (★ v2: 메모리 내 거리 비교) ──
        float chainDamage = baseDamage * chainDamageRate;
        float secRadiusSqr = chainSearchRadius * chainSearchRadius;
        int totalChains = 0;

        int finalMaxChain = maxChainCount + HayulPassive_Dosa.ChainBonus;
        if (finalMaxChain <= 0) finalMaxChain = maxChainCount;

        for (int s = 0; s < _mainHitEntries.Count; s++)
        {
            var source = _mainHitEntries[s];
            if (!_alreadyHit.Contains(source.RootId)) continue;

            for (int t = 0; t < _allEntries.Count; t++)
            {
                var target = _allEntries[t];
                if (target.RootId == source.RootId) continue;

                float dx = target.Position.x - source.Position.x;
                float dy = target.Position.y - source.Position.y;
                if (dx * dx + dy * dy > secRadiusSqr) continue;

                _chainCountMap.TryGetValue(target.RootId, out int currentCount);
                if (currentCount >= finalMaxChain) continue;

                if (TryDealDamage(target.Root, chainDamage))
                {
                    _chainCountMap[target.RootId] = currentCount + 1;
                    totalChains++;
                    RefreshAttachedSpark(target.Root);
                }
            }
        }

        GameLogger.Log($"[하율 궁극기 HitResolver] 본체={damagedCount} 전파={totalChains} (쿼리1회)");
    }

    public void SetBaseDamage(float damage) => baseDamage = damage;

    /// <summary>궁극기 시작 시 캐시 초기화용. 외부에서 호출.</summary>
    public void ClearCache() => _effectCache.Clear();

    private bool TryDealDamage(GameObject target, float damage)
    {
        if (target == null) return false;
        int intDamage = Mathf.RoundToInt(damage);
        if (intDamage <= 0) return false;
        return DamageUtil2D.TryApplyDamage(target, intDamage, DamageElement2D.Electric);
    }

    // ★ v2: GetComponent 캐싱
    private void RefreshAttachedSpark(GameObject target)
    {
        if (target == null || attachedSparkVfxPrefab == null) return;

        int id = target.GetInstanceID();
        if (!_effectCache.TryGetValue(id, out var effect) || effect == null)
        {
            effect = target.GetComponent<AttachedElectricEffect2D>();
            if (effect == null)
                effect = target.AddComponent<AttachedElectricEffect2D>();
            _effectCache[id] = effect;
        }

        effect.Refresh(
            attachedSparkVfxPrefab,
            attachedSparkHoldTime,
            attachedSparkOffset,
            attachedSparkScale,
            attachedSparkSortingLayer,
            attachedSparkOrderInLayer
        );
    }

    private void Awake()
    {
        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(enemyMask);
        _enemyFilter.useLayerMask = true;
        _enemyFilter.useTriggers = true;
        EnsurePlayerRef();
    }

    private void EnsurePlayerRef()
    {
        if (playerTransform != null) return;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 c = playerTransform != null ? playerTransform.position : transform.position;
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(c, hitRadius);
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(c, chainSearchRadius);
    }
#endif
}