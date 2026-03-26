// 모든 투사체의 이동/충돌/소멸을 단일 Update()에서 처리하는 중앙 매니저.
// MonoBehaviour.Update 1개만 실행 — 투사체 200개여도 Update 호출 1회.
//
// [기존 GameProjectileManager(DarkOrb 전용)를 일반화한 버전]
//
// [GC 0 보장 규칙]
// - Update 내부에서 new 금지
// - List/Array 재사용
// - delegate 할당 금지
// - LINQ 금지
// - string 생성은 #if UNITY_EDITOR 내에서만
//
// [Hierarchy / Inspector]
// Hierarchy: [CentralProjectileManager] (빈 GameObject)
// 컴포넌트: CentralProjectileManager + CentralViewPool
//
// Inspector:
//   Max Projectiles      → 512
//   Default Enemy Mask   → Enemy 레이어
//   View Pool            → 같은 오브젝트의 CentralViewPool
//   Hit Cache Size       → 2048 (중복 히트 방지 공유 버퍼)
//
// [기존 시스템과의 관계]
// - SkillRunner, UltimateExecutor, CommonSkillWeapon2D: 건드리지 않음
// - 기존 무기 클래스(DarkOrbWeapon2D 등)가 이 매니저의 Spawn()을 호출
// - 기존 투사체 MonoBehaviour(ShurikenProjectile2D 등)는 점진적으로 교체
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

public sealed class CentralProjectileManager : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════
    //  Singleton
    // ══════════════════════════════════════════════════════════════
    public static CentralProjectileManager Instance { get; private set; }

    // ══════════════════════════════════════════════════════════════
    //  Inspector
    // ══════════════════════════════════════════════════════════════

    [Header("풀 크기")]
    [Tooltip("동시 활성 투사체 최대 수 (전 종류 합산)")]
    [SerializeField] private int maxProjectiles = 512;

    [Header("중복 히트 방지")]
    [Tooltip("중복 히트 방지용 공유 버퍼 크기. 투사체당 최대 hitCache 사용량 합산.")]
    [SerializeField] private int hitCacheSize = 2048;

    [Header("기본 설정")]
    [SerializeField] private LayerMask defaultEnemyMask;

    [Header("뷰 풀 (★필수)")]
    [Tooltip("같은 오브젝트의 CentralViewPool을 연결하세요.")]
    [SerializeField] private CentralViewPool viewPool;

    // ══════════════════════════════════════════════════════════════
    //  내부 상태
    // ══════════════════════════════════════════════════════════════

    private ProjectileSlot[] _slots;
    private int _activeCount;

    // 분열 대기열 (SplitOnExpiry)
    private readonly Queue<SplitRequest> _splitQueue = new Queue<SplitRequest>(64);

    // 물리 쿼리 공유 버퍼 (GC 0)
    private Collider2D[] _overlapBuffer;
    private const int OVERLAP_BUFFER_SIZE = 32;

    // 중복 히트 방지 공유 버퍼
    // _slots[i].HitCacheStartIndex ~ +HitCacheCount 범위를 사용
    private int[] _hitCacheBuffer;
    private int _hitCacheUsed;

    // ══════════════════════════════════════════════════════════════
    //  생명주기
    // ══════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _slots = new ProjectileSlot[maxProjectiles];
        _overlapBuffer = new Collider2D[OVERLAP_BUFFER_SIZE];
        _hitCacheBuffer = new int[hitCacheSize];
        _hitCacheUsed = 0;
        _activeCount = 0;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1단계: 모든 활성 슬롯 처리 (이동 → 충돌 → 수명)
        for (int i = _slots.Length - 1; i >= 0; i--)
        {
            if (!_slots[i].Active) continue;
            ProcessSlot(ref _slots[i], i, dt);
        }

        // 2단계: 분열 대기열 처리
        FlushSplitQueue();

        // 3단계: 뷰 위치 동기화
        SyncViews();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ══════════════════════════════════════════════════════════════
    //  Public API — 투사체 생성
    // ══════════════════════════════════════════════════════════════

    /// <summary>현재 활성 투사체 수.</summary>
    public int ActiveCount => _activeCount;

    /// <summary>기본 적 레이어마스크.</summary>
    public LayerMask DefaultEnemyMask => defaultEnemyMask;

    /// <summary>
    /// 투사체 1개를 생성합니다.
    /// 외부(CommonSkillWeapon2D 등)에서 호출.
    /// slot 참조를 반환하여 추가 설정 가능.
    /// 실패 시 -1 반환.
    /// </summary>
    public int Spawn(ref ProjectileSlot template)
    {
        int idx = FindFreeSlot();
        if (idx < 0)
        {
            #if UNITY_EDITOR
            GameLogger.LogWarning("[CentralPM] 슬롯 부족! maxProjectiles를 늘리세요.");
            #endif
            return -1;
        }

        // 템플릿 복사
        _slots[idx] = template;
        _slots[idx].Active = true;

        // 히트 캐시 할당
        _slots[idx].HitCacheStartIndex = _hitCacheUsed;
        _slots[idx].HitCacheCount = 0;

        // 뷰 획득 (★ v2: VisualId 기반)
        if (viewPool != null)
        {
            _slots[idx].ViewId = viewPool.Acquire(
                template.VisualId, template.Position);
        }

        _activeCount++;
        return idx;
    }

    /// <summary>
    /// 슬롯을 강제로 비활성화합니다. 외부에서 조기 회수 시 사용.
    /// </summary>
    public void Kill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Length) return;
        if (!_slots[slotIndex].Active) return;
        DeactivateSlot(ref _slots[slotIndex], slotIndex);
    }

    /// <summary>특정 MoveKind의 모든 투사체를 제거합니다.</summary>
    public void KillAllOfKind(ProjectileMoveKind kind)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].Active) continue;
            if (_slots[i].MoveKind != kind) continue;
            DeactivateSlot(ref _slots[i], i);
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  핵심: 슬롯 처리 (종류별 분기)
    // ══════════════════════════════════════════════════════════════

    private void ProcessSlot(ref ProjectileSlot s, int idx, float dt)
    {
        // ── 수명 감소 ──
        s.Lifetime -= dt;

        // ── 이동 (종류별) ──
        switch (s.MoveKind)
        {
            case ProjectileMoveKind.Straight:
                MoveStraight(ref s, dt);
                break;

            case ProjectileMoveKind.Homing:
                MoveHoming(ref s, dt);
                break;

            case ProjectileMoveKind.Boomerang:
                MoveBoomerang(ref s, dt);
                break;

            case ProjectileMoveKind.SplitOnExpiry:
                MoveStraight(ref s, dt); // 분열도 직선 비행
                break;

            case ProjectileMoveKind.Orbit:
                MoveOrbit(ref s, dt);
                break;
        }

        // ── 수명 만료 처리 ──
        if (s.Lifetime <= 0f)
        {
            OnLifetimeExpired(ref s, idx);
            return;
        }

        // ── 충돌 판정 (종류별) ──
        switch (s.HitKind)
        {
            case ProjectileHitKind.HitAndDie:
                CheckHitAndDie(ref s, idx);
                break;

            case ProjectileHitKind.Pierce:
                CheckPierce(ref s, idx);
                break;

            case ProjectileHitKind.AreaPeriodic:
                CheckAreaPeriodic(ref s, idx, dt);
                break;

            case ProjectileHitKind.HomingHit:
                CheckHomingHit(ref s, idx);
                break;

            case ProjectileHitKind.Bounce:
                CheckBounce(ref s, idx);
                break;

            // AreaOnExpiry는 수명 만료 시 처리 (위의 OnLifetimeExpired)
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  이동 로직
    // ══════════════════════════════════════════════════════════════

    private static void MoveStraight(ref ProjectileSlot s, float dt)
    {
        s.Position += s.Direction * s.Speed * dt;
    }

    private void MoveHoming(ref ProjectileSlot s, float dt)
    {
        // 타겟 유효성 확인
        GameObject target = GetTargetById(s.TargetInstanceId);
        if (target == null || !target.activeInHierarchy)
        {
            // 재탐색
            s.TargetInstanceId = FindNearestEnemyId(s.Position, s.EnemyMask, 20f);
            target = GetTargetById(s.TargetInstanceId);
        }

        if (target != null)
        {
            Vector2 toTarget = ((Vector2)target.transform.position - s.Position).normalized;

            // 부드러운 회전
            float maxTurn = s.HomingTurnSpeed * dt * Mathf.Deg2Rad;
            float currentAngle = Mathf.Atan2(s.Direction.y, s.Direction.x);
            float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x);
            float newAngle = Mathf.MoveTowardsAngle(
                currentAngle * Mathf.Rad2Deg,
                targetAngle * Mathf.Rad2Deg,
                s.HomingTurnSpeed * dt
            ) * Mathf.Deg2Rad;

            s.Direction = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle));
        }

        s.Position += s.Direction * s.Speed * dt;
    }

    private void MoveBoomerang(ref ProjectileSlot s, float dt)
    {
        float moveDist = s.Speed * dt;

        if (!s.Returning)
        {
            s.Position += s.Direction * moveDist;
            s.TraveledDist += moveDist;

            if (s.TraveledDist >= s.MaxDistance)
            {
                s.Returning = true;
            }
        }
        else
        {
            // 오너 방향으로 복귀
            GameObject owner = GetTargetById(s.OwnerInstanceId);
            if (owner != null)
            {
                Vector2 ownerPos = (Vector2)owner.transform.position;
                Vector2 toOwner = (ownerPos - s.Position).normalized;
                s.Direction = toOwner;
                s.Position += toOwner * moveDist;

                // 오너에 도달하면 소멸
                float distSqr = (ownerPos - s.Position).sqrMagnitude;
                if (distSqr < 0.5f * 0.5f)
                {
                    s.Lifetime = -1f; // 다음 프레임에 정리
                }
            }
            else
            {
                s.Lifetime = -1f;
            }
        }
    }

    private void MoveOrbit(ref ProjectileSlot s, float dt)
    {
        s.OrbitAngle += s.OrbitAngularSpeed * dt;

        GameObject owner = GetTargetById(s.OwnerInstanceId);
        Vector2 center = owner != null ? (Vector2)owner.transform.position : s.Position;

        s.Position = center + new Vector2(
            Mathf.Cos(s.OrbitAngle) * s.OrbitRadius,
            Mathf.Sin(s.OrbitAngle) * s.OrbitRadius
        );
    }

    // ══════════════════════════════════════════════════════════════
    //  충돌 판정 (Physics 최소화 — 거리 기반)
    // ══════════════════════════════════════════════════════════════

    private void CheckHitAndDie(ref ProjectileSlot s, int idx)
    {
        int count = OverlapAt(s.Position, s.HitRadius, s.EnemyMask);
        for (int i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (col == null) continue;

            int rootId = DamageUtil2D.GetRootId(col);
            if (IsInHitCache(ref s, rootId)) continue;

            if (DamageUtil2D.TryApplyDamage(col, s.Damage, s.Element))
            {
                DeactivateSlot(ref s, idx);
                return;
            }
        }
    }

    private void CheckPierce(ref ProjectileSlot s, int idx)
    {
        int count = OverlapAt(s.Position, s.HitRadius, s.EnemyMask);
        for (int i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (col == null) continue;

            int rootId = DamageUtil2D.GetRootId(col);
            if (IsInHitCache(ref s, rootId)) continue;

            if (DamageUtil2D.TryApplyDamage(col, s.Damage, s.Element))
            {
                AddToHitCache(ref s, rootId);
                s.CurrentHitCount++;

                if (s.CurrentHitCount >= s.MaxHitCount)
                {
                    DeactivateSlot(ref s, idx);
                    return;
                }
            }
        }
    }

    private void CheckAreaPeriodic(ref ProjectileSlot s, int idx, float dt)
    {
        s.HitTimer -= dt;
        if (s.HitTimer > 0f) return;

        s.HitTimer = s.HitInterval;

        int count = OverlapAt(s.Position, s.ExplosionRadius, s.EnemyMask);
        for (int i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (col == null) continue;
            DamageUtil2D.TryApplyDamage(col, s.Damage, s.Element);
        }
    }

    private void CheckHomingHit(ref ProjectileSlot s, int idx)
    {
        GameObject target = GetTargetById(s.TargetInstanceId);
        if (target == null) return;

        float distSqr = ((Vector2)target.transform.position - s.Position).sqrMagnitude;
        if (distSqr <= s.HitRadius * s.HitRadius)
        {
            DamageUtil2D.TryApplyDamage(target, s.Damage, s.Element);
            s.CurrentHitCount++;

            if (s.CurrentHitCount >= s.MaxHitCount)
            {
                DeactivateSlot(ref s, idx);
            }
            else
            {
                // 재탐색
                s.TargetInstanceId = FindNearestEnemyId(s.Position, s.EnemyMask, 20f);
            }
        }
    }

    private void CheckBounce(ref ProjectileSlot s, int idx)
    {
        int count = OverlapAt(s.Position, s.HitRadius, s.EnemyMask);
        for (int i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (col == null) continue;

            int rootId = DamageUtil2D.GetRootId(col);
            if (IsInHitCache(ref s, rootId)) continue;

            if (DamageUtil2D.TryApplyDamage(col, s.Damage, s.Element))
            {
                AddToHitCache(ref s, rootId);
                s.CurrentBounceCount++;

                if (s.CurrentBounceCount >= s.MaxBounceCount)
                {
                    DeactivateSlot(ref s, idx);
                    return;
                }

                // 다음 타겟으로 방향 전환
                int nextId = FindNearestEnemyIdExcluding(
                    s.Position, s.EnemyMask, 8f, ref s);
                GameObject next = GetTargetById(nextId);
                if (next != null)
                {
                    s.Direction = ((Vector2)next.transform.position - s.Position).normalized;
                }
                else
                {
                    DeactivateSlot(ref s, idx);
                }
                return;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  수명 만료 처리
    // ══════════════════════════════════════════════════════════════

    private void OnLifetimeExpired(ref ProjectileSlot s, int idx)
    {
        // 폭발형: 범위 데미지 + 분열
        if (s.HitKind == ProjectileHitKind.AreaOnExpiry)
        {
            // 범위 데미지
            int count = OverlapAt(s.Position, s.ExplosionRadius, s.EnemyMask);
            for (int i = 0; i < count; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null) continue;
                DamageUtil2D.TryApplyDamage(col, s.Damage, s.Element);
            }

            // VFX 콜백 (폭발 이펙트)
            OnExplosionVFX?.Invoke(s.Position, s.MoveKind);

            // 분열 큐잉
            if (s.MoveKind == ProjectileMoveKind.SplitOnExpiry
                && s.Generation < s.MaxGeneration)
            {
                _splitQueue.Enqueue(new SplitRequest
                {
                    ParentPosition      = s.Position,
                    ParentDirection     = s.Direction,
                    SplitAngleDeg       = s.SplitAngleDeg,
                    Speed               = s.SplitSpeed,
                    Lifetime            = s.SplitLifetime,
                    ExplosionRadius     = s.ExplosionRadius,
                    ExplosionDamage     = s.Damage,
                    EnemyMask           = s.EnemyMask,
                    Element             = s.Element,
                    ChildGeneration     = s.Generation + 1,
                    MaxGeneration       = s.MaxGeneration,
                    ChildSplitAngleDeg  = s.SplitAngleDeg,
                    ChildSplitSpeed     = s.SplitSpeed,
                    ChildSplitLifetime  = s.SplitLifetime,
                    HitRadius           = s.HitRadius,
                    VisualId            = s.VisualId,
                });
            }
        }

        DeactivateSlot(ref s, idx);
    }

    // ══════════════════════════════════════════════════════════════
    //  분열 대기열
    // ══════════════════════════════════════════════════════════════

    private void FlushSplitQueue()
    {
        while (_splitQueue.Count > 0)
        {
            var req = _splitQueue.Dequeue();
            SpawnSplitChild(req, +req.SplitAngleDeg);
            SpawnSplitChild(req, -req.SplitAngleDeg);
        }
    }

    private void SpawnSplitChild(SplitRequest req, float angleDeg)
    {
        Vector2 childDir = RotateVector2(req.ParentDirection, angleDeg);
        Vector2 spawnPos = req.ParentPosition + childDir * 0.15f;

        var template = new ProjectileSlot
        {
            MoveKind        = ProjectileMoveKind.SplitOnExpiry,
            HitKind         = ProjectileHitKind.AreaOnExpiry,
            Element         = req.Element,
            Position        = spawnPos,
            Direction       = childDir,
            Speed           = req.Speed,
            Lifetime        = req.Lifetime,
            Damage          = req.ExplosionDamage,
            HitRadius       = req.HitRadius,
            ExplosionRadius = req.ExplosionRadius,
            EnemyMask       = req.EnemyMask,
            Generation      = req.ChildGeneration,
            MaxGeneration   = req.MaxGeneration,
            SplitAngleDeg   = req.ChildSplitAngleDeg,
            SplitSpeed      = req.ChildSplitSpeed,
            SplitLifetime   = req.ChildSplitLifetime,
            VisualId        = req.VisualId,
            ViewId          = -1,
            VfxViewId       = -1,
        };

        Spawn(ref template);
    }

    // ══════════════════════════════════════════════════════════════
    //  VFX 콜백 (외부 시스템에서 구독)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 폭발 시 호출되는 콜백. VFX 매니저가 구독합니다.
    /// delegate가 아닌 static event → GC 0.
    /// </summary>
    public static event System.Action<Vector2, ProjectileMoveKind> OnExplosionVFX;

    // ══════════════════════════════════════════════════════════════
    //  뷰 동기화
    // ══════════════════════════════════════════════════════════════

    private void SyncViews()
    {
        if (viewPool == null) return;

        for (int i = 0; i < _slots.Length; i++)
        {
            ref var s = ref _slots[i];
            if (!s.Active) continue;
            if (s.ViewId < 0) continue;

            viewPool.SetPosition(s.ViewId, s.Position);

            // 방향에 따른 회전 (직선/호밍/바운스)
            if (s.MoveKind != ProjectileMoveKind.Orbit)
            {
                float angle = Mathf.Atan2(s.Direction.y, s.Direction.x) * Mathf.Rad2Deg;
                viewPool.SetRotation(s.ViewId, angle);
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  슬롯 비활성화
    // ══════════════════════════════════════════════════════════════

    private void DeactivateSlot(ref ProjectileSlot s, int idx)
    {
        if (!s.Active) return;

        // 뷰 반환
        if (viewPool != null && s.ViewId >= 0)
        {
            viewPool.Release(s.ViewId);
        }

        // 히트 캐시 영역 반환 (마지막 할당이면 되감기)
        if (s.HitCacheStartIndex + s.HitCacheCount == _hitCacheUsed)
        {
            _hitCacheUsed = s.HitCacheStartIndex;
        }

        s.Active = false;
        s.ViewId = -1;
        s.VfxViewId = -1;
        _activeCount = Mathf.Max(0, _activeCount - 1);
    }

    // ══════════════════════════════════════════════════════════════
    //  히트 캐시 (중복 피격 방지)
    // ══════════════════════════════════════════════════════════════

    private bool IsInHitCache(ref ProjectileSlot s, int instanceId)
    {
        int end = s.HitCacheStartIndex + s.HitCacheCount;
        for (int i = s.HitCacheStartIndex; i < end; i++)
        {
            if (_hitCacheBuffer[i] == instanceId) return true;
        }
        return false;
    }

    private void AddToHitCache(ref ProjectileSlot s, int instanceId)
    {
        int end = s.HitCacheStartIndex + s.HitCacheCount;
        if (end >= _hitCacheBuffer.Length) return; // 오버플로 방지
        _hitCacheBuffer[end] = instanceId;
        s.HitCacheCount++;
        if (end + 1 > _hitCacheUsed) _hitCacheUsed = end + 1;
    }

    // ══════════════════════════════════════════════════════════════
    //  물리 쿼리 (GC 0)
    // ══════════════════════════════════════════════════════════════

    private int OverlapAt(Vector2 center, float radius, LayerMask mask)
    {
        if (radius <= 0f) return 0;
        return Physics2D.OverlapCircleNonAlloc(center, radius, _overlapBuffer, mask);
    }

    // ══════════════════════════════════════════════════════════════
    //  타겟 탐색 헬퍼
    // ══════════════════════════════════════════════════════════════

    private int FindNearestEnemyId(Vector2 from, LayerMask mask, float searchRadius)
    {
        int count = OverlapAt(from, searchRadius, mask);
        float bestDistSqr = float.MaxValue;
        int bestId = 0;

        for (int i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (col == null) continue;

            var hp = col.GetComponentInParent<IDamageable2D>();
            if (hp == null || hp.IsDead) continue;

            float distSqr = ((Vector2)col.transform.position - from).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                bestId = col.gameObject.GetInstanceID();
            }
        }

        return bestId;
    }

    private int FindNearestEnemyIdExcluding(
        Vector2 from, LayerMask mask, float searchRadius, ref ProjectileSlot s)
    {
        int count = OverlapAt(from, searchRadius, mask);
        float bestDistSqr = float.MaxValue;
        int bestId = 0;

        for (int i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (col == null) continue;

            int rootId = DamageUtil2D.GetRootId(col);
            if (IsInHitCache(ref s, rootId)) continue;

            var hp = col.GetComponentInParent<IDamageable2D>();
            if (hp == null || hp.IsDead) continue;

            float distSqr = ((Vector2)col.transform.position - from).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                bestId = rootId;
            }
        }

        return bestId;
    }

    private static GameObject GetTargetById(int instanceId)
    {
        if (instanceId == 0) return null;

        // EnemyRegistryExtensions: Dictionary 기반 O(1) 조회
        if (EnemyRegistryExtensions.TryGetById(instanceId, out var member))
            return member.gameObject;

        return null;
    }

    // ══════════════════════════════════════════════════════════════
    //  유틸리티
    // ══════════════════════════════════════════════════════════════

    private int FindFreeSlot()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].Active) return i;
        }
        return -1;
    }

    private static Vector2 RotateVector2(Vector2 v, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // ══════════════════════════════════════════════════════════════
    //  디버그
    // ══════════════════════════════════════════════════════════════

    #if UNITY_EDITOR
    [Header("디버그")]
    [SerializeField] private bool showGizmos = false;

    private void OnDrawGizmos()
    {
        if (!showGizmos || _slots == null) return;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].Active) continue;

            Vector3 pos = (Vector3)(Vector2)_slots[i].Position;

            // 투사체 위치
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pos, _slots[i].HitRadius);

            // 폭발 범위
            if (_slots[i].ExplosionRadius > 0f)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
                Gizmos.DrawWireSphere(pos, _slots[i].ExplosionRadius);
            }

            // 이동 방향
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos, pos + (Vector3)(Vector2)(_slots[i].Direction * 0.5f));
        }
    }
    #endif
}