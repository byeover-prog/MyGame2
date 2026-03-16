// UTF-8
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// - VFX가 끝났는데 바닥에 남는 문제를 막기 위해
///   비활성화 시 Trail/Particle을 강제로 비운다.
/// - 폭발 VFX는 수명 기반 + 파티클 종료 콜백 둘 다 지원한다.
/// - 풀링 복귀 시 이전 프레임 잔상이 남지 않게 Clear를 반드시 호출한다.
///
/// [v2 수정]
/// - maxLifetime, timer, registered를 internal로 노출
///   → VFXSpawner.Setup()에서 수명 설정
///   → VFXReturnManager에서 일괄 틱 (자체 Update 제거)
/// </summary>
[DisallowMultipleComponent]
public sealed class VFXAutoReturn : MonoBehaviour
{
    [HideInInspector] public GameObject sourcePrefab;

    [Header("자동 반환")]
    [Tooltip("강제 반환 최대 시간(초). 파티클 종료 콜백이 없어도 이 시간이 지나면 반환됩니다.")]
    [SerializeField] internal float maxLifetime = 1.5f;

    [Tooltip("켜질 때 자동으로 재생합니다.")]
    [SerializeField] private bool playOnEnable = true;

    // ★ VFXReturnManager가 일괄 관리하는 필드
    [System.NonSerialized] internal float timer;
    [System.NonSerialized] internal bool registered;

    private ParticleSystem[] _particles;
    private TrailRenderer[] _trails;
    private bool _returned;

    private void Awake()
    {
        Cache();
    }

    private void OnEnable()
    {
        Cache();

        timer = Mathf.Max(0.05f, maxLifetime);
        _returned = false;
        registered = false;

        ClearVisuals();

        if (playOnEnable)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i] == null) continue;
                _particles[i].Play(true);
            }
        }

        // VFXReturnManager에 등록 (있으면 자체 Update 불필요)
        if (VFXReturnManager.Instance != null && !registered)
        {
            VFXReturnManager.Instance.Register(this);
            registered = true;
        }
    }

    private void Update()
    {
        // VFXReturnManager가 있으면 거기서 일괄 틱 → 자체 Update 스킵
        if (registered) return;

        if (_returned) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
            ReturnNow();
    }

    private void OnDisable()
    {
        // Manager에서 해제
        if (registered && VFXReturnManager.Instance != null)
        {
            VFXReturnManager.Instance.Unregister(this);
            registered = false;
        }

        ClearVisuals();
    }

    private void OnParticleSystemStopped()
    {
        if (_returned) return;
        ReturnNow();
    }

    public void SetLifetime(float lifetime)
    {
        maxLifetime = Mathf.Max(0.05f, lifetime);
    }

    public void ReturnNow()
    {
        if (_returned) return;
        _returned = true;

        ClearVisuals();

        if (sourcePrefab != null)
            VFXPool.Return(sourcePrefab, gameObject);
        else
            gameObject.SetActive(false);
    }

    private void Cache()
    {
        if (_particles == null || _particles.Length == 0)
            _particles = GetComponentsInChildren<ParticleSystem>(true);

        if (_trails == null || _trails.Length == 0)
            _trails = GetComponentsInChildren<TrailRenderer>(true);
    }

    private void ClearVisuals()
    {
        for (int i = 0; i < _trails.Length; i++)
        {
            if (_trails[i] == null) continue;
            _trails[i].Clear();
            _trails[i].emitting = false;
        }

        for (int i = 0; i < _particles.Length; i++)
        {
            if (_particles[i] == null) continue;
            _particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particles[i].Clear(true);
        }
    }
}