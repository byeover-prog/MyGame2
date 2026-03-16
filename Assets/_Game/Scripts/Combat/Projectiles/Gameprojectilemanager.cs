using UnityEngine;

/// <summary>
/// 공용 투사체 시뮬레이션 매니저.
///
/// [v6 설계도 기준]
/// - 다크오브는 "수명 만료형 폭발 구체". 비행 중 접촉 판정 없음.
/// - 폭발 시에만 OverlapCircle 1회 → 범위 데미지.
/// - 분열은 depth 기반 재귀 트리. 부모 1개 폭발 → 자식 2개 (±고정각).
/// - 1→2→4→8 구조. splitCount가 아닌 maxDepth 개념.
/// - SplitRequest 큐로 즉시 재귀 방지.
/// - VFX는 폭발 시점에 명시 호출 (OnDisable 폭발 금지).
///
/// [성능]
/// - 비행 중 OverlapCircle = 0회
/// - 폭발 시에만 depth당 1회 (Lv4 최대: 루트1+자식2+손자4+증손자8 = 15회, 하지만 프레임 분산)
/// - Dense array + free list + swap-back (GC 0)
/// </summary>
[DisallowMultipleComponent]
public sealed class GameProjectileManager : MonoBehaviour
{
    public static GameProjectileManager Instance { get; private set; }

    [Header("용량")]
    [SerializeField] private int maxProjectiles = 128;
    [SerializeField] private int maxDarkOrbs = 60;

    [Header("VFX")]
    [SerializeField] private GameObject darkOrbExplosionVfxPrefab;
    [SerializeField] private GameObject darkOrbBodyVfxPrefab;

    [Header("참조")]
    [SerializeField] private GameProjectileViewPool viewPool;

    // ══════════════════════════════════════════════════════
    // State
    // ══════════════════════════════════════════════════════

    public struct ProjectileState
    {
        public bool Active;
        public GameProjectileKind Kind;
        public int DenseIndex;
        public int ViewId;
        public int OwnerInstanceId;

        // 이동
        public Vector2 Position;
        public Vector2 Direction;   // normalized
        public float Speed;
        public float Lifetime;      // 0 이하 → 폭발

        // 폭발
        public float ExplosionRadius;
        public int Damage;
        public LayerMask EnemyMask;

        // 재귀 트리
        public int Depth;           // 현재 깊이 (루트=1)
        public int MaxDepth;        // 최대 깊이 (1=분열없음, 4=1→2→4→8)
        public float SplitAngleDeg;
        public float SplitSpeed;
        public float SplitLifetime;
        public int SplitDamage;

        public bool PendingDespawn;
    }

    private struct SplitRequest
    {
        public Vector2 Position;
        public Vector2 ParentDirection;
        public int NextDepth;
        public int MaxDepth;
        public float AngleDeg;
        public float Speed;
        public float Lifetime;
        public int Damage;
        public float ExplosionRadius;
        public float SplitAngleDeg;
        public float SplitSpeed;
        public float SplitLifetime;
        public int SplitDamage;
        public LayerMask EnemyMask;
        public int OwnerInstanceId;
    }

    // ══════════════════════════════════════════════════════
    // Arrays
    // ══════════════════════════════════════════════════════

    private ProjectileState[] _states;
    private int[] _activeIds;
    private int[] _freeIds;
    private int _activeCount;
    private int _freeTop;

    private SplitRequest[] _splitQueue;
    private int _splitCount;

    private readonly Collider2D[] _overlapHits = new Collider2D[32];
    private ContactFilter2D _enemyFilter;
    private bool _filterReady;

    private int _darkOrbCount;

    private static readonly float[] _cosCache = new float[360];
    private static readonly float[] _sinCache = new float[360];
    private static bool _trigCacheReady;

    // ══════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════

    public int ActiveProjectileCount => _activeCount;
    public int ActiveDarkOrbCount => _darkOrbCount;

    /// <summary>
    /// DarkOrb 루트 1개 발사.
    /// 이동 → 수명 만료 → 폭발 → 자식 2개 (depth 허용 시).
    /// </summary>
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
        s.ExplosionRadius = spec.ExplosionRadius;
        s.Damage = spec.Damage;
        s.EnemyMask = spec.EnemyMask;

        // 재귀 트리
        s.Depth = 1;                // 루트 = 깊이 1
        s.MaxDepth = spec.MaxDepth;
        s.SplitAngleDeg = spec.SplitAngleDeg;
        s.SplitSpeed = spec.SplitSpeed;
        s.SplitLifetime = spec.SplitLifetime;
        s.SplitDamage = spec.SplitDamage;

        s.PendingDespawn = false;

        // 뷰 연결
        s.ViewId = viewPool != null
            ? viewPool.Acquire(GameProjectileKind.DarkOrb, spawnPos, spec.OrbAlpha)
            : GameProjectileViewPool.InvalidId;

        s.DenseIndex = _activeCount;
        _activeIds[_activeCount++] = id;
        _darkOrbCount++;
        return true;
    }

    public bool TrySpawnLinear(in LinearProjectileSpec spec, Vector2 spawnPos, Vector2 dir, int ownerInstanceId)
    {
        return false; // 2차 이식용
    }

    public void DespawnProjectile(int projectileId)
    {
        if (projectileId < 0 || projectileId >= _states.Length) return;
        if (!_states[projectileId].Active) return;
        DespawnInternal(projectileId);
    }

    public void ClearAllProjectiles()
    {
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
        Debug.Log($"<color=lime>[GameProjectileManager] ★★★ v6 설계도 기준 ★★★ viewPool={(viewPool != null ? "연결됨" : "NULL!")}</color>", this);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        SimulateProjectiles(dt);
        FlushSplits();
        SyncViews();
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
        if (_filterReady && _enemyFilter.layerMask == mask) return;
        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(mask);
        _enemyFilter.useTriggers = true;
        _filterReady = true;
    }

    // ══════════════════════════════════════════════════════
    // Simulate — 접촉 판정 없음. 수명 만료만 체크.
    // ══════════════════════════════════════════════════════

    private void SimulateProjectiles(float dt)
    {
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

            if (s.PendingDespawn)
                DespawnInternal(id);
        }
    }

    /// <summary>
    /// 다크오브 프레임 처리.
    /// 이동 → 수명 감소 → 수명 ≤ 0이면 폭발. 그게 전부.
    /// 비행 중 접촉 판정 없음.
    /// </summary>
    private void ProcessDarkOrb(int id, ref ProjectileState s, float dt)
    {
        // 1. 이동
        s.Position += s.Direction * (s.Speed * dt);

        // 2. 수명 감소
        s.Lifetime -= dt;

        // 3. 수명 만료 → 폭발
        if (s.Lifetime <= 0f)
            ExplodeDarkOrb(id, ref s);
    }

    private void ProcessLinear(int id, ref ProjectileState s, float dt)
    {
        s.Position += s.Direction * (s.Speed * dt);
        s.Lifetime -= dt;
        if (s.Lifetime <= 0f)
            s.PendingDespawn = true;
    }

    // ══════════════════════════════════════════════════════
    // 폭발 — 범위 데미지 1회 + 재귀 분열 큐잉
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 폭발: 범위 데미지 → 자식 2개 큐잉 (depth 허용 시) → VFX → 제거.
    /// OverlapCircle은 여기서만, 폭발 시 1회만 발생.
    /// </summary>
    private void ExplodeDarkOrb(int id, ref ProjectileState s)
    {
        if (s.PendingDespawn) return;

        // 1. 범위 데미지 (OverlapCircle 1회)
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

        // 2. 재귀 분열 큐잉 (depth < maxDepth이면 자식 2개)
        if (s.Depth < s.MaxDepth)
            QueueSplit(ref s);

        // 3. 폭발 VFX (명시 호출)
        EmitExplosionVfx(s.Position);

        // 4. 본체 제거 예약
        s.PendingDespawn = true;
    }

    // ══════════════════════════════════════════════════════
    // 분열 — depth 기반 재귀 트리. 부모 1개 → 자식 2개.
    // ══════════════════════════════════════════════════════

    private void QueueSplit(ref ProjectileState s)
    {
        if (_splitCount >= _splitQueue.Length) return;

        ref var req = ref _splitQueue[_splitCount++];
        req.Position = s.Position;
        req.ParentDirection = s.Direction;
        req.NextDepth = s.Depth + 1;
        req.MaxDepth = s.MaxDepth;
        req.AngleDeg = s.SplitAngleDeg;
        req.Speed = s.SplitSpeed > 0f ? s.SplitSpeed : s.Speed;
        req.Lifetime = s.SplitLifetime > 0f ? s.SplitLifetime : s.Lifetime;
        req.Damage = s.SplitDamage > 0 ? s.SplitDamage : s.Damage;
        req.ExplosionRadius = s.ExplosionRadius;
        req.SplitAngleDeg = s.SplitAngleDeg;
        req.SplitSpeed = s.SplitSpeed;
        req.SplitLifetime = s.SplitLifetime;
        req.SplitDamage = s.SplitDamage;
        req.EnemyMask = s.EnemyMask;
        req.OwnerInstanceId = s.OwnerInstanceId;
    }

    private void FlushSplits()
    {
        for (int i = 0; i < _splitCount; i++)
        {
            ref readonly var req = ref _splitQueue[i];
            SpawnChildren(in req);
        }
        _splitCount = 0;
    }

    /// <summary>
    /// 부모 1개 폭발 → 자식 2개 생성.
    /// 방향: 부모 진행방향 기준 +고정각 / -고정각.
    /// 자식도 동일 규칙으로 수명 만료 → 폭발 → 다시 분열.
    /// </summary>
    private void SpawnChildren(in SplitRequest req)
    {
        // 항상 2개 (재귀 트리 규칙)
        for (int c = 0; c < 2; c++)
        {
            if (_freeTop <= 0) break;
            if (_darkOrbCount >= maxDarkOrbs) break;

            // 부모 진행방향 기준 +각 / -각
            float angle = (c == 0) ? +req.AngleDeg : -req.AngleDeg;
            Vector2 childDir = RotateDir(req.ParentDirection, angle);

            int id = AllocSlot();
            ref var s = ref _states[id];

            s.Active = true;
            s.Kind = GameProjectileKind.DarkOrb;
            s.OwnerInstanceId = req.OwnerInstanceId;
            s.Position = req.Position + childDir * 0.4f; // 약간 밀어서 겹침 방지
            s.Direction = childDir;
            s.Speed = req.Speed;
            s.Lifetime = req.Lifetime;
            s.ExplosionRadius = req.ExplosionRadius;
            s.Damage = req.Damage;
            s.EnemyMask = req.EnemyMask;

            // 재귀 트리 정보 전달
            s.Depth = req.NextDepth;
            s.MaxDepth = req.MaxDepth;
            s.SplitAngleDeg = req.SplitAngleDeg;
            s.SplitSpeed = req.SplitSpeed;
            s.SplitLifetime = req.SplitLifetime;
            s.SplitDamage = req.SplitDamage;

            s.PendingDespawn = false;

            // 뷰 연결
            s.ViewId = viewPool != null
                ? viewPool.Acquire(GameProjectileKind.DarkOrb, s.Position, 0.55f)
                : GameProjectileViewPool.InvalidId;

            s.DenseIndex = _activeCount;
            _activeIds[_activeCount++] = id;
            _darkOrbCount++;

            // 자식 바디 VFX (선택)
            EmitBodyVfx(s.Position);
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
        int vid = s.ViewId;
        s.ViewId = GameProjectileViewPool.InvalidId;
        if (viewPool != null && vid != GameProjectileViewPool.InvalidId)
            viewPool.Release(vid);

        if (s.Kind == GameProjectileKind.DarkOrb)
            _darkOrbCount--;

        // swap-back
        int removeIndex = s.DenseIndex;
        int lastIndex = _activeCount - 1;
        if (removeIndex != lastIndex)
        {
            int lastId = _activeIds[lastIndex];
            _activeIds[removeIndex] = lastId;
            _states[lastId].DenseIndex = removeIndex;
        }
        _activeCount--;

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
        for (int i = 0; i < _activeCount; i++)
        {
            int id = _activeIds[i];
            ref var s = ref _states[id];
            if (s.ViewId != GameProjectileViewPool.InvalidId)
                viewPool.SetPosition(s.ViewId, s.Position);
        }
    }

    // ══════════════════════════════════════════════════════
    // VFX — 폭발 시점 명시 호출만. OnDisable 폭발 금지.
    // ══════════════════════════════════════════════════════

    private void EmitExplosionVfx(Vector2 pos)
    {
        if (darkOrbExplosionVfxPrefab != null)
            VFXSpawner.Spawn(darkOrbExplosionVfxPrefab, pos, Quaternion.identity, 2f);
    }

    private void EmitBodyVfx(Vector2 pos)
    {
        if (darkOrbBodyVfxPrefab != null)
            VFXSpawner.Spawn(darkOrbBodyVfxPrefab, pos, Quaternion.identity, 0.8f);
    }

    // ══════════════════════════════════════════════════════
    // Util (GC 0)
    // ══════════════════════════════════════════════════════

    private static Vector2 RotateDir(Vector2 dir, float angleDeg)
    {
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