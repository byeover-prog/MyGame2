// UTF-8
using System;
using System.Collections;
using UnityEngine;

public abstract class CommonSkillWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("공통")]
    [SerializeField] protected CommonSkillConfigSO config;
    [SerializeField] protected LayerMask enemyMask;
    [SerializeField] protected bool requireTargetToFire = true;

    [Header("밸런스(JSON 오버라이드)")]
    [Tooltip("JSON에서 찾을 스킬 ID.\n비워두면 기본값으로 'weapon_' + Kind(소문자) 를 사용합니다.\n예) weapon_darkorb, weapon_boomerang")]
    [SerializeField] private string balanceIdOverride;

    [Header("겹침 방지(추천)")]
    [Tooltip("발사 시작 위치를 플레이어 중심에서 살짝 분산한다(유닛). 0이면 분산 없음.")]
    [Min(0f)]
    [SerializeField] private float spawnOffsetRadius = 0.25f;

    [Tooltip("발사 타이밍을 살짝 흔들어서(초) 동시에 나가도 겹쳐 보이지 않게 한다.")]
    [Min(0f)]
    [SerializeField] private float fireJitterDelayMin = 0.05f;

    [Min(0f)]
    [SerializeField] private float fireJitterDelayMax = 0.10f;

    [Header("겹침 방지(정렬)")]
    [Tooltip("투사체 SpriteRenderer sortingOrder 기본값. 무기 종류별로 여기에 step이 더해진다.")]
    [SerializeField] private int projectileSortingBase = 0;

    [Tooltip("무기 종류(CommonSkillKind)마다 sortingOrder를 얼마나 벌릴지.")]
    [Min(0)]
    [SerializeField] private int projectileSortingStepPerKind = 10;

    [Header("스탯(폴백: config/JSON이 비었을 때만 사용)")]
    [Tooltip("config/JSON에서 쿨다운을 못 얻을 때 쓰는 임시 쿨다운(초)")]
    [Min(0.01f)]
    [SerializeField] private float fallbackCooldownSeconds = 1.0f;

    [Tooltip("config/JSON에서 데미지를 못 얻을 때 쓰는 임시 데미지")]
    [Min(0)]
    [SerializeField] private int fallbackDamage = 10;

    [Tooltip("config/JSON에서 투사체 속도를 못 얻을 때 쓰는 임시 속도")]
    [Min(0.01f)]
    [SerializeField] private float fallbackProjectileSpeed = 12f;

    [Tooltip("config/JSON에서 투사체 수명을 못 얻을 때 쓰는 임시 수명(초)")]
    [Min(0.01f)]
    [SerializeField] private float fallbackProjectileLife = 2.0f;

    protected Transform owner;
    protected int level = 1;
    protected float cooldownTimer;

    private bool _firePending;

    // 런타임 파라미터(= SO 기본값 + JSON 덮어쓰기 + 레벨증가치 반영)
    private CommonSkillLevelParams _runtimeP;

    // 마지막으로 적용된 JSON Row(무기 전용 필드 적용용)
    private SkillBalanceDB2D.SkillRow2D _lastBalanceRow;

    public CommonSkillKind Kind => config != null ? config.kind : 0;
    public int Level => level;

    // 무기들은 이제 P를 통해 "최종 적용 값"을 받는다.
    protected CommonSkillLevelParams P => _runtimeP;

    // --------------------------------------------------------------------
    // [핵심] 무기들이 앞으로 공통으로 쓰는 “스탯 Getter(폴백 포함)”
    // - 주의: 폴백은 config/JSON 둘 다 못 구했을 때만 쓴다.
    // --------------------------------------------------------------------
    private bool HasAnySourceStats => config != null || _lastBalanceRow != null;

    protected int StatLevel => Mathf.Max(1, level);

    protected float StatCooldownSeconds
    {
        get
        {
            if (HasAnySourceStats) return Mathf.Max(0.01f, P.cooldown);
            return Mathf.Max(0.01f, fallbackCooldownSeconds);
        }
    }

    protected int StatDamage
    {
        get
        {
            if (HasAnySourceStats) return Mathf.Max(0, P.damage);
            return Mathf.Max(0, fallbackDamage);
        }
    }

    protected float StatProjectileSpeed
    {
        get
        {
            if (HasAnySourceStats) return Mathf.Max(0f, P.projectileSpeed);
            return Mathf.Max(0f, fallbackProjectileSpeed);
        }
    }

    protected float StatProjectileLife
    {
        get
        {
            if (HasAnySourceStats) return Mathf.Max(0.05f, P.lifeSeconds);
            return Mathf.Max(0.05f, fallbackProjectileLife);
        }
    }

    protected int StatProjectileCount
    {
        get
        {
            if (HasAnySourceStats) return Mathf.Max(1, P.projectileCount);
            return 1;
        }
    }

    public virtual void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        config = cfg;
        owner = ownerTr;
        level = Mathf.Max(1, startLevel);
        cooldownTimer = 0f;
        _firePending = false;

        RefreshRuntimeParams();
        OnLevelChanged();
    }

    public void SetOwner(Transform ownerTr)
    {
        owner = ownerTr;
    }

    public void SetLevel(int newLevel)
    {
        int lv = Mathf.Max(1, newLevel);
        if (lv == level) return;

        level = lv;
        RefreshRuntimeParams();
        OnLevelChanged();
    }

    private void RefreshRuntimeParams()
    {
        _lastBalanceRow = null;

        // 1) SO 기본값
        _runtimeP = (config != null) ? config.GetLevelParams(level) : default;

        // 2) JSON 오버라이드
        string id = GetBalanceId();
        if (!string.IsNullOrEmpty(id) && SkillBalanceService2D.IsLoaded)
        {
            if (SkillBalanceService2D.TryGet(id, out var row))
            {
                _lastBalanceRow = row;
                ApplyBalanceRow(row, level, ref _runtimeP);
            }
        }
    }

    private static void ApplyBalanceRow(SkillBalanceDB2D.SkillRow2D row, int level, ref CommonSkillLevelParams p)
    {
        int lvMinus1 = Mathf.Max(0, level - 1);

        if (row.HasDamage()) p.damage = row.damage;
        if (row.damageAddPerLevel != 0) p.damage = Mathf.Max(0, p.damage + row.damageAddPerLevel * lvMinus1);

        if (row.HasCooldown()) p.cooldown = row.cooldown;
        if (row.cooldownAddPerLevel != 0f) p.cooldown = Mathf.Max(0.01f, p.cooldown + row.cooldownAddPerLevel * lvMinus1);

        if (row.HasSpeed()) p.projectileSpeed = row.speed;
        if (row.speedAddPerLevel != 0f) p.projectileSpeed = Mathf.Max(0f, p.projectileSpeed + row.speedAddPerLevel * lvMinus1);

        if (row.HasLife()) p.lifeSeconds = row.life;
        if (row.lifeAddPerLevel != 0f) p.lifeSeconds = Mathf.Max(0.05f, p.lifeSeconds + row.lifeAddPerLevel * lvMinus1);

        if (row.HasCount()) p.projectileCount = row.count;
        if (row.countAddPerLevel != 0) p.projectileCount = Mathf.Max(1, p.projectileCount + row.countAddPerLevel * lvMinus1);

        if (row.HasBounceCount()) p.bounceCount = row.bounceCount;
        if (row.bounceAddPerLevel != 0) p.bounceCount = Mathf.Max(0, p.bounceCount + row.bounceAddPerLevel * lvMinus1);

        if (row.HasChainCount()) p.chainCount = row.chainCount;
        if (row.chainAddPerLevel != 0) p.chainCount = Mathf.Max(0, p.chainCount + row.chainAddPerLevel * lvMinus1);

        if (row.HasSplitCount()) p.splitCount = row.splitCount;
        if (row.splitAddPerLevel != 0) p.splitCount = Mathf.Max(0, p.splitCount + row.splitAddPerLevel * lvMinus1);

        if (row.HasExplosionRadius()) p.explosionRadius = row.explosionRadius;
        if (row.explosionRadiusAddPerLevel != 0f) p.explosionRadius = Mathf.Max(0.01f, p.explosionRadius + row.explosionRadiusAddPerLevel * lvMinus1);

        if (row.HasChildSpeed()) p.childSpeed = row.childSpeed;
        if (row.childSpeedAddPerLevel != 0f) p.childSpeed = Mathf.Max(0f, p.childSpeed + row.childSpeedAddPerLevel * lvMinus1);

        if (row.HasHitInterval()) p.hitInterval = row.hitInterval;
        if (row.hitIntervalAddPerLevel != 0f) p.hitInterval = Mathf.Max(0.01f, p.hitInterval + row.hitIntervalAddPerLevel * lvMinus1);

        if (row.HasOrbitRadius()) p.orbitRadius = row.orbitRadius;
        if (row.orbitRadiusAddPerLevel != 0f) p.orbitRadius = Mathf.Max(0f, p.orbitRadius + row.orbitRadiusAddPerLevel * lvMinus1);

        if (row.HasOrbitSpeed()) p.orbitAngularSpeed = row.orbitSpeed;
        if (row.orbitSpeedAddPerLevel != 0f) p.orbitAngularSpeed = p.orbitAngularSpeed + row.orbitSpeedAddPerLevel * lvMinus1;
    }

    protected bool TryGetBalanceRow(out SkillBalanceDB2D.SkillRow2D row)
    {
        row = _lastBalanceRow;
        return row != null;
    }

    protected virtual string GetBalanceId()
    {
        if (!string.IsNullOrEmpty(balanceIdOverride))
            return balanceIdOverride.Trim();

        string kindName = Kind.ToString();
        if (string.IsNullOrEmpty(kindName)) return null;

        return "weapon_" + kindName.ToLowerInvariant();
    }

    protected bool TryGetNearest(out EnemyRegistryMember2D enemy)
    {
        if (owner == null) { enemy = null; return false; }
        return EnemyRegistry2D.TryGetNearest(owner.position, out enemy);
    }

    protected bool TryGetFarthest(out EnemyRegistryMember2D enemy)
    {
        if (owner == null) { enemy = null; return false; }
        return EnemyRegistry2D.TryGetFarthest(owner.position, out enemy);
    }

    protected virtual void OnLevelChanged() { }

    protected Vector2 GetSpawnOrigin(Transform spawnPointOrNull)
    {
        Vector2 origin = owner != null ? (Vector2)owner.position : (Vector2)transform.position;
        if (spawnPointOrNull != null) origin = spawnPointOrNull.position;

        float r = Mathf.Max(0f, spawnOffsetRadius);
        if (r <= 0f) return origin;

        Vector2 offset = UnityEngine.Random.insideUnitCircle * r;
        return origin + offset;
    }

    protected float GetFireJitterDelay()
    {
        float a = Mathf.Max(0f, fireJitterDelayMin);
        float b = Mathf.Max(0f, fireJitterDelayMax);
        if (b < a) b = a;
        if (b <= 0f) return 0f;
        return UnityEngine.Random.Range(a, b);
    }

    protected int GetProjectileSortingOrder()
    {
        int step = Mathf.Max(0, projectileSortingStepPerKind);
        return projectileSortingBase + ((int)Kind * step);
    }

    protected void ApplyProjectileSorting(GameObject projectileRoot)
    {
        if (projectileRoot == null) return;

        int order = GetProjectileSortingOrder();
        var srs = projectileRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < srs.Length; i++)
        {
            if (srs[i] == null) continue;
            srs[i].sortingOrder = order;
        }
    }

    protected bool TryBeginFire(Action fireAction)
    {
        return TryBeginFireConsumeCooldown(fireAction);
    }

    protected bool TryBeginFireConsumeCooldown(Action fireAction)
    {
        if (fireAction == null) return false;
        if (_firePending) return false;

        float delay = GetFireJitterDelay();
        if (delay <= 0f)
        {
            fireAction.Invoke();
            ConsumeCooldown();
            return true;
        }

        _firePending = true;
        StartCoroutine(FireDelayed(delay, fireAction));
        return true;
    }

    private IEnumerator FireDelayed(float delay, Action fireAction)
    {
        yield return new WaitForSeconds(delay);

        _firePending = false;

        if (this == null || !isActiveAndEnabled) yield break;
        fireAction.Invoke();
        ConsumeCooldown();
    }

    private void ConsumeCooldown()
    {
        // P.cooldown이 0이거나 config가 비어도, 폴백으로 안전하게 쿨다운이 돈다.
        cooldownTimer = StatCooldownSeconds;
    }

    // ILevelableSkill이 요구하는 오타 메서드 호환용(인터페이스가 OnAttaced를 요구하면 이걸로 통과)
    public void OnAttaced(Transform newOwner) => OnAttached(newOwner);

    public void OnAttached(Transform newOwner)
    {
        if (config == null)
            Debug.LogWarning($"[CommonSkillWeapon2D] config가 비어있습니다: {name}", this);

        Initialize(config, newOwner, 1);
    }

    public void ApplyLevel(int newLevel)
    {
        if (newLevel <= 0) return;
        SetLevel(newLevel);
    }
}