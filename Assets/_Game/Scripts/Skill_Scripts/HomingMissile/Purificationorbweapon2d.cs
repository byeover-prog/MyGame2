// UTF-8
// ============================================================================
// PurificationOrbWeapon2D.cs
// 경로: Assets/_Game/Scripts/Skill_Scripts/HomingMissile/PurificationOrbWeapon2D.cs
//
// [구현 원리]
// MonoBehaviour + ILevelableSkill 패턴 (DarkOrbWeapon2D와 동일).
// CommonSkillWeapon2D를 상속하지 않는 이유:
//   정화구는 "발사 → 부착 → 지속 피해" 라는 독자적인 사이클이라
//   베이스 클래스의 TryBeginFire/cooldownTimer 흐름과 맞지 않음.
//
// [레벨 테이블] (기획서 확정)
//  Lv │ 틱 데미지 │ 틱 횟수 │ 총 데미지 │ 부착 시간
//  ───┼──────────┼────────┼──────────┼──────────
//   1 │   5.0    │   5    │   25.0   │  2.5초
//   2 │   7.5    │   5    │   37.5   │  2.5초
//   3 │  10.0    │   5    │   50.0   │  2.5초
//   4 │  10.0    │   6    │   60.0   │  3.0초
//   5 │  10.0    │   7    │   70.0   │  3.5초
//   6 │  10.0    │   8    │   80.0   │  4.0초
//   7 │  10.0    │   9    │   90.0   │  4.5초
//   8 │  10.0    │  10    │  100.0   │  5.0초
//
// [기존 시스템 연동]
// CommonSkillManager2D가 무기 프리팹을 생성하면:
//   1. OnAttached(Transform owner) 호출 → 플레이어 참조 획득
//   2. ApplyLevel(int level) 호출 → 레벨 적용
//   3. 이후 Update()에서 자동 발사
//
// [Inspector 설정] — Weapon_PurificationOrb 프리팹
//   Pool               → ProjectilePool2D (같은 오브젝트 또는 자식)
//   Enemy Mask          → Enemy
//   Aim Range           → 20
//   Cooldown            → 2 (재사용 대기시간)
//   Projectile Speed    → 8
//   Tick Interval       → 0.5
//   Debug Log           → ✅ (디버깅 후 끄기)
//
// [Hierarchy 구조]
// Weapon_PurificationOrb (이 스크립트 + ProjectilePool2D)
//   → ProjectilePool2D의 Prefab 필드에 PurificationOrb_Projectile 연결
// ============================================================================
using UnityEngine;

/// <summary>
/// 정화구 무기. 우선순위 타겟에 부착형 지속 피해 투사체를 발사한다.
/// DarkOrbWeapon2D와 동일한 MonoBehaviour + ILevelableSkill 패턴.
/// </summary>
[DisallowMultipleComponent]
public sealed class PurificationOrbWeapon2D : MonoBehaviour, ILevelableSkill
{
    // ══════════════════════════════════════════════════════════════
    // Inspector
    // ══════════════════════════════════════════════════════════════

    [Header("풀")]
    [SerializeField, Tooltip("투사체 풀입니다. ProjectilePool2D를 연결하세요.")]
    private ProjectilePool2D pool;

    [Header("타겟")]
    [SerializeField, Tooltip("적 레이어마스크입니다.")]
    private LayerMask enemyMask;

    [SerializeField, Min(1f), Tooltip("타겟 탐색 범위입니다.")]
    private float aimRange = 20f;

    [Header("발사")]
    [SerializeField, Min(0.1f), Tooltip("재사용 대기시간(초)입니다.")]
    private float cooldown = 2f;

    [SerializeField, Min(0.1f), Tooltip("투사체 추적 속도입니다.")]
    private float projectileSpeed = 8f;

    [SerializeField, Min(0.1f), Tooltip("틱 간격(초)입니다. 0.5초 고정 권장.")]
    private float tickInterval = 0.5f;

    [Header("디버그")]
    [SerializeField]
    private bool debugLog;

    // ══════════════════════════════════════════════════════════════
    // 런타임 상태
    // ══════════════════════════════════════════════════════════════

    private Transform _owner;
    private int       _level = 1;
    private float     _cooldownTimer;
    private bool      _attached;
    private PlayerCombatStats2D _stats;

    // 적 탐색용 (GC 0)
    private readonly Collider2D[] _enemyHits = new Collider2D[64];
    private ContactFilter2D _enemyFilter;
    private bool _filterReady;

    // ══════════════════════════════════════════════════════════════
    // 레벨 테이블 (하드코딩 — 기획서 확정치)
    // ══════════════════════════════════════════════════════════════

    private static readonly float[] TickDamageByLevel = { 5f, 7.5f, 10f, 10f, 10f, 10f, 10f, 10f };
    private static readonly int[]   TickCountByLevel  = { 5,  5,    5,   6,   7,   8,   9,   10  };

    // ══════════════════════════════════════════════════════════════
    // ILevelableSkill 인터페이스 구현
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// CommonSkillManager2D에서 호출 (오타 호환).
    /// </summary>
    public void OnAttaced(Transform owner) => OnAttached(owner);

    /// <summary>
    /// 무기 장착 시 호출. 플레이어 참조 획득.
    /// </summary>
    public void OnAttached(Transform owner)
    {
        _owner = owner;
        _attached = true;
        _stats = owner.GetComponentInChildren<PlayerCombatStats2D>();
        _cooldownTimer = 0f;

        if (debugLog)
            Debug.Log($"[정화구 무기] 장착 완료 — 소유자:{owner.name}");
    }

    /// <summary>
    /// 레벨 적용. CommonSkillManager2D에서 레벨업 시 호출.
    /// </summary>
    public void ApplyLevel(int level)
    {
        _level = Mathf.Clamp(level, 1, 8);
        if (debugLog)
            Debug.Log($"[정화구 무기] 레벨 적용 → Lv{_level} (틱 데미지:{GetTickDamage()}, 틱 횟수:{GetTickCount()})");
    }

    // ══════════════════════════════════════════════════════════════
    // Update — 쿨다운 + 자동 발사
    // ══════════════════════════════════════════════════════════════

    private void Update()
    {
        if (!_attached || _owner == null) return;

        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer > 0f) return;

        Transform target = FindPriorityTarget();
        if (target == null) return;

        Fire(target);
        _cooldownTimer = ComputeCooldown();
    }

    // ══════════════════════════════════════════════════════════════
    // 발사
    // ══════════════════════════════════════════════════════════════

    private void Fire(Transform target)
    {
        if (pool == null)
        {
            Debug.LogError("[정화구 무기] ProjectilePool2D가 연결되지 않았습니다!", this);
            return;
        }

        // PooledObject2D 상속 덕분에 제네릭 제약 충족
        var orb = pool.Get<PurificationOrbProjectile2D>(
            _owner.position, Quaternion.identity);

        if (orb == null)
        {
            if (debugLog) Debug.LogWarning("[정화구 무기] 풀에서 투사체를 꺼낼 수 없습니다.");
            return;
        }

        int tickDmg = GetTickDamage();
        int tickCnt = GetTickCount();

        orb.Init(enemyMask, target, tickDmg, tickCnt, tickInterval, projectileSpeed);
        orb.gameObject.SetActive(true);

        if (debugLog)
            Debug.Log($"[정화구 무기] 발사 → {target.name} (Lv{_level}, 틱:{tickDmg}×{tickCnt})");
    }

    // ══════════════════════════════════════════════════════════════
    // 레벨 테이블 조회
    // ══════════════════════════════════════════════════════════════

    private int GetTickDamage()
    {
        int idx = Mathf.Clamp(_level - 1, 0, TickDamageByLevel.Length - 1);
        float dmg = TickDamageByLevel[idx];

        if (_stats != null)
            dmg *= _stats.DamageMul;

        return Mathf.Max(1, Mathf.RoundToInt(dmg));
    }

    private int GetTickCount()
    {
        int idx = Mathf.Clamp(_level - 1, 0, TickCountByLevel.Length - 1);
        return TickCountByLevel[idx];
    }

    private float ComputeCooldown()
    {
        float cd = cooldown;
        if (_stats != null)
            cd *= Mathf.Max(0.05f, _stats.CooldownMul);
        return Mathf.Max(0.1f, cd);
    }

    // ══════════════════════════════════════════════════════════════
    // 우선순위 기반 타겟 탐색 (Boss > Elite > Normal)
    // ══════════════════════════════════════════════════════════════

    private Transform FindPriorityTarget()
    {
        EnsureFilter();

        float seekRange = aimRange;
        if (_stats != null)
            seekRange *= Mathf.Max(0.1f, _stats.AreaMul);

        int count = Physics2D.OverlapCircle(
            (Vector2)_owner.position, seekRange, _enemyFilter, _enemyHits);

        if (count == 0) return null;

        Transform bestTarget = null;
        EnemyGrade bestGrade = (EnemyGrade)999;
        float bestDistSq = float.PositiveInfinity;
        Vector2 ownerPos = _owner.position;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _enemyHits[i];
            if (hit == null) continue;

            GameObject enemyGo = hit.gameObject;
            if (!enemyGo.activeInHierarchy) continue;

            if (!PurificationOrbAttachTracker.CanAttachTo(enemyGo)) continue;

            EnemyGrade grade = EnemyGrade.Normal;
            EnemyGradeTag gradeTag = enemyGo.GetComponent<EnemyGradeTag>();
            if (gradeTag != null) grade = gradeTag.Grade;

            float distSq = ((Vector2)enemyGo.transform.position - ownerPos).sqrMagnitude;

            if (grade < bestGrade || (grade == bestGrade && distSq < bestDistSq))
            {
                bestTarget = enemyGo.transform;
                bestGrade = grade;
                bestDistSq = distSq;
            }
        }

        return bestTarget;
    }

    private void EnsureFilter()
    {
        if (_filterReady) return;
        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(enemyMask);
        _enemyFilter.useTriggers = true;
        _filterReady = true;
    }
}