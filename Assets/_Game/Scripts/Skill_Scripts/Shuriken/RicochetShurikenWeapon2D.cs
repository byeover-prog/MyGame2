// UTF-8
using UnityEngine;

/// <summary>
/// 수리검 무기.
/// [구현 원리 요약]
/// - 레벨은 CommonSkillWeapon2D.level을 정본으로 쓰고, OnLevelChanged에서만 런타임 값을 갱신한다.
/// - 발사 시 가장 가까운 적 후보를 정렬해서 여러 발이면 타겟/위치/각도를 분산한다.
/// - 투사체는 내부 static 풀에서 꺼내서 재사용하므로 ProjectilePool2D가 없어도 GC 스파이크가 나지 않는다.
/// </summary>
[DisallowMultipleComponent]
public sealed class RicochetShurikenWeapon2D : CommonSkillWeapon2D
{
    [Header("프리팹")]
    [Tooltip("수리검 투사체 프리팹(루트나 자식에 RicochetShurikenProjectile2D 필요)")]
    [SerializeField] private GameObject shurikenProjectilePrefab;

    [Header("탐지")]
    [Tooltip("적 탐지 반경")]
    [Min(0.1f)] [SerializeField] private float detectRange = 15f;

    [Header("발사 위치")]
    [Tooltip("비우면 SpawnPoint를 자동 탐색하고, 그래도 없으면 자신 위치를 사용")]
    [SerializeField] private Transform firePivot;

    [Header("겹침 방지")]
    [Tooltip("여러 발 발사 시 생성 위치를 옆으로 벌리는 간격")]
    [Min(0f)] [SerializeField] private float spawnSeparation = 0.25f;

    [Tooltip("여러 발이 겹치지 않게 시작 각도를 벌리는 값")]
    [Min(0f)] [SerializeField] private float fanAngleDeg = 12f;

    [Tooltip("한 번 발사에서 고려할 최대 타겟 수")]
    [Range(1, 32)] [SerializeField] private int maxTargetCandidates = 12;

    [Header("풀")]
    [Tooltip("게임 시작 시 미리 만들어 둘 수리검 수")]
    [Range(0, 64)] [SerializeField] private int prewarmCount = 12;

    private readonly Collider2D[] _hits = new Collider2D[32];
    private readonly EnemyRegistryMember2D[] _candidates = new EnemyRegistryMember2D[32];
    private readonly float[] _candidateSqr = new float[32];

    private float _timer;
    [SerializeField, Min(1)] private int _runtimeLevel = 1;

    private void Awake()
    {
        ResolveFirePivot();
    }

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);

        ResolveFirePivot();
        _timer = 0f;
        _runtimeLevel = Mathf.Clamp(level, 1, 8);

        if (shurikenProjectilePrefab != null && prewarmCount > 0)
            RicochetShurikenProjectile2D.Prewarm(shurikenProjectilePrefab, prewarmCount);
    }

    protected override void OnLevelChanged()
    {
        _runtimeLevel = Mathf.Clamp(level, 1, 8);
    }

    private void ResolveFirePivot()
    {
        if (firePivot != null) return;

        Transform found = transform.Find("SpawnPoint");
        if (found != null)
            firePivot = found;
    }

    private void Update()
    {
        if (owner == null || config == null) return;

        transform.position = owner.position;
        _timer += Time.deltaTime;

        float cd = Mathf.Max(0.05f, P.cooldown);
        if (_timer < cd) return;
        _timer = 0f;

        FireShuriken();
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
        return 2 + (lv - 3);
    }

    private void FireShuriken()
    {
        if (shurikenProjectilePrefab == null) return;

        Vector2 origin = GetSpawnOrigin(firePivot);

        int hitCount = Physics2DCompat.OverlapCircleNonAlloc(origin, detectRange, _hits, enemyMask);
        if (hitCount <= 0 && requireTargetToFire) return;

        int candidateCount = BuildCandidates(origin, hitCount);
        if (requireTargetToFire && candidateCount <= 0) return;

        int projCount = GetProjectileCount();
        int ricochetCount = GetRicochetCount();

        Vector2 baseDir = Vector2.right;
        if (candidateCount > 0)
        {
            Vector2 toFirst = _candidates[0].Position - origin;
            if (toFirst.sqrMagnitude > 0.0001f)
                baseDir = toFirst.normalized;
        }

        Vector2 perp = new Vector2(-baseDir.y, baseDir.x);
        float mid = (projCount - 1) * 0.5f;

        for (int i = 0; i < projCount; i++)
        {
            EnemyRegistryMember2D target = null;
            if (candidateCount > 0)
                target = _candidates[i % candidateCount];

            if (requireTargetToFire && target == null) continue;

            float side = (i - mid) * spawnSeparation;
            Vector2 spawnPos = origin + perp * side;

            float angle = projCount > 1 ? (i - mid) * fanAngleDeg : 0f;
            Vector2 startDir;

            if (target != null)
            {
                Vector2 toTarget = target.Position - spawnPos;
                startDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : baseDir;
            }
            else
            {
                startDir = baseDir;
            }

            startDir = Rotate(startDir, angle);
            if (startDir.sqrMagnitude < 0.0001f)
                startDir = Vector2.right;

            RicochetShurikenProjectile2D projectile = RicochetShurikenProjectile2D.Spawn(shurikenProjectilePrefab, spawnPos, Quaternion.identity);
            if (projectile == null) continue;

            ApplyProjectileSorting(projectile.gameObject);
            projectile.Init(
                enemyMask,
                Mathf.Max(1, P.damage),
                Mathf.Max(0.1f, P.projectileSpeed),
                Mathf.Max(0.1f, P.lifeSeconds),
                ricochetCount,
                target,
                startDir);
        }
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
            {
                if (_candidates[k] == member)
                {
                    dup = true;
                    break;
                }
            }

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
            {
                if (_candidateSqr[b] < _candidateSqr[best])
                    best = b;
            }

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