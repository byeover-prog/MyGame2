using UnityEngine;

/// <summary>
/// 월참 (月斬) — 하린 전용 스킬 #2 (음 속성, 직선 관통 검기)
///
/// 컨셉: 마우스 방향(PC) 또는 자동 조준(모바일)으로 초승달형 검기를 발사.
///   - 적을 관통하며 직선 이동
///   - 같은 적에게 중복 피해 불가능
///
/// 레벨 스케일링:
///   - 레벨당 피해량 +15%
///
/// 각성 (Lv7+):
///   - 마우스 기준 십자(상/하/좌/우) 4방향으로 동시 발사
///
/// 프리팹 구조:
///   Weapon_Wolcham (프리팹 루트)
///   ├── LevelableSkillMarker2D
///   ├── WolchamWeapon2D (이 스크립트)
///   └── Pool_Wolcham (ProjectilePool2D + PF_WolchamCrescent)
/// </summary>
[DisallowMultipleComponent]
public sealed class WolchamWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("투사체 풀")]
    [Tooltip("WolchamCrescent2D 풀입니다. 비워두면 자동 탐색.")]
    [SerializeField] private ProjectilePool2D pool;

    [Header("타겟 설정")]
    [SerializeField] private LayerMask enemyMask;

    [Tooltip("자동 조준 시 적 탐색 최대 반경입니다.")]
    [SerializeField] private float autoAimRadius = 15f;

    [Header("기본 수치 (Lv1)")]
    [SerializeField] private int baseDamage = 15;

    [Tooltip("재사용 대기시간(초)입니다.")]
    [SerializeField] private float baseCooldown = 3.0f;

    [Tooltip("검기 이동 속도(유닛/초)입니다.")]
    [SerializeField] private float projectileSpeed = 14f;

    [Tooltip("검기 최대 비행 시간(초)입니다.")]
    [SerializeField] private float projectileLifetime = 1.2f;

    [Tooltip("검기 시작 시 플레이어 위치에서의 오프셋(유닛). 캐릭터 몸 안에서 시작 방지.")]
    [SerializeField] private float spawnOffset = 0.5f;

    [Header("레벨 스케일링")]
    [Tooltip("레벨당 피해량 증가 비율입니다. 0.15 = +15%.")]
    [SerializeField] private float damagePerLevel = 0.15f;

    [Header("각성 보너스 (Lv7+)")]
    [SerializeField] private int awakeningLevel = 7;

    [Tooltip("각성 시 십자 4방향 발사 활성화.")]
    [SerializeField] private bool awakeningCrossFire = true;

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
                if (p.name.Contains("Wolcham") || p.name.Contains("월참"))
                { pool = p; break; }
            }
            if (pool == null)
                Debug.LogError("[월참] 'Wolcham' 풀을 찾을 수 없습니다!", this);
        }

        _cooldownTimer = 0f;

        if (_currentLevel <= 0)
            RecalculateStats(1);

        _initialized = true;

        if (enableLogs)
            GameLogger.Log($"[월참] 무기 장착 완료 — owner={owner?.name}", this);
    }

    public void ApplyLevel(int newLevel)
    {
        _currentLevel = Mathf.Max(1, newLevel);
        RecalculateStats(_currentLevel);

        if (enableLogs)
            GameLogger.Log(
                $"[월참] Lv.{_currentLevel} — 피해량={_damage}, 쿨타임={_cooldown:F2}초, 각성={_awakened}", this);
    }

    private void RecalculateStats(int level)
    {
        float scale = 1f + damagePerLevel * (level - 1);
        _damage = Mathf.RoundToInt(baseDamage * scale);
        _cooldown = baseCooldown;
        _awakened = (level >= awakeningLevel);
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

        if (_awakened && awakeningCrossFire)
        {
            // 각성: 십자 4방향 발사
            Vector2[] dirs = AimInputProvider.GetCrossDirections(ownerPos, enemyMask, autoAimRadius);
            for (int i = 0; i < dirs.Length; i++)
                FireOne(ownerPos, dirs[i], finalDamage);

            if (enableLogs)
                CombatLog.Log($"[월참 각성] 십자 4방향 발사! dmg={finalDamage}");
        }
        else
        {
            // 일반: 1방향 발사
            Vector2 dir = AimInputProvider.GetAimDirection(ownerPos, enemyMask, autoAimRadius);
            FireOne(ownerPos, dir, finalDamage);

            if (enableLogs)
                CombatLog.Log($"[월참] 발사! dmg={finalDamage} dir={dir}");
        }
    }

    private void FireOne(Vector3 ownerPos, Vector2 direction, int damage)
    {
        Vector3 spawnPos = ownerPos + (Vector3)(direction * spawnOffset);

        // 회전: 검기가 진행 방향을 향하도록
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        var crescent = pool.Get<WolchamCrescent2D>(spawnPos, rot);
        if (crescent == null) return;

        crescent.Initialize(
            damage: damage,
            direction: direction,
            speed: projectileSpeed,
            lifetime: projectileLifetime,
            enemyMask: enemyMask);
    }
}