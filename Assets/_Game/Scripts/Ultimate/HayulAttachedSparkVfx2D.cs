using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// 하율 궁극기 전기 스파크 부착 연출 전용.
/// 같은 적에게 1개만 붙고, 틱이 들어올 때마다 재생을 갱신한다.
/// </summary>
public sealed class HayulAttachedSparkVfx2D : MonoBehaviour
{
    [Header("전기 스파크 설정")]
    [SerializeField, Tooltip("적에게 붙일 전기 스파크 프리팹입니다.")]
    private GameObject sparkPrefab;

    [SerializeField, Tooltip("적 기준 로컬 오프셋입니다.")]
    private Vector3 localOffset = Vector3.zero;

    [SerializeField, Tooltip("마지막 갱신 후 자동 제거까지 대기 시간입니다.")]
    private float autoReleaseDelay = 0.35f;

    private GameObject _spawnedInstance;
    private Transform _cachedTransform;
    private float _lastRefreshTime = -999f;

    private ParticleSystem[] _particleSystems;
    private VisualEffect[] _visualEffects;

    private void Awake()
    {
        _cachedTransform = transform;
    }

    private void Update()
    {
        if (_spawnedInstance == null) return;

        // 마지막 갱신 후 일정 시간이 지나면 숨긴다.
        if (Time.time - _lastRefreshTime >= autoReleaseDelay)
        {
            SetInstanceVisible(false);
        }
    }

    /// <summary>
    /// 외부에서 틱 피해가 들어올 때 호출.
    /// 없으면 생성하고, 있으면 재생만 갱신한다.
    /// </summary>
    public void Refresh(GameObject prefab, Vector3 offset, float releaseDelay)
    {
        if (prefab == null) return;

        sparkPrefab = prefab;
        localOffset = offset;
        autoReleaseDelay = releaseDelay;
        _lastRefreshTime = Time.time;

        if (_spawnedInstance == null)
        {
            CreateInstance();
        }

        if (_spawnedInstance == null) return;

        _spawnedInstance.transform.SetParent(_cachedTransform, false);
        _spawnedInstance.transform.localPosition = localOffset;
        _spawnedInstance.transform.localRotation = Quaternion.identity;
        _spawnedInstance.transform.localScale = Vector3.one;

        SetInstanceVisible(true);
        RestartEffect();
    }

    private void CreateInstance()
    {
        if (sparkPrefab == null) return;

        _spawnedInstance = Instantiate(sparkPrefab, _cachedTransform);
        _spawnedInstance.transform.localPosition = localOffset;
        _spawnedInstance.transform.localRotation = Quaternion.identity;
        _spawnedInstance.transform.localScale = Vector3.one;

        _particleSystems = _spawnedInstance.GetComponentsInChildren<ParticleSystem>(true);
        _visualEffects = _spawnedInstance.GetComponentsInChildren<VisualEffect>(true);
    }

    private void RestartEffect()
    {
        if (_particleSystems != null)
        {
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i] == null) continue;
                _particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _particleSystems[i].Play(true);
            }
        }

        if (_visualEffects != null)
        {
            for (int i = 0; i < _visualEffects.Length; i++)
            {
                if (_visualEffects[i] == null) continue;
                _visualEffects[i].Reinit();
                _visualEffects[i].Play();
            }
        }
    }

    private void SetInstanceVisible(bool visible)
    {
        if (_spawnedInstance == null) return;

        if (_spawnedInstance.activeSelf != visible)
        {
            _spawnedInstance.SetActive(visible);
        }
    }

    private void OnDisable()
    {
        SetInstanceVisible(false);
    }

    private void OnDestroy()
    {
        if (_spawnedInstance != null)
        {
            Destroy(_spawnedInstance);
            _spawnedInstance = null;
        }
    }
}