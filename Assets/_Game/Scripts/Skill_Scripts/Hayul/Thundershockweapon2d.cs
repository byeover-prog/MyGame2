using System.Collections.Generic;
using UnityEngine;

// 뇌진 (雷震) — 하율 전용 스킬 #1
// 마늘형 전기 오라.
// 플레이어 주변 원형 범위 내 모든 적에게 틱 데미지를 준다.
// 영구 지속 (장착하면 항상 활성).

[DisallowMultipleComponent]
public sealed class ThunderShockWeapon2D : MonoBehaviour, ILevelableSkill
{
    //  인스펙터

    [Header("=== 타겟 설정 ===")]
    [SerializeField] private LayerMask enemyMask;

    [Header("=== 기본 수치 (Lv1) ===")]
    [SerializeField] private int   baseDamage    = 15;
    [SerializeField] private float baseRadius    = 3.0f;
    [SerializeField] private float baseInterval  = 1.0f;

    [Header("=== 레벨 스케일링 ===")]
    [Tooltip("레벨당 피해량 증가 배율")]
    [SerializeField] private float damagePerLevel  = 0.10f;

    [Tooltip("레벨당 범위 증가 배율")]
    [SerializeField] private float radiusPerLevel  = 0.10f;

    [Header("=== 각성 보너스 (Lv7+) ===")]
    [SerializeField] private int   awakeningLevel           = 7;
    [SerializeField] private float awakeningIntervalReduce  = 0.5f;
    [SerializeField] private float awakeningDamageMultiplier = 1.5f;

    [Header("=== 과부하 ===")]
    [Tooltip("이 수 이상의 적이 범위 내에 있으면 과부하 발동")]
    [SerializeField] private int   overloadThreshold       = 3;

    [Tooltip("과부하 시 피해량 배율")]
    [SerializeField] private float overloadDamageMultiplier = 1.5f;

    [Header("=== VFX ===")]
    [Tooltip("오라 VFX 오브젝트 (자식). 범위에 맞춰 스케일 조정됨.")]
    [SerializeField] private Transform vfxRoot;

    [Tooltip("VFX 기준 반지름 (localScale 1일 때의 시각적 반지름)")]
    [SerializeField] private float vfxBaseRadius = 1.0f;

    [Header("=== 히트 쿨다운 (같은 적 연타 방지) ===")]
    [Tooltip("같은 적에게 다시 데미지를 줄 수 있는 최소 간격 (초)")]
    [SerializeField] private float perEnemyCooldown = 0.5f;

    [Header("=== 디버그 ===")]
    [SerializeField] private bool enableLogs = false;
    
    //  내부 상태

    private Transform _owner;
    private PlayerCombatStats2D _combatStats;
    private bool _initialized;
    private int  _currentLevel;

    // 현재 레벨 기준 계산된 수치
    private int   _damage;
    private float _radius;
    private float _interval;

    private float _tickTimer;

    // 같은 적 연타 방지: rootInstanceId → 다음 히트 가능 시간
    private readonly Dictionary<int, float> _hitCooldowns = new(32);

    // OverlapCircle 버퍼
    private readonly List<Collider2D> _hitBuffer = new(64);
    private ContactFilter2D _enemyFilter;
    
    //  ILevelableSkill 구현

    public void OnAttaced(Transform newOwner) => OnAttached(newOwner);

    public void OnAttached(Transform owner)
    {
        _owner = owner;
        _combatStats = owner != null ? owner.GetComponent<PlayerCombatStats2D>() : null;

        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(enemyMask);
        _enemyFilter.useLayerMask = true;
        _enemyFilter.useTriggers  = true;

        _tickTimer = 0f;
        _hitCooldowns.Clear();
        _initialized = true;

        // 레벨이 아직 안 들어왔으면 Lv1로 초기화
        if (_currentLevel <= 0)
            RecalculateStats(1);

        if (enableLogs)
            GameLogger.Log($"[뇌진] 장착 완료 — owner={owner?.name}", this);
    }

    public void ApplyLevel(int newLevel)
    {
        _currentLevel = Mathf.Max(1, newLevel);
        RecalculateStats(_currentLevel);

        if (enableLogs)
            GameLogger.Log($"[뇌진] Lv.{_currentLevel} — 피해량={_damage}, 범위={_radius:F1}, 간격={_interval:F2}초", this);
    }


    //  레벨별 수치 계산

    private void RecalculateStats(int level)
    {
        // 레벨 스케일링: Lv1 = 1.0배, Lv2 = 1.1배, Lv3 = 1.2배 ...
        float levelScale = 1f + damagePerLevel * (level - 1);

        _damage   = Mathf.RoundToInt(baseDamage * levelScale);
        _radius   = baseRadius * (1f + radiusPerLevel * (level - 1));
        _interval = baseInterval;

        // 각성 보너스
        if (level >= awakeningLevel)
        {
            _interval = Mathf.Max(0.1f, _interval - awakeningIntervalReduce);
            _damage   = Mathf.RoundToInt(_damage * awakeningDamageMultiplier);
        }

        // VFX 스케일 동기화
        UpdateVFXScale();
    }

    private void UpdateVFXScale()
    {
        if (vfxRoot == null) return;
        if (vfxBaseRadius <= 0f) return;

        float scale = _radius / vfxBaseRadius;
        vfxRoot.localScale = new Vector3(scale, scale, 1f);
    }
    
    //  Update — 틱 데미지 루프

    private void Update()
    {
        if (!_initialized) return;
        if (_owner == null) return;

        _tickTimer -= Time.deltaTime;
        if (_tickTimer > 0f) return;

        _tickTimer = _interval;
        Tick();
    }

    private void Tick()
    {
        Vector2 center = _owner.position;
        _hitBuffer.Clear();

        int hitCount = Physics2D.OverlapCircle(center, _radius, _enemyFilter, _hitBuffer);
        if (hitCount == 0) return;

        // 죽은 적 필터링 + 유효 히트 수 카운트
        int aliveCount = 0;
        float now = Time.time;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = _hitBuffer[i];
            if (col == null) continue;

            var health = col.GetComponentInParent<EnemyHealth2D>();
            if (health != null && health.IsDead) continue;

            aliveCount++;
        }

        // 과부하 판정
        bool overloaded = aliveCount >= overloadThreshold;

        // 최종 데미지 계산 (combatStats 배율 + 과부하)
        float damageMul = _combatStats != null ? _combatStats.DamageMul : 1f;
        if (overloaded)
            damageMul *= overloadDamageMultiplier;

        int finalDamage = Mathf.RoundToInt(_damage * damageMul);
        if (finalDamage <= 0) return;

        // 데미지 적용
        int actualHits = 0;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = _hitBuffer[i];
            if (col == null) continue;

            var health = col.GetComponentInParent<EnemyHealth2D>();
            if (health != null && health.IsDead) continue;

            // 같은 적 연타 방지
            int rootId = DamageUtil2D.GetRootId(col);
            if (_hitCooldowns.TryGetValue(rootId, out float nextHitTime) && now < nextHitTime)
                continue;

            _hitCooldowns[rootId] = now + perEnemyCooldown;

            // 전기 속성으로 데미지 적용 → 전파 패시브 트리거
            if (DamageUtil2D.TryApplyDamage(col, finalDamage, DamageElement2D.Electric))
                actualHits++;
        }

        // 오래된 쿨다운 엔트리 정리 (메모리 방지)
        CleanupExpiredCooldowns(now);

        if (enableLogs && actualHits > 0)
        {
            string overloadTag = overloaded ? " [과부하!]" : "";
            CombatLog.Log($"[뇌진] 틱! 히트={actualHits} 데미지={finalDamage}{overloadTag} 범위 내 생존={aliveCount}");
        }
    }
    
    //  유틸
    // 만료된 히트 쿨다운을 정리한다. 매 틱마다 호출.
    private void CleanupExpiredCooldowns(float now)
    {
        // 64개 이하면 정리 스킵 (성능 절약)
        if (_hitCooldowns.Count <= 64) return;

        var expired = new List<int>(16);
        foreach (var kv in _hitCooldowns)
        {
            if (now >= kv.Value)
                expired.Add(kv.Key);
        }

        for (int i = 0; i < expired.Count; i++)
            _hitCooldowns.Remove(expired[i]);
    }
    
    //  에디터 기즈모
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 c = _owner != null ? _owner.position : transform.position;

        // 오라 범위
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.2f);
        Gizmos.DrawSphere(c, _radius > 0f ? _radius : baseRadius);

        // 와이어프레임
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.6f);
        Gizmos.DrawWireSphere(c, _radius > 0f ? _radius : baseRadius);
    }
#endif
}