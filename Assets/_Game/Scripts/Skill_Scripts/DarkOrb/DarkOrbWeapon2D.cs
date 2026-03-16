// UTF-8
// Assets/_Game/Scripts/Skill_Scripts/DarkOrb/DarkOrbWeapon2D.cs
//
// [변경 요약]
// 기존: 직접 Instantiate → 이동/충돌/분열/VFX 전부 담당
// 변경: 발사만 담당 → GameProjectileManager.TrySpawnDarkOrb() 호출
//       이동/충돌/분열/VFX는 Manager가 처리

using UnityEngine;

[DisallowMultipleComponent]
public sealed class DarkOrbWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("타겟")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float aimRange = 25f;

    [Header("발사")]
    [SerializeField] private float cooldown = 1.2f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLifeSeconds = 0.8f;

    [Header("폭발")]
    [SerializeField] private float explosionRadius = 1.0f;
    [SerializeField] private float collisionRadius = 0.3f;

    [Header("데미지")]
    [SerializeField] private int baseExplosionDamage = 12;
    [SerializeField] private int bonusDamagePerLevelFrom5 = 6;

    [Header("분열 규칙")]
    [SerializeField] private bool useSimpleSplitRule = true;
    [SerializeField] private float splitAngleDeg = 40f;
    [SerializeField] private float splitSpeed = 10f;
    [SerializeField] private float splitLifeSeconds = 0.6f;
    [SerializeField] private int splitDamage = 0;

    [Header("렉 방지")]
    [SerializeField] private float collisionGracePeriod = 0.05f;

    [Header("표현")]
    [Range(0.1f, 1f)]
    [SerializeField] private float orbAlpha = 0.55f;

    private Transform _owner;
    private float _nextFireTime;
    private int _level;
    private PlayerCombatStats2D _stats;

    // 적 탐색용 버퍼 (GC 0)
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
    }

    public void ApplyLevel(int level)
    {
        _level = Mathf.Clamp(level, 1, 8);
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

    private void Update()
    {
        if (_level <= 0 || _owner == null) return;
        if (Time.time < _nextFireTime) return;

        // Manager 존재 체크
        if (GameProjectileManager.Instance == null) return;

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

    /// <summary>발사만 담당. 이동/충돌/분열은 Manager가 처리.</summary>
    private void Fire(Transform target)
    {
        // 데미지 계산
        float dmgF = Mathf.Max(1f, baseExplosionDamage);
        if (_level >= 5)
            dmgF += bonusDamagePerLevelFrom5 * (_level - 4);
        if (_stats != null)
            dmgF *= (_stats.DamageMul * _stats.ElementDamageMul);
        int dmg = Mathf.Max(1, Mathf.RoundToInt(dmgF));

        // 분열 규칙
        byte maxGen = 0;
        if (useSimpleSplitRule)
        {
            if (_level <= 1) maxGen = 0;
            else if (_level == 2) maxGen = 1;
            else if (_level == 3) maxGen = 2;
            else maxGen = 2; // 상한 (1→2→4 = 7개)
        }

        int childDmg = (splitDamage > 0) ? splitDamage : dmg;

        // Spec 작성 (DTO)
        var spec = new DarkOrbProjectileSpec
        {
            EnemyMask = enemyMask,
            Damage = dmg,
            Speed = projectileSpeed,
            Lifetime = Mathf.Max(0.2f, projectileLifeSeconds),
            ExplosionRadius = Mathf.Max(0.1f, explosionRadius),
            CollisionRadius = Mathf.Max(0.05f, collisionRadius),
            CollisionGracePeriod = collisionGracePeriod,
            MaxGeneration = maxGen,
            SplitChildrenCount = 2,
            SplitAngleDeg = splitAngleDeg,
            SplitSpeed = Mathf.Max(0.1f, splitSpeed),
            SplitLifetime = Mathf.Max(0.1f, splitLifeSeconds),
            SplitDamage = childDmg,
            OrbAlpha = orbAlpha,
        };

        // Manager에 발사 요청
        GameProjectileManager.Instance.TrySpawnDarkOrb(
            in spec,
            (Vector2)_owner.position,
            (Vector2)target.position,
            _owner.GetInstanceID());
    }

    /// <summary>가장 가까운 적 탐색. Physics2D + ContactFilter2D (GC 0).</summary>
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