using UnityEngine;

/// <summary>
/// 속성별 피격 이펙트 옵저버 매니저.
/// DamageEvents2D.OnElementHitRequested를 구독하여,
/// 적에게 속성별 부착 이펙트를 자동으로 관리한다.
///
/// [사용법]
/// 1. Hierarchy에 빈 GameObject 생성 (이름: ElementVFXObserver)
/// 2. 이 컴포넌트를 부착
/// 3. Inspector에서 속성별 VFX 프리팹 연결 (없는 속성은 비워두면 됨)
///
/// [동작 원리]
/// - DamageUtil2D가 데미지를 적용하면 → DamageEvents2D.RaiseElementHit() 발생
/// - 이 옵저버가 이벤트를 받아서 해당 속성의 VFX 프리팹을 적에게 부착
/// - 적에는 ElementAttachedVfxHost2D가 자동으로 붙어서 VFX 1개만 유지
/// - 같은 속성 재피격 시 VFX 재생성 없이 지속시간만 갱신 (정화구 방식)
/// </summary>
[DisallowMultipleComponent]
public sealed class ElementVFXObserver2D : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  속성별 피격 VFX 프리팹
    // ═══════════════════════════════════════════════════════

    [Header("속성별 피격 VFX 프리팹")]
    [Tooltip("빙결 피격 VFX 프리팹")]
    [SerializeField] private GameObject iceVfxPrefab;

    [Tooltip("화염 피격 VFX 프리팹")]
    [SerializeField] private GameObject fireVfxPrefab;

    [Tooltip("전기 피격 VFX 프리팹 (현재 스파크_Slow)")]
    [SerializeField] private GameObject electricVfxPrefab;

    [Tooltip("땅 피격 VFX 프리팹")]
    [SerializeField] private GameObject earthVfxPrefab;

    [Tooltip("바람 피격 VFX 프리팹")]
    [SerializeField] private GameObject windVfxPrefab;

    [Tooltip("물 피격 VFX 프리팹")]
    [SerializeField] private GameObject waterVfxPrefab;

    [Tooltip("양 피격 VFX 프리팹")]
    [SerializeField] private GameObject lightVfxPrefab;

    [Tooltip("음 피격 VFX 프리팹")]
    [SerializeField] private GameObject darkVfxPrefab;

    [Tooltip("물리 피격 VFX 프리팹 (보통 비워둠)")]
    [SerializeField] private GameObject physicalVfxPrefab;

    // ═══════════════════════════════════════════════════════
    //  이펙트 설정
    // ═══════════════════════════════════════════════════════

    [Header("이펙트 설정")]
    [Tooltip("마지막 갱신 후 VFX가 유지되는 시간 (초)")]
    [SerializeField] private float effectDuration = 1.5f;

    // ═══════════════════════════════════════════════════════
    //  이벤트 구독
    // ═══════════════════════════════════════════════════════

    private void OnEnable()
    {
        DamageEvents2D.OnElementHitRequested += HandleElementHit;
    }

    private void OnDisable()
    {
        DamageEvents2D.OnElementHitRequested -= HandleElementHit;
    }

    private void Awake()
    {
        int count = 0;
        if (iceVfxPrefab != null) count++;
        if (fireVfxPrefab != null) count++;
        if (electricVfxPrefab != null) count++;
        if (earthVfxPrefab != null) count++;
        if (windVfxPrefab != null) count++;
        if (waterVfxPrefab != null) count++;
        if (lightVfxPrefab != null) count++;
        if (darkVfxPrefab != null) count++;
        if (physicalVfxPrefab != null) count++;

        Debug.Log($"[속성 VFX 옵저버] 초기화 완료 | 연결된 프리팹={count}개 effectDuration={effectDuration}s");
    }

    // ═══════════════════════════════════════════════════════
    //  이벤트 핸들러
    // ═══════════════════════════════════════════════════════

    private void HandleElementHit(DamageEvents2D.ElementHitRequest request)
    {
        if (request.Target == null) return;

        // 해당 속성의 VFX 프리팹 가져오기
        GameObject prefab = GetPrefab(request.Element);
        if (prefab == null) return; // 이 속성은 VFX가 없음 (비워둔 슬롯)

        // 적 루트 찾기
        GameObject targetRoot = request.Target.transform.root != null
            ? request.Target.transform.root.gameObject
            : request.Target;

        // ElementAttachedVfxHost2D 가져오거나 생성
        if (!targetRoot.TryGetComponent<ElementAttachedVfxHost2D>(out var host))
            host = targetRoot.AddComponent<ElementAttachedVfxHost2D>();

        // VFX 부착 또는 지속시간 갱신
        host.Refresh(request.Element, prefab, effectDuration);
    }

    /// <summary>
    /// 속성에 해당하는 VFX 프리팹을 반환한다. 없으면 null.
    /// </summary>
    private GameObject GetPrefab(DamageElement2D element)
    {
        return element switch
        {
            DamageElement2D.Physical => physicalVfxPrefab,
            DamageElement2D.Ice      => iceVfxPrefab,
            DamageElement2D.Fire     => fireVfxPrefab,
            DamageElement2D.Electric => electricVfxPrefab,
            DamageElement2D.Earth    => earthVfxPrefab,
            DamageElement2D.Wind     => windVfxPrefab,
            DamageElement2D.Water    => waterVfxPrefab,
            DamageElement2D.Light    => lightVfxPrefab,
            DamageElement2D.Dark     => darkVfxPrefab,
            _ => null
        };
    }
}