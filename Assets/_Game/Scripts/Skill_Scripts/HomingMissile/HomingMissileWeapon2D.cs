// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 쿨타임마다 "가장 가까운 적"을 찾아 호밍 미사일 1발 발사
// - 레벨 규칙(요구사항):
//   1레벨: chainCount=1 (총 2회 타격)
//   2~6레벨: chainCount 레벨당 +1 (최대 6)
//   6~8레벨: 공격력 증가(인스펙터 배율 적용)
// - EnemyRegistry2D로 타겟 탐색(할당 최소)
// - SkillBalanceService2D는 static 서비스(Instance 없음)
// - ProjectilePool2D.Get<T>()는 제네릭이므로 T를 명시해야 함

[DisallowMultipleComponent]
public sealed class HomingMissileWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("발사")]
    [Tooltip("발사할 호밍 미사일 프리팹(HomingMissileProjectile2D 포함)")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("미사일 이동 속도")]
    [SerializeField] private float projectileSpeed = 10f;

    [Tooltip("재사용 대기시간(초)")]
    [SerializeField] private float cooldownSeconds = 1.8f;

    [Tooltip("타겟 탐색/추적 반경(EnemyRegistry2D 거리 제한에 사용)")]
    [SerializeField] private float aimRange = 18f;

    [Tooltip("초당 회전 속도(도) - 값이 클수록 급격히 꺾음")]
    [SerializeField] private float turnSpeedDeg = 540f;

    [Tooltip("기본 공격력")]
    [SerializeField] private int damage = 4;

    [Tooltip("미사일 최대 생존 시간(초)")]
    [SerializeField] private float projectileLifeSeconds = 3.0f;

    [Tooltip("적 레이어")]
    [SerializeField] private LayerMask enemyMask;

    [Tooltip("발사 위치(없으면 이 오브젝트 Transform)")]
    [SerializeField] private Transform fireOrigin;

    [Header("레벨 규칙(요구사항 강제)")]
    [Tooltip("요구사항 레벨 규칙을 강제로 적용할지\n- ON: 1~6레벨 chain=레벨(최대6), 6~8레벨 공격력 보정 적용\n- OFF: JSON/인스펙터 값을 그대로 사용")]
    [SerializeField] private bool enforceDesignLevelRules = true;

    [Tooltip("6~8레벨 공격력 증가 배율(레벨당)\n예) 0.2면 6레벨 1.2배, 7레벨 1.4배, 8레벨 1.6배")]
    [SerializeField, Min(0f)] private float damageMulPerLevelAfter5 = 0.20f;

    [Header("JSON 밸런스(선택)")]
    [Tooltip("skill_balance.json에서 이 무기가 참조할 id\n(기본값: weapon_homing)")]
    [SerializeField] private string balanceId = "weapon_homing";

    [Tooltip("SkillBalanceService2D가 로드되어 있으면 수치를 덮어쓸지")]
    [SerializeField] private bool useSkillBalanceJson = true;

    [Header("디버그")]
    [SerializeField] private bool debugLog;

    private Transform _owner;
    private int _level = 1;
    private float _cooldownTimer;

    private ProjectilePool2D _pool;
    private PlayerCombatStats2D _stats;

    public int CurrentLevel => _level;

    private void Awake()
    {
        _pool = GetComponent<ProjectilePool2D>();
        if (fireOrigin == null) fireOrigin = transform;
    }

    private void Update()
    {
        if (_owner == null) return;

        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer > 0f) return;

        if (TryFire())
        {
            _cooldownTimer = ComputeCooldownSeconds();
        }
        else
        {
            // 타겟이 없으면 짧게 재시도
            _cooldownTimer = 0.10f;
        }
    }

    // 정상 메서드
    public void OnAttached(Transform owner)
    {
        _owner = owner;

        if (_stats == null && _owner != null)
            _stats = _owner.GetComponent<PlayerCombatStats2D>();

        if (fireOrigin == null) fireOrigin = transform;
        _cooldownTimer = 0f;
    }

    // ★ 네 프로젝트 ILevelableSkill에 오타 메서드가 같이 들어있어서 이걸도 구현해야 컴파일 됨
    void ILevelableSkill.OnAttaced(Transform newOwner)
    {
        OnAttached(newOwner);
    }

    public void ApplyLevel(int newLevel)
    {
        _level = Mathf.Max(1, newLevel);

        if (debugLog)
            Debug.Log($"[HomingMissileWeapon2D] ApplyLevel -> {_level}");
    }

    private bool TryFire()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("[HomingMissileWeapon2D] projectilePrefab이 비었습니다.");
            return false;
        }

        float seekRange = ComputeSeekRange();

        // EnemyRegistry2D 시그니처에 맞춤: (from, maxDistance, out result)
        if (!EnemyRegistry2D.TryGetNearest(fireOrigin.position, seekRange, out var member) || member == null)
            return false;

        Transform target = member.transform;
        if (target == null)
            return false;

        Vector3 spawnPos = fireOrigin.position;
        Vector2 dir = (Vector2)(target.position - spawnPos);
        if (dir.sqrMagnitude <= 0.0001f) dir = Vector2.right;
        dir.Normalize();

        // 풀 우선(ProjectilePool2D는 Get<T>() 제네릭이므로 타입을 명시해야 함)
        GameObject go = null;

        if (_pool != null)
        {
            // 풀 프리팹이 PooledObject2D 기반일 때만 정상 동작
            var pooled = _pool.Get<PooledObject2D>(spawnPos, Quaternion.identity);
            go = pooled != null ? pooled.gameObject : null;
        }

        if (go == null)
            go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        var proj = go.GetComponent<HomingMissileProjectile2D>();
        if (proj == null)
        {
            Debug.LogError("[HomingMissileWeapon2D] projectilePrefab에 HomingMissileProjectile2D가 없습니다.");
            Destroy(go);
            return false;
        }

        int finalDamage = ComputeDamage();
        int finalChain = ComputeChainCount();
        float finalSpeed = ComputeProjectileSpeed();
        float finalLife = ComputeLifeSeconds();

        proj.Init(enemyMask, seekRange, finalDamage, finalSpeed, turnSpeedDeg, finalChain, finalLife, dir, target);

        if (debugLog)
            Debug.Log($"[HomingMissileWeapon2D] Fire -> dmg={finalDamage}, chain={finalChain}, cd={ComputeCooldownSeconds():0.00}");

        return true;
    }

    private bool TryGetBalanceRow(out SkillBalanceDB2D.SkillRow2D row)
    {
        row = null;
        if (!useSkillBalanceJson) return false;
        if (!SkillBalanceService2D.IsLoaded) return false;
        if (string.IsNullOrWhiteSpace(balanceId)) return false;

        // SkillBalanceService2D는 static이고 Instance가 없음
        return SkillBalanceService2D.TryGet(balanceId, out row);
    }

    private float ComputeCooldownSeconds()
    {
        float cd = cooldownSeconds;
        int lv = Mathf.Max(1, _level);

        if (TryGetBalanceRow(out var row) && row != null)
        {
            if (row.cooldown >= 0f)
                cd = row.cooldown + row.cooldownAddPerLevel * (lv - 1);
        }

        if (_stats != null)
            cd *= Mathf.Max(0.05f, _stats.CooldownMul);

        return Mathf.Max(0.05f, cd);
    }

    private int ComputeDamage()
    {
        int lv = Mathf.Max(1, _level);
        float dmg = damage;

        if (TryGetBalanceRow(out var row) && row != null)
        {
            if (row.damage >= 0)
                dmg = row.damage + row.damageAddPerLevel * (lv - 1);
        }

        if (enforceDesignLevelRules && lv >= 6 && damageMulPerLevelAfter5 > 0f)
        {
            float extraMul = 1f + damageMulPerLevelAfter5 * (lv - 5);
            dmg *= extraMul;
        }

        if (_stats != null)
            dmg *= (_stats.DamageMul * _stats.ElementDamageMul);

        return Mathf.Max(1, Mathf.RoundToInt(dmg));
    }

    private int ComputeChainCount()
    {
        int lv = Mathf.Max(1, _level);

        // 요구사항 강제 규칙
        if (enforceDesignLevelRules)
            return Mathf.Clamp(lv, 1, 6);

        // JSON 사용 시
        if (TryGetBalanceRow(out var row) && row != null)
        {
            if (row.chainCount >= 0 || row.chainAddPerLevel != 0)
                return Mathf.Max(0, row.chainCount + row.chainAddPerLevel * (lv - 1));
        }

        // 기본값(강제 OFF일 때만 의미)
        return 0;
    }

    private float ComputeProjectileSpeed()
    {
        int lv = Mathf.Max(1, _level);
        float spd = projectileSpeed;

        if (TryGetBalanceRow(out var row) && row != null)
        {
            if (row.speed >= 0f)
                spd = row.speed + row.speedAddPerLevel * (lv - 1);
        }

        return Mathf.Max(0.1f, spd);
    }

    private float ComputeLifeSeconds()
    {
        int lv = Mathf.Max(1, _level);
        float life = projectileLifeSeconds;

        if (TryGetBalanceRow(out var row) && row != null)
        {
            if (row.life >= 0f)
                life = row.life + row.lifeAddPerLevel * (lv - 1);
        }

        return Mathf.Max(0.1f, life);
    }

    private float ComputeSeekRange()
    {
        // 네 SkillBalanceDB2D에는 radius 같은 필드가 없음.
        // 따라서 탐색 반경은 인스펙터 aimRange를 그대로 사용한다.
        return Mathf.Max(0.1f, aimRange);
    }
}