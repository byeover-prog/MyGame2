// UTF-8
// Assets/_Game/Scripts/Combat/Projectiles/GameProjectileManager.cs
using UnityEngine;

/// <summary>
/// 공용 투사체 시뮬레이션 매니저.
/// 
/// [아키텍처]
/// - 투사체 하나 = MonoBehaviour 하나 금지
/// - 모든 투사체 상태를 struct 배열로 관리
/// - Update 1회에 전체 투사체 일괄 시뮬레이션
/// - 분열은 즉시 재귀 금지 → SplitRequest 큐 → FlushSplits
/// - 비주얼은 GameProjectileViewPool이 담당
/// - Physics2D Collider 없음 → Manager가 직접 충돌 판정
/// 
/// [성능 보장]
/// - Dense array + free list + swap-back remove (O(1) 생성/삭제)
/// - GC 0B/frame (핫패스에서 new/LINQ/람다/문자열 금지)
/// - ProfilerMarker로 구간 계측
/// </summary>
[DisallowMultipleComponent]
public sealed class GameProjectileManager : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // Singleton
    // ══════════════════════════════════════════════════════

    public static GameProjectileManager Instance { get; private set; }

    // ══════════════════════════════════════════════════════
    // Inspector
    // ══════════════════════════════════════════════════════

    [Header("용량")]
    [Tooltip("동시 활성 투사체 하드 캡")]
    [SerializeField] private int maxProjectiles = 256;

    [Header("DarkOrb 설정")]
    [Tooltip("동시 활성 DarkOrb 하드 캡 (Root + 자식 합산)")]
    [SerializeField] private int maxDarkOrbs = 40;

    [Header("VFX 예산")]
    [Tooltip("프레임당 분열 VFX 최대 수")]
    [SerializeField] private int splitVfxBudgetPerFrame = 4;

    [Header("폭발 VFX")]
    [Tooltip("DarkOrb 폭발 VFX 프리팹 (기존 eff_weapon_darkorb_explosion)")]
    [SerializeField] private GameObject darkOrbExplosionVfxPrefab;

    [Tooltip("DarkOrb 본체 VFX 프리팹 (기존 eff_weapon_darkorb)")]
    [SerializeField] private GameObject darkOrbBodyVfxPrefab;

    [Header("참조")]
    [SerializeField] private GameProjectileViewPool viewPool;

    [Header("충돌 설정")]
    [SerializeField] private LayerMask defaultEnemyMask;

    // ══════════════════════════════════════════════════════
    // 공통 상태 struct
    // ══════════════════════════════════════════════════════

    public struct ProjectileState
    {
        public bool Active;
        public GameProjectileKind Kind;

        public int DenseIndex;
        public int ViewId;
        public int OwnerInstanceId;

        public Vector2 Position;
        public Vector2 Direction; // normalized
        public float Speed;
        public float Lifetime;
        public float CollisionRadius;
        public float ExplosionRadius;

        public int Damage;
        public int PierceLeft;

        public byte Generation;
        public byte MaxGeneration;

        public int SplitChildrenCount;
        public float SplitAngleDeg;
        public float SplitSpeed;
        public float SplitLifetime;
        public int SplitDamage;

        public bool SplitQueued;
        public bool PendingDespawn;

        public float CollisionGraceRemaining;

        public GameProjectileFlags Flags;

        public LayerMask EnemyMask;
    }

    private struct SplitRequest
    {
        public int ParentId;
        public Vector2 Position;
        public Vector2 ParentDirection;
        public byte NextGeneration;
        public byte MaxGeneration;
        public int ChildrenCount;
        public float AngleDeg;
        public float Speed;
        public float Lifetime;
        public int Damage;
        public float CollisionRadius;
        public float ExplosionRadius;
        public float CollisionGrace;
        public LayerMask EnemyMask;
        public int OwnerInstanceId;
        public GameProjectileFlags Flags;
    }

    // ══════════════════════════════════════════════════════
    // Dense Array + Free List
    // ══════════════════════════════════════════════════════

    private ProjectileState[] _states;
    private int[] _activeIds;
    private int[] _freeIds;
    private int _activeCount;
    private int _freeTop;

    // 분열 큐
    private SplitRequest[] _splitQueue;
    private int _splitCount;

    // 물리 쿼리 버퍼 (재사용, new 금지)
    private readonly Collider2D[] _overlapHits = new Collider2D[32];
    private ContactFilter2D _enemyFilter;
    private bool _filterReady;

    // DarkOrb 카운트
    private int _darkOrbCount;

    // 분열 방향 캐시 (매 프레임 trig 남발 방지)
    private static readonly float[] _cosCache = new float[360];
    private static readonly float[] _sinCache = new float[360];
    private static bool _trigCacheReady;

    // 프레임별 VFX 카운트
    private int _splitVfxThisFrame;

    // ══════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════

    public int ActiveProjectileCount => _activeCount;
    public int ActiveDarkOrbCount => _darkOrbCount;

    /// <summary>DarkOrb 발사. 성공 시 true.</summary>
    public bool TrySpawnDarkOrb(in DarkOrbProjectileSpec spec, Vector2 spawnPos, Vector2 targetPos, int ownerInstanceId)
    {
        if (_darkOrbCount >= maxDarkOrbs) return false;
        if (_freeTop <= 0) return false;

        Vector2 dir = (targetPos - spawnPos);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        int id = AllocSlot();
        ref var s = ref _states[id];

        s.Active = true;
        s.Kind = GameProjectileKind.DarkOrb;
        s.OwnerInstanceId = ownerInstanceId;
        s.Position = spawnPos;
        s.Direction = dir;
        s.Speed = spec.Speed;
        s.Lifetime = spec.Lifetime;
        s.CollisionRadius = spec.CollisionRadius;
        s.ExplosionRadius = spec.ExplosionRadius;
        s.Damage = spec.Damage;
        s.PierceLeft = 0;
        s.Generation = 0;
        s.MaxGeneration = spec.MaxGeneration;
        s.SplitChildrenCount = spec.SplitChildrenCount > 0 ? spec.SplitChildrenCount : 2;
        s.SplitAngleDeg = spec.SplitAngleDeg;
        s.SplitSpeed = spec.SplitSpeed;
        s.SplitLifetime = spec.SplitLifetime;
        s.SplitDamage = spec.SplitDamage;
        s.SplitQueued = false;
        s.PendingDespawn = false;
        s.CollisionGraceRemaining = spec.CollisionGracePeriod;
        s.EnemyMask = spec.EnemyMask;

        s.Flags = GameProjectileFlags.CanSplit | GameProjectileFlags.SplitOnContact | GameProjectileFlags.AreaDamage;

        // 뷰 연결
        s.ViewId = viewPool != null
            ? viewPool.Acquire(GameProjectileKind.DarkOrb, spawnPos, spec.OrbAlpha)
            : GameProjectileViewPool.InvalidId;

        s.DenseIndex = _activeCount;
        _activeIds[_activeCount++] = id;
        _darkOrbCount++;

        return true;
    }

    /// <summary>직선형 투사체 발사 (2차 이식용 API 뼈대).</summary>
    public bool TrySpawnLinear(in LinearProjectileSpec spec, Vector2 spawnPos, Vector2 dir, int ownerInstanceId)
    {
        // 아직 내부 구현 미완성. API 뼈대만 존재.
        // 2차 이식 시 여기에 구현.
        return false;
    }

    /// <summary>특정 투사체 제거.</summary>
    public void DespawnProjectile(int projectileId)
    {
        if (projectileId < 0 || projectileId >= _states.Length) return;
        if (!_states[projectileId].Active) return;
        DespawnInternal(projectileId);
    }

    /// <summary>전체 투사체 제거.</summary>
    public void ClearAllProjectiles()
    {
        // 역순으로 제거 (swap-back 안전)
        for (int i = _activeCount - 1; i >= 0; i--)
            DespawnInternal(_activeIds[i]);
    }

    // ══════════════════════════════════════════════════════
    // Lifecycle
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        InitArrays();
        InitTrigCache();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        using (GameProjectileProfiler.Update.Auto())
        {
            float dt = Time.deltaTime;
            _splitVfxThisFrame = 0;

            SimulateProjectiles(dt);
            FlushSplits();
            SyncViews();
        }
    }

    // ══════════════════════════════════════════════════════
    // Init
    // ══════════════════════════════════════════════════════

    private void InitArrays()
    {
        int cap = Mathf.Max(32, maxProjectiles);

        _states = new ProjectileState[cap];
        _activeIds = new int[cap];
        _freeIds = new int[cap];
        _splitQueue = new SplitRequest[cap];

        _freeTop = cap;
        for (int i = 0; i < cap; i++)
            _freeIds[i] = cap - 1 - i;

        _activeCount = 0;
        _splitCount = 0;
        _darkOrbCount = 0;
    }

    private static void InitTrigCache()
    {
        if (_trigCacheReady) return;
        for (int i = 0; i < 360; i++)
        {
            float rad = i * Mathf.Deg2Rad;
            _cosCache[i] = Mathf.Cos(rad);
            _sinCache[i] = Mathf.Sin(rad);
        }
        _trigCacheReady = true;
    }

    private void EnsureFilter(LayerMask mask)
    {
        // 간단 캐시: 마스크가 같으면 재사용
        if (_filterReady && _enemyFilter.layerMask == mask) return;
        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(mask);
        _enemyFilter.useTriggers = true;
        _filterReady = true;
    }

    // ══════════════════════════════════════════════════════
    // Simulate
    // ══════════════════════════════════════════════════════

    private void SimulateProjectiles(float dt)
    {
        using (GameProjectileProfiler.Simulate.Auto())
        {
            // 역순 순회 (swap-back remove 안전)
            for (int i = _activeCount - 1; i >= 0; i--)
            {
                int id = _activeIds[i];
                ref var s = ref _states[id];

                switch (s.Kind)
                {
                    case GameProjectileKind.DarkOrb:
                        ProcessDarkOrb(id, ref s, dt);
                        break;
                    case GameProjectileKind.Linear:
                        ProcessLinear(id, ref s, dt);
                        break;
                }

                // PendingDespawn 처리
                if (s.PendingDespawn)
                    DespawnInternal(id);
            }
        }
    }

    private void ProcessDarkOrb(int id, ref ProjectileState s, float dt)
    {
        // 1. 이동
        s.Position += s.Direction * (s.Speed * dt);

        // 2. 수명 감소
        s.Lifetime -= dt;

        // 3. 충돌 유예 감소
        if (s.CollisionGraceRemaining > 0f)
            s.CollisionGraceRemaining -= dt;

        // 4. 수명 만료 → 폭발 분열
        if (s.Lifetime <= 0f)
        {
            ExplodeDarkOrb(id, ref s);
            return;
        }

        // 5. 적 접촉 판정 (유예 중이면 스킵)
        if (s.CollisionGraceRemaining <= 0f)
        {
            using (GameProjectileProfiler.Collision.Auto())
            {
                if (CheckDarkOrbContact(ref s))
                {
                    ExplodeDarkOrb(id, ref s);
                    return;
                }
            }
        }
    }

    private void ProcessLinear(int id, ref ProjectileState s, float dt)
    {
        // 2차 이식용 뼈대
        s.Position += s.Direction * (s.Speed * dt);
        s.Lifetime -= dt;
        if (s.Lifetime <= 0f)
            s.PendingDespawn = true;
    }

    // ══════════════════════════════════════════════════════
    // DarkOrb 충돌/폭발
    // ══════════════════════════════════════════════════════

    /// <summary>비행 중 적 접촉 판정. Collider 없이 Manager가 직접 수행.</summary>
    private bool CheckDarkOrbContact(ref ProjectileState s)
    {
        EnsureFilter(s.EnemyMask);

        int count = Physics2D.OverlapCircle(
            s.Position, s.CollisionRadius, _enemyFilter, _overlapHits);

        return count > 0;
    }

    /// <summary>폭발: 범위 데미지 + 분열 큐잉 + 본체 제거 예약.</summary>
    private void ExplodeDarkOrb(int id, ref ProjectileState s)
    {
        if (s.PendingDespawn || s.SplitQueued) return;

        // 1. 범위 데미지 (Physics2D.OverlapCircle)
        if (s.ExplosionRadius > 0.01f)
        {
            EnsureFilter(s.EnemyMask);

            int hitCount = Physics2D.OverlapCircle(
                s.Position, s.ExplosionRadius, _enemyFilter, _overlapHits);

            for (int i = 0; i < hitCount; i++)
            {
                var col = _overlapHits[i];
                if (col == null) continue;
                DamageUtil2D.ApplyDamage(col, s.Damage);
            }
        }

        // 2. 분열 큐잉 (즉시 생성 금지)
        if (s.Generation < s.MaxGeneration &&
            (s.Flags & GameProjectileFlags.CanSplit) != 0)
        {
            QueueSplit(id, ref s);
        }

        // 3. 폭발 VFX
        EmitExplosionVfx(s.Position);

        // 4. 본체 제거 예약
        s.PendingDespawn = true;
    }

    // ══════════════════════════════════════════════════════
    // Split Queue
    // ══════════════════════════════════════════════════════

    private void QueueSplit(int parentId, ref ProjectileState s)
    {
        if (s.SplitQueued) return; // 같은 부모 중복 방지
        if (_splitCount >= _splitQueue.Length) return; // 큐 오버플로 방지

        s.SplitQueued = true;

        ref var req = ref _splitQueue[_splitCount++];
        req.ParentId = parentId;
        req.Position = s.Position;
        req.ParentDirection = s.Direction;
        req.NextGeneration = (byte)(s.Generation + 1);
        req.MaxGeneration = s.MaxGeneration;
        req.ChildrenCount = s.SplitChildrenCount;
        req.AngleDeg = s.SplitAngleDeg;
        req.Speed = s.SplitSpeed;
        req.Lifetime = s.SplitLifetime;
        req.Damage = s.SplitDamage > 0 ? s.SplitDamage : s.Damage;
        req.CollisionRadius = s.CollisionRadius;
        req.ExplosionRadius = s.ExplosionRadius;
        req.CollisionGrace = s.CollisionGraceRemaining > 0 ? s.CollisionGraceRemaining : 0.05f;
        req.EnemyMask = s.EnemyMask;
        req.OwnerInstanceId = s.OwnerInstanceId;
        req.Flags = s.Flags;
    }

    private void FlushSplits()
    {
        using (GameProjectileProfiler.FlushSplits.Auto())
        {
            for (int i = 0; i < _splitCount; i++)
            {
                ref readonly var req = ref _splitQueue[i];
                SpawnSplitChildren(in req);
            }
            _splitCount = 0;
        }
    }

    private void SpawnSplitChildren(in SplitRequest req)
    {
        int count = Mathf.Max(1, req.ChildrenCount);
        float halfSpread = req.AngleDeg;

        for (int c = 0; c < count; c++)
        {
            if (_freeTop <= 0) break; // 하드 캡
            if (_darkOrbCount >= maxDarkOrbs) break;

            // 분열 방향 계산
            float angleDeg;
            if (count == 2)
                angleDeg = (c == 0) ? +halfSpread : -halfSpread;
            else
                angleDeg = Mathf.Lerp(-halfSpread, +halfSpread, (float)c / Mathf.Max(1, count - 1));

            Vector2 childDir = RotateDir(req.ParentDirection, angleDeg);

            int id = AllocSlot();
            ref var s = ref _states[id];

            s.Active = true;
            s.Kind = GameProjectileKind.DarkOrb;
            s.OwnerInstanceId = req.OwnerInstanceId;
            s.Position = req.Position + childDir * 0.4f; // spawnEps
            s.Direction = childDir;
            s.Speed = req.Speed;
            s.Lifetime = req.Lifetime;
            s.CollisionRadius = req.CollisionRadius;
            s.ExplosionRadius = req.ExplosionRadius;
            s.Damage = req.Damage;
            s.PierceLeft = 0;
            s.Generation = req.NextGeneration;
            s.MaxGeneration = req.MaxGeneration;
            s.SplitChildrenCount = req.ChildrenCount;
            s.SplitAngleDeg = req.AngleDeg;
            s.SplitSpeed = req.Speed;
            s.SplitLifetime = req.Lifetime;
            s.SplitDamage = req.Damage;
            s.SplitQueued = false;
            s.PendingDespawn = false;
            s.CollisionGraceRemaining = req.CollisionGrace;
            s.EnemyMask = req.EnemyMask;
            s.Flags = req.Flags;

            // 뷰 연결
            s.ViewId = viewPool != null
                ? viewPool.Acquire(GameProjectileKind.DarkOrb, s.Position, 0.55f)
                : GameProjectileViewPool.InvalidId;

            s.DenseIndex = _activeCount;
            _activeIds[_activeCount++] = id;
            _darkOrbCount++;

            // 분열 VFX (예산 제한)
            EmitSplitVfx(s.Position);
        }
    }

    // ══════════════════════════════════════════════════════
    // Despawn (swap-back remove)
    // ══════════════════════════════════════════════════════

    private void DespawnInternal(int id)
    {
        ref var s = ref _states[id];
        if (!s.Active) return;

        // 뷰 반환
        if (viewPool != null && s.ViewId != GameProjectileViewPool.InvalidId)
            viewPool.Release(s.ViewId);

        // DarkOrb 카운트 감소
        if (s.Kind == GameProjectileKind.DarkOrb)
            _darkOrbCount--;

        // swap-back remove
        int removeIndex = s.DenseIndex;
        int lastIndex = _activeCount - 1;

        if (removeIndex != lastIndex)
        {
            int lastId = _activeIds[lastIndex];
            _activeIds[removeIndex] = lastId;
            _states[lastId].DenseIndex = removeIndex;
        }
        _activeCount--;

        // 슬롯 초기화 + free list 반환
        s = default;
        _freeIds[_freeTop++] = id;
    }

    private int AllocSlot()
    {
        return _freeIds[--_freeTop];
    }

    // ══════════════════════════════════════════════════════
    // View Sync
    // ══════════════════════════════════════════════════════

    private void SyncViews()
    {
        if (viewPool == null) return;

        using (GameProjectileProfiler.SyncViews.Auto())
        {
            for (int i = 0; i < _activeCount; i++)
            {
                int id = _activeIds[i];
                ref var s = ref _states[id];

                if (s.ViewId != GameProjectileViewPool.InvalidId)
                    viewPool.SetPosition(s.ViewId, s.Position);
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // VFX
    // ══════════════════════════════════════════════════════

    private void EmitExplosionVfx(Vector2 pos)
    {
        using (GameProjectileProfiler.EmitVfx.Auto())
        {
            if (darkOrbExplosionVfxPrefab != null)
                VFXSpawner.Spawn(darkOrbExplosionVfxPrefab, pos, Quaternion.identity, 2f);
        }
    }

    private void EmitSplitVfx(Vector2 pos)
    {
        // 프레임당 예산 제한
        if (_splitVfxThisFrame >= splitVfxBudgetPerFrame) return;
        _splitVfxThisFrame++;

        if (darkOrbBodyVfxPrefab != null)
            VFXSpawner.Spawn(darkOrbBodyVfxPrefab, pos, Quaternion.identity, 1f);
    }

    // ══════════════════════════════════════════════════════
    // Util (GC 0)
    // ══════════════════════════════════════════════════════

    private static Vector2 RotateDir(Vector2 dir, float angleDeg)
    {
        // 정수 각도면 캐시 사용, 아니면 직접 계산
        int intAngle = Mathf.RoundToInt(angleDeg) % 360;
        if (intAngle < 0) intAngle += 360;

        float cos, sin;
        float frac = angleDeg - Mathf.RoundToInt(angleDeg);
        if (frac * frac < 0.01f && intAngle >= 0 && intAngle < 360)
        {
            cos = _cosCache[intAngle];
            sin = _sinCache[intAngle];
        }
        else
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            cos = Mathf.Cos(rad);
            sin = Mathf.Sin(rad);
        }

        return new Vector2(
            dir.x * cos - dir.y * sin,
            dir.x * sin + dir.y * cos);
    }
}