using UnityEngine;

/// <summary>
/// 암흑구 무기 발사기. DarkOrbProjectile2D만 발사한다.
/// 
/// [최적화]
/// - maxActiveOrbs: 동시 활성 DarkOrb 제한 (분열 트리 겹침 방지)
/// - Physics2D.OverlapCircle + ContactFilter2D (Unity 6, GC 0)
/// - Prewarm으로 첫 발사 시 Instantiate 0
/// </summary>
[DisallowMultipleComponent]
public sealed class DarkOrbWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("투사체(메인 오브) 프리팹")]
    [SerializeField] private DarkOrbProjectile2D projectilePrefab;

    [Header("분열(옵션)")]
    [SerializeField] private ProjectilePool2D splitPool;

    [Header("타겟")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float aimRange = 25f;

    [Header("발사")]
    [SerializeField] private float cooldown = 1.2f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLifeSeconds = 0.8f;

    [Header("폭발")]
    [SerializeField] private float explosionRadius = 1.0f;

    [Header("데미지")]
    [SerializeField] private int baseExplosionDamage = 12;
    [SerializeField] private int bonusDamagePerLevelFrom5 = 6;

    [Header("분열 규칙")]
    [SerializeField] private bool useSimpleSplitRule = true;
    [SerializeField] private float splitSpeed = 10f;
    [SerializeField] private float splitLifeSeconds = 0.6f;
    [SerializeField] private int splitDamage = 0;

    [Header("표현")]
    [Range(0.1f, 1f)]
    [SerializeField] private float orbAlpha = 0.55f;

    [Header("성능 제한")]
    [Tooltip("동시에 존재할 수 있는 DarkOrb 최대 수 (Root + 자식 포함).\n0이면 제한 없음.")]
    [SerializeField] private int maxActiveOrbs = 30;

    [Tooltip("게임 시작 시 미리 생성할 투사체 수")]
    [SerializeField] private int prewarmCount = 16;

    [Header("디버그")]
    [SerializeField] private bool log = false;

    private Transform _owner;
    private float _nextFireTime;
    private int _level;
    private PlayerCombatStats2D _stats;

    private readonly Collider2D[] _enemyHits = new Collider2D[64];
    private ContactFilter2D _enemyFilter;
    private bool _filterReady;

    public void OnAttaced(Transform owner) => OnAttached(owner);

    public void OnAttached(Transform owner)
    {
        _owner = owner;
        if (_owner != null)
        {
            _stats = _owner.GetComponent<PlayerCombatStats2D>();
            if (_stats == null) _stats = _owner.GetComponentInParent<PlayerCombatStats2D>();
        }

        if (projectilePrefab != null && prewarmCount > 0)
            DarkOrbProjectile2D.Prewarm(projectilePrefab, prewarmCount);

#if UNITY_EDITOR
        if (log) Debug.Log($"[DarkOrbWeapon2D] OnAttached owner={owner?.name}, prewarm={prewarmCount}", this);
#endif
    }

    public void ApplyLevel(int level)
    {
        _level = Mathf.Clamp(level, 1, 8);
#if UNITY_EDITOR
        if (log) Debug.Log($"[DarkOrbWeapon2D] ApplyLevel => Lv.{_level}", this);
#endif
    }

    private void EnsureFilter()
    {
        if (_filterReady) return;

        if (enemyMask.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) enemyMask = LayerMask.GetMask("Enemy");
        }

        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(enemyMask);
        _enemyFilter.useTriggers = true;
        _filterReady = true;
    }

    private void Update()
    {
        if (_level <= 0 || _owner == null) return;
        if (Time.time < _nextFireTime) return;

        if (projectilePrefab == null)
        {
            _nextFireTime = Time.time + 0.5f;
            return;
        }

        // ★ [지시문 §7] 동시 활성 제한
        if (maxActiveOrbs > 0 && DarkOrbProjectile2D.ActiveCount >= maxActiveOrbs)
        {
            _nextFireTime = Time.time + 0.1f;
            return;
        }

        Transform t = FindNearestEnemy();
        if (t == null)
        {
            _nextFireTime = Time.time + 0.05f;
            return;
        }

        Fire(t);

        float finalCd = cooldown;
        if (_stats != null)
            finalCd *= Mathf.Max(0.05f, _stats.CooldownMul);
        _nextFireTime = Time.time + Mathf.Max(0.05f, finalCd);
    }

    private void Fire(Transform target)
    {
        Vector2 dir = (Vector2)(target.position - _owner.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        float dmgF = Mathf.Max(1f, baseExplosionDamage);
        if (_level >= 5)
            dmgF += bonusDamagePerLevelFrom5 * (_level - 4);

        if (_stats != null)
            dmgF *= (_stats.DamageMul * _stats.ElementDamageMul);

        int dmg = Mathf.Max(1, Mathf.RoundToInt(dmgF));

        EnsureFilter();

        int splitCount = 0;
        if (useSimpleSplitRule)
        {
            if (_level <= 1) splitCount = 0;
            else if (_level == 2) splitCount = 2;
            else if (_level == 3) splitCount = 4;
            else splitCount = 8;
        }

        int childDmg = (splitDamage > 0) ? splitDamage : dmg;

        var proj = DarkOrbProjectile2D.Spawn(projectilePrefab, _owner.position);
        if (proj == null) return;

        proj.Init(
            enemyMask, dmg, projectileSpeed,
            Mathf.Max(0.2f, projectileLifeSeconds), dir,
            Mathf.Max(0.1f, explosionRadius),
            Mathf.Max(0, splitCount),
            Mathf.Max(0.1f, splitSpeed),
            Mathf.Max(0.1f, splitLifeSeconds),
            Mathf.Max(0, childDmg),
            splitPool, orbAlpha);

#if UNITY_EDITOR
        if (log) Debug.Log($"[DarkOrbWeapon2D] Fire dmg={dmg} split={splitCount} active={DarkOrbProjectile2D.ActiveCount}", this);
#endif
    }

    private Transform FindNearestEnemy()
    {
        EnsureFilter();

        int count = Physics2D.OverlapCircle(
            _owner.position, aimRange, _enemyFilter, _enemyHits);

        if (count == 0) return null;

        float best = float.PositiveInfinity;
        Transform bestT = null;
        Vector2 o = _owner.position;

        for (int i = 0; i < count; i++)
        {
            var h = _enemyHits[i];
            if (h == null) continue;
            float d = ((Vector2)h.transform.position - o).sqrMagnitude;
            if (d < best) { best = d; bestT = h.transform; }
        }

        return bestT;
    }
}