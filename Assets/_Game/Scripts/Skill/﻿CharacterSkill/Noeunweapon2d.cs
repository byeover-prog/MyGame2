using UnityEngine;

/// <summary>
/// 뇌운 (雷雲) — 하율 전용 스킬 #2 (전기 속성, 추적 유도형 + 번개 낙하)
///
/// 컨셉: 가장 가까운 적을 쫓아다니는 전기 구름을 소환.
///   - 구름 본체는 데미지 X (이동만)
///   - 구름에서 0.5초마다 번개가 아래로 떨어지며 적 피격
///   - 지속 시간 동안 적을 따라다님
///
/// 레벨 스케일링:
///   - 레벨당 피해량 +10%
///   - 레벨당 스킬 범위(번개 폭발 범위) +10%
///
/// 각성 (Lv7+):
///   - 시전 횟수 +3 (총 4개 구름 동시 소환)
///
/// 프리팹 구조:
///   Weapon_Noeun (프리팹 루트)
///   ├── LevelableSkillMarker2D
///   ├── NoeunWeapon2D (이 스크립트)
///   ├── Pool_Noeun_Cloud (ProjectilePool2D + PF_NoeunCloud)
///   └── Pool_Noeun_Bolt (ProjectilePool2D + PF_NoeunBolt)
/// </summary>
[DisallowMultipleComponent]
public sealed class NoeunWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("투사체 풀")]
    [Tooltip("구름 본체 풀입니다.")]
    [SerializeField] private ProjectilePool2D cloudPool;

    [Tooltip("번개 자식 풀입니다. NoeunCloud2D로 전달되어 사용.")]
    [SerializeField] private ProjectilePool2D boltPool;

    [Header("타겟 설정")]
    [SerializeField] private LayerMask enemyMask;

    [Tooltip("초기 타겟 탐색 최대 반경입니다.")]
    [SerializeField] private float initialSeekRadius = 12f;

    [Header("기본 수치 (Lv1)")]
    [SerializeField] private int baseDamage = 5;

    [Tooltip("재사용 대기시간(초)입니다.")]
    [SerializeField] private float baseCooldown = 5.0f;

    [Tooltip("번개 폭발 범위(유닛)입니다. 레벨당 +10% 증가.")]
    [SerializeField] private float baseBoltRadius = 1.0f;

    [Tooltip("구름 지속 시간(초)입니다.")]
    [SerializeField] private float cloudLifetime = 5.0f;

    [Tooltip("번개 발사 간격(초)입니다.")]
    [SerializeField] private float boltInterval = 0.5f;

    [Tooltip("구름이 적을 따라가는 속도(유닛/초). 적보다 살짝 빠르게.")]
    [SerializeField] private float cloudFollowSpeed = 4.0f;

    [Tooltip("초기 구름 스폰 시 플레이어로부터의 반경(유닛). 0이면 플레이어 위치.")]
    [SerializeField] private float spawnRadius = 1.5f;

    [Header("레벨 스케일링")]
    [Tooltip("레벨당 피해량 증가 비율입니다. 0.10 = +10%.")]
    [SerializeField] private float damagePerLevel = 0.10f;

    [Tooltip("레벨당 번개 폭발 범위 증가 비율입니다. 0.10 = +10%.")]
    [SerializeField] private float radiusPerLevel = 0.10f;

    [Header("각성 보너스 (Lv7+)")]
    [SerializeField] private int awakeningLevel = 7;

    [Tooltip("각성 시 추가 시전 횟수.")]
    [SerializeField] private int awakeningExtraCasts = 3;

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
    private float _boltRadius;
    private bool _awakened;
    private int _castCount;

    // ── ILevelableSkill ──
    public void OnAttaced(Transform newOwner) => OnAttached(newOwner);

    public void OnAttached(Transform owner)
    {
        _owner = owner;
        _combatStats = owner != null ? owner.GetComponent<PlayerCombatStats2D>() : null;

        // 풀 자동 탐색
        if (cloudPool == null || boltPool == null)
        {
            var allPools = FindObjectsByType<ProjectilePool2D>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in allPools)
            {
                if (cloudPool == null && (p.name.Contains("Cloud") || p.name.Contains("구름")))
                    cloudPool = p;
                else if (boltPool == null && (p.name.Contains("Bolt") || p.name.Contains("번개")))
                    boltPool = p;
            }

            if (cloudPool == null)
                Debug.LogError("[뇌운] Cloud 풀을 찾을 수 없습니다!", this);
            if (boltPool == null)
                Debug.LogError("[뇌운] Bolt 풀을 찾을 수 없습니다!", this);
        }

        _cooldownTimer = 0f;

        if (_currentLevel <= 0)
            RecalculateStats(1);

        _initialized = true;

        if (enableLogs)
            GameLogger.Log($"[뇌운] 무기 장착 완료 — owner={owner?.name}", this);
    }

    public void ApplyLevel(int newLevel)
    {
        _currentLevel = Mathf.Max(1, newLevel);
        RecalculateStats(_currentLevel);

        if (enableLogs)
            GameLogger.Log(
                $"[뇌운] Lv.{_currentLevel} — 피해량={_damage}, 범위={_boltRadius:F2}, " +
                $"시전수={_castCount}, 각성={_awakened}", this);
    }

    private void RecalculateStats(int level)
    {
        // 데미지: 레벨당 +10%
        float damageScale = 1f + damagePerLevel * (level - 1);
        _damage = Mathf.RoundToInt(baseDamage * damageScale);

        // 폭발 범위: 레벨당 +10%
        float radiusScale = 1f + radiusPerLevel * (level - 1);
        _boltRadius = baseBoltRadius * radiusScale;

        // 쿨다운: 변동 없음
        _cooldown = baseCooldown;

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
        if (cloudPool == null || boltPool == null) return;

        Vector3 ownerPos = _owner.position;
        int finalDamage = _combatStats != null
            ? Mathf.RoundToInt(_damage * _combatStats.DamageMul)
            : _damage;

        for (int i = 0; i < _castCount; i++)
        {
            // 다발 발사 시 플레이어 주위에 분산
            Vector3 spawnPos = ownerPos;
            if (_castCount > 1)
            {
                float angle = (i / (float)_castCount) * 2f * Mathf.PI;
                spawnPos += new Vector3(
                    Mathf.Cos(angle) * spawnRadius,
                    Mathf.Sin(angle) * spawnRadius,
                    0f);
            }
            else if (spawnRadius > 0.01f)
            {
                // 1개 시전이라도 플레이어 살짝 위에 스폰
                spawnPos += new Vector3(0f, spawnRadius, 0f);
            }

            var cloud = cloudPool.Get<NoeunCloud2D>(spawnPos, Quaternion.identity);
            if (cloud == null) continue;

            cloud.Initialize(
                damage: finalDamage,
                boltRadius: _boltRadius,
                lifetime: cloudLifetime,
                boltInterval: boltInterval,
                followSpeed: cloudFollowSpeed,
                seekRadius: initialSeekRadius,
                enemyMask: enemyMask,
                boltPool: boltPool);
        }

        if (enableLogs)
            CombatLog.Log(
                $"[뇌운] 구름 {_castCount}개 소환! dmg={finalDamage} 범위={_boltRadius:F2}");
    }
}