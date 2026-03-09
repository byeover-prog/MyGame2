// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 쿨마다 "가장 가까운 적(들)"을 기준으로 수리검 발사.
// - 레벨업이 "먹은 것처럼 로그는 뜨는데 체감이 없는" 대표 원인 2개를 동시에 막는다.
//   (1) 투사체가 같은 위치/같은 타겟으로 겹쳐서 '한 발처럼' 보임 -> 발사 시 위치/타겟을 분산
//   (2) base(CommonSkillWeapon2D)의 level 갱신 이슈 가능 -> 이 무기 자체가 _runtimeLevel을 유지
// - 레벨 규칙(요구사항):
//   Lv1: 투사체 1, 튕김 2
//   Lv2: 투사체 2, 튕김 2
//   Lv3: 투사체 3, 튕김 2
//   Lv4~8: 투사체 3, 튕김 = 2 + (lv - 3)

[DisallowMultipleComponent]
public sealed class RicochetShurikenWeapon2D : CommonSkillWeapon2D, ILevelableSkill
{
    [Header("프리팹")]
    [Tooltip("수리검 투사체 프리팹(자식 포함 RicochetShurikenProjectile2D 필요)")]
    [SerializeField] private GameObject shurikenProjectilePrefab;

    [Header("탐지")]
    [Tooltip("적 탐지 반경")]
    [Min(0.1f)]
    [SerializeField] private float detectRange = 15f;

    [Header("발사 위치(권장: Player/SpawnPoint)")]
    [Tooltip("비우면 자신(weapon 오브젝트) 위치를 사용")]
    [SerializeField] private Transform firePivot;

    [Header("겹침 방지(체감용)")]
    [Tooltip("여러 발 발사 시 생성 위치를 옆으로 벌리는 간격(유닛). 0이면 벌리지 않음")]
    [Min(0f)]
    [SerializeField] private float spawnSeparation = 0.25f;

    [Tooltip("여러 발이 같은 적을 노릴 때 시작 방향을 조금 벌리는 각도(도). 0이면 벌리지 않음")]
    [Min(0f)]
    [SerializeField] private float fanAngleDeg = 10f;

    [Tooltip("한 번 발사에서 타겟 후보를 최대 몇 마리까지 고려할지(성능 보호)")]
    [Range(1, 32)]
    [SerializeField] private int maxTargetCandidates = 12;

    // 풀 참조(있으면 사용)
    private ProjectilePool2D _pool;

    private readonly Collider2D[] _hits = new Collider2D[32];

    // 타겟 후보 캐시(할당 방지)
    private readonly EnemyRegistryMember2D[] _candidates = new EnemyRegistryMember2D[32];
    private readonly float[] _candidateSqr = new float[32];

    private float _timer;

    // 이 무기에서 실제 레벨로 사용할 값(중요)
    [SerializeField, Min(1)] private int _runtimeLevel = 1;

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

        // startLevel을 무기 레벨로 확정
        _runtimeLevel = Mathf.Clamp(startLevel, 1, 8);

        if (_pool == null)
            _pool = GetComponent<ProjectilePool2D>();

        Debug.Log($"[Shuriken] Initialize -> Lv{_runtimeLevel} (id={gameObject.GetInstanceID()})", this);
    }

    // SkillRunner가 호출하는 진짜 레벨 적용
    void ILevelableSkill.ApplyLevel(int newLevel)
    {
        _runtimeLevel = Mathf.Clamp(newLevel, 1, 8);

        Debug.Log($"[Shuriken] ApplyLevel -> Lv{_runtimeLevel} (proj={GetProjectileCount()} ric={GetRicochetCount()}) (id={gameObject.GetInstanceID()})", this);
    }

    private void Update()
    {
        if (owner == null || config == null) return;

        // 무기는 플레이어에 붙어다니는 방식(무기 오브젝트가 따로 있으면 위치 동기화)
        transform.position = owner.position;

        _timer += Time.deltaTime;

        // P는 base(CommonSkillWeapon2D)의 "폴백 포함 스탯" getter라고 가정
        float cd = Mathf.Max(0.05f, P.cooldown);
        if (_timer < cd) return;

        _timer = 0f;
        FireShuriken();
    }

    // 투사체 수: 요구사항 하드코딩
    private int GetProjectileCount()
    {
        int lv = _runtimeLevel;
        if (lv >= 3) return 3;
        if (lv == 2) return 2;
        return 1;
    }

    // 튕김 횟수: 요구사항 하드코딩
    private int GetRicochetCount()
    {
        int lv = _runtimeLevel;
        if (lv <= 3) return 2;
        return 2 + (lv - 3); // Lv4:3, Lv5:4, Lv6:5, Lv7:6, Lv8:7
    }

    private void FireShuriken()
    {
        if (shurikenProjectilePrefab == null) return;

        Vector2 origin = GetSpawnOrigin(firePivot);

        // 1) 적 탐색
        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, detectRange, _hits, enemyMask);
        if (hitCount <= 0 && requireTargetToFire) return;

        // 2) 타겟 후보 수집(가까운 순으로 정렬)
        int candidateCount = BuildCandidates(origin, hitCount);
        if (requireTargetToFire && candidateCount <= 0) return;

        // 3) 레벨 규칙 적용
        int projCount = GetProjectileCount();
        int ricochetCount = GetRicochetCount();

        // 발사 체감 확인용 로그(문제 재발 시 여기만 보면 됨)
        Debug.Log($"[Shuriken] Fire Lv{_runtimeLevel} proj={projCount} ric={ricochetCount} candidates={candidateCount} (id={gameObject.GetInstanceID()})", this);

        // 4) 발사 방향 기준(후보가 있으면 0번 기준, 없으면 오른쪽)
        Vector2 baseDir = Vector2.right;
        if (candidateCount > 0)
        {
            baseDir = (_candidates[0].Position - origin).normalized;
            if (baseDir.sqrMagnitude < 0.0001f) baseDir = Vector2.right;
        }

        Vector2 perp = new Vector2(-baseDir.y, baseDir.x);

        // 5) 여러 발: (a) 생성 위치를 옆으로 벌리고, (b) 타겟을 분산, (c) 시작 각도도 약간 분산
        float mid = (projCount - 1) * 0.5f;

        for (int i = 0; i < projCount; i++)
        {
            // 타겟 분산: 후보가 여러 마리면 i번째로, 없으면 0번(같은 적)
            EnemyRegistryMember2D target = null;
            if (candidateCount > 0)
            {
                int ti = Mathf.Clamp(i, 0, candidateCount - 1);
                target = _candidates[ti];
            }

            if (requireTargetToFire && target == null) continue;

            // 생성 위치 벌리기(겹쳐서 한 발처럼 보이는 문제 방지)
            float side = (i - mid) * spawnSeparation;
            Vector2 spawnPos = origin + perp * side;

            // fan 각도 계산(투사체가 '겹쳐 보이는' 문제 완화)
            float ang = 0f;
            if (projCount > 1) ang = (i - mid) * fanAngleDeg;

            // 투사체 생성
            GameObject go;
            RicochetShurikenProjectile2D ricochet;

            if (_pool != null)
            {
                ricochet = _pool.Get<RicochetShurikenProjectile2D>(spawnPos, Quaternion.identity);
                if (ricochet == null) continue;
                go = ricochet.gameObject;
            }
            else
            {
                go = Instantiate(shurikenProjectilePrefab, spawnPos, Quaternion.identity);
                ricochet = go.GetComponentInChildren<RicochetShurikenProjectile2D>(true);
            }

            ApplyProjectileSorting(go);

            // 메인 전용 투사체가 있으면 그걸 우선 사용
            if (ricochet != null)
            {
                if (target == null)
                {
                    ricochet.ReturnToPool();
                    continue;
                }

                // ★ 중요: 투사체 Init에서 ricochetCount를 매번 주입(풀링 상태 오염 방지)
                ricochet.Init(
                    enemyMask,
                    P.damage,
                    P.projectileSpeed,
                    Mathf.Max(0.1f, P.lifeSeconds),
                    ricochetCount,
                    target
                );

                // (옵션) 투사체가 "초기 방향"을 받는 기능이 없다면 여기서 끝.
                // 만약 나중에 Init에 dir을 추가하면, 아래 Rotate를 사용해서 시작 방향을 분산시킬 수 있음.
                if (fanAngleDeg > 0.001f)
                    go.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.right, Rotate(baseDir, ang)));

                continue;
            }

            // fallback: 범용 투사체(직선 발사)
            var proj = go.GetComponentInChildren<ProjectileBase2D>(true);
            if (proj != null)
            {
                Vector2 dir = (target != null)
                    ? (target.Position - spawnPos).normalized
                    : Rotate(baseDir, ang);

                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
                dir = Rotate(dir, ang);

                proj.Launch(dir, P.damage, P.projectileSpeed, P.lifeSeconds, enemyMask, owner);
                continue;
            }

            Debug.LogWarning("[RicochetShurikenWeapon2D] 프리팹에 투사체 스크립트가 없습니다!", go);
        }
    }

    // 후보를 수집하고 거리순(가까운 순)으로 정렬한다.
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

            // 중복 방지(같은 Enemy가 여러 콜라이더를 가질 수 있음)
            bool dup = false;
            for (int k = 0; k < n; k++)
            {
                if (_candidates[k] == member) { dup = true; break; }
            }
            if (dup) continue;

            _candidates[n] = member;
            _candidateSqr[n] = (member.Position - origin).sqrMagnitude;
            n++;

            if (n >= cap) break;
        }

        // 단순 선택 정렬(최대 12~32라 O(n^2)여도 충분)
        for (int a = 0; a < n - 1; a++)
        {
            int best = a;
            for (int b = a + 1; b < n; b++)
            {
                if (_candidateSqr[b] < _candidateSqr[best]) best = b;
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