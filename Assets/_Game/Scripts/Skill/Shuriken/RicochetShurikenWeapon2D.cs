// UTF-8
// ============================================================================
// RicochetShurikenWeapon2D.cs
//
// [수정 v3]
// 1. _burstFiring 잠김 방지: OnDisable에서 강제 리셋 + 안전 타임아웃
// 2. 순차 발사 중 타겟 사망 → 매 발사마다 타겟 재탐색
// 3. _runtimeLevel 동기화 (OnLevelChanged)
// 4. JSON burstInterval 지원
// ============================================================================
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RicochetShurikenWeapon2D : CommonSkillWeapon2D
{
    [Header("프리팹")]
    [SerializeField] private GameObject shurikenProjectilePrefab;

    [Header("탐지")]
    [Min(0.1f)]
    [SerializeField] private float detectRange = 15f;

    [Header("발사 위치")]
    [SerializeField] private Transform firePivot;

    [Header("순차 발사")]
    [Tooltip("투사체 간 발사 간격(초). JSON burstInterval 우선.")]
    [Min(0.01f)]
    [SerializeField] private float fireInterval = 0.2f;

    [Header("겹침 방지")]
    [Min(0f)]
    [SerializeField] private float spawnSeparation = 0f;

    [Min(0f)]
    [SerializeField] private float fanAngleDeg = 0f;

    [Range(1, 32)]
    [SerializeField] private int maxTargetCandidates = 12;

    private ProjectilePool2D _pool;
    private readonly Collider2D[] _hits = new Collider2D[32];
    private readonly EnemyRegistryMember2D[] _candidates = new EnemyRegistryMember2D[32];
    private readonly float[] _candidateSqr = new float[32];

    private float _timer;
    private int _runtimeLevel = 1;
    private bool _burstFiring;
    private float _burstSafetyTimer; // ★ 안전 타임아웃
    private float _burstInterval = 0.2f;

    private void Awake()
    {
        if (firePivot == null)
        {
            var t = transform.Find("SpawnPoint");
            if (t != null) firePivot = t;
        }
        _pool = GetComponent<ProjectilePool2D>();
    }

    // ★ 핵심 수정 1: OnDisable에서 _burstFiring 강제 리셋
    // 레벨업 시간정지 → 코루틴 중단 → _burstFiring=true 영구 잠김 방지
    private void OnDisable()
    {
        StopAllCoroutines();
        _burstFiring = false;
    }

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);
        _timer = 0f;
        _burstFiring = false;
        _runtimeLevel = Mathf.Clamp(startLevel, 1, 8);
        if (_pool == null) _pool = GetComponent<ProjectilePool2D>();
        CacheBurstInterval();
    }

    protected override void OnLevelChanged()
    {
        _runtimeLevel = Mathf.Clamp(level, 1, 8);
        CacheBurstInterval();
    }

    private void CacheBurstInterval()
    {
        _burstInterval = fireInterval;
        if (TryGetBalanceRow(out var row))
        {
            float bi = GetBurstIntervalFromRow(row);
            if (bi > 0.01f) _burstInterval = bi;
        }
    }

    private static float GetBurstIntervalFromRow(SkillBalanceDB2D.SkillRow2D row)
    {
        if (row == null) return -1f;
        try
        {
            var field = row.GetType().GetField("burstInterval");
            if (field != null && field.FieldType == typeof(float))
                return (float)field.GetValue(row);
        }
        catch { }
        return -1f;
    }

    private void Update()
    {
        if (owner == null || config == null) return;
        transform.position = owner.position;

        // ★ 안전 타임아웃: _burstFiring이 2초 이상 풀리지 않으면 강제 리셋
        if (_burstFiring)
        {
            _burstSafetyTimer += Time.deltaTime;
            if (_burstSafetyTimer > 2f)
            {
                _burstFiring = false;
                _burstSafetyTimer = 0f;
            }
            return;
        }

        _timer += Time.deltaTime;
        float cd = Mathf.Max(0.05f, P.cooldown);
        if (_timer < cd) return;

        _timer = 0f;
        _burstSafetyTimer = 0f;
        StartCoroutine(FireBurst());
    }

    // ══════════════════════════════════════════════════════════════
    // 순차 발사 코루틴
    // ══════════════════════════════════════════════════════════════

    private IEnumerator FireBurst()
    {
        _burstFiring = true;

        int projCount = GetProjectileCount();
        int ricochetCount = GetRicochetCount();

        for (int i = 0; i < projCount; i++)
        {
            // ★ 핵심 수정 2: 매 발사마다 타겟 재탐색
            // 첫 수리검이 적을 죽이면, 다음 수리검은 새로운 가장 가까운 적을 찾음
            Vector2 origin = (owner != null)
                ? (firePivot != null ? (Vector2)firePivot.position : (Vector2)owner.position)
                : (Vector2)transform.position;

            int hitCount = Physics2DCompat.OverlapCircleNonAlloc(origin, detectRange, _hits, enemyMask);
            int candidateCount = BuildCandidates(origin, hitCount);

            if (requireTargetToFire && candidateCount <= 0)
            {
                // 적이 없으면 이 발은 건너뜀 (다음 발에서 다시 탐색)
                if (i < projCount - 1)
                    yield return new WaitForSeconds(_burstInterval);
                continue;
            }

            // 가장 가까운 적 (직선 발사)
            EnemyRegistryMember2D target = _candidates[0];
            Vector2 baseDir = (target.Position - origin).normalized;
            if (baseDir.sqrMagnitude < 0.0001f) baseDir = Vector2.right;

            Vector2 perp = new Vector2(-baseDir.y, baseDir.x);
            float mid = (projCount - 1) * 0.5f;
            float side = (i - mid) * spawnSeparation;
            Vector2 spawnPos = origin + perp * side;
            float ang = projCount > 1 ? (i - mid) * fanAngleDeg : 0f;

            FireOneShuriken(spawnPos, baseDir, ang, ricochetCount, target);

            // 다음 발까지 대기 (마지막 발은 대기 안 함)
            if (i < projCount - 1)
                yield return new WaitForSeconds(_burstInterval);
        }

        _burstFiring = false;
    }

    private void FireOneShuriken(Vector2 spawnPos, Vector2 baseDir, float angleDeg,
                                  int ricochetCount, EnemyRegistryMember2D target)
    {
        if (shurikenProjectilePrefab == null) return;

        GameObject go;
        RicochetShurikenProjectile2D ricochet;

        if (_pool != null)
        {
            ricochet = _pool.Get<RicochetShurikenProjectile2D>(spawnPos, Quaternion.identity);
            if (ricochet == null) return;
            go = ricochet.gameObject;
        }
        else
        {
            go = Instantiate(shurikenProjectilePrefab, spawnPos, Quaternion.identity);
            ricochet = go.GetComponentInChildren<RicochetShurikenProjectile2D>(true);
        }

        ApplyProjectileSorting(go);

        if (ricochet != null)
        {
            if (target == null)
            {
                ricochet.ReturnToPool();
                return;
            }

            ricochet.Init(
                enemyMask,
                P.damage,
                P.projectileSpeed,
                Mathf.Max(0.1f, P.lifeSeconds),
                ricochetCount,
                target
            );

            if (Mathf.Abs(angleDeg) > 0.001f)
                go.transform.rotation = Quaternion.Euler(0f, 0f,
                    Vector2.SignedAngle(Vector2.right, Rotate(baseDir, angleDeg)));
            return;
        }

        var proj = go.GetComponentInChildren<ProjectileBase2D>(true);
        if (proj != null)
        {
            Vector2 dir = target != null
                ? (target.Position - spawnPos).normalized
                : Rotate(baseDir, angleDeg);
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            proj.Launch(dir, P.damage, P.projectileSpeed, P.lifeSeconds, enemyMask, owner);
        }
    }

    private int GetProjectileCount()
    {
        int lv = _runtimeLevel;
        if (lv >= 3) return 3;
        if (lv == 2) return 2;
        return 1;
    }

    private int GetRicochetCount()
    {
        int lv = _runtimeLevel;
        if (lv <= 3) return 2;
        return 2 + (lv - 3); // Lv4=3, Lv5=4, Lv6=5, Lv7=6, Lv8=7
    }

    private int BuildCandidates(Vector2 origin, int hitCount)
    {
        int n = 0;
        int cap = Mathf.Clamp(maxTargetCandidates, 1, 32);

        for (int i = 0; i < hitCount; i++)
        {
            var hit = _hits[i];
            if (hit == null) continue;

            var member = hit.GetComponentInParent<EnemyRegistryMember2D>();
            if (member == null || !member.IsValidTarget) continue;

            bool dup = false;
            for (int k = 0; k < n; k++)
                if (_candidates[k] == member) { dup = true; break; }
            if (dup) continue;

            _candidates[n] = member;
            _candidateSqr[n] = (member.Position - origin).sqrMagnitude;
            n++;
            if (n >= cap) break;
        }

        for (int a = 0; a < n - 1; a++)
        {
            int best = a;
            for (int b = a + 1; b < n; b++)
                if (_candidateSqr[b] < _candidateSqr[best]) best = b;
            if (best != a)
            {
                (_candidates[a], _candidates[best]) = (_candidates[best], _candidates[a]);
                (_candidateSqr[a], _candidateSqr[best]) = (_candidateSqr[best], _candidateSqr[a]);
            }
        }

        return n;
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        if (Mathf.Abs(deg) < 0.0001f) return v;
        float rad = deg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}