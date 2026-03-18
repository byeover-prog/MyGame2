// UTF-8
// ============================================================================
// PurificationOrbProjectile2D.cs
// 경로: Assets/_Game/Scripts/Skill_Scripts/SkillSystem/CommonSkill/PurificationOrb/PurificationOrbProjectile2D.cs
//
// [구현 원리]
// 정화구 투사체는 2개의 상태로 동작하는 간단한 상태 머신:
//
//   ┌──────────┐    충돌    ┌──────────┐    틱 소진    ┌──────────┐
//   │  추적    │ ────────→ │  부착    │ ──────────→  │  소멸    │
//   │ (Chase)  │           │ (Attach) │              │          │
//   └──────────┘           └──────────┘              └──────────┘
//                               │ 대상 사망
//                               ↓
//                          재탐색 → 추적
//
// [타겟팅 우선순위]
// Boss(0) > Elite(1) > Normal(2) → 같은 등급 내에서 가장 가까운 적.
// EnemyGradeTag 컴포넌트가 없으면 Normal로 처리.
//
// [부착 규칙]
// - 기본: 동일 대상 1개만 부착 → PurificationOrbAttachTracker로 관리
// - 유물 확장 시: 동일 대상 중첩 부착 가능, 추가분 피해 50% 감소
//
// [풀링]
// ProjectilePool2D에서 관리. OnDisable에서 부착 해제 처리.
//
// [VFX]
// - 추적 중: 본체 스프라이트 활성
// - 부착 중: 대상 위에서 약한 공전 또는 고정 + 틱마다 tickVfxPrefab 재생
// - 종료 시: fadeOut 후 풀 반환
//
// [주의사항]
// - Transform.SetParent 사용 금지 (풀링 깨짐 방지)
//   → 대신 매 프레임 target.position을 추적하여 위치 동기화
// - OnDisable에서 반드시 UnregisterAttach 호출
// - rb.linearVelocity는 Unity 6 전용 API
// ============================================================================
using UnityEngine;

/// <summary>
/// 정화구 투사체. 추적 → 부착 → 지속 피해 → 소멸/재탐색.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class PurificationOrbProjectile2D : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════
    // 상태 enum
    // ══════════════════════════════════════════════════════════════

    private enum OrbState
    {
        Chase,      // 타겟을 추적 중
        Attached,   // 적에게 부착되어 DoT 진행 중
        Dead,       // 소멸 대기 (풀 반환 예정)
    }

    // ══════════════════════════════════════════════════════════════
    // Inspector
    // ══════════════════════════════════════════════════════════════

    [Header("추적 설정")]
    [SerializeField, Tooltip("추적 이동 속도입니다.")]
    private float chaseSpeed = 8f;

    [SerializeField, Tooltip("추적 시 회전 속도(도/초)입니다. 높을수록 즉시 방향 전환.")]
    private float chaseRotateSpeed = 720f;

    [SerializeField, Tooltip("타겟을 잃은 후 재탐색 범위입니다.")]
    private float reSearchRange = 15f;

    [SerializeField, Tooltip("추적 최대 시간(초). 이 시간 내 부착 못 하면 소멸합니다.")]
    private float maxChaseTime = 5f;

    [Header("부착 설정")]
    [SerializeField, Tooltip("부착 시 대상 중심에서의 오프셋 반경입니다. 0이면 중심 고정.")]
    private float attachOrbitRadius = 0.3f;

    [SerializeField, Tooltip("부착 시 공전 속도(도/초)입니다. 0이면 고정.")]
    private float attachOrbitSpeed = 180f;

    [Header("VFX")]
    [SerializeField, Tooltip("틱마다 재생할 데미지 이펙트 프리팹입니다.")]
    private GameObject tickVfxPrefab;

    [SerializeField, Tooltip("소멸 시 재생할 이펙트 프리팹입니다.")]
    private GameObject despawnVfxPrefab;

    [Header("비주얼")]
    [SerializeField] private SpriteRenderer orbSprite;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    // ══════════════════════════════════════════════════════════════
    // 런타임 상태 (외부에서 Init으로 주입)
    // ══════════════════════════════════════════════════════════════

    private OrbState       _state;
    private Transform      _target;
    private Rigidbody2D    _rb;
    private Collider2D     _collider;

    // 초기화 파라미터
    private LayerMask _enemyMask;
    private int       _tickDamage;
    private int       _remainingTicks;
    private float     _tickInterval;
    private float     _damageMultiplier;  // 중첩 부착 패널티 반영
    private int       _attachOrder;       // 이 정화구가 해당 적에 몇 번째로 부착됐는지

    // 타이머
    private float _tickTimer;
    private float _chaseTimer;

    // 부착 공전
    private float _orbitAngle;

    // 적 탐색용 (GC 0)
    private readonly Collider2D[] _searchHits = new Collider2D[32];
    private ContactFilter2D _searchFilter;
    private bool _filterReady;

    // 부착된 적 참조 (UnregisterAttach용)
    private GameObject _attachedEnemy;

    // ══════════════════════════════════════════════════════════════
    // 초기화
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// PurificationOrbWeapon2D에서 발사 시 호출.
    /// </summary>
    /// <param name="enemyMask">적 레이어마스크</param>
    /// <param name="target">최초 추적 타겟</param>
    /// <param name="tickDamage">틱당 피해량 (정수)</param>
    /// <param name="tickCount">총 틱 횟수</param>
    /// <param name="tickInterval">틱 간격(초)</param>
    /// <param name="speed">추적 속도</param>
    public void Init(LayerMask enemyMask, Transform target,
                     int tickDamage, int tickCount, float tickInterval, float speed)
    {
        _enemyMask      = enemyMask;
        _target         = target;
        _tickDamage     = tickDamage;
        _remainingTicks = tickCount;
        _tickInterval   = tickInterval;
        chaseSpeed      = speed;

        _state           = OrbState.Chase;
        _chaseTimer      = 0f;
        _tickTimer       = 0f;
        _orbitAngle      = 0f;
        _attachedEnemy   = null;
        _attachOrder     = 0;
        _damageMultiplier = 1f;

        // Rigidbody 설정
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;

        // Collider는 추적 중 트리거 역할
        if (_collider == null) _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
        _collider.enabled = true;

        // 비주얼 리셋
        if (orbSprite != null)
        {
            var c = orbSprite.color;
            c.a = 1f;
            orbSprite.color = c;
        }

        if (debugLog)
            Debug.Log($"[정화구] 초기화 완료 — 틱 데미지:{tickDamage}, 틱 횟수:{tickCount}, 타겟:{(target != null ? target.name : "없음")}");
    }

    // ══════════════════════════════════════════════════════════════
    // Unity 라이프사이클
    // ══════════════════════════════════════════════════════════════

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    private void OnDisable()
    {
        // 풀 반환 시 부착 해제 보장
        if (_attachedEnemy != null)
        {
            PurificationOrbAttachTracker.UnregisterAttach(_attachedEnemy);
            _attachedEnemy = null;
        }
        _state = OrbState.Dead;
        _rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        switch (_state)
        {
            case OrbState.Chase:
                UpdateChase();
                break;

            case OrbState.Attached:
                UpdateAttached();
                break;

            case OrbState.Dead:
                // 아무것도 안 함 — 풀 반환 대기
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 상태 1: 추적 (Chase)
    // ══════════════════════════════════════════════════════════════

    private void UpdateChase()
    {
        _chaseTimer += Time.deltaTime;

        // 추적 시간 초과 → 소멸
        if (_chaseTimer >= maxChaseTime)
        {
            if (debugLog) Debug.Log("[정화구] 추적 시간 초과 → 소멸");
            Die();
            return;
        }

        // 타겟 유효성 확인
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            _target = FindPriorityTarget();
            if (_target == null)
            {
                if (debugLog) Debug.Log("[정화구] 재탐색 실패 → 소멸");
                Die();
                return;
            }
            if (debugLog) Debug.Log($"[정화구] 재탐색 성공 → {_target.name}");
        }

        // 부착 가능 여부 실시간 체크 (유물 확장 고려)
        if (!PurificationOrbAttachTracker.CanAttachTo(_target.gameObject))
        {
            // 이 타겟은 이미 꽉 참 → 다른 타겟 탐색
            Transform alt = FindPriorityTarget(_target.gameObject);
            if (alt != null)
            {
                _target = alt;
                if (debugLog) Debug.Log($"[정화구] 부착 불가 대상 → 대체 타겟: {alt.name}");
            }
            // 대체 타겟도 없으면 일단 현재 타겟을 향해 계속 이동 (다음 프레임에 다시 확인)
        }

        // 유도 이동
        Vector2 toTarget = ((Vector2)_target.position - (Vector2)transform.position);
        float dist = toTarget.magnitude;

        if (dist < 0.15f)
        {
            // 충돌 판정 — 부착 시도
            TryAttachToTarget();
            return;
        }

        Vector2 desired = toTarget.normalized * chaseSpeed;
        Vector2 current = _rb.linearVelocity;
        float maxDelta = chaseRotateSpeed * Mathf.Deg2Rad * Time.deltaTime * chaseSpeed;
        _rb.linearVelocity = Vector2.MoveTowards(current, desired, maxDelta);

        // 스프라이트 회전 (진행 방향)
        float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // OnTriggerEnter2D로도 충돌 감지 (거리 판정 보완)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_state != OrbState.Chase) return;
        if (_target == null) return;

        // 타겟과 동일한 오브젝트인지 확인
        if (other.transform == _target || other.transform.root == _target.root)
        {
            TryAttachToTarget();
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 부착 시도
    // ══════════════════════════════════════════════════════════════

    private void TryAttachToTarget()
    {
        if (_target == null || _state != OrbState.Chase) return;

        GameObject enemyGO = _target.gameObject;

        // 부착 가능 여부 최종 확인
        if (!PurificationOrbAttachTracker.CanAttachTo(enemyGO))
        {
            if (debugLog) Debug.Log($"[정화구] 부착 거부됨 (최대 초과) → {enemyGO.name}");
            // 다른 타겟 탐색
            _target = FindPriorityTarget(enemyGO);
            if (_target == null)
            {
                Die();
            }
            return;
        }

        // ★ 부착 등록
        _attachOrder = PurificationOrbAttachTracker.GetAttachCount(enemyGO);
        PurificationOrbAttachTracker.RegisterAttach(enemyGO);
        _attachedEnemy = enemyGO;
        _damageMultiplier = PurificationOrbAttachTracker.GetDamageMultiplier(_attachOrder);

        // 상태 전환
        _state = OrbState.Attached;
        _rb.linearVelocity = Vector2.zero;
        _collider.enabled = false;
        _tickTimer = 0f;
        _orbitAngle = Random.Range(0f, 360f);

        if (debugLog)
            Debug.Log($"[정화구] 부착 성공 → {enemyGO.name} (순서:{_attachOrder}, 배율:{_damageMultiplier:F1})");
    }

    // ══════════════════════════════════════════════════════════════
    // 상태 2: 부착 (Attached)
    // ══════════════════════════════════════════════════════════════

    private void UpdateAttached()
    {
        // 대상 사망 체크
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            if (debugLog) Debug.Log("[정화구] 부착 대상 사망 → 재탐색");
            DetachAndReSearch();
            return;
        }

        // 위치 동기화 (Transform.SetParent 사용 안 함 — 풀링 안전)
        if (attachOrbitRadius > 0.01f && attachOrbitSpeed > 0f)
        {
            _orbitAngle += attachOrbitSpeed * Time.deltaTime;
            float rad = _orbitAngle * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * attachOrbitRadius;
            transform.position = (Vector2)_target.position + offset;
        }
        else
        {
            transform.position = _target.position;
        }

        // 틱 타이머
        _tickTimer += Time.deltaTime;
        if (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            ApplyTickDamage();
            _remainingTicks--;

            if (debugLog)
                Debug.Log($"[정화구] 틱 피해 적용 → 남은 틱:{_remainingTicks}");

            if (_remainingTicks <= 0)
            {
                if (debugLog) Debug.Log("[정화구] 틱 소진 → 소멸");
                Die();
                return;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 데미지 적용
    // ══════════════════════════════════════════════════════════════

    private void ApplyTickDamage()
    {
        if (_target == null) return;

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(_tickDamage * _damageMultiplier));

        // DamageUtil2D.TryApplyDamage 사용 (프로젝트 표준)
        DamageUtil2D.TryApplyDamage(_target.gameObject, finalDamage);

        // 틱 VFX
        if (tickVfxPrefab != null)
        {
            VFXSpawner.Spawn(tickVfxPrefab, _target.position, Quaternion.identity);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 부착 해제 + 재탐색
    // ══════════════════════════════════════════════════════════════

    private void DetachAndReSearch()
    {
        // 부착 해제
        if (_attachedEnemy != null)
        {
            PurificationOrbAttachTracker.UnregisterAttach(_attachedEnemy);
            _attachedEnemy = null;
        }

        // 아직 틱이 남아있으면 재탐색
        if (_remainingTicks > 0)
        {
            _target = FindPriorityTarget();
            if (_target != null)
            {
                _state = OrbState.Chase;
                _collider.enabled = true;
                _chaseTimer = 0f; // 추적 타이머 리셋
                if (debugLog) Debug.Log($"[정화구] 재탐색 성공 → {_target.name}");
                return;
            }
        }

        // 재탐색 실패 또는 틱 없음 → 소멸
        Die();
    }

    // ══════════════════════════════════════════════════════════════
    // 소멸
    // ══════════════════════════════════════════════════════════════

    private void Die()
    {
        if (_state == OrbState.Dead) return;
        _state = OrbState.Dead;

        // 부착 해제
        if (_attachedEnemy != null)
        {
            PurificationOrbAttachTracker.UnregisterAttach(_attachedEnemy);
            _attachedEnemy = null;
        }

        _rb.linearVelocity = Vector2.zero;

        // 소멸 VFX
        if (despawnVfxPrefab != null)
        {
            VFXSpawner.Spawn(despawnVfxPrefab, transform.position, Quaternion.identity);
        }

        // 풀 반환 (ProjectilePool2D 패턴)
        gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════
    // 우선순위 기반 타겟 탐색
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Boss > Elite > Normal 우선순위로 가장 가까운 적을 탐색합니다.
    /// </summary>
    /// <param name="exclude">제외할 대상 (선택)</param>
    private Transform FindPriorityTarget(GameObject exclude = null)
    {
        EnsureFilter();

        int count = Physics2D.OverlapCircle(
            (Vector2)transform.position, reSearchRange, _searchFilter, _searchHits);

        if (count == 0) return null;

        Transform bestTarget = null;
        EnemyGrade bestGrade = (EnemyGrade)999;
        float bestDistSq = float.PositiveInfinity;
        Vector2 myPos = transform.position;

        for (int i = 0; i < count; i++)
        {
            var hit = _searchHits[i];
            if (hit == null) continue;

            GameObject enemyGO = hit.gameObject;

            // 제외 대상 스킵
            if (exclude != null && enemyGO == exclude) continue;

            // 비활성 스킵
            if (!enemyGO.activeInHierarchy) continue;

            // 부착 가능 여부 확인
            if (!PurificationOrbAttachTracker.CanAttachTo(enemyGO)) continue;

            // 등급 확인
            EnemyGrade grade = EnemyGrade.Normal;
            var gradeTag = enemyGO.GetComponent<EnemyGradeTag>();
            if (gradeTag != null) grade = gradeTag.Grade;

            float distSq = ((Vector2)enemyGO.transform.position - myPos).sqrMagnitude;

            // 우선순위 비교: 등급이 더 높으면(값이 작으면) 무조건 선택
            // 등급이 같으면 거리가 가까운 쪽 선택
            if (grade < bestGrade || (grade == bestGrade && distSq < bestDistSq))
            {
                bestTarget = enemyGO.transform;
                bestGrade = grade;
                bestDistSq = distSq;
            }
        }

        return bestTarget;
    }

    private void EnsureFilter()
    {
        if (_filterReady) return;
        _searchFilter = new ContactFilter2D();
        _searchFilter.SetLayerMask(_enemyMask);
        _searchFilter.useTriggers = true;
        _filterReady = true;
    }
}