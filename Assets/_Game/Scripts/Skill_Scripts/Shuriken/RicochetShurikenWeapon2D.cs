// UTF-8
// ============================================================================
// RicochetShurikenWeapon2D.cs
//
// [수정 사항]
// 1. _runtimeLevel이 갱신 안 되는 버그 수정
//    → explicit interface ApplyLevel은 리플렉션에서 안 잡힘
//    → OnLevelChanged()에서 base.level과 동기화
// 2. 순차 발사 0.2초 간격 (기획서: 겐지처럼 직선으로 슈슈슉, 부채꼴X)
// 3. JSON burstInterval 필드 지원
//
// [기획서 규칙]
// Lv1: 투사체 1, 튕김 2, 데미지 15, 쿨다운 2초
// Lv2: 투사체 2, 튕김 2, 발사간격 0.2초
// Lv3: 투사체 3, 튕김 2, 발사간격 0.2초
// Lv4~8: 투사체 3, 튕김 = Lv3 이후 레벨당 +1, 데미지 +2/레벨
//
// [Inspector 설정 — Weapon_Shuriken]
// config              → CS_Shuriken
// Enemy Mask          → Enemy
// Shuriken Projectile Prefab → Shuriken 프리팹
// Detect Range        → 10
// Fire Pivot          → SpawnPoint (자식)
// Fan Angle Deg       → 0 (부채꼴X, 직선 발사)
// Spawn Separation    → 0 (같은 위치에서 발사)
// ============================================================================
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RicochetShurikenWeapon2D : CommonSkillWeapon2D
{
    [Header("프리팹")]
    [Tooltip("수리검 투사체 프리팹 (RicochetShurikenProjectile2D 필요)")]
    [SerializeField] private GameObject shurikenProjectilePrefab;

    [Header("탐지")]
    [Min(0.1f)]
    [SerializeField] private float detectRange = 15f;

    [Header("발사 위치")]
    [SerializeField] private Transform firePivot;

    [Header("순차 발사")]
    [Tooltip("투사체 간 발사 간격(초). JSON burstInterval이 있으면 그걸 우선 사용.")]
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

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);
        _timer = 0f;
        _runtimeLevel = Mathf.Clamp(startLevel, 1, 8);
        if (_pool == null) _pool = GetComponent<ProjectilePool2D>();
        CacheBurstInterval();
    }

    // ══════════════════════════════════════════════════════════════
    // ★ 핵심 수정: OnLevelChanged에서 _runtimeLevel 동기화
    //
    // [왜 이게 필요한가]
    // LevelableSkillMarker2D가 리플렉션으로 ApplyLevel(int) 호출
    //   → base.ApplyLevel → SetLevel → level 갱신 → OnLevelChanged
    // 이전: explicit interface의 ApplyLevel에서만 _runtimeLevel 갱신
    //   → 리플렉션이 explicit method를 못 찾아서 영원히 호출 안 됨
    //   → _runtimeLevel=1 고정 → 투사체 항상 1개 → 순차 발사 불가
    // ══════════════════════════════════════════════════════════════

    protected override void OnLevelChanged()
    {
        _runtimeLevel = Mathf.Clamp(level, 1, 8);
        CacheBurstInterval();

        Debug.Log($"[Shuriken] Lv{_runtimeLevel} proj={GetProjectileCount()} ric={GetRicochetCount()} burst={_burstInterval:F2}s", this);
    }

    private void CacheBurstInterval()
    {
        _burstInterval = fireInterval;

        if (TryGetBalanceRow(out var row))
        {
            float bi = GetBurstIntervalFromRow(row);
            if (bi > 0.01f)
                _burstInterval = bi;
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

        _timer += Time.deltaTime;
        float cd = Mathf.Max(0.05f, P.cooldown);
        if (_timer < cd) return;
        if (_burstFiring) return;

        _timer = 0f;
        StartCoroutine(FireBurst());
    }

    // ══════════════════════════════════════════════════════════════
    // 순차 발사 코루틴 (겐지처럼 슈슈슉)
    // ══════════════════════════════════════════════════════════════

    private IEnumerator FireBurst()
    {
        _burstFiring = true;

        Vector2 origin = GetSpawnOrigin(firePivot);

        int hitCount = Physics2DCompat.OverlapCircleNonAlloc(origin, detectRange, _hits, enemyMask);
        int candidateCount = BuildCandidates(origin, hitCount);

        if (requireTargetToFire && candidateCount <= 0)
        {
            _burstFiring = false;
            yield break;
        }

        int projCount = GetProjectileCount();
        int ricochetCount = GetRicochetCount();

        Vector2 baseDir = Vector2.right;
        if (candidateCount > 0)
        {
            baseDir = (_candidates[0].Position - origin).normalized;
            if (baseDir.sqrMagnitude < 0.0001f) baseDir = Vector2.right;
        }

        Vector2 perp = new Vector2(-baseDir.y, baseDir.x);
        float mid = (projCount - 1) * 0.5f;

        for (int i = 0; i < projCount; i++)
        {
            if (owner != null)
                origin = firePivot != null ? (Vector2)firePivot.position : (Vector2)owner.position;

            // ★ 겐지처럼: 전부 같은 적을 노림 (직선)
            EnemyRegistryMember2D target = null;
            if (candidateCount > 0)
                target = _candidates[0];

            if (requireTargetToFire && target == null) continue;

            float side = (i - mid) * spawnSeparation;
            Vector2 spawnPos = origin + perp * side;
            float ang = projCount > 1 ? (i - mid) * fanAngleDeg : 0f;

            FireOneShuriken(spawnPos, baseDir, ang, ricochetCount, target);

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

    // ══════════════════════════════════════════════════════════════
    // 레벨 규칙 (기획서 하드코딩)
    // ══════════════════════════════════════════════════════════════

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
        return Mathf.Min(6, 2 + (lv - 3));
    }

    // ══════════════════════════════════════════════════════════════
    // 타겟 수집
    // ══════════════════════════════════════════════════════════════

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