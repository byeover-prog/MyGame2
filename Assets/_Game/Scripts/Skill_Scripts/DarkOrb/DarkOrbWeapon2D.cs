// ============================================================================
// DarkOrbWeapon2D.cs
// 경로: Assets/_Game/Scripts/Skill_Scripts/DarkOrb/DarkOrbWeapon2D.cs
// 용도: 암흑구 발사기. Manager에 발사 요청만 함.
//
// [기존 스킬 시스템 호환]
// ILevelableSkill 인터페이스 구현:
//   OnAttached(Transform owner) → 플레이어 참조 획득
//   OnAttaced(Transform owner) → 오타 호환
//   ApplyLevel(int level) → 레벨 적용
//
// [설계도 기준]
// - Instantiate 직접 호출 금지. Manager.TrySpawnDarkOrb() 위임만.
// ============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DarkOrbWeapon2D : MonoBehaviour, ILevelableSkill
{
    // ── Inspector ──
    [Header("타겟")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float aimRange = 25f;

    [Header("발사")]
    [SerializeField] private float cooldown = 3f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLifeSeconds = 0.8f;

    [Header("폭발")]
    [SerializeField] private float explosionRadius = 1.0f;

    [Header("데미지")]
    [Tooltip("Lv1~4 기본 폭발 데미지")]
    [SerializeField] private int baseExplosionDamage = 12;
    [Tooltip("Lv5부터 레벨당 추가 데미지")]
    [SerializeField] private int bonusDamagePerLevelFrom5 = 6;

    [Header("분열")]
    [Range(1f, 89f)]
    [SerializeField] private float splitAngleDeg = 40f;
    [SerializeField] private float splitSpeed = 10f;
    [SerializeField] private float splitLifeSeconds = 0.6f;

    [Header("비주얼")]
    [Range(0.1f, 1f)]
    [SerializeField] private float orbAlpha = 0.55f;

    // ── 런타임 ──
    private Transform _owner;
    private int       _level;
    private float     _nextFireTime;
    private PlayerCombatStats2D _stats;

    private readonly Collider2D[] _enemyHits = new Collider2D[64];
    private ContactFilter2D _enemyFilter;
    private bool _filterReady;

    // ══════════════════════════════════════════════════════════════
    // ILevelableSkill (기존 스킬 시스템 호환)
    // ══════════════════════════════════════════════════════════════

    public void OnAttaced(Transform owner) => OnAttached(owner);

    public void OnAttached(Transform owner)
    {
        _owner = owner;
        if (_owner != null)
        {
            _stats = _owner.GetComponent<PlayerCombatStats2D>();
            if (_stats == null)
                _stats = _owner.GetComponentInParent<PlayerCombatStats2D>();
        }
        Debug.Log($"<color=green>[DarkOrbWeapon2D] ★★★ 설계도 기준 OnAttached ★★★ owner={owner?.name}</color>", this);
    }

    public void ApplyLevel(int level)
    {
        _level = Mathf.Clamp(level, 1, 8);
        Debug.Log($"[DarkOrbWeapon2D] ApplyLevel → Lv.{_level}", this);
    }

    // ══════════════════════════════════════════════════════════════
    // Update (자동 발사)
    // ══════════════════════════════════════════════════════════════

    private void Update()
    {
        if (_level <= 0 || _owner == null) return;
        if (GameProjectileManager.Instance == null) return;
        if (Time.time < _nextFireTime) return;

        Transform target = FindNearestEnemy();
        if (target == null)
        {
            _nextFireTime = Time.time + 0.05f;
            return;
        }

        Fire(target);

        float finalCd = cooldown;
        if (_stats != null)
            finalCd *= Mathf.Max(0.05f, _stats.CooldownMul);
        _nextFireTime = Time.time + Mathf.Max(0.05f, finalCd);
    }

    // ══════════════════════════════════════════════════════════════
    // 발사 → Manager 위임
    // ══════════════════════════════════════════════════════════════

    private void Fire(Transform target)
    {
        // ★ enemyMask가 비어있으면 자동 보정 (EnsureFilter에서 설정됨)
        EnsureFilter();

        // 데미지
        float dmgF = Mathf.Max(1f, baseExplosionDamage);
        if (_level >= 5)
            dmgF += bonusDamagePerLevelFrom5 * (_level - 4);
        if (_stats != null)
            dmgF *= (_stats.DamageMul * _stats.ElementDamageMul);

        // 분열 깊이: Lv1=0, Lv2=1, Lv3=2, Lv4+=3
        int maxGen = 0;
        if (_level <= 1) maxGen = 0;
        else if (_level == 2) maxGen = 1;
        else if (_level == 3) maxGen = 2;
        else maxGen = 3;

        // 방향
        Vector2 myPos = (Vector2)_owner.position;
        Vector2 dir = ((Vector2)target.position - myPos).normalized;
        if (dir.sqrMagnitude < 0.001f) return;

        // Manager에 위임
        var spec = new DarkOrbProjectileSpec
        {
            SpawnPosition   = myPos,
            Direction       = dir,
            Speed           = projectileSpeed,
            Lifetime        = projectileLifeSeconds,
            ExplosionRadius = explosionRadius,
            ExplosionDamage = Mathf.Max(1f, dmgF),
            EnemyMask       = enemyMask,
            MaxGeneration   = maxGen,
            SplitAngleDeg   = splitAngleDeg,
            SplitSpeed      = splitSpeed,
            SplitLifetime   = splitLifeSeconds,
            OrbAlpha        = orbAlpha
        };

        GameProjectileManager.Instance.TrySpawnDarkOrb(spec);
    }

    // ══════════════════════════════════════════════════════════════
    // 적 탐색
    // ══════════════════════════════════════════════════════════════

    private Transform FindNearestEnemy()
    {
        EnsureFilter();
        Vector2 myPos = (Vector2)_owner.position;
        int count = Physics2D.OverlapCircle(myPos, aimRange, _enemyFilter, _enemyHits);
        if (count == 0) return null;

        Transform nearest = null;
        float minDistSq = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (_enemyHits[i] == null) continue;
            float distSq = ((Vector2)_enemyHits[i].transform.position - myPos).sqrMagnitude;
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                nearest = _enemyHits[i].transform;
            }
        }
        return nearest;
    }

    private void EnsureFilter()
    {
        if (_filterReady) return;
        if (enemyMask.value == 0)
        {
            int layer = LayerMask.NameToLayer("Enemy");
            if (layer >= 0) enemyMask = LayerMask.GetMask("Enemy");
        }
        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(enemyMask);
        _enemyFilter.useTriggers = true;
        _filterReady = true;
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, aimRange);
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
    #endif
}