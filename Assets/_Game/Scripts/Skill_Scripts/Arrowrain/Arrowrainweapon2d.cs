// UTF-8
// [구현 원리 요약]
// - 화살비는 발동 시점에 체력이 가장 높은 적을 골라 보스/엘리트 압박 역할을 살린다.
// - 물리 전체 탐색 대신 EnemyRegistry2D를 우선 사용한다.
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화살비(화차) 스킬 무기.
/// 가장 체력이 높은 적 위치에 장판을 깔아 지속 피해를 준다.
/// </summary>
[DisallowMultipleComponent]
public sealed class ArrowRainWeapon2D : CommonSkillWeapon2D
{
    [Header("화살비 — 장판")]
    [Tooltip("장판 프리팹 (ArrowRainArea2D 포함)")]
    [SerializeField] private ArrowRainArea2D areaPrefab;

    [Header("화살비 — 타겟")]
    [Tooltip("타겟 탐색 반경")]
    [SerializeField] private float targetSearchRadius = 20f;

    [Header("화살비 — 각성")]
    [Tooltip("각성 상태 여부 (다중 장판)")]
    [SerializeField] private bool isAwakened = false;

    [Tooltip("각성 시 동시 장판 개수")]
    [Min(1)]
    [SerializeField] private int awakenedAreaCount = 4;

#pragma warning disable 0414
    [Tooltip("보스 태그 (보스 우선 타겟용)")]
    [SerializeField] private string bossTag = "Boss";
#pragma warning restore 0414

    [Tooltip("각성 시 장판 간 위치 분산 범위")]
    [Min(0f)]
    [SerializeField] private float overlapOffset = 0.35f;

    [Header("화살비 — 안전장치")]
    [Tooltip("explosionRadius가 0일 때 사용할 기본 반경")]
    [Min(0.5f)]
    [SerializeField] private float fallbackRadius = 2.0f;

    [Header("화살비 — 풀/성능")]
    [Tooltip("장판 풀 최대 크기")]
    [Min(1)]
    [SerializeField] private int maxPoolSize = 8;

    [Header("디버그")]
    [Tooltip("화살비 동작 로그 출력")]
    [SerializeField] private bool debugLog = false;

    private readonly List<ArrowRainArea2D> _pool = new List<ArrowRainArea2D>(8);

    public void SetAwakened(bool awakened) { isAwakened = awakened; }
    public bool IsAwakened => isAwakened;

    private void Update()
    {
        if (config == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        Vector2 origin = owner != null ? (Vector2)owner.position : (Vector2)transform.position;

        if (!EnemyRegistry2D.TryGetHighestHp(origin, targetSearchRadius, out var targetEnemy) || targetEnemy == null)
            return;

        TryBeginFire(() => FireAreas(targetEnemy));
    }

    private void FireAreas(EnemyRegistryMember2D targetEnemy)
    {
        if (owner == null || areaPrefab == null) return;
        if (targetEnemy == null) return;

        var p = P;

        float areaRadius = p.explosionRadius;
        if (areaRadius < 0.5f)
            areaRadius = fallbackRadius;

        float duration = Mathf.Max(0.5f, p.lifeSeconds);
        float tickIvl = Mathf.Max(0.05f, p.hitInterval);
        int tickDmg = Mathf.Max(1, p.damage);
        int areaCount = isAwakened ? awakenedAreaCount : 1;

        if (debugLog)
            Debug.Log($"[ArrowRainWeapon2D] 발사: {areaCount}개 | 피해량={tickDmg} | 반경={areaRadius:F1}", this);

        Vector2 basePos = targetEnemy.Position;

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
