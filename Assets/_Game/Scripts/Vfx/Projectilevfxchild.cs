// UTF-8
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// - 투사체 몸통 VFX와 폭발 VFX를 분리한다.
/// - 현재 성능 병목은 암흑구 몸통/폭발 VFX이므로, 코드에서 선택적으로 차단 가능하게 만든다.
/// - 분열 자식 구체는 폭발 VFX를 금지해서 1→2→4→8 타이밍의 스파이크를 막는다.
/// </summary>
[DisallowMultipleComponent]
public sealed class ProjectileVFXChild : MonoBehaviour
{
    [Header("프리팹")]
    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private GameObject explosionVfxPrefab;

    [Header("바디 VFX")]
    [Tooltip("투사체가 켜질 때 몸통 VFX를 생성할지 여부")]
    [SerializeField] private bool enableBodyVfx = true;

    [Tooltip("몸통 VFX를 투사체 자식으로 붙입니다.")]
    [SerializeField] private bool attachToProjectile = true;

    [Tooltip("몸통 VFX 최대 수명(초)")]
    [SerializeField] private float bodyLifetime = 1.0f;

    [Header("폭발 VFX")]
    [Tooltip("폭발 VFX를 허용합니다.")]
    [SerializeField] private bool enableExplosionVfx = true;

    [Tooltip("암흑구 분열 자식이면 체크. 자식은 폭발 VFX를 생략합니다.")]
    [SerializeField] private bool suppressExplosionForChildFragment = false;

    [Tooltip("암흑구 폭발일 때 프레임 예산 제한을 적용합니다.")]
    [SerializeField] private bool useDarkOrbExplosionBudget = false;

    [Tooltip("폭발 VFX 최대 수명(초)")]
    [SerializeField] private float explosionLifetime = 1.2f;

    [Header("스프라이트")]
    [Tooltip("몸통 VFX를 쓰는 동안 원본 스프라이트를 숨깁니다.")]
    [SerializeField] private bool hideSpriteRenderer = false;

    private GameObject _currentBody;
    private SpriteRenderer[] _sprites;
    private bool _cached;

    private void Awake()
    {
        Cache();
    }

    private void OnEnable()
    {
        Cache();
        RemoveLeftoverChildren();

        if (!enableBodyVfx)
        {
            SetSpritesVisible(true);
            return;
        }

        if (hideSpriteRenderer)
            SetSpritesVisible(false);
        else
            SetSpritesVisible(true);

        if (vfxPrefab == null) return;

        _currentBody = attachToProjectile
            ? VFXSpawner.SpawnAsChild(vfxPrefab, transform, bodyLifetime)
            : VFXSpawner.Spawn(vfxPrefab, transform.position, Quaternion.identity, bodyLifetime);
    }

    private void OnDisable()
    {
        RemoveLeftoverChildren();
        SetSpritesVisible(true);
        _currentBody = null;
    }

    public void SetVFXEnabled(bool body, bool explosion)
    {
        enableBodyVfx = body;
        enableExplosionVfx = explosion;
    }

    public void TriggerExplosionVFX(Vector3 position)
    {
        if (!enableExplosionVfx) return;
        if (suppressExplosionForChildFragment) return;
        if (explosionVfxPrefab == null) return;

        if (useDarkOrbExplosionBudget && !SkillVFXBudget2D.TryConsumeDarkOrbExplosion())
            return;

        VFXSpawner.Spawn(explosionVfxPrefab, position, Quaternion.identity, explosionLifetime);
    }

    private void Cache()
    {
        if (_cached) return;
        _sprites = GetComponentsInChildren<SpriteRenderer>(true);
        _cached = true;
    }

    private void SetSpritesVisible(bool visible)
    {
        if (_sprites == null) return;

        for (int i = 0; i < _sprites.Length; i++)
        {
            if (_sprites[i] != null)
                _sprites[i].enabled = visible;
        }
    }

    private void RemoveLeftoverChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            var autoReturn = child.GetComponent<VFXAutoReturn>();
            if (autoReturn == null) continue;

            autoReturn.ReturnNow();
        }
    }
}