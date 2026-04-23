// UTF-8
// ============================================================================
// BoomerangWeapon2D.cs
// 경로: Assets/_Game/Scripts/WeaponSystem/WeaponPrefab_Scripts/BoomerangWeapon2D.cs
//
// [수정 사항]
// 1. 버스트 중 이미 타겟팅한 적 제외 (Physics2D.OverlapCircle 직접 탐색)
// 2. 발사 간격 0.2초 (Inspector 조정 가능)
// 3. BoomerangSpin2D를 비주얼 자식에만 적용 (VFX 꼬임 방지)
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BoomerangWeapon2D : CommonSkillWeapon2D
{
    [Header("풀")]
    [SerializeField] private ProjectilePool2D pool;

    [Header("스폰")]
    [SerializeField] private Transform spawnPoint;

    [Header("연사(순차 발사)")]
    [Tooltip("투사체 간 발사 간격(초). 각 발사마다 새로운 가장 먼 적을 탐색합니다.")]
    [SerializeField, Min(0f)] private float burstIntervalSeconds = 0.2f;

    [Header("탐색")]
    [Tooltip("가장 먼 적 탐색 범위입니다.")]
    [SerializeField, Min(1f)] private float searchRange = 25f;

    [Header("비주얼")]
    [Tooltip("부메랑 스프라이트가 회전하는 속도(도/초). 0이면 회전 없음.")]
    [SerializeField] private float spinDegreesPerSecond = 720f;

    private bool _isBurstFiring;

    // 버스트 중 이미 타겟팅한 적 InstanceID 추적
    private readonly HashSet<int> _burstTargetedIds = new HashSet<int>(16);

    // 적 탐색용 (GC 0)
    private readonly Collider2D[] _searchHits = new Collider2D[64];
    private ContactFilter2D _searchFilter;
    private bool _filterReady;

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;
    }

    private void Update()
    {
        if (config == null) return;
        if (_isBurstFiring) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        if (requireTargetToFire)
        {
            if (!TryGetFarthest(out var target) || target == null)
                return;
        }

        StartCoroutine(FireBurst());
        cooldownTimer = Mathf.Max(0.01f, P.cooldown);
    }

    private System.Collections.IEnumerator FireBurst()
    {
        if (pool == null || owner == null)
            yield break;

        _isBurstFiring = true;
        _burstTargetedIds.Clear();

        int count = Mathf.Max(1, P.projectileCount);
        float speed = Mathf.Max(0.5f, P.projectileSpeed);
        float backSpeed = speed * 1.2f;

        for (int i = 0; i < count; i++)
        {
            Vector2 origin = spawnPoint != null
                ? (Vector2)spawnPoint.position
                : (Vector2)owner.position;

            // ★ 이미 타겟팅한 적을 제외하고 가장 먼 적 탐색
            Transform target = FindFarthestExcluding(origin);

            Vector2 dir;
            float actualMaxDist;

            if (target != null)
            {
                _burstTargetedIds.Add(target.gameObject.GetInstanceID());
                dir = ((Vector2)target.position - origin).normalized;
                float targetDist = Vector2.Distance(origin, target.position);
                actualMaxDist = targetDist + 1.5f;
            }
            else
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                actualMaxDist = 5f;
            }

            float estimatedLife = (actualMaxDist / speed) + (actualMaxDist / backSpeed) + 1f;

            var proj = pool.Get<BoomerangProjectile2D>(origin, Quaternion.identity);
            proj.Init(owner, dir, enemyMask, P.damage, speed, backSpeed, actualMaxDist, estimatedLife);

            ApplySpinToVisual(proj);

            if (i < count - 1 && burstIntervalSeconds > 0f)
                yield return new WaitForSeconds(burstIntervalSeconds);
        }

        _isBurstFiring = false;
    }

    /// <summary>
    /// 제외 목록에 없는 적 중에서 가장 먼 적의 Transform을 반환한다.
    /// Physics2D.OverlapCircle 직접 탐색 (EnemyRegistry2D 미사용).
    /// </summary>
    private Transform FindFarthestExcluding(Vector2 origin)
    {
        EnsureFilter();

        int count = Physics2D.OverlapCircle(
            origin, searchRange, _searchFilter, _searchHits);

        if (count == 0) return null;

        Transform best = null;
        float bestDistSq = -1f;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _searchHits[i];
            if (hit == null) continue;

            GameObject go = hit.gameObject;
            if (!go.activeInHierarchy) continue;

            // 이미 이번 버스트에서 타겟팅한 적은 제외
            if (_burstTargetedIds.Contains(go.GetInstanceID())) continue;

            float distSq = ((Vector2)go.transform.position - origin).sqrMagnitude;
            if (distSq > bestDistSq)
            {
                bestDistSq = distSq;
                best = go.transform;
            }
        }

        return best;
    }

    private void EnsureFilter()
    {
        if (_filterReady) return;
        _searchFilter = new ContactFilter2D();
        _searchFilter.SetLayerMask(enemyMask);
        _searchFilter.useTriggers = true;
        _filterReady = true;
    }

    /// <summary>
    /// Spin을 SpriteRenderer가 있는 자식에만 적용 (VFX 꼬임 방지).
    /// </summary>
    private void ApplySpinToVisual(BoomerangProjectile2D proj)
    {
        if (spinDegreesPerSecond == 0f) return;

        SpriteRenderer sr = proj.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

        GameObject visualObj = sr.gameObject;

        if (!visualObj.TryGetComponent<BoomerangSpin2D>(out var spin))
            spin = visualObj.AddComponent<BoomerangSpin2D>();

        spin.SetSpin(spinDegreesPerSecond);

        // 루트에 기존 Spin이 있으면 회전 중지
        if (visualObj != proj.gameObject
            && proj.TryGetComponent<BoomerangSpin2D>(out var rootSpin))
        {
            rootSpin.SetSpin(0f);
        }
    }
}