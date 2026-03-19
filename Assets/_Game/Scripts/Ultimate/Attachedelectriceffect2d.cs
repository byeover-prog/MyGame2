using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// [구현 원리 요약]
/// 적 1마리당 전기 부착 이펙트 1개만 유지한다.
/// 틱 피해가 다시 들어오면 새로 만들지 않고 지속시간만 갱신한다.
/// 위치는 Collider 중심을 따라가서 정화구처럼 자연스럽게 붙어 보이게 한다.
/// </summary>
public sealed class AttachedElectricEffect2D : MonoBehaviour
{
    [Header("전기 부착 설정")]
    [SerializeField, Tooltip("적에게 붙일 전기 이펙트 프리팹입니다.")]
    private GameObject effectPrefab;

    [SerializeField, Tooltip("이펙트가 마지막 틱 이후 추가로 남아있는 시간입니다.")]
    private float holdDuration = 0.25f;

    [SerializeField, Tooltip("콜라이더 중심에서 추가로 올릴 위치 보정값입니다.")]
    private Vector3 localOffset = Vector3.zero;

    [SerializeField, Tooltip("부모 스케일 영향을 무시하고 강제로 적용할 로컬 스케일입니다.")]
    private Vector3 forcedLocalScale = Vector3.one;

    [Header("정렬")]
    [SerializeField, Tooltip("비워두면 프리팹 원래 정렬값을 사용합니다.")]
    private string overrideSortingLayerName = "";

    [SerializeField, Tooltip("0이면 프리팹 원래 Order를 사용합니다.")]
    private int overrideOrderInLayer = 0;

    private GameObject _instance;
    private Transform _instanceTransform;
    private Collider2D _cachedCollider;
    private ParticleSystem[] _particleSystems;
    private VisualEffect[] _visualEffects;
    private SpriteRenderer[] _spriteRenderers;
    private Renderer[] _renderers;

    private float _remainTime;
    private bool _isInitialized;
    private bool _isVisible;

    private void Awake()
    {
        _cachedCollider = GetComponent<Collider2D>();
    }

    private void LateUpdate()
    {
        if (!_isInitialized || _instance == null)
            return;

        FollowTargetCenter();

        if (_remainTime > 0f)
        {
            _remainTime -= Time.deltaTime;
            if (_remainTime <= 0f)
            {
                HideEffect();
            }
        }
    }

    /// <summary>
    /// 외부에서 틱 피해 시 호출.
    /// 이미 붙어 있으면 지속시간만 갱신한다.
    /// </summary>
    public void Refresh(
        GameObject newEffectPrefab,
        float newHoldDuration,
        Vector3 newLocalOffset,
        Vector3 newForcedLocalScale,
        string newSortingLayerName,
        int newOrderInLayer)
    {
        if (newEffectPrefab == null)
            return;

        effectPrefab = newEffectPrefab;
        holdDuration = newHoldDuration;
        localOffset = newLocalOffset;
        forcedLocalScale = newForcedLocalScale;
        overrideSortingLayerName = newSortingLayerName;
        overrideOrderInLayer = newOrderInLayer;

        if (_instance == null)
        {
            CreateInstance();
        }

        if (_instance == null)
            return;

        _remainTime = holdDuration;

        FollowTargetCenter();
        ApplyForcedScale();
        ApplySortingOverride();

        if (!_isVisible)
        {
            ShowEffect();
        }

        RestartEffect();
    }

    private void CreateInstance()
    {
        if (effectPrefab == null)
            return;

        _instance = Instantiate(effectPrefab);
        _instance.name = $"{effectPrefab.name}_부착형";
        _instanceTransform = _instance.transform;

        _particleSystems = _instance.GetComponentsInChildren<ParticleSystem>(true);
        _visualEffects = _instance.GetComponentsInChildren<VisualEffect>(true);
        _spriteRenderers = _instance.GetComponentsInChildren<SpriteRenderer>(true);
        _renderers = _instance.GetComponentsInChildren<Renderer>(true);

        _isInitialized = true;
        _isVisible = true;

        FollowTargetCenter();
        ApplyForcedScale();
        ApplySortingOverride();
    }

    private void FollowTargetCenter()
    {
        if (_instanceTransform == null)
            return;

        Vector3 center = GetTargetCenterWorld();
        center += localOffset;
        center.z = 0f;

        _instanceTransform.position = center;
        _instanceTransform.rotation = Quaternion.identity;
    }

    private Vector3 GetTargetCenterWorld()
    {
        if (_cachedCollider != null)
            return _cachedCollider.bounds.center;

        return transform.position;
    }

    private void ApplyForcedScale()
    {
        if (_instanceTransform == null)
            return;

        _instanceTransform.localScale = forcedLocalScale;
    }

    private void ApplySortingOverride()
    {
        bool hasLayerOverride = !string.IsNullOrWhiteSpace(overrideSortingLayerName);
        bool hasOrderOverride = overrideOrderInLayer != 0;

        if (!hasLayerOverride && !hasOrderOverride)
            return;

        if (_renderers == null)
            return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer r = _renderers[i];
            if (r == null)
                continue;

            if (hasLayerOverride)
                r.sortingLayerName = overrideSortingLayerName;

            if (hasOrderOverride)
                r.sortingOrder = overrideOrderInLayer;
        }
    }

    private void RestartEffect()
    {
        if (_particleSystems != null)
        {
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem ps = _particleSystems[i];
                if (ps == null)
                    continue;

                ps.Clear(true);
                ps.Play(true);
            }
        }

        if (_visualEffects != null)
        {
            for (int i = 0; i < _visualEffects.Length; i++)
            {
                VisualEffect vfx = _visualEffects[i];
                if (vfx == null)
                    continue;

                vfx.Reinit();
                vfx.Play();
            }
        }
    }

    private void ShowEffect()
    {
        if (_instance == null)
            return;

        _instance.SetActive(true);
        _isVisible = true;
    }

    private void HideEffect()
    {
        if (_instance == null)
            return;

        _instance.SetActive(false);
        _isVisible = false;
    }

    private void OnDisable()
    {
        HideEffect();
    }

    private void OnDestroy()
    {
        if (_instance != null)
        {
            Destroy(_instance);
            _instance = null;
        }
    }
}