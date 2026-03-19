// [구현 원리]
// 좌격요세 — 하린의 기본 스킬 (횡베기).
// 투사체를 발사하지 않고, 플레이어 앞쪽에 참격 판정 영역을 생성하여
// 범위 내 모든 적에게 동시 데미지를 넣는 근접 범위 공격.
//
// [발사 흐름]
// Update에서 쿨다운 소모 → 쿨다운 완료 시 Fire() 호출
//   → ProjectilePool2D에서 참격 오브젝트(JwagyeokYoseSlash2D) Get
//   → 참격 오브젝트가 OverlapCircle로 범위 내 적 탐색 + 데미지
//   → 수명 후 풀 반환
//
// [기존 시스템 연동]
// CommonSkillManager2D.Upgrade(config) → 무기 프리팹 Instantiate
//   → ILevelableSkill.OnAttached() → 플레이어 참조 획득
//   → ILevelableSkill.ApplyLevel()  → 레벨별 수치 적용
// ============================================================================
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 좌격요세 무기 — 하린 기본 스킬 (횡베기).
/// 가장 가까운 적 방향으로 근접 범위 참격을 시전한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class JwagyeokYoseWeapon2D : MonoBehaviour, ILevelableSkill
{
    // ══════════════════════════════════════════════════════
    //  Inspector — 풀
    // ══════════════════════════════════════════════════════

    [Header("참격 풀")]
    [Tooltip("좌격요세 전용 ProjectilePool2D.\n풀의 Prefab 슬롯에 JwagyeokYose_Slash 프리팹을 넣어둘 것.")]
    [SerializeField] private ProjectilePool2D pool;

    // ══════════════════════════════════════════════════════
    //  Inspector — 타겟
    // ══════════════════════════════════════════════════════

    [Header("타겟 설정")]
    [Tooltip("적 레이어마스크")]
    [SerializeField] private LayerMask enemyMask;

    [Tooltip("적 탐색 최대 범위")]
    [SerializeField] private float aimRange = 20f;

    // ══════════════════════════════════════════════════════
    //  Inspector — 기본 수치 (SO에서 오버라이드됨)
    // ══════════════════════════════════════════════════════

    [Header("기본 수치 (레벨 데이터로 오버라이드)")]
    [Tooltip("피해량")]
    [SerializeField] private int damage = 20;

    [Tooltip("재사용 대기시간 (초)")]
    [SerializeField] private float cooldown = 1.2f;

    [Tooltip("참격 판정 반경")]
    [SerializeField] private float slashRadius = 2.0f;

    [Tooltip("참격 오프셋 (플레이어로부터의 거리)")]
    [SerializeField] private float slashOffset = 1.5f;

    [Tooltip("참격 수명 (초) — VFX 재생 시간")]
    [SerializeField] private float slashLifetime = 0.4f;

    // ══════════════════════════════════════════════════════
    //  런타임
    // ══════════════════════════════════════════════════════

    private Transform _owner;
    private float _cooldownTimer;
    private int _currentLevel;
    private bool _initialized;
    private CommonSkillConfigSO _config;

    // 적 탐색용 (GC 0)
    private readonly List<Collider2D> _searchBuffer = new(32);
    private ContactFilter2D _enemyFilter;

    // ══════════════════════════════════════════════════════
    //  ILevelableSkill 구현
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// ILevelableSkill 레거시 오타 버전 — OnAttached로 위임.
    /// </summary>
    public void OnAttaced(Transform newOwner) => OnAttached(newOwner);

    /// <summary>
    /// CommonSkillManager2D가 무기 프리팹 생성 후 호출.
    /// 플레이어 Transform 참조를 받는다.
    /// </summary>
    public void OnAttached(Transform owner)
    {
        _owner = owner;

        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(enemyMask);
        _enemyFilter.useLayerMask = true;
        _enemyFilter.useTriggers = true;

        // Pool이 Inspector에서 연결 안 된 경우 (프리팹 → 씬 참조 불가) 자동 탐색
        if (pool == null)
        {
            // 이름에 "JwagyeokYose" 또는 "좌격" 포함된 전용 풀 우선 탐색
            var allPools = FindObjectsByType<ProjectilePool2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in allPools)
            {
                if (p.name.Contains("JwagyeokYose") || p.name.Contains("좌격"))
                {
                    pool = p;
                    break;
                }
            }

#if UNITY_EDITOR
            if (pool != null)
                Debug.Log($"[좌격요세] 전용 풀 자동 탐색 성공 — {pool.name}");
            else
                Debug.LogError("[좌격요세] 'JwagyeokYose' 이름의 ProjectilePool2D를 씬에서 찾을 수 없습니다!\n" +
                               "Hierarchy에 빈 오브젝트 'JwagyeokYosePool'을 만들고 ProjectilePool2D 컴포넌트를 추가하세요.");
#endif
        }

        _cooldownTimer = 0f;
        _initialized = true;

        Debug.Log($"[좌격요세] 무기 장착 완료 — owner={owner.name}");
    }

    /// <summary>
    /// CommonSkillManager2D가 SO 참조를 전달할 때 호출.
    /// ApplyLevel()보다 먼저 호출되어야 레벨 데이터를 읽을 수 있다.
    /// </summary>
    public void SetConfig(CommonSkillConfigSO config)
    {
        _config = config;
    }

    /// <summary>
    /// 레벨 변경 시 호출. 저장된 SO(_config)에서 레벨 데이터를 읽어 적용.
    /// SO가 없으면 Inspector 기본값을 유지한다.
    /// </summary>
    public void ApplyLevel(int level)
    {
        _currentLevel = level;

        if (_config != null)
        {
            CommonSkillLevelParams p = _config.GetLevelParams(level);
            damage       = p.damage;
            cooldown     = p.cooldown;
            slashRadius  = p.explosionRadius > 0f ? p.explosionRadius : slashRadius;
            slashLifetime = p.lifeSeconds > 0f ? p.lifeSeconds : slashLifetime;
        }

        Debug.Log($"[좌격요세] 레벨 적용 Lv.{level} — 피해량={damage}, 쿨타임={cooldown:F2}초, 범위={slashRadius:F1}");
    }

    // ══════════════════════════════════════════════════════
    //  Update — 쿨다운 & 자동 발사
    // ══════════════════════════════════════════════════════

    private void Update()
    {
        if (!_initialized) return;

        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer > 0f) return;

        // 적 탐색 — 가장 가까운 적 방향 결정
        Vector2 slashDir = FindClosestEnemyDirection();
        if (slashDir == Vector2.zero) return; // 범위 내 적 없음

        Fire(slashDir);
        _cooldownTimer = cooldown;
    }

    // ══════════════════════════════════════════════════════
    //  발사 — 참격 오브젝트 생성
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 참격을 시전한다. 풀에서 참격 오브젝트를 꺼내서
    /// 플레이어 앞쪽(slashOffset)에 배치한다.
    /// </summary>
    private void Fire(Vector2 direction)
    {
        if (pool == null)
        {
            Debug.LogWarning("[좌격요세] pool이 None입니다!");
            return;
        }

        // 참격 위치 = 플레이어 위치 + (방향 × 오프셋)
        Vector2 ownerPos = _owner.position;
        Vector2 spawnPos = ownerPos + direction.normalized * slashOffset;

        // 방향에 따라 회전 (좌→우 베기 비주얼 방향)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);

        // 풀에서 꺼내기 — Get<T>(pos, rot)는 위치/회전 설정 + SetActive(true)까지 처리
        var slash = pool.Get<JwagyeokYoseSlash2D>(spawnPos, rot);
        if (slash == null)
        {
            Debug.LogWarning("[좌격요세] 풀에서 참격 오브젝트를 가져올 수 없습니다!");
            return;
        }

        // 참격 초기화
        slash.Initialize(damage, slashRadius, slashLifetime, enemyMask);

#if UNITY_EDITOR
        Debug.Log($"[좌격요세] 참격 시전 — 위치=({spawnPos.x:F1},{spawnPos.y:F1}), 방향=({direction.x:F2},{direction.y:F2}), 피해량={damage}");
#endif
    }

    // ══════════════════════════════════════════════════════
    //  적 탐색 — 가장 가까운 적 방향
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 탐색 범위 내 가장 가까운 적의 방향 벡터를 반환.
    /// 적이 없으면 Vector2.zero.
    /// </summary>
    private Vector2 FindClosestEnemyDirection()
    {
        Vector2 ownerPos = _owner.position;
        _searchBuffer.Clear();

        int count = Physics2D.OverlapCircle(ownerPos, aimRange, _enemyFilter, _searchBuffer);
        if (count == 0) return Vector2.zero;

        float closestDist = float.MaxValue;
        Vector2 closestDir = Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _searchBuffer[i];
            if (col == null) continue;

            // 죽은 적 필터링
            var health = col.GetComponentInParent<EnemyHealth2D>();
            if (health != null && health.IsDead) continue;

            Vector2 enemyPos = col.transform.position;
            float dist = (enemyPos - ownerPos).sqrMagnitude;
            if (dist < closestDist)
            {
                closestDist = dist;
                closestDir = (enemyPos - ownerPos).normalized;
            }
        }

        return closestDir;
    }

    // ══════════════════════════════════════════════════════
    //  기즈모 — 에디터 시각화
    // ══════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 탐색 범위 (파랑)
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.15f);
        Vector3 center = _owner != null ? _owner.position : transform.position;
        Gizmos.DrawWireSphere(center, aimRange);

        // 참격 범위 (빨강)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        Vector3 slashCenter = center + Vector3.right * slashOffset;
        Gizmos.DrawWireSphere(slashCenter, slashRadius);
    }
#endif
}