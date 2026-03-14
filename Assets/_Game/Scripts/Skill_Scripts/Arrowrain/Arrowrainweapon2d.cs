// UTF-8
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공통스킬: 화살비(ArrowRain)
/// 설계 문서: "가장 가까운 적 대상 장판 고정형 메커니즘"
/// </summary>
[DisallowMultipleComponent]
public sealed class ArrowRainWeapon2D : CommonSkillWeapon2D
{
    [Header("화살비 — 장판")]
    [SerializeField] private ArrowRainArea2D areaPrefab;

    [Header("화살비 — 타겟")]
    [SerializeField] private float targetSearchRadius = 20f;

    [Header("화살비 — 각성")]
    [SerializeField] private bool isAwakened = false;
    [Min(1)]
    [SerializeField] private int awakenedAreaCount = 4;
    [SerializeField] private string bossTag = "Boss";
    [Min(0f)]
    [SerializeField] private float overlapOffset = 0.35f;

    [Header("화살비 — 안전장치")]
    [Tooltip("explosionRadius가 0일 때 사용할 기본 반경")]
    [Min(0.5f)]
    [SerializeField] private float fallbackRadius = 2.0f;

    [Header("화살비 — 풀/성능")]
    [Min(1)]
    [SerializeField] private int maxPoolSize = 8;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private readonly List<ArrowRainArea2D> _pool = new List<ArrowRainArea2D>(8);

    public void SetAwakened(bool awakened) { isAwakened = awakened; }
    public bool IsAwakened => isAwakened;

    private void Update()
    {
        if (config == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        // 설계 문서: "가장 가까운 적 대상"
        if (!TryGetNearest(out EnemyRegistryMember2D nearestEnemy))
            return;

        TryBeginFire(() => FireAreas(nearestEnemy));
    }

    private void FireAreas(EnemyRegistryMember2D targetEnemy)
    {
        if (owner == null || areaPrefab == null) return;

        // ★ 발사 딜레이 중에 적이 죽으면 MissingReferenceException 방지
        if (targetEnemy == null) return;

        var p = P;

        // explosionRadius 0 안전장치
        float areaRadius = p.explosionRadius;
        if (areaRadius < 0.5f)
            areaRadius = fallbackRadius;

        float duration = Mathf.Max(0.5f, p.lifeSeconds);
        float tickIvl  = Mathf.Max(0.05f, p.hitInterval);
        int   tickDmg  = Mathf.Max(1, p.damage);
        int   areaCount = isAwakened ? awakenedAreaCount : 1;

        if (debugLog)
            Debug.Log($"[ArrowRainWeapon2D] Fire: {areaCount}개 | dmg={tickDmg} | r={areaRadius:F1}", this);

        Vector2 basePos = (Vector2)targetEnemy.transform.position;

        for (int i = 0; i < areaCount; i++)
        {
            Vector2 pos = basePos;
            if (i > 0 && overlapOffset > 0f)
                pos += Random.insideUnitCircle * overlapOffset;

            ArrowRainArea2D area = GetAreaFromPool();
            area.transform.position = pos;

            area.Setup(areaRadius, duration, tickIvl, tickDmg, enemyMask);

            if (!area.gameObject.activeSelf)
                area.gameObject.SetActive(true);
        }
    }

    private ArrowRainArea2D GetAreaFromPool()
    {
        for (int i = 0; i < _pool.Count; i++)
            if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                return _pool[i];

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