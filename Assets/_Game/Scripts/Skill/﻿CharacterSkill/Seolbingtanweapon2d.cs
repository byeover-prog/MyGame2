using UnityEngine;

/// <summary>
/// 설빙탄 (雪氷彈) — 윤설 전용 스킬 #2 (빙결 속성, 부착 후 폭발 투사체)
///
/// 컨셉: 가장 가까운 적을 추적하는 화살을 발사.
///   - 적에게 명중하면 부착되어 0.5초 동안 따라다님
///   - 타이머 만료 후 AOE 폭발 (빙결 적용)
///
/// 레벨 스케일링:
///   - 레벨당 피해량 +10%
///   - 레벨당 재사용 대기시간 -10%
///
/// 각성 (Lv7+):
///   - 시전 횟수 +3 (총 4발 동시 발사)
///
/// 프리팹 구조:
///   Weapon_Seolbingtan (프리팹 루트)
///   ├── LevelableSkillMarker2D
///   ├── SeolbingtanWeapon2D (이 스크립트)
///   └── Pool_Seolbingtan (ProjectilePool2D + PF_SeolbingtanArrow)
/// </summary>
[DisallowMultipleComponent]
public sealed class SeolbingtanWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("투사체 풀")]
    [SerializeField] private ProjectilePool2D pool;

    [Header("타겟 설정")]
    [SerializeField] private LayerMask enemyMask;

    [Tooltip("화살이 추적할 적의 최대 탐색 반경입니다.")]
    [SerializeField] private float seekRadius = 12f;

    [Header("기본 수치 (Lv1)")]
    [SerializeField] private int baseDamage = 10;

    [Tooltip("재사용 대기시간(초)입니다.")]
    [SerializeField] private float baseCooldown = 2.5f;

    [Tooltip("화살 비행 속도(유닛/초)입니다.")]
    [SerializeField] private float arrowSpeed = 12f;

    [Tooltip("화살 최대 비행 시간(초)입니다. 적 못 맞히면 사라짐.")]
    [SerializeField] private float arrowMaxFlightTime = 2.0f;

    [Tooltip("부착 후 폭발까지 시간(초)입니다.")]
    [SerializeField] private float attachDelay = 0.5f;

    [Tooltip("폭발 반경(유닛)입니다.")]
    [SerializeField] private float explosionRadius = 1.5f;

    [Tooltip("발사 시 플레이어 위치에서의 오프셋(유닛).")]
    [SerializeField] private float spawnOffset = 0.5f;

    [Header("빙결 효과")]
    [Tooltip("폭발 시 적용되는 동상 지속 시간(초)입니다.")]
    [SerializeField] private float frostDuration = 2.0f;

    [Tooltip("동상 이속 감소 비율입니다. 0.5 = 50% 감속.")]
    [SerializeField] private float frostSlowMultiplier = 0.5f;

    [Header("레벨 스케일링")]
    [Tooltip("레벨당 피해량 증가 비율입니다. 0.10 = +10%.")]
    [SerializeField] private float damagePerLevel = 0.10f;

    [Tooltip("레벨당 쿨다운 감소 비율입니다. 0.10 = -10%/Lv.")]
    [SerializeField] private float cooldownReductionPerLevel = 0.10f;

    [Tooltip("쿨다운 감소 최저 한도(곱). 0.4 = 최대 60% 감소까지.")]
    [SerializeField] private float cooldownMinMultiplier = 0.4f;

    [Header("각성 보너스 (Lv7+)")]
    [SerializeField] private int awakeningLevel = 7;

    [Tooltip("각성 시 추가 시전 횟수.")]
    [SerializeField] private int awakeningExtraCasts = 3;

    [Tooltip("다발 발사 시 화살 사이 발사 각도(도). 0이면 같은 방향.")]
    [SerializeField] private float multiCastAngleSpread = 15f;

    [Header("디버그")]
    [SerializeField] private bool enableLogs = false;

    // ── 내부 상태 ──
    private Transform _owner;
    private PlayerCombatStats2D _combatStats;
    private float _cooldownTimer;
    private int _currentLevel;
    private bool _initialized;
    private int _damage;
    private float _cooldown;
    private bool _awakened;
    private int _castCount;

    // ── ILevelableSkill ──
    public void OnAttaced(Transform newOwner) => OnAttached(newOwner);

    public void OnAttached(Transform owner)
    {
        _owner = owner;
        _combatStats = owner != null ? owner.GetComponent<PlayerCombatStats2D>() : null;

        if (pool == null)
        {
            var allPools = FindObjectsByType<ProjectilePool2D>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in allPools)
            {
                if (p.name.Contains("Seolbingtan") || p.name.Contains("설빙탄"))
                { pool = p; break; }
            }
            if (pool == null)
                Debug.LogError("[설빙탄] 'Seolbingtan' 풀을 찾을 수 없습니다!", this);
        }

        _cooldownTimer = 0f;

        if (_currentLevel <= 0)
            RecalculateStats(1);

        _initialized = true;

        if (enableLogs)
            GameLogger.Log($"[설빙탄] 무기 장착 완료 — owner={owner?.name}", this);
    }

    public void ApplyLevel(int newLevel)
    {
        _currentLevel = Mathf.Max(1, newLevel);
        RecalculateStats(_currentLevel);

        if (enableLogs)
            GameLogger.Log(
                $"[설빙탄] Lv.{_currentLevel} — 피해량={_damage}, 쿨타임={_cooldown:F2}초, " +
                $"시전수={_castCount}, 각성={_awakened}", this);
    }

    private void RecalculateStats(int level)
    {
        // 데미지: 레벨당 +10%
        float damageScale = 1f + damagePerLevel * (level - 1);
        _damage = Mathf.RoundToInt(baseDamage * damageScale);

        // 쿨다운: 레벨당 -10% (최저 cooldownMinMultiplier 까지)
        float cdMultiplier = Mathf.Max(cooldownMinMultiplier,
            1f - cooldownReductionPerLevel * (level - 1));
        _cooldown = baseCooldown * cdMultiplier;

        // 각성: 시전 횟수 추가
        _awakened = (level >= awakeningLevel);
        _castCount = _awakened ? (1 + awakeningExtraCasts) : 1;
    }

    private void Update()
    {
        if (!_initialized || _owner == null) return;

        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer > 0f) return;

        Fire();
        _cooldownTimer = _cooldown;
    }

    private void Fire()
    {
        if (pool == null) return;

        Vector3 ownerPos = _owner.position;
        int finalDamage = _combatStats != null
            ? Mathf.RoundToInt(_damage * _combatStats.DamageMul)
            : _damage;

        // 가장 가까운 적 탐색
        Transform mainTarget = FindNearestEnemy(ownerPos);

        // _castCount만큼 발사
        for (int i = 0; i < _castCount; i++)
        {
            // 각성 다발 발사 시 약간의 각도 분산
            float angleOffset = 0f;
            if (_castCount > 1)
            {
                // -spread*(n-1)/2 ~ +spread*(n-1)/2 사이로 균등 분산
                float halfSpread = multiCastAngleSpread * (_castCount - 1) * 0.5f;
                angleOffset = -halfSpread + multiCastAngleSpread * i;
            }

            FireOne(ownerPos, mainTarget, finalDamage, angleOffset);
        }

        if (enableLogs)
            CombatLog.Log($"[설빙탄] 발사! {_castCount}발 dmg={finalDamage} target={mainTarget?.name}");
    }

    private void FireOne(Vector3 ownerPos, Transform target, int damage, float angleOffsetDeg)
    {
        // 발사 방향 결정
        Vector2 direction;
        if (target != null)
        {
            direction = ((Vector2)target.position - (Vector2)ownerPos).normalized;
        }
        else
        {
            // 적 없으면 오른쪽으로
            direction = Vector2.right;
        }

        // 각도 분산 적용
        if (Mathf.Abs(angleOffsetDeg) > 0.01f)
        {
            float rad = angleOffsetDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            direction = new Vector2(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos);
        }

        Vector3 spawnPos = ownerPos + (Vector3)(direction * spawnOffset);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        var arrow = pool.Get<SeolbingtanArrow2D>(spawnPos, rot);
        if (arrow == null) return;

        arrow.Initialize(
            damage: damage,
            direction: direction,
            speed: arrowSpeed,
            maxFlightTime: arrowMaxFlightTime,
            attachDelay: attachDelay,
            explosionRadius: explosionRadius,
            enemyMask: enemyMask,
            frostDuration: frostDuration,
            frostSlowMultiplier: frostSlowMultiplier);
    }

    private Transform FindNearestEnemy(Vector3 origin)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, seekRadius, enemyMask);
        if (hits == null || hits.Length == 0) return null;

        Transform closest = null;
        float closestDistSqr = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i];
            if (col == null) continue;

            var health = col.GetComponentInParent<EnemyHealth2D>();
            if (health != null && health.IsDead) continue;

            float distSqr = ((Vector2)col.bounds.center - (Vector2)origin).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = col.transform;
            }
        }

        return closest;
    }
}