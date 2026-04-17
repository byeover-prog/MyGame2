// UTF-8
using UnityEngine;

/// <summary>
/// 맵의 실제 플레이 가능 영역 Bounds를 다른 시스템이 안전하게 읽어갈 수 있게 해 주는 제공자 컴포넌트입니다.
///
/// 구현 원리:
/// 1. 맵의 외곽 경계를 대표하는 Collider2D 하나를 참조합니다.
/// 2. 내부 시스템은 이 컴포넌트에게 Bounds를 요청하고, 직접 Collider를 만지지 않습니다.
/// 3. 이렇게 하면 Hazard, 몬스터 스폰, 카메라 제한 같은 시스템이 모두 같은 기준 영역을 공유할 수 있습니다.
/// 4. 또한 Collider가 비어 있거나 비활성화된 경우를 한 곳에서 검사할 수 있어 디버깅이 쉬워집니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MapBoundsProvider : MonoBehaviour
{
    [Header("플레이 영역 참조")]
    [Tooltip("맵의 실제 플레이 가능 영역을 대표하는 Collider2D입니다. 보통 맵 외곽을 감싸는 BoxCollider2D 또는 CompositeCollider2D를 연결합니다.")]
    [SerializeField] private Collider2D playAreaCollider;

    [Header("디버그")]
    [Tooltip("체크하면 씬 뷰에서 현재 플레이 영역 Bounds를 기즈모로 표시합니다.")]
    [SerializeField] private bool drawBoundsGizmo = true;

    [Tooltip("체크하면 Bounds를 가져오지 못했을 때 경고 로그를 출력합니다.")]
    [SerializeField] private bool debugLog = false;

    public Collider2D PlayAreaCollider => playAreaCollider;

    private void Reset()
    {
        if (playAreaCollider == null)
            playAreaCollider = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (playAreaCollider == null)
            playAreaCollider = GetComponent<Collider2D>();
    }

    /// <summary>
    /// 현재 플레이 가능 영역의 월드 Bounds를 반환합니다.
    /// </summary>
    public bool TryGetWorldBounds(out Bounds bounds)
    {
        bounds = default;

        if (playAreaCollider == null)
        {
            if (debugLog)
                Debug.LogWarning("[MapBoundsProvider] Play Area Collider가 비어 있습니다.", this);
            return false;
        }

        if (!playAreaCollider.gameObject.activeInHierarchy)
        {
            if (debugLog)
                Debug.LogWarning("[MapBoundsProvider] Play Area Collider 오브젝트가 비활성화되어 있습니다.", this);
            return false;
        }

        if (!playAreaCollider.enabled)
        {
            if (debugLog)
                Debug.LogWarning("[MapBoundsProvider] Play Area Collider가 비활성화되어 있습니다.", this);
            return false;
        }

        bounds = playAreaCollider.bounds;

        if (bounds.size.sqrMagnitude <= 0.0001f)
        {
            if (debugLog)
                Debug.LogWarning("[MapBoundsProvider] Collider Bounds 크기가 0에 가깝습니다.", this);
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawBoundsGizmo)
            return;

        if (playAreaCollider == null)
            playAreaCollider = GetComponent<Collider2D>();

        if (playAreaCollider == null)
            return;

        Bounds bounds = playAreaCollider.bounds;
        Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
