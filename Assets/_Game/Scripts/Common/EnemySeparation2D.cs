// UTF-8
using UnityEngine;

/// <summary>
/// 적끼리 겹침 방지(밀어내기).
/// [최적화] 매 프레임 전체가 아니라 K프레임에 1번만 업데이트.
///   40마리 × 매 프레임 OverlapCircle = 2000회/초
///   → 5프레임에 1번 = 400회/초 (80% 감소)
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemySeparation2D : MonoBehaviour
{
    [Header("Separation")]
    [SerializeField] private float separationRadius = 0.35f;
    [SerializeField] private float pushStrength = 1.8f;
    [SerializeField] private float maxPushPerSecond = 2.5f;
    [SerializeField] private LayerMask enemyMask;

    [Header("성능")]
    [Tooltip("이 값 프레임에 1번만 물리 탐색.\n5면 5프레임에 1번 업데이트.")]
    [Min(1)]
    [SerializeField] private int updateInterval = 3;

    private static readonly Collider2D[] _buffer = new Collider2D[64];

    // ★ 인스턴스마다 다른 오프셋으로 분산
    private int _frameOffset;
    private static int _globalCounter;

    private ContactFilter2D _filter;
    private bool _filterReady;

    private void OnEnable()
    {
        // 각 적이 서로 다른 프레임에 업데이트되도록 오프셋 분산
        _frameOffset = _globalCounter++;
    }

    private void EnsureFilter()
    {
        if (_filterReady) return;
        _filter = new ContactFilter2D();
        _filter.SetLayerMask(enemyMask);
        _filter.useTriggers = true;
        _filterReady = true;
    }

    private void LateUpdate()
    {
        // ★ K프레임에 1번만 실행
        if ((Time.frameCount + _frameOffset) % updateInterval != 0) return;

        EnsureFilter();

        Vector2 pos = transform.position;

        int count = Physics2D.OverlapCircle(pos, separationRadius, _filter, _buffer);
        if (count <= 1) return;

        Vector2 sum = Vector2.zero;
        int contributors = 0;

        for (int i = 0; i < count; i++)
        {
            var col = _buffer[i];
            if (col == null || col.transform == transform) continue;

            Vector2 other = col.bounds.center;
            Vector2 diff = pos - other;
            float dist = diff.magnitude;

            if (dist < 0.0001f) continue;

            float t = 1f - Mathf.Clamp01(dist / separationRadius);
            if (t <= 0f) continue;

            sum += diff.normalized * t;
            contributors++;
        }

        if (contributors <= 0) return;

        Vector2 dir = (sum / contributors).normalized;

        // ★ updateInterval 보정: 실행 빈도가 낮으니 그만큼 더 밀어줌
        float step = Mathf.Min(maxPushPerSecond * Time.deltaTime * updateInterval, maxPushPerSecond);
        transform.position += (Vector3)(dir * pushStrength * step);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
#endif
}