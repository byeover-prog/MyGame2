// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 가장 가까운 적(EnemyRegistryMember2D)을 찾는다.
// - 투사체를 생성한다.
// - 레벨 규칙:
//   Lv1: 투사체 1, 튕김 2
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

        transform.position = owner.position;

        _timer += Time.deltaTime;
        float cooldown = Mathf.Max(0.05f, P.cooldown);

        if (_timer >= cooldown)
        {
            _timer = 0f;
            FireShuriken();
        }
    }

    // ★ 레벨 기반 투사체 수
    private int GetProjectileCount()
    {
        int lv = level;
        if (lv >= 3) return 3;
        if (lv == 2) return 2;
        return 1;
    }

    // ★ 레벨 기반 튕김 횟수
    private int GetRicochetCount()
    {
        int lv = level;
        if (lv <= 3) return 2;
        // Lv4: 3, Lv5: 4, Lv6: 5, Lv7: 6, Lv8: 7
        return 2 + (lv - 3);
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

        // 3) ★ 레벨 기반 값 계산
        int projCount = GetProjectileCount();
        int ricochetCount = GetRicochetCount();

        for (int i = 0; i < projCount; i++)
        {
            GameObject go = Instantiate(shurikenProjectilePrefab, origin, Quaternion.identity);

            ApplyProjectileSorting(go);

            var ricochet = go.GetComponentInChildren<RicochetShurikenProjectile2D>(true);
            if (ricochet != null)
            {
                if (startTarget == null)
                {
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

            Debug.LogWarning("[RicochetShurikenWeapon2D] 투사체 프리팹에 RicochetShurikenProjectile2D 또는 ProjectileBase2D가 없습니다!", go);
        }
    }

    private static void SafeDisposeProjectile(GameObject go, RicochetShurikenProjectile2D ricochet)
    {
        if (ricochet != null)
        {
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