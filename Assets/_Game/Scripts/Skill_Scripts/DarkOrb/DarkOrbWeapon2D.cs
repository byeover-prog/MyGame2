// ============================================================================
// DarkOrbWeapon2D.cs
// 경로: Assets/_Game/Scripts/Skill_Scripts/DarkOrb/DarkOrbWeapon2D.cs
// 용도: 암흑구 발사기. CentralProjectileManager에 발사 요청만 함.
//
// [v2 변경사항]
// - GameProjectileManager → CentralProjectileManager 전환
// - DarkOrbProjectileSpec → ProjectileSlot 템플릿 사용
// - VisualId = ProjectileVisualId.DarkOrb 지정
// - Inspector 필드/기존 스킬 시스템 호환: 변경 없음
//
// [기존 스킬 시스템 호환]
// ILevelableSkill 인터페이스 구현:
//   OnAttached(Transform owner) → 플레이어 참조 획득
//   OnAttaced(Transform owner) → 오타 호환
//   ApplyLevel(int level) → 레벨 적용
// ============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DarkOrbWeapon2D : MonoBehaviour, ILevelableSkill
{
    // ── Inspector (기존과 완전 동일, 변경 없음) ──
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
    // ILevelableSkill (기존과 동일)
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
        GameLogger.Log($"[DarkOrbWeapon2D] OnAttached owner={owner?.name}");
    }

    public void ApplyLevel(int level)
    {
        _level = Mathf.Clamp(level, 1, 8);
        GameLogger.Log($"[DarkOrbWeapon2D] ApplyLevel → Lv.{_level}");
    }

    // ══════════════════════════════════════════════════════════════
    // Update (자동 발사)
    // ══════════════════════════════════════════════════════════════

    private void Update()
    {
        if (_level <= 0 || _owner == null) return;

        // ★ v2: CentralProjectileManager 사용
        if (CentralProjectileManager.Instance == null) return;

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
    // ★ v2: Fire() → CentralProjectileManager.Spawn()
    // ══════════════════════════════════════════════════════════════

    private void Fire(Transform target)
    {
        EnsureFilter();

        // 데미지 (기존과 동일)
        float dmgF = Mathf.Max(1f, baseExplosionDamage);
        if (_level >= 5)
            dmgF += bonusDamagePerLevelFrom5 * (_level - 4);
        if (_stats != null)
            dmgF *= (_stats.DamageMul * _stats.ElementDamageMul);

        // 분열 깊이 (기존과 동일): Lv1=0, Lv2=1, Lv3=2, Lv4+=3
        int maxGen = 0;
        if (_level <= 1) maxGen = 0;
        else if (_level == 2) maxGen = 1;
        else if (_level == 3) maxGen = 2;
        else maxGen = 3;

        // 방향 (기존과 동일)
        Vector2 myPos = (Vector2)_owner.position;
        Vector2 dir = ((Vector2)target.position - myPos).normalized;
        if (dir.sqrMagnitude < 0.001f) return;

        // ★ v2: ProjectileSlot 템플릿 구성
        var template = new ProjectileSlot
        {
            // 종류: 수명 만료 시 폭발 + depth 기반 분열
            MoveKind        = ProjectileMoveKind.SplitOnExpiry,
            HitKind         = ProjectileHitKind.AreaOnExpiry,
            Element         = DamageElement2D.Dark,

            // 운동
            Position        = myPos,
            Direction       = dir,
            Speed           = projectileSpeed,
            Lifetime        = projectileLifeSeconds,

            // 전투
            Damage          = Mathf.Max(1, Mathf.RoundToInt(dmgF)),
            HitRadius       = 0.3f,
            ExplosionRadius = explosionRadius,
            EnemyMask       = enemyMask,

            // 분열
            Generation      = 0,
            MaxGeneration   = maxGen,
            SplitAngleDeg   = splitAngleDeg,
            SplitSpeed      = splitSpeed,
            SplitLifetime   = splitLifeSeconds,

            // 뷰
            VisualId        = ProjectileVisualId.DarkOrb,
            ViewId          = -1,
            VfxViewId       = -1,
        };

        CentralProjectileManager.Instance.Spawn(ref template);
    }

    // ══════════════════════════════════════════════════════════════
    // 적 탐색 (기존과 동일)
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