// UTF-8
// [구현 원리 요약]
// - 호밍 미사일은 발사 시점에 가장 먼 적을 골라 원거리 위협 제거 역할을 맡는다.
// - 발사체 생성은 기존 구조를 유지하고 타겟 선택만 레지스트리 기반으로 경량화한다.
using UnityEngine;

/// <summary>
/// 정화구(호밍 미사일) 스킬 무기.
/// 가장 먼 적을 향해 유도 미사일을 발사한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class HomingMissileWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("발사")]
    [Tooltip("호밍 미사일 프리팹 (HomingMissileProjectile2D 포함)")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("미사일 이동 속도")]
    [SerializeField] private float projectileSpeed = 10f;

    [Tooltip("재사용 대기시간 (초)")]
    [SerializeField] private float cooldownSeconds = 1.8f;

    [Tooltip("타겟 탐색/추적 반경")]
    [SerializeField] private float aimRange = 18f;

    [Tooltip("초당 회전 속도 (도)")]
    [SerializeField] private float turnSpeedDeg = 540f;

    [Tooltip("기본 공격력")]
    [SerializeField] private int damage = 4;

    [Tooltip("미사일 최대 생존 시간 (초)")]
    [SerializeField] private float projectileLifeSeconds = 3.0f;

    [Tooltip("적 레이어 마스크")]
    [SerializeField] private LayerMask enemyMask;

    [Tooltip("발사 위치 (비우면 이 오브젝트 Transform)")]
    [SerializeField] private Transform fireOrigin;

    [Header("레벨 규칙")]
    [Tooltip("기획서 레벨 규칙 강제 적용 여부")]
    [SerializeField] private bool enforceDesignLevelRules = true;

    [Tooltip("레벨 5 이후 레벨당 피해량 배율 증가")]
    [SerializeField, Min(0f)] private float damageMulPerLevelAfter5 = 0.20f;

    [Header("JSON 밸런스")]
    [Tooltip("밸런스 테이블 ID")]
    [SerializeField] private string balanceId = "weapon_homing";

    [Tooltip("JSON 밸런스 테이블 사용 여부")]
    [SerializeField] private bool useSkillBalanceJson = true;

    [Header("디버그")]
    [Tooltip("발사 로그 출력")]
    [SerializeField] private bool debugLog;

    private Transform _owner;
    private int _level = 1;
    private float _cooldownTimer;

    private ProjectilePool2D _pool;
    private PlayerCombatStats2D _stats;

    /// <summary>현재 스킬 레벨</summary>
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
            _cooldownTimer = ComputeCooldownSeconds();
        else
            _cooldownTimer = 0.10f;
    }

    /// <summary>소유자(플레이어) 연결</summary>
    public void OnAttached(Transform owner)
    {
        _owner = owner;

        if (_stats == null && _owner != null)
            _stats = _owner.GetComponent<PlayerCombatStats2D>();

        if (fireOrigin == null) fireOrigin = transform;
        _cooldownTimer = 0f;
    }

    void ILevelableSkill.OnAttaced(Transform newOwner)
    {
        OnAttached(newOwner);
    }

    /// <summary>스킬 레벨 적용</summary>
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
            Debug.LogError("[HomingMissileWeapon2D] projectilePrefab이 비었습니다.", this);
            return false;
        }

        float seekRange = ComputeSeekRange();

        if (!EnemyRegistry2D.TryGetFarthest(fireOrigin.position, seekRange, out var member) || member == null)
            return false;

        Transform target = member.Transform;
        if (target == null)
            return false;

        Vector3 spawnPos = fireOrigin.position;
        Vector2 dir = (Vector2)(target.position - spawnPos);
        if (dir.sqrMagnitude <= 0.0001f) dir = Vector2.right;
        dir.Normalize();

        GameObject go = null;

        if (_pool != null)
        {
            var pooled = _pool.Get<PooledObject2D>(spawnPos, Quaternion.identity);
            go = pooled != null ? pooled.gameObject : null;
        }

        if (go == null)
            go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        var proj = go.GetComponent<HomingMissileProjectile2D>();
        if (proj == null)
        {
            Debug.LogError("[HomingMissileWeapon2D] projectilePrefab에 HomingMissileProjectile2D가 없습니다.", this);
            Destroy(go);
            return false;
        }

        int finalDamage = ComputeDamage();
        int finalChain = ComputeChainCount();
        float finalSpeed = ComputeProjectileSpeed();
        float finalLife = ComputeLifeSeconds();

        proj.Init(enemyMask, seekRange, finalDamage, finalSpeed, turnSpeedDeg, finalChain, finalLife, dir, target);

        if (debugLog)
            Debug.Log($"[HomingMissileWeapon2D] 발사 -> 피해량={finalDamage}, 타격횟수={finalChain}, 쿨타임={ComputeCooldownSeconds():0.00}");

        return true;
    }

    private bool TryGetBalanceRow(out SkillBalanceDB2D.SkillRow2D row)
    {
        row = null;
        if (!useSkillBalanceJson) return false;
        if (!SkillBalanceService2D.IsLoaded) return false;
        if (string.IsNullOrWhiteSpace(balanceId)) return false;
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

        // 기획서: 레벨당 수명 타격 횟수 1 증가
        if (enforceDesignLevelRules)
            return Mathf.Clamp(lv, 1, 6);

        if (TryGetBalanceRow(out var row) && row != null)
        {
            if (row.chainCount >= 0 || row.chainAddPerLevel != 0)
                return Mathf.Max(0, row.chainCount + row.chainAddPerLevel * (lv - 1));
        }

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

        return Mathf.Max(0.2f, life);
    }

    private float ComputeSeekRange()
    {
        float range = Mathf.Max(0.5f, aimRange);
        if (_stats != null)
            range *= Mathf.Max(0.1f, _stats.AreaMul);
        return range;
    }
}
