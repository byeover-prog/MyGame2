// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 가장 가까운 적(EnemyRegistryMember2D)을 찾는다.
// - 투사체를 생성한다.
// - RicochetShurikenProjectile2D는 루트/자식 어디에 붙어있든 찾아서 Init(...)을 "반드시" 호출한다(필수).
// - (fallback) ProjectileBase2D도 루트/자식 어디에 붙어있든 찾아 Launch(...)를 호출한다.
// - 타겟이 없는데 튕김형이 생성되는 케이스는 즉시 정리(난사/에러 방지).
[DisallowMultipleComponent]
public sealed class RicochetShurikenWeapon2D : CommonSkillWeapon2D
{
    [Header("발사 설정")]
    [Tooltip("수리검 투사체 프리팹 (RicochetShurikenProjectile2D가 붙어있어야 정상 동작)\n※ 루트가 아니라 '자식'에 붙어 있어도 자동으로 찾아 Init을 호출합니다.")]
    [SerializeField] private GameObject shurikenProjectilePrefab;

    [Tooltip("적을 감지할 최대 사거리(반경)")]
    [SerializeField] private float detectRange = 15f;

    [Header("튕김 설정")]
    [Tooltip("튕김 횟수(투사체 Init으로 전달)")]
    [SerializeField, Min(0)] private int ricochetCount = 2;

    [Header("발사 피벗(권장: SpawnPoint)")]
    [SerializeField] private Transform firePivot;

    // NonAlloc 버퍼(OverlapCircleNonAlloc)
    private readonly Collider2D[] _hits = new Collider2D[32];

    private float _timer;

    private void Awake()
    {
        if (firePivot == null)
        {
            var t = transform.Find("SpawnPoint");
            if (t != null) firePivot = t;
        }
    }

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);
        _timer = 0f;
    }

    private void Update()
    {
        if (owner == null || config == null) return;

        transform.position = owner.position; // 플레이어 위치 따라가기

        _timer += Time.deltaTime;
        float cooldown = Mathf.Max(0.05f, P.cooldown);

        if (_timer >= cooldown)
        {
            _timer = 0f;
            FireShuriken();
        }
    }

    private void FireShuriken()
    {
        if (shurikenProjectilePrefab == null) return;

        Vector2 origin = GetSpawnOrigin(firePivot);

        // 1) 적 탐색
        int countHit = Physics2D.OverlapCircleNonAlloc(origin, detectRange, _hits, enemyMask);
        if (countHit <= 0)
        {
            if (requireTargetToFire) return;
        }

        // 2) 첫 타겟 확보(Init에 필요)
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

        // 3) 투사체 생성 및 발사
        int count = Mathf.Max(1, P.projectileCount);
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(shurikenProjectilePrefab, origin, Quaternion.identity);

            // 정렬 order 통일(겹침/가림 방지)
            ApplyProjectileSorting(go);

            // ✅ 1순위: RicochetShurikenProjectile2D (루트/자식 포함 검색)
            // - TryGetComponent는 루트만 보므로, 프리팹 구조가 자식에 스크립트가 붙어 있으면 Init이 누락된다.
            var ricochet = go.GetComponentInChildren<RicochetShurikenProjectile2D>(true);
            if (ricochet != null)
            {
                if (startTarget == null)
                {
                    // 튕김형은 타겟이 없으면 의미 없음(난사/에러 방지)
                    // 풀 사용 중이면 ReturnToPool, 아니면 Destroy로 안전 처리
                    SafeDisposeProjectile(go, ricochet);
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

            // (fallback) 범용 투사체(루트/자식 포함 검색)
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

            Debug.LogWarning("[RicochetShurikenWeapon2D] 투사체 프리팹에 RicochetShurikenProjectile2D 또는 ProjectileBase2D가 없습니다!", go);
        }
    }

    // ✅ 안전 처분: 풀 기반이면 ReturnToPool, 아니면 Destroy
    private static void SafeDisposeProjectile(GameObject go, RicochetShurikenProjectile2D ricochet)
    {
        if (ricochet != null)
        {
            // ReturnToPool이 내부에서 SetActive(false) / pool 반환을 한다는 전제로 사용
            ricochet.ReturnToPool();
            return;
        }

        if (go != null)
            Destroy(go);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}