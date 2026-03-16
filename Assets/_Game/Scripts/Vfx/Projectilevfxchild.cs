using UnityEngine;

/// <summary>
/// 투사체 프리팹에 직접 부착하는 VFX 컴포넌트.
/// 
/// ★ 분열 최적화:
///   SetVFXEnabled()를 호출하면 OnEnable에서 VFX 생성을 차단할 수 있다.
///   반드시 SetActive(true) 호출 전에 설정해야 한다.
/// </summary>
public class ProjectileVFXChild : MonoBehaviour
{
    [Header("투사체 VFX 설정")]
    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private GameObject explosionVfxPrefab;

    [Header("동작 방식")]
    [SerializeField] private bool attachToProjectile = true;
    [SerializeField] private float vfxLifetime = 3f;

    [Header("시각 설정")]
    [SerializeField] private bool hideSpriteRenderer = true;

    // ── 런타임 제어 (SetActive 전에 설정) ─────────────────
    private bool _bodyEnabled = true;
    private bool _explosionEnabled = true;

    private GameObject _currentVFX;
    private bool _hasExploded;
    private SpriteRenderer[] _sprites;
    private bool _spritesCached;
    private static bool _appQuitting;

    private void Awake() => CacheSprites();
    private void OnApplicationQuit() => _appQuitting = true;

    // ── 외부 제어 ─────────────────────────────────────────

    /// <summary>
    /// VFX 허용 여부 설정. 반드시 gameObject.SetActive(true) 전에 호출.
    /// 비활성 상태에서도 호출 가능.
    /// </summary>
    public void SetVFXEnabled(bool body, bool explosion)
    {
        _bodyEnabled = body;
        _explosionEnabled = explosion;
    }

    // ── 라이프사이클 ──────────────────────────────────────

    private void OnEnable()
    {
        _hasExploded = false;
        if (_appQuitting) return;

        ReturnLeftoverVFX();

        if (_bodyEnabled)
        {
            if (hideSpriteRenderer) SetSpritesVisible(false);
            SpawnVFX();
        }
    }

    private void OnDisable()
    {
        if (_appQuitting) return;

        if (explosionVfxPrefab != null && _explosionEnabled && !_hasExploded)
        {
            VFXSpawner.Spawn(explosionVfxPrefab, transform.position,
                              Quaternion.identity, 2f);
            _hasExploded = true;
        }

        _currentVFX = null;

        if (hideSpriteRenderer) SetSpritesVisible(true);

        // 풀 재사용 대비: 기본값 리셋
        _bodyEnabled = true;
        _explosionEnabled = true;
    }

    // ── 잔류 VFX 정리 ─────────────────────────────────────

    private void ReturnLeftoverVFX()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            var ar = child.GetComponent<VFXAutoReturn>();
            if (ar != null && ar.sourcePrefab != null)
                VFXPool.Return(ar.sourcePrefab, child.gameObject);
        }
    }

    // ── VFX 생성 ──────────────────────────────────────────

    private void SpawnVFX()
    {
        if (vfxPrefab == null) return;

        if (attachToProjectile)
            _currentVFX = VFXSpawner.SpawnAsChild(vfxPrefab, transform, vfxLifetime);
        else
            _currentVFX = VFXSpawner.Spawn(vfxPrefab, transform.position,
                                             Quaternion.identity, vfxLifetime);
    }

    // ── 스프라이트 ────────────────────────────────────────

    private void CacheSprites()
    {
        if (_spritesCached) return;
        _sprites = GetComponentsInChildren<SpriteRenderer>(true);
        _spritesCached = true;
    }

    private void SetSpritesVisible(bool visible)
    {
        if (!_spritesCached) CacheSprites();
        if (_sprites == null) return;
        for (int i = 0; i < _sprites.Length; i++)
            if (_sprites[i] != null) _sprites[i].enabled = visible;
    }

    public void TriggerExplosionVFX(Vector3 position)
    {
        if (explosionVfxPrefab == null || _appQuitting || !_explosionEnabled) return;
        VFXSpawner.Spawn(explosionVfxPrefab, position, Quaternion.identity, 2f);
        _hasExploded = true;
    }
}