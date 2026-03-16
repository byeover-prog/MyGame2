using UnityEngine;

/// <summary>
/// 다크오브 투사체 매니저.
/// struct 배열(DarkOrbState[])로 모든 투사체 상태를 관리한다.
/// MonoBehaviour 투사체 스크립트가 전혀 없다.
/// 구조: DarkOrbManager(struct 배열) + 경량 뷰 프리팹(DarkOrbViewPool) + VFX 직접 호출
/// </summary>
public class DarkOrbManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════════════════

    [Header("풀 설정")]
    [Tooltip("동시 활성 가능한 다크오브 최대 수 (루트 + 분열 자식 전부 포함)")]
    [SerializeField] private int _maxProjectiles = 64;

    [Header("뷰 풀")]
    [Tooltip("DarkOrbViewPool 컴포넌트 참조")]
    [SerializeField] private DarkOrbViewPool _viewPool;

    [Header("VFX")]
    [Tooltip("폭발 VFX 프리팹")]
    [SerializeField] private GameObject _explosionVFXPrefab;

    [Tooltip("VFX 풀 초기 크기")]
    [SerializeField] private int _vfxPoolSize = 16;

    [Header("분열 설정")]
    [Tooltip("자식 분열 시 부모 진행 방향으로부터의 고정 편향각 (도)")]
    [SerializeField] private float _splitAngleDeg = 30f;

    [Tooltip("분열 자식 이동 속도")]
    [SerializeField] private float _splitChildSpeed = 4f;

    [Tooltip("분열 자식 수명 (초)")]
    [SerializeField] private float _splitChildLifetime = 0.6f;

    [Tooltip("분열 자식 데미지 배율 (부모 대비)")]
    [SerializeField] private float _splitDamageMultiplier = 0.7f;

    [Tooltip("분열 자식 폭발 반경 배율 (부모 대비)")]
    [SerializeField] private float _splitRadiusMultiplier = 0.8f;

    [Header("데미지 레이어")]
    [Tooltip("적 레이어 마스크 (OverlapCircle 판정용)")]
    [SerializeField] private LayerMask _enemyLayer;

    // ═══════════════════════════════════════════════════════════════
    //  내부 상태
    // ═══════════════════════════════════════════════════════════════

    private DarkOrbState[] _states;
    private int _activeCount;

    // VFX 풀 (간이)
    private GameObject[] _vfxPool;
    private ParticleSystem[] _vfxParticles;
    private int _vfxPoolCapacity;

    // 재사용 버퍼 (GC 방지)
    private readonly Collider2D[] _hitBuffer = new Collider2D[32];
    private ContactFilter2D _enemyFilter;

    // ═══════════════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        _states = new DarkOrbState[_maxProjectiles];
        _activeCount = 0;

        // ViewPool 초기화
        if (_viewPool != null)
            _viewPool.Initialize();

        // VFX 풀 초기화
        InitializeVFXPool();
    }

    private void InitializeVFXPool()
    {
        if (_explosionVFXPrefab == null) return;

        _vfxPoolCapacity = _vfxPoolSize;
        _vfxPool = new GameObject[_vfxPoolCapacity];
        _vfxParticles = new ParticleSystem[_vfxPoolCapacity];

        for (int i = 0; i < _vfxPoolCapacity; i++)
        {
            GameObject vfx = Instantiate(_explosionVFXPrefab, transform);
            vfx.name = $"DarkOrbVFX_{i}";
            vfx.SetActive(false);
            _vfxPool[i] = vfx;
            _vfxParticles[i] = vfx.GetComponent<ParticleSystem>();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  매 프레임 업데이트
    // ═══════════════════════════════════════════════════════════════

    private void Update()
    {
        float dt = Time.deltaTime;

        for (int i = 0; i < _maxProjectiles; i++)
        {
            if (!_states[i].IsActive) continue;

            // 1) 이동
            _states[i].Position += _states[i].Direction * _states[i].Speed * dt;

            // 2) 뷰 동기화
            if (_viewPool != null && _states[i].ViewIndex >= 0)
                _viewPool.UpdatePosition(_states[i].ViewIndex, _states[i].Position);

            // 3) 수명 감소
            _states[i].LifetimeRemaining -= dt;

            // 4) 수명 만료 → 폭발
            if (_states[i].LifetimeRemaining <= 0f)
            {
                ExplodeDarkOrb(i);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  공개 API — DarkOrbWeapon2D가 호출
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 루트 다크오브를 발사한다.
    /// DarkOrbWeapon2D에서 호출하는 유일한 진입점.
    /// </summary>
    /// <param name="origin">발사 위치</param>
    /// <param name="direction">발사 방향 (정규화)</param>
    /// <param name="speed">이동 속도</param>
    /// <param name="lifetime">수명 (초)</param>
    /// <param name="damage">폭발 데미지</param>
    /// <param name="radius">폭발 반경</param>
    /// <param name="splitDepth">분열 깊이 (레벨에 따라 결정)</param>
    /// <param name="scaleMultiplier">투사체 크기 배율</param>
    public void SpawnRoot(
        Vector2 origin,
        Vector2 direction,
        float speed,
        float lifetime,
        float damage,
        float radius,
        int splitDepth,
        float scaleMultiplier)
    {
        SpawnInternal(origin, direction, speed, lifetime, damage, radius, splitDepth, scaleMultiplier);
    }

    /// <summary>모든 활성 다크오브를 즉시 제거. 씬 전환/스킬 리셋 시 사용</summary>
    public void ClearAll()
    {
        for (int i = 0; i < _maxProjectiles; i++)
        {
            if (_states[i].IsActive)
            {
                ReturnView(i);
                _states[i].IsActive = false;
            }
        }
        _activeCount = 0;

        if (_viewPool != null)
            _viewPool.ReturnAll();
    }

    /// <summary>현재 활성 투사체 수</summary>
    public int ActiveCount => _activeCount;

    // ═══════════════════════════════════════════════════════════════
    //  폭발 처리 — VFX 직접 호출 + 데미지 + 분열
    // ═══════════════════════════════════════════════════════════════

    private void ExplodeDarkOrb(int index)
    {
        ref DarkOrbState state = ref _states[index];
        Vector2 pos = state.Position;
        float damage = state.ExplosionDamage;
        float radius = state.ExplosionRadius;
        int depthRemaining = state.SplitDepthRemaining;
        Vector2 dir = state.Direction;
        float scaleMultiplier = state.ScaleMultiplier;

        // ── 1) 범위 데미지 ──
        DealExplosionDamage(pos, damage, radius);

        // ── 2) VFX 직접 호출 (ProjectileVFXChild 경유 안 함) ──
        PlayExplosionVFX(pos, radius * scaleMultiplier);

        // ── 3) 뷰 반납 ──
        ReturnView(index);

        // ── 4) 슬롯 비활성화 ──
        state.IsActive = false;
        _activeCount--;

        // ── 5) 분열 자식 생성 (depth > 0 일 때만) ──
        if (depthRemaining > 0)
        {
            SpawnSplitChildren(pos, dir, damage, radius, depthRemaining, scaleMultiplier);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  분열 자식 생성 — depth 기반 재귀 트리
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 부모 폭발 위치에서 자식 2개를 ±고정각으로 생성한다.
    /// 자식의 SplitDepthRemaining = depthRemaining - 1.
    /// depth가 0이 되면 더 이상 분열하지 않는다.
    /// </summary>
    private void SpawnSplitChildren(
        Vector2 parentPos,
        Vector2 parentDir,
        float parentDamage,
        float parentRadius,
        int depthRemaining,
        float parentScale)
    {
        float childDamage = parentDamage * _splitDamageMultiplier;
        float childRadius = parentRadius * _splitRadiusMultiplier;
        int childDepth = depthRemaining - 1;
        float childScale = parentScale * _splitRadiusMultiplier;

        // ±고정각으로 2개 방향
        Vector2 dirLeft = RotateVector(parentDir, +_splitAngleDeg);
        Vector2 dirRight = RotateVector(parentDir, -_splitAngleDeg);

        SpawnInternal(parentPos, dirLeft, _splitChildSpeed, _splitChildLifetime,
            childDamage, childRadius, childDepth, childScale);
        SpawnInternal(parentPos, dirRight, _splitChildSpeed, _splitChildLifetime,
            childDamage, childRadius, childDepth, childScale);
    }

    // ═══════════════════════════════════════════════════════════════
    //  내부 생성
    // ═══════════════════════════════════════════════════════════════

    private void SpawnInternal(
        Vector2 position,
        Vector2 direction,
        float speed,
        float lifetime,
        float damage,
        float radius,
        int splitDepth,
        float scaleMultiplier)
    {
        int slot = FindFreeSlot();
        if (slot < 0)
        {
            Debug.LogWarning("[DarkOrbManager] 슬롯 부족! _maxProjectiles를 늘리세요.");
            return;
        }

        ref DarkOrbState state = ref _states[slot];
        state.Position = position;
        state.Direction = direction.normalized;
        state.Speed = speed;
        state.LifetimeRemaining = lifetime;
        state.ExplosionDamage = damage;
        state.ExplosionRadius = radius;
        state.SplitDepthRemaining = splitDepth;
        state.ScaleMultiplier = scaleMultiplier;
        state.IsActive = true;

        // 뷰 할당
        if (_viewPool != null)
            state.ViewIndex = _viewPool.Rent(position, scaleMultiplier);
        else
            state.ViewIndex = -1;

        _activeCount++;
    }

    private int FindFreeSlot()
    {
        for (int i = 0; i < _maxProjectiles; i++)
        {
            if (!_states[i].IsActive) return i;
        }
        return -1;
    }

    // ═══════════════════════════════════════════════════════════════
    //  데미지 판정
    // ═══════════════════════════════════════════════════════════════

    private void DealExplosionDamage(Vector2 center, float damage, float radius)
    {
        _enemyFilter.useTriggers = true;
        _enemyFilter.useLayerMask = true;
        _enemyFilter.layerMask = _enemyLayer;

        int hitCount = Physics2D.OverlapCircle(center, radius, _enemyFilter, _hitBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            // TODO: 프로젝트의 데미지 인터페이스에 맞게 교체
            // 예: _hitBuffer[i].GetComponent<IDamageable>()?.TakeDamage(damage);
            var damageable = _hitBuffer[i].GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  VFX 직접 호출 (ProjectileVFXChild 경유 안 함)
    // ═══════════════════════════════════════════════════════════════

    private void PlayExplosionVFX(Vector2 position, float scale)
    {
        if (_vfxPool == null) return;

        for (int i = 0; i < _vfxPoolCapacity; i++)
        {
            if (!_vfxPool[i].activeInHierarchy)
            {
                Transform t = _vfxPool[i].transform;
                t.position = new Vector3(position.x, position.y, 0f);
                t.localScale = Vector3.one * scale;
                _vfxPool[i].SetActive(true);

                if (_vfxParticles[i] != null)
                    _vfxParticles[i].Play();

                return;
            }
        }

        Debug.LogWarning("[DarkOrbManager] VFX 풀 부족! _vfxPoolSize를 늘리세요.");
    }

    // ═══════════════════════════════════════════════════════════════
    //  유틸리티
    // ═══════════════════════════════════════════════════════════════

    private void ReturnView(int index)
    {
        if (_viewPool != null && _states[index].ViewIndex >= 0)
        {
            _viewPool.Return(_states[index].ViewIndex);
            _states[index].ViewIndex = -1;
        }
    }

    /// <summary>2D 벡터를 degree 만큼 회전</summary>
    private static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }

    // ═══════════════════════════════════════════════════════════════
    //  기즈모 (에디터 디버그용)
    // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_states == null) return;

        for (int i = 0; i < _maxProjectiles; i++)
        {
            if (!_states[i].IsActive) continue;

            Gizmos.color = _states[i].SplitDepthRemaining > 0
                ? new Color(0.6f, 0f, 1f, 0.4f)   // 보라 = 분열 가능
                : new Color(1f, 0.3f, 0f, 0.4f);   // 주황 = 최종 폭발

            Gizmos.DrawWireSphere(
                new Vector3(_states[i].Position.x, _states[i].Position.y, 0f),
                _states[i].ExplosionRadius * _states[i].ScaleMultiplier);
        }
    }
#endif
}

// ═══════════════════════════════════════════════════════════════
//  IDamageable 인터페이스 (프로젝트에 이미 있으면 삭제)
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// 데미지를 받을 수 있는 오브젝트의 인터페이스.
/// 프로젝트에 이미 동일한 인터페이스가 있으면 이 정의를 삭제하고 기존 것을 사용할 것.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
}