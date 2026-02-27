// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 가장 가까운 적을 찾아 수리검을 발사.
// - ★ 풀(ProjectilePool2D)을 사용하여 투사체 생성 (Instantiate 금지!)
// - 투사체 수/튕김은 하드코딩 레벨 규칙:
//   Lv1: 투사체 1, 튕김 2 (총 3타격)
//   Lv2: 투사체 2, 튕김 2
//   Lv3: 투사체 3, 튕김 2
//   Lv4~8: 투사체 3, 튕김 = 2 + (lv - 3)
[DisallowMultipleComponent]
public sealed class RicochetShurikenWeapon2D : CommonSkillWeapon2D
{
    [Header("발사 설정")]
    [Tooltip("수리검 투사체 프리팹")]
    [SerializeField] private GameObject shurikenProjectilePrefab;

    [Tooltip("적을 감지할 최대 사거리(반경)")]
    [SerializeField] private float detectRange = 15f;

    [Header("발사 피벗(권장: SpawnPoint)")]
    [SerializeField] private Transform firePivot;

    // ★ 풀 참조
    private ProjectilePool2D _pool;

    private readonly Collider2D[] _hits = new Collider2D[32];
    private float _timer;

    private void Awake()
    {
        if (firePivot == null)
        {
            var t = transform.Find("SpawnPoint");
            if (t != null) firePivot = t;
        }

        // ★ 같은 GameObject에 붙어있는 ProjectilePool2D를 찾아 연결
        _pool = GetComponent<ProjectilePool2D>();
    }

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);
        _timer = 0f;

        // Initialize가 나중에 호출될 수 있으므로 풀도 재확인
        if (_pool == null)
            _pool = GetComponent<ProjectilePool2D>();
    }

    private void Update()
    {
        if (owner == null || config == null) return;

        transform.position = owner.position;

        _timer += Time.deltaTime;
        float cd = Mathf.Max(0.05f, P.cooldown);

        if (_timer >= cd)
        {
            _timer = 0f;
            FireShuriken();
        }
    }

    // ★ 투사체 수: 하드코딩
    private int GetProjectileCount()
    {
        int lv = level;
        if (lv >= 3) return 3;
        if (lv == 2) return 2;
        return 1;
    }

    // ★ 튕김 횟수: 하드코딩
    private int GetRicochetCount()
    {
        int lv = level;
        if (lv <= 3) return 2;
        return 2 + (lv - 3); // Lv4:3, Lv5:4, Lv6:5, Lv7:6, Lv8:7
    }

    protected override void OnLevelChanged()
    {
        base.OnLevelChanged();
        Debug.Log($"[Shuriken] Lv{level} → 투사체={GetProjectileCount()} 튕김={GetRicochetCount()} | cooldown={P.cooldown:F2} damage={P.damage}");
    }

    private void FireShuriken()
    {
        if (shurikenProjectilePrefab == null) return;

        Vector2 origin = GetSpawnOrigin(firePivot);

        // 1) 적 탐색
        int countHit = Physics2D.OverlapCircleNonAlloc(origin, detectRange, _hits, enemyMask);
        if (countHit <= 0 && requireTargetToFire) return;

        // 2) 첫 타겟 확보
        EnemyRegistryMember2D startTarget = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < countHit; i++)
        {
            var hit = _hits[i];
            if (hit == null) continue;

            var member = hit.GetComponentInParent<EnemyRegistryMember2D>();
            if (member == null || !member.IsValidTarget) continue;

            float sqr = (member.Position - origin).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                startTarget = member;
            }
        }

        if (requireTargetToFire && startTarget == null) return;

        // 3) 레벨 규칙
        int projCount = GetProjectileCount();
        int ricochetCount = GetRicochetCount();

        for (int i = 0; i < projCount; i++)
        {
            // ★ 풀에서 꺼내기 (풀이 없으면 Instantiate fallback)
            GameObject go;
            RicochetShurikenProjectile2D ricochet;

            if (_pool != null)
            {
                ricochet = _pool.Get<RicochetShurikenProjectile2D>(origin, Quaternion.identity);
                if (ricochet == null) continue;
                go = ricochet.gameObject;
            }
            else
            {
                // fallback: 풀이 없으면 Instantiate (비권장)
                go = Instantiate(shurikenProjectilePrefab, origin, Quaternion.identity);
                ricochet = go.GetComponentInChildren<RicochetShurikenProjectile2D>(true);
            }

            ApplyProjectileSorting(go);

            if (ricochet != null)
            {
                if (startTarget == null)
                {
                    ricochet.ReturnToPool();
                    continue;
                }

                ricochet.Init(
                    enemyMask,
                    P.damage,
                    P.projectileSpeed,
                    Mathf.Max(0.1f, P.lifeSeconds),
                    ricochetCount,
                    startTarget
                );
                continue;
            }

            // (fallback) 범용 투사체
            var proj = go.GetComponentInChildren<ProjectileBase2D>(true);
            if (proj != null)
            {
                Vector2 dir = (startTarget != null)
                    ? (startTarget.Position - origin).normalized
                    : Vector2.right;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

                proj.Launch(dir, P.damage, P.projectileSpeed, P.lifeSeconds, enemyMask, owner);
                continue;
            }

            Debug.LogWarning("[RicochetShurikenWeapon2D] 프리팹에 투사체 스크립트가 없습니다!", go);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}