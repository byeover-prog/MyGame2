// UTF-8
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 좌격요세 무기 — 하린 기본 스킬 (근접 횡베기).
/// 가장 가까운 적 방향 자동 조준 (뱀서라이크 + 모바일 호환).
/// 방향값을 Slash에 전달하여 VFX/판정 방향 일치.
/// </summary>
[DisallowMultipleComponent]
public sealed class JwagyeokYoseWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("참격 풀")]
    [SerializeField] private ProjectilePool2D pool;

    [Header("타겟 설정")]
    [SerializeField] private LayerMask enemyMask;

    [Tooltip("적 탐색 최대 범위 (참격 범위보다 살짝 넓게)")]
    [SerializeField] private float detectRange = 6.0f;

    [Header("기본 수치 (레벨 데이터로 오버라이드)")]
    [SerializeField] private int damage = 20;
    [SerializeField] private float cooldown = 1.2f;
    [SerializeField] private float slashRadius = 3.0f;
    [SerializeField] private float slashLifetime = 0.4f;

    private Transform _owner;
    private PlayerCombatStats2D _combatStats;
    private float _cooldownTimer;
    private int _currentLevel;
    private bool _initialized;
    private CommonSkillConfigSO _config;

    private readonly List<Collider2D> _searchBuffer = new(32);
    private ContactFilter2D _enemyFilter;

    public void OnAttaced(Transform newOwner) => OnAttached(newOwner);

    public void OnAttached(Transform owner)
    {
        _owner = owner;
        _combatStats = owner.GetComponent<PlayerCombatStats2D>();

        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(enemyMask);
        _enemyFilter.useLayerMask = true;
        _enemyFilter.useTriggers = true;

        if (pool == null)
        {
            var allPools = FindObjectsByType<ProjectilePool2D>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in allPools)
            {
                if (p.name.Contains("JwagyeokYose") || p.name.Contains("좌격"))
                { pool = p; break; }
            }
#if UNITY_EDITOR
            if (pool != null) GameLogger.Log($"[좌격요세] 전용 풀 탐색 성공 — {pool.name}");
            else Debug.LogError("[좌격요세] 'JwagyeokYose' 풀을 찾을 수 없습니다!");
#endif
        }

        _cooldownTimer = 0f;
        _initialized = true;
        GameLogger.Log($"[좌격요세] 무기 장착 완료 — owner={owner.name}");
    }

    public void SetConfig(CommonSkillConfigSO config) => _config = config;

    public void ApplyLevel(int level)
    {
        _currentLevel = level;
        if (_config != null)
        {
            CommonSkillLevelParams p = _config.GetLevelParams(level);
            damage        = p.damage;
            cooldown      = p.cooldown;
            slashRadius   = p.explosionRadius > 0f ? p.explosionRadius : slashRadius;
            slashLifetime = p.lifeSeconds > 0f ? p.lifeSeconds : slashLifetime;
        }
        GameLogger.Log($"[좌격요세] Lv.{level} — 피해량={damage}, 쿨타임={cooldown:F2}초, 범위={slashRadius:F1}");
    }

    private void Update()
    {
        if (!_initialized) return;
        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer > 0f) return;

        Vector2 dir = FindClosestEnemyDir();
        if (dir == Vector2.zero) return;

        Fire(dir);
        _cooldownTimer = cooldown;
    }

    private void Fire(Vector2 aimDir)
    {
        if (pool == null) return;

        Vector3 ownerPos = _owner.position;
        float angleDeg = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        int finalDamage = _combatStats != null
            ? Mathf.RoundToInt(damage * _combatStats.DamageMul) : damage;

        var slash = pool.Get<JwagyeokYoseSlash2D>(ownerPos, Quaternion.identity);
        if (slash == null) return;

        slash.Initialize(finalDamage, slashRadius, slashLifetime,
                         enemyMask, _owner, aimDir, angleDeg);

#if UNITY_EDITOR
        GameLogger.Log($"[좌격요세] 횡베기! 각도={angleDeg:F0}° 피해량={finalDamage} 범위={slashRadius:F1}");
#endif
    }

    /// <summary>
    /// detectRange 내 가장 가까운 살아있는 적 방향.
    /// 적이 없으면 Vector2.zero → 스킬 발동 안 함.
    /// </summary>
    private Vector2 FindClosestEnemyDir()
    {
        Vector2 ownerPos = _owner.position;
        _searchBuffer.Clear();
        int count = Physics2D.OverlapCircle(ownerPos, detectRange, _enemyFilter, _searchBuffer);
        if (count == 0) return Vector2.zero;

        float closestSqr = float.MaxValue;
        Vector2 closestDir = Vector2.zero;
        for (int i = 0; i < count; i++)
        {
            Collider2D col = _searchBuffer[i];
            if (col == null) continue;
            var health = col.GetComponentInParent<EnemyHealth2D>();
            if (health != null && health.IsDead) continue;
            Vector2 toEnemy = (Vector2)col.transform.position - ownerPos;
            float sqr = toEnemy.sqrMagnitude;
            if (sqr < closestSqr) { closestSqr = sqr; closestDir = toEnemy.normalized; }
        }
        return closestDir;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 c = _owner != null ? _owner.position : transform.position;
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.15f);
        Gizmos.DrawWireSphere(c, detectRange);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(c, slashRadius);
    }
#endif
}