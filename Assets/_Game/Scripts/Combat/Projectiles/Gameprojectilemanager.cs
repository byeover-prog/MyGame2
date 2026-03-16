// ============================================================================
// GameProjectileManager.cs
// 경로: Assets/_Game/Scripts/Combat/Projectiles/GameProjectileManager.cs
// 용도: 다크오브 투사체 전체 로직 매니저 (struct 배열 기반)
//
// [설계도 핵심]
// 1. 발사: DarkOrbWeapon2D → TrySpawnDarkOrb(spec)
// 2. 비행: 매 프레임 이동 + 수명 감소. 접촉 판정 없음!
// 3. 폭발: 수명 ≤ 0 → OverlapCircleNonAlloc 1회 → 범위 데미지
// 4. 분열: depth 기반 재귀 트리 (부모 1 → 자식 2, ±고정각)
// 5. VFX: 폭발 시점에 VFXSpawner.Spawn() 직접 호출
// 6. 풀링: struct 배열 + ViewPool (경량 프리팹)
//
// [Hierarchy 설정]
// 오브젝트: [GameProjectileManager]
// 컴포넌트: GameProjectileManager + GameProjectileViewPool
//
// [Inspector 설정]
//   Max Dark Orbs             → 60
//   Dark Orb Explosion Vfx    → eff_weapon_darkorb_explosion
//   Dark Orb Body Vfx         → eff_weapon_darkorb
//   View Pool                 → 같은 오브젝트의 GameProjectileViewPool (★필수!)
//   Default Enemy Mask        → Enemy
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

public sealed class GameProjectileManager : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════
    // Singleton
    // ══════════════════════════════════════════════════════════════

    public static GameProjectileManager Instance { get; private set; }

    // ══════════════════════════════════════════════════════════════
    // Inspector
    // ══════════════════════════════════════════════════════════════

    [Header("풀 크기")]
    [Tooltip("DarkOrb 동시 활성 최대 수 (루트+자식 합산)")]
    [SerializeField] private int maxDarkOrbs = 60;

    [Header("VFX 프리팹")]
    [Tooltip("폭발 VFX (Project 창에서 eff_weapon_darkorb_explosion 드래그)")]
    [SerializeField] private GameObject darkOrbExplosionVfxPrefab;
    [Tooltip("본체 VFX (Project 창에서 eff_weapon_darkorb 드래그) - 미사용 시 비워도 됨")]
    [SerializeField] private GameObject darkOrbBodyVfxPrefab;

    [Header("뷰 풀 (★필수 연결!)")]
    [Tooltip("같은 오브젝트의 GameProjectileViewPool을 드래그하세요")]
    [SerializeField] private GameProjectileViewPool viewPool;

    [Header("기본 설정")]
    [SerializeField] private LayerMask defaultEnemyMask;

    // ══════════════════════════════════════════════════════════════
    // 내부 상태
    // ══════════════════════════════════════════════════════════════

    private DarkOrbState[] _darkOrbs;
    private int _activeCount;

    // 분열 대기열 (한 프레임에 폭발 → 분열을 바로 처리하면 배열이 꼬임 방지)
    private readonly Queue<DarkOrbSplitRequest> _splitQueue = new Queue<DarkOrbSplitRequest>(32);

    // OverlapCircle 결과 버퍼 (GC 0)
    private readonly Collider2D[] _hitBuffer = new Collider2D[32];

    // ══════════════════════════════════════════════════════════════
    // 생명주기
    // ══════════════════════════════════════════════════════════════

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameProjectileManager] 중복 인스턴스 감지. 기존 것을 사용합니다.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 배열 초기화
        _darkOrbs = new DarkOrbState[maxDarkOrbs];
        _activeCount = 0;

        // 검증
        if (viewPool == null)
        {
            Debug.LogError("[GameProjectileManager] viewPool이 비어있습니다! Inspector에서 같은 오브젝트의 GameProjectileViewPool을 연결하세요.");
        }

        Debug.Log($"<color=green>[GameProjectileManager] ★★★ 설계도 기준 ★★★ maxOrbs={maxDarkOrbs}, viewPool={( viewPool != null ? "연결됨" : "❌ 미연결!")}</color>");
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1. 모든 활성 오브 처리 (역순 순회 — 중간 제거에 안전)
        for (int i = _darkOrbs.Length - 1; i >= 0; i--)
        {
            if (!_darkOrbs[i].Active) continue;
            ProcessDarkOrb(ref _darkOrbs[i], i, dt);
        }

        // 2. 분열 대기열 처리
        FlushSplits();

        // 3. 뷰 위치 동기화
        SyncViews();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ══════════════════════════════════════════════════════════════
    // Public API (DarkOrbWeapon2D가 호출)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 다크오브를 발사합니다.
    /// DarkOrbWeapon2D에서만 호출. 직접 Instantiate하지 마세요.
    /// </summary>
    /// <returns>성공 여부</returns>
    public bool TrySpawnDarkOrb(DarkOrbProjectileSpec spec)
    {
        int slot = FindFreeSlot();
        if (slot < 0)
        {
            Debug.LogWarning("[GameProjectileManager] DarkOrb 슬롯 부족. maxDarkOrbs를 늘리세요.");
            return false;
        }

        // 뷰 획득
        int viewId = GameProjectileViewPool.InvalidId;
        if (viewPool != null)
            viewId = viewPool.Acquire(spec.SpawnPosition, spec.OrbAlpha);

        // 상태 초기화
        _darkOrbs[slot] = new DarkOrbState
        {
            Position        = spec.SpawnPosition,
            Direction       = spec.Direction.normalized,
            Speed           = spec.Speed,
            Lifetime        = spec.Lifetime,
            ExplosionRadius = spec.ExplosionRadius,
            ExplosionDamage = spec.ExplosionDamage,
            EnemyMask       = spec.EnemyMask,
            Generation      = 0,            // 루트 = depth 0
            MaxGeneration   = spec.MaxGeneration,
            SplitAngleDeg   = spec.SplitAngleDeg,
            SplitSpeed      = spec.SplitSpeed,
            SplitLifetime   = spec.SplitLifetime,
            ViewId          = viewId,
            Active          = true
        };

        _activeCount++;
        return true;
    }

    /// <summary>
    /// 현재 활성 다크오브 개수.
    /// </summary>
    public int ActiveDarkOrbCount => _activeCount;

    // ══════════════════════════════════════════════════════════════
    // 핵심 로직
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 다크오브 1개 처리: 이동 → 수명 감소 → 수명 만료 시 폭발.
    /// ★ 접촉 판정 없음. 비행 중 OverlapCircle = 0회. ★
    /// </summary>
    private void ProcessDarkOrb(ref DarkOrbState s, int slotIndex, float dt)
    {
        // 1. 이동
        s.Position += s.Direction * s.Speed * dt;

        // 2. 수명 감소
        s.Lifetime -= dt;

        // 3. 수명 만료 → 폭발
        if (s.Lifetime <= 0f)
        {
            ExplodeDarkOrb(ref s, slotIndex);
        }
    }

    /// <summary>
    /// 다크오브 폭발 처리.
    /// 1. 범위 데미지 (OverlapCircleNonAlloc 1회)
    /// 2. 분열 자식 큐잉
    /// 3. VFX 생성
    /// 4. 뷰 반환 + 슬롯 비활성화
    /// </summary>
    private void ExplodeDarkOrb(ref DarkOrbState s, int slotIndex)
    {
        // ── 1. 범위 데미지 ──
        // Unity 2023+ 에서 NonAlloc이 deprecated → OverlapCircle(버퍼 오버로드) 사용
        int hitCount = Physics2D.OverlapCircle(
            s.Position, s.ExplosionRadius, new ContactFilter2D
            {
                useLayerMask = true,
                layerMask    = s.EnemyMask,
                useTriggers  = true
            }, _hitBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            var col = _hitBuffer[i];
            if (col == null) continue;

            // IDamageable 인터페이스 또는 Health 컴포넌트로 데미지 적용
            // ★ 프로젝트의 데미지 시스템에 맞춰 수정 필요 ★
            var health = col.GetComponent<IHealth>();
            if (health != null)
            {
                health.TakeDamage(s.ExplosionDamage);
            }
        }

        // ── 2. 분열 (자식 2개 큐잉) ──
        if (s.Generation < s.MaxGeneration)
        {
            _splitQueue.Enqueue(new DarkOrbSplitRequest
            {
                ParentPosition     = s.Position,
                ParentDirection    = s.Direction,
                SplitAngleDeg      = s.SplitAngleDeg,
                Speed              = s.SplitSpeed,
                Lifetime           = s.SplitLifetime,
                ExplosionRadius    = s.ExplosionRadius,
                ExplosionDamage    = s.ExplosionDamage,
                EnemyMask          = s.EnemyMask,
                ChildGeneration    = s.Generation + 1,
                MaxGeneration      = s.MaxGeneration,
                ChildSplitAngleDeg = s.SplitAngleDeg,
                ChildSplitSpeed    = s.SplitSpeed,
                ChildSplitLifetime = s.SplitLifetime
            });
        }

        // ── 3. 폭발 VFX (직접 호출) ──
        SpawnExplosionVfx(s.Position);

        // ── 4. 뷰 반환 + 슬롯 비활성화 ──
        if (viewPool != null && s.ViewId != GameProjectileViewPool.InvalidId)
        {
            viewPool.Release(s.ViewId);
        }

        s.Active = false;
        s.ViewId = GameProjectileViewPool.InvalidId;
        _activeCount = Mathf.Max(0, _activeCount - 1);
    }

    /// <summary>
    /// 분열 대기열을 처리하여 자식 오브를 생성.
    /// 부모 방향 기준 ±splitAngle로 2개 생성 (depth 기반 재귀 트리).
    /// </summary>
    private void FlushSplits()
    {
        while (_splitQueue.Count > 0)
        {
            var req = _splitQueue.Dequeue();

            // 자식 2개: +angle, -angle
            SpawnChild(req, +req.SplitAngleDeg);
            SpawnChild(req, -req.SplitAngleDeg);
        }
    }

    /// <summary>
    /// 분열 자식 1개를 생성.
    /// </summary>
    private void SpawnChild(DarkOrbSplitRequest req, float angleDeg)
    {
        int slot = FindFreeSlot();
        if (slot < 0)
        {
            Debug.LogWarning("[GameProjectileManager] 분열 슬롯 부족!");
            return;
        }

        // 부모 방향에서 angleDeg만큼 회전
        Vector2 childDir = RotateVector(req.ParentDirection, angleDeg);

        // 부모 위치에서 살짝 밀어내기 (겹침 방지)
        Vector2 spawnPos = req.ParentPosition + childDir * 0.15f;

        // 뷰 획득
        int viewId = GameProjectileViewPool.InvalidId;
        if (viewPool != null)
            viewId = viewPool.Acquire(spawnPos, 0.55f);

        _darkOrbs[slot] = new DarkOrbState
        {
            Position        = spawnPos,
            Direction       = childDir,
            Speed           = req.Speed,
            Lifetime        = req.Lifetime,
            ExplosionRadius = req.ExplosionRadius,
            ExplosionDamage = req.ExplosionDamage,
            EnemyMask       = req.EnemyMask,
            Generation      = req.ChildGeneration,
            MaxGeneration   = req.MaxGeneration,
            SplitAngleDeg   = req.ChildSplitAngleDeg,
            SplitSpeed      = req.ChildSplitSpeed,
            SplitLifetime   = req.ChildSplitLifetime,
            ViewId          = viewId,
            Active          = true
        };

        _activeCount++;
    }

    // ══════════════════════════════════════════════════════════════
    // 뷰 동기화
    // ══════════════════════════════════════════════════════════════

    private void SyncViews()
    {
        if (viewPool == null) return;

        for (int i = 0; i < _darkOrbs.Length; i++)
        {
            ref var s = ref _darkOrbs[i];
            if (!s.Active) continue;
            if (s.ViewId == GameProjectileViewPool.InvalidId) continue;

            viewPool.SetPosition(s.ViewId, s.Position);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // VFX
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 폭발 VFX를 생성합니다.
    /// VFXSpawner가 있으면 풀링 사용, 없으면 직접 Instantiate (폴백).
    /// ★ 프로젝트에 VFXSpawner/VFXPool이 있으면 이 메서드만 수정하세요. ★
    /// </summary>
    private void SpawnExplosionVfx(Vector2 position)
    {
        if (darkOrbExplosionVfxPrefab == null) return;

        // ── VFXSpawner 사용 (프로젝트에 존재하면) ──
        // VFXSpawner.Spawn()이 정적 메서드나 싱글톤이라면 이렇게 호출:
        // VFXSpawner.Spawn(darkOrbExplosionVfxPrefab, position);
        
        // ── VFXPool 사용 (이미 적용된 시스템) ──
        Vector3 pos3 = new Vector3(position.x, position.y, 0f);
        var vfxGo = VFXPool.Get(darkOrbExplosionVfxPrefab, pos3, Quaternion.identity, null);
        if (vfxGo != null)
        {
            vfxGo.SetActive(true);

            // VFXAutoReturn이 있으면 자동 반환됨
            // 없으면 수동으로 ParticleSystem 재생
            var ps = vfxGo.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Clear();
                ps.Play();
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 유틸리티
    // ══════════════════════════════════════════════════════════════

    private int FindFreeSlot()
    {
        for (int i = 0; i < _darkOrbs.Length; i++)
        {
            if (!_darkOrbs[i].Active)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 2D 벡터를 angleDeg만큼 회전.
    /// </summary>
    private static Vector2 RotateVector(Vector2 v, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }
}

// ============================================================================
// IHealth 인터페이스 (프로젝트에 이미 있으면 이 부분 삭제)
// 프로젝트의 데미지 시스템에 맞게 교체하세요.
// ============================================================================
public interface IHealth
{
    void TakeDamage(float damage);
}