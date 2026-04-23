using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class YeolchamWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("참격 풀")]
    [Tooltip("YeolchamSlash2D 투사체 풀입니다. 비워두면 자동 탐색합니다.")]
    [SerializeField] private ProjectilePool2D pool;

    [Header("타겟 설정")]
    [Tooltip("적 레이어 마스크입니다.")]
    [SerializeField] private LayerMask enemyMask;

    [Header("기본 수치 (Lv1)")]
    [Tooltip("기본 피해량입니다.")]
    [SerializeField] private int baseDamage = 10;

    [Tooltip("재사용 대기시간(초)입니다.")]
    [SerializeField] private float baseCooldown = 4.0f;

    [Tooltip("참격 반경(월드 유닛)입니다.")]
    [SerializeField] private float baseSlashRadius = 3.0f;

    [Tooltip("참격 지속시간(초)입니다.")]
    [SerializeField] private float slashLifetime = 0.5f;

    [Header("외곽/내부 차등 데미지")]
    [Tooltip("외곽 링이 차지하는 반경 비율입니다. 0.7 = 외곽 30% 영역.")]
    [Range(0.5f, 0.95f)]
    [SerializeField] private float outerRingRatio = 0.7f;

    [Tooltip("외곽 링 적중 시 데미지 배율입니다.")]
    [SerializeField] private float outerDamageMultiplier = 1.30f;

    [Tooltip("내부 영역 적중 시 데미지 배율입니다.")]
    [SerializeField] private float innerDamageMultiplier = 0.70f;

    [Header("레벨 스케일링")]
    [Tooltip("레벨당 피해량 증가 비율입니다. 0.15 = +15%.")]
    [SerializeField] private float damagePerLevel = 0.15f;

    [Header("각성 보너스 (Lv7+)")]
    [Tooltip("각성 발동 레벨입니다.")]
    [SerializeField] private int awakeningLevel = 7;

    [Tooltip("각성 시 재사용 대기시간 감소 비율입니다. 0.30 = -30%.")]
    [SerializeField] private float awakeningCooldownReduction = 0.30f;

    [Tooltip("각성 시 외곽 적중 적에게 부여하는 출혈 지속시간(초)입니다.")]
    [SerializeField] private float awakeningBleedDuration = 3.0f;

    [Tooltip("각성 시 출혈 초당 피해량(현재 데미지의 비율)입니다. 0.10 = 10%/초.")]
    [SerializeField] private float awakeningBleedDpsRatio = 0.10f;

    [Header("디버그")]
    [SerializeField] private bool enableLogs = false;

    // ═══════════════════════════════════════════════
    //  내부 상태
    // ═══════════════════════════════════════════════

    private Transform _owner;
    private PlayerCombatStats2D _combatStats;
    private float _cooldownTimer;
    private int _currentLevel;
    private bool _initialized;

    // 현재 레벨 기준 계산값
    private int _damage;
    private float _cooldown;
    private bool _awakened;

    // ═══════════════════════════════════════════════
    //  ILevelableSkill 구현
    // ═══════════════════════════════════════════════

    public void OnAttaced(Transform newOwner) => OnAttached(newOwner);

    public void OnAttached(Transform owner)
    {
        _owner = owner;
        _combatStats = owner != null ? owner.GetComponent<PlayerCombatStats2D>() : null;

        // 풀 자동 탐색
        if (pool == null)
        {
            var allPools = FindObjectsByType<ProjectilePool2D>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in allPools)
            {
                if (p.name.Contains("Yeolcham") || p.name.Contains("열참"))
                { pool = p; break; }
            }
            if (pool == null)
                Debug.LogError("[열참] 'Yeolcham' 풀을 찾을 수 없습니다!", this);
        }

        _cooldownTimer = 0f;

        if (_currentLevel <= 0)
            RecalculateStats(1);

        _initialized = true;

        if (enableLogs)
            GameLogger.Log($"[열참] 무기 장착 완료 — owner={owner?.name}", this);
    }

    public void ApplyLevel(int newLevel)
    {
        _currentLevel = Mathf.Max(1, newLevel);
        RecalculateStats(_currentLevel);

        if (enableLogs)
            GameLogger.Log(
                $"[열참] Lv.{_currentLevel} — 피해량={_damage}, 쿨타임={_cooldown:F2}초, " +
                $"각성={_awakened}", this);
    }

    private void RecalculateStats(int level)
    {
        // 레벨 스케일링: Lv1 = 1.0배, Lv2 = 1.15배 ...
        float levelScale = 1f + damagePerLevel * (level - 1);
        _damage = Mathf.RoundToInt(baseDamage * levelScale);
        _cooldown = baseCooldown;

        // 각성
        _awakened = (level >= awakeningLevel);
        if (_awakened)
            _cooldown = baseCooldown * (1f - awakeningCooldownReduction);
    }

    // ═══════════════════════════════════════════════
    //  Update — 자동 발동
    // ═══════════════════════════════════════════════

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

        var slash = pool.Get<YeolchamSlash2D>(ownerPos, Quaternion.identity);
        if (slash == null) return;

        slash.Initialize(
            damage: finalDamage,
            radius: baseSlashRadius,
            lifetime: slashLifetime,
            enemyMask: enemyMask,
            owner: _owner,
            outerRingRatio: outerRingRatio,
            outerMultiplier: outerDamageMultiplier,
            innerMultiplier: innerDamageMultiplier,
            awakened: _awakened,
            bleedDuration: awakeningBleedDuration,
            bleedDpsRatio: awakeningBleedDpsRatio);

        if (enableLogs)
            CombatLog.Log($"[열참] 원형 검 휘두름! 피해량={finalDamage} 반경={baseSlashRadius:F1} 각성={_awakened}");
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 c = _owner != null ? _owner.position : transform.position;

        // 외곽 링 (빨강)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(c, baseSlashRadius);

        // 내부 (파랑)
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireSphere(c, baseSlashRadius * outerRingRatio);
    }
#endif
}